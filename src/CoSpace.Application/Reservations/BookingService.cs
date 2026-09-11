using CoSpace.Domain;

namespace CoSpace.Application.Reservations;

public interface IBookingRepository
{
    Task<IReadOnlyList<Reserva>> FindBySpaceAndDateAsync(string espacioId, DateOnly fecha, CancellationToken ct);
    Task<Reserva?> FindByIdAsync(string reservaId, CancellationToken ct);
    Task<IReadOnlyList<Reserva>> AllAsync(CancellationToken ct);
    Task AddAsync(Reserva reserva, CancellationToken ct);
    Task UpdateAsync(Reserva reserva, CancellationToken ct);
}

public interface IPaymentRepository
{
    Task<string> NextInvoiceNumberAsync(CancellationToken ct);
    Task AddAsync(Pago pago, CancellationToken ct);
    Task<Pago?> FindByReservationAsync(string reservaId, CancellationToken ct);
    Task UpdateAsync(Pago pago, CancellationToken ct);
}

public sealed class BookingService(IBookingRepository bookings, IPaymentRepository payments)
{
    public async Task<(Reserva Reserva, Pago Pago)> ConfirmAsync(
        string usuarioId, string espacioId, DateOnly fecha, TimeOnly inicio, TimeOnly fin,
        decimal subtotal, MetodoPago metodo, CancellationToken ct)
    {
        if (fin <= inicio) throw new ArgumentException("La hora final debe ser posterior a la inicial.");
        var existing = await bookings.FindBySpaceAndDateAsync(espacioId, fecha, ct);
        if (existing.Any(r => r.Estado != EstadoReserva.Cancelada && inicio < r.HoraFin && fin > r.HoraInicio))
            throw new InvalidOperationException("El espacio ya tiene una reserva en ese horario.");

        var reserva = new Reserva(Guid.NewGuid().ToString("N"), usuarioId, espacioId, fecha, inicio, fin,
            EstadoReserva.Confirmada, Math.Round(subtotal * 1.19m, 2), DateTime.UtcNow);
        await bookings.AddAsync(reserva, ct);
        var pago = new Pago(Guid.NewGuid().ToString("N"), reserva.Id, string.Empty, reserva.PrecioTotal, metodo, EstadoPago.Pendiente, DateTime.UtcNow);
        await payments.AddAsync(pago, ct);
        return (reserva, pago);
    }
}