using CoSpace.Domain;
using CoSpace.Application.Reservations;

namespace CoSpace.Application.Payments;

public sealed record PaymentHistory(Pago Pago, Reserva Reserva, Espacio Espacio);

public sealed class PaymentService(IBookingRepository bookings, IPaymentRepository payments)
{
    public async Task<Pago> PayAsync(string usuarioId, string reservaId, MetodoPago metodo, CancellationToken ct)
    {
        var reservation = await bookings.FindByIdAsync(reservaId, ct) ?? throw new KeyNotFoundException("Reserva no encontrada.");
        if (reservation.UsuarioId != usuarioId) throw new UnauthorizedAccessException();
        var payment = await payments.FindByReservationAsync(reservaId, ct) ?? throw new KeyNotFoundException("Pago pendiente no encontrado.");
        if (payment.Estado == EstadoPago.Pagado) return payment;
        var paid = payment with { Metodo = metodo, NumeroFactura = await payments.NextInvoiceNumberAsync(ct), Estado = EstadoPago.Pagado, Fecha = DateTime.UtcNow };
        await payments.UpdateAsync(paid, ct);
        return paid;
    }

    public async Task<IReadOnlyList<PaymentHistory>> HistoryAsync(string usuarioId, CancellationToken ct)
    {
        var result = new List<PaymentHistory>();
        foreach (var reservation in (await bookings.AllAsync(ct)).Where(item => item.UsuarioId == usuarioId))
        {
            var payment = await payments.FindByReservationAsync(reservation.Id, ct);
            var space = CatalogSeed.Espacios.FirstOrDefault(item => item.Id == reservation.EspacioId);
            if (payment is not null && space is not null) result.Add(new PaymentHistory(payment, reservation, space));
        }
        return result.OrderByDescending(item => item.Pago.Fecha).ToList();
    }
}
