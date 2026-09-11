using System.Collections.Concurrent;
using CoSpace.Application.Members;
using CoSpace.Domain;

namespace CoSpace.Infrastructure.Firebase;

public sealed class FirebaseMemberStore : IMemberStore
{
    private readonly ConcurrentDictionary<string, List<MemberReservation>> reservations = new();
    private readonly ConcurrentDictionary<string, MemberPayment> payments = new();

    public Task<IReadOnlyList<MemberReservation>> ReservationsAsync(string usuarioId, CancellationToken ct)
    {
        var list = reservations.GetOrAdd(usuarioId, SeedReservations);
        return Task.FromResult<IReadOnlyList<MemberReservation>>(list);
    }

    public async Task<MemberPayment?> LastPaymentAsync(string usuarioId, CancellationToken ct)
    {
        await ReservationsAsync(usuarioId, ct);
        return payments.TryGetValue(usuarioId, out var payment) ? payment : null;
    }

    public async Task CancelAsync(string usuarioId, string reservaId, CancellationToken ct)
    {
        var list = (List<MemberReservation>)await ReservationsAsync(usuarioId, ct);
        var index = list.FindIndex(item => item.Id == reservaId);
        if (index < 0) throw new KeyNotFoundException("Reserva no encontrada.");
        var reservation = list[index];
        if (reservation.Fecha < DateOnly.FromDateTime(DateTime.UtcNow)) throw new InvalidOperationException("Solo puedes cancelar reservas futuras.");
        if (reservation.Estado == EstadoReserva.Cancelada) return;
        list[index] = reservation with { Estado = EstadoReserva.Cancelada };
    }

    public async Task<string> InvoiceAsync(string usuarioId, string reservaId, CancellationToken ct)
    {
        var list = await ReservationsAsync(usuarioId, ct);
        var reservation = list.FirstOrDefault(item => item.Id == reservaId);
        if (reservation is null || string.IsNullOrWhiteSpace(reservation.NumeroFactura)) throw new KeyNotFoundException("La reserva no tiene factura.");
        return $"Factura {reservation.NumeroFactura}\nCoSpace\nReserva: {reservation.Espacio.Nombre}\nUbicación: {reservation.Espacio.Ubicacion}\nFecha: {reservation.Fecha:dd/MM/yyyy}\nTotal pagado: {reservation.PrecioTotal:C0}\nEstado: Pagado";
    }

    private List<MemberReservation> SeedReservations(string usuarioId)
    {
        var spaces = CatalogSeed.Espacios;
        MemberReservation Item(int index, DateOnly date, string start, string end, EstadoReserva status, string? invoice, decimal total) =>
            new($"rsv-member-{index}", spaces[index], date, TimeOnly.Parse(start), TimeOnly.Parse(end), status, invoice, total);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = new List<MemberReservation>
        {
            Item(0, today.AddDays(5), "09:00", "13:00", EstadoReserva.Confirmada, null, 48000),
            Item(41, today.AddDays(-12), "14:00", "16:00", EstadoReserva.Completada, "FAC-000741", 190400),
            Item(46, today.AddDays(-22), "09:00", "17:00", EstadoReserva.Completada, "FAC-000728", 333200),
            Item(71, today.AddDays(-35), "10:00", "12:00", EstadoReserva.Cancelada, "FAC-000699", 166600),
            Item(43, today.AddDays(-50), "15:00", "17:00", EstadoReserva.Completada, "FAC-000651", 226100),
            Item(56, today.AddDays(-58), "09:00", "18:00", EstadoReserva.Completada, "FAC-000640", 416500)
        };
        payments[usuarioId] = new MemberPayment("FAC-000741", 190400, MetodoPago.Tarjeta, EstadoPago.Pagado, DateTime.UtcNow.AddDays(-12), items[1].Id);
        return items;
    }
}
