namespace CoSpace.Domain;

public enum Rol { Miembro, Administrador }
public enum TipoEspacio { EscritorioFlexible, OficinaPrivada, SalaReunion }
public enum EstadoEspacio { Activo, EnMantenimiento, Inactivo }
public enum EstadoReserva { Confirmada, Completada, Cancelada }
public enum MetodoPago { Tarjeta, Pse, BilleteraDigital }
public enum EstadoPago { Pagado, Pendiente, Vencido }

public sealed record Usuario(string Id, string Nombre, string Correo, string Telefono, string Contrasena, Rol Rol, string? PlanId, string? SedePreferidaId, DateTime FechaRegistro);
public sealed record Sede(string Id, string Nombre, string Direccion, string Horario, IReadOnlyList<string> Servicios, string Foto);
public sealed record Espacio(string Id, string Nombre, TipoEspacio Tipo, int Capacidad, string SedeId, string Ubicacion, decimal PrecioHora, decimal? PrecioDia, EstadoEspacio Estado, string Foto);
public sealed record Reserva(string Id, string UsuarioId, string EspacioId, DateOnly Fecha, TimeOnly HoraInicio, TimeOnly HoraFin, EstadoReserva Estado, decimal PrecioTotal, DateTime CreadoEn);
public sealed record Pago(string Id, string ReservaId, string NumeroFactura, decimal Monto, MetodoPago Metodo, EstadoPago Estado, DateTime Fecha);
public sealed record Plan(string Id, string Nombre, decimal PrecioMes, IReadOnlyList<string> Beneficios);