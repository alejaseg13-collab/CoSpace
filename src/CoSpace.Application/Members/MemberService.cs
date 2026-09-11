using CoSpace.Domain;
using CoSpace.Application.Authentication;

namespace CoSpace.Application.Members;

public sealed record MemberProfile(Usuario Usuario, Plan Plan, DateOnly FechaRenovacion);
public sealed record MemberReservation(string Id, Espacio Espacio, DateOnly Fecha, TimeOnly Inicio, TimeOnly Fin, EstadoReserva Estado, string? NumeroFactura, decimal PrecioTotal);
public sealed record MemberPayment(string NumeroFactura, decimal Monto, MetodoPago Metodo, EstadoPago Estado, DateTime Fecha, string ReservaId);

public interface IMemberStore
{
    Task<IReadOnlyList<MemberReservation>> ReservationsAsync(string usuarioId, CancellationToken ct);
    Task<MemberPayment?> LastPaymentAsync(string usuarioId, CancellationToken ct);
    Task CancelAsync(string usuarioId, string reservaId, CancellationToken ct);
    Task<string> InvoiceAsync(string usuarioId, string reservaId, CancellationToken ct);
}

public sealed class MemberService(IUserRepository users, IMemberStore members)
{
    public async Task<MemberProfile?> ProfileAsync(string userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId, ct);
        if (user is null || user.Rol != Rol.Miembro) return null;
        var plan = CatalogSeed.Planes.FirstOrDefault(item => item.Id == user.PlanId) ?? CatalogSeed.Planes[0];
        return new MemberProfile(user, plan, DateOnly.FromDateTime(user.FechaRegistro).AddMonths(1));
    }

    public Task<IReadOnlyList<MemberReservation>> ReservationsAsync(string userId, CancellationToken ct) => members.ReservationsAsync(userId, ct);
    public Task<MemberPayment?> LastPaymentAsync(string userId, CancellationToken ct) => members.LastPaymentAsync(userId, ct);
    public Task CancelAsync(string userId, string reservationId, CancellationToken ct) => members.CancelAsync(userId, reservationId, ct);
    public Task<string> InvoiceAsync(string userId, string reservationId, CancellationToken ct) => members.InvoiceAsync(userId, reservationId, ct);
}
