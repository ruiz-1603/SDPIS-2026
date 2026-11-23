using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SDPIS.Api.Middleware;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autenticacion;
using SDPIS.Api.Services.Autorizacion;      
using SDPIS.Api.Services.GestionUsuarios;       
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IDenunciaRepository, DenunciaRepository>();
builder.Services.AddScoped<IPermisosRepository, PermisosRepository>();
builder.Services.AddScoped<IGestionUsuariosRepository, GestionUsuariosRepository>();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.AddScoped<IPermisosService, PermisosService>();
builder.Services.AddScoped<IGestionUsuariosService, GestionUsuariosService>();
builder.Services.AddScoped<IPlazosAtencionRepository, PlazosAtencionRepository>();
builder.Services.AddScoped<IPlazosAtencionService, PlazosAtencionService>();
builder.Services.AddScoped<IVisibilidadDenunciasService, VisibilidadDenunciasService>();
builder.Services.AddScoped<IDenunciaService, DenunciaService>();
builder.Services.AddScoped<IDisponibilidadRepository, DisponibilidadRepository>();
builder.Services.AddScoped<IDisponibilidadService, DisponibilidadService>();
builder.Services.AddScoped<IAsignacionRepository, AsignacionRepository>();
builder.Services.AddScoped<IAsignacionService, AsignacionService>();
builder.Services.AddScoped<IAsignacionEncargadoRepository, AsignacionEncargadoRepository>();
builder.Services.AddScoped<IAsignacionEncargadoService, AsignacionEncargadoService>();
builder.Services.AddScoped<ICriteriosTecnicosRepository, CriteriosTecnicosRepository>();
builder.Services.AddScoped<ICriteriosTecnicosService, CriteriosTecnicosService>();
builder.Services.AddScoped<IEvaluacionCriticidadRepository, EvaluacionCriticidadRepository>();
builder.Services.AddScoped<IEvaluacionCriticidadService, EvaluacionCriticidadService>();
builder.Services.AddScoped<IBitacoraRepository, BitacoraRepository>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<ISolicitudCierreRepository, SolicitudCierreRepository>();
builder.Services.AddScoped<ISolicitudCierreService, SolicitudCierreService>();
builder.Services.AddScoped<IResolucionCierreRepository, ResolucionCierreRepository>();
builder.Services.AddScoped<IResolucionCierreService, ResolucionCierreService>();
builder.Services.AddScoped<IUsuarioTemporalRepository, UsuarioTemporalRepository>();
builder.Services.AddScoped<IBitacoraSesionRepository, BitacoraSesionRepository>();
builder.Services.AddScoped<ITokenRevocadoRepository, TokenRevocadoRepository>();
builder.Services.AddScoped<IMapeadorRol, MapeadorRol>();
builder.Services.AddScoped<IProveedorAutenticacion, ProveedorAutenticacionLocal>();
builder.Services.AddScoped<IServicioJwt, ServicioJwt>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddHostedService<RecordatorioAsignacionWorker>();

// Agregar CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("DesarrolloLocal", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// JWT (HU-001): tokens firmados con la clave de Autenticacion:ClaveFirmaJwt.
// La configuracion de autenticacion (clave JWT, emisor, audiencia,
// expiracion, proveedor) vive en user-secrets o en variables de entorno
// Autenticacion__*; en un equipo nuevo: tools/configurar-entorno.ps1.
// Solo los endpoints explicitos con [Authorize] exigen token; no se aplica
// politica global para no romper los endpoints publicos existentes.
var claveFirmaBase64 = builder.Configuration["Autenticacion:ClaveFirmaJwt"];
if (string.IsNullOrWhiteSpace(claveFirmaBase64))
    throw new InvalidOperationException(
        "Falta 'Autenticacion:ClaveFirmaJwt' (minimo 32 bytes en base64). Ejecute " +
        "tools/configurar-entorno.ps1 o: dotnet user-secrets set \"Autenticacion:ClaveFirmaJwt\" \"<clave base64>\" --project SDPIS.Api");

// El controlador lee los claims por nombre JWT estandar (sub, jti) y
// custom (identificacion, rol); sin esto .NET los renombra a URIs.
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

byte[] claveFirma;
try
{
    claveFirma = Convert.FromBase64String(claveFirmaBase64);
}
catch (FormatException)
{
    claveFirma = Encoding.UTF8.GetBytes(claveFirmaBase64);
}

var emisor = builder.Configuration["Autenticacion:Emisor"] ?? "SDPIS";
var audiencia = builder.Configuration["Autenticacion:Audiencia"] ?? "SDPIS-Web";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = emisor,
            ValidateAudience = true,
            ValidAudience = audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(claveFirma),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };

        // 401 JSON: el frontend distingue sesion expirada de token invalido.
        options.Events = new JwtBearerEvents
        {
            // HU-028: rechaza cualquier token revocado en el cierre de
            // sesion (mirada en token_revocado por claim JTI). El fallo
            // cae en OnChallenge y responde 401 "token_invalido" igual
            // que un token mal formado: no se revela el mecanismo.
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (string.IsNullOrWhiteSpace(jti)) return;

                var repositorio = context.HttpContext.RequestServices
                    .GetRequiredService<ITokenRevocadoRepository>();
                if (await repositorio.EstaRevocadoAsync(jti, context.HttpContext.RequestAborted))
                    context.Fail(new SecurityTokenException("token_revocado"));
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                var expirado = context.AuthenticateFailure is SecurityTokenExpiredException;
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = expirado ? "sesion_expirada" : "token_invalido",
                    mensaje = expirado ? "Su sesion ha expirado. Inicie sesion nuevamente."
                                       : "No autorizado. Debe iniciar sesion."
                });
            }
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DesarrolloLocal");           // ← CORS primero
                                          //app.UseHttpsRedirection();                // ← Redirect después
                                          // Deshabilitado en local: rompe el
                                          // preflight de CORS al redirigir
                                          // http://localhost:5290 -> https://localhost:7064
app.UseMiddleware<ManejadorDeErroresMiddleware>();
app.UseAuthentication();   // +HU-001: valida el token JWT de [Authorize]
app.UseAuthorization();
app.MapControllers();

app.Run();