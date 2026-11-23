namespace SDPIS.Api.Models.DTOs;

// Informacion registrada de la denuncia (HU-002/HU-004) que se muestra al
// funcionario validador en la pantalla de criterios tecnicos, como insumo
// para decidir el dano potencial a la salud, la relacion con comercio
// electronico y si requiere criterio especializado de UVC.
// A proposito NO incluye datos personales del denunciante: el funcionario
// no los necesita para evaluar la denuncia y pueden ser confidenciales.
public record DetalleDenunciaDto(
    long IdDenuncia,
    string Consecutivo,
    DateTime FechaCreacion,
    string Estado,
    DetalleDenunciaHechoDto? Hecho,
    DetalleDenunciaUbicacionDto? Ubicacion,
    List<DetalleDenunciaProductoDto> Productos
);

public record DetalleDenunciaHechoDto(
    string NombreEstablecimiento,
    string DescripcionHecho,
    string? DenunciadoOtraInstitucion,
    string? DetalleOtraInstitucion,
    string? DenunciadoPreviamenteMinsalud,
    string? DetallePrevioMinsalud
);

public record DetalleDenunciaUbicacionDto(
    string Provincia,
    string Canton,
    string? Distrito,
    string? DireccionExacta
);

public record DetalleDenunciaProductoDto(
    long IdDetalleProducto,
    string NombreProducto,
    string? TipoProducto,
    string DescripcionProducto,
    string? RegistroSanitario,
    string? Marca,
    string? NumeroLote,
    string? Fabricante,
    string? PaisOrigen,
    string? Presentacion,
    DateTime? FechaCompra,
    List<string> Motivos
);
