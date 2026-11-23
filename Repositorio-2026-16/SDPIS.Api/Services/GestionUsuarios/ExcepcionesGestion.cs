namespace SDPIS.Api.Services.GestionUsuarios;

// 403: el usuario autenticado no tiene el permiso o el alcance necesario.
public class PermisoDenegadoException(string mensaje) : Exception(mensaje);

// 422: la solicitud es valida en forma pero viola una regla de HU-015
// (cupos, auto-modificacion, jefe con denuncias asignadas, etc.).
public class ReglaNegocioException(string mensaje) : Exception(mensaje);

// 404: funcionario o area inexistente.
public class RecursoNoEncontradoException(string mensaje) : Exception(mensaje);
