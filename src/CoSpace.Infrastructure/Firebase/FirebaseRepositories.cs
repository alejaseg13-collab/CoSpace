using CoSpace.Application.Reservations;
using CoSpace.Domain;
using System.Collections.Concurrent;

namespace CoSpace.Infrastructure.Firebase;

// Implementaciones reales deben usar FirebaseAdmin y transacciones para el consecutivo.
public sealed class FirebaseBookingRepository : IBookingRepository
{
    private readonly ConcurrentBag<Reserva> reservations = [];
    public FirebaseBookingRepository()
    {
        var users = new[] { "member-demo", "member-2", "member-3", "member-4", "member-5", "member-6", "member-7" };
        for (var index = 0; index < 18; index++)
        {
            var space = CatalogSeed.Espacios[index * 7];
            reservations.Add(new Reserva($"seed-rsv-{index + 1:000}", users[index % users.Length], space.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-index * 3)), new TimeOnly(9, 0), new TimeOnly(11, 0), index % 6 == 0 ? EstadoReserva.Completada : EstadoReserva.Confirmada, space.PrecioHora * 2 * 1.19m, DateTime.UtcNow.AddDays(-index * 3)));
        }
    }
    public Task<IReadOnlyList<Reserva>> FindBySpaceAndDateAsync(string espacioId, DateOnly fecha, CancellationToken ct) => Task.FromResult<IReadOnlyList<Reserva>>(reservations.Where(item => item.EspacioId == espacioId && item.Fecha == fecha).ToList());
    public Task<Reserva?> FindByIdAsync(string reservaId, CancellationToken ct) => Task.FromResult(reservations.FirstOrDefault(item => item.Id == reservaId));
    public IReadOnlyList<Reserva> All() => reservations.ToList();
    public Task AddAsync(Reserva reserva, CancellationToken ct) { reservations.Add(reserva); return Task.CompletedTask; }
    public Task UpdateAsync(Reserva reserva, CancellationToken ct) => Task.CompletedTask;
}

public sealed class FirebasePaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<string, Pago> payments = new();
    private int invoiceSequence = 1245;
    public FirebasePaymentRepository()
    {
        for (var index = 0; index < 18; index++)
            payments.TryAdd($"seed-pay-{index + 1:000}", new Pago($"seed-pay-{index + 1:000}", $"seed-rsv-{index + 1:000}", $"FAC-SEED-{index + 1:0000}", 50000 + index * 12500, index % 2 == 0 ? MetodoPago.Tarjeta : MetodoPago.Pse, index % 5 == 0 ? EstadoPago.Pendiente : EstadoPago.Pagado, DateTime.UtcNow.AddDays(-index * 3)));
    }
    public Task<string> NextInvoiceNumberAsync(CancellationToken ct) => Task.FromResult($"FAC-{Interlocked.Increment(ref invoiceSequence):000000}");
    public Task AddAsync(Pago pago, CancellationToken ct) { payments[pago.Id] = pago; return Task.CompletedTask; }
    public Task<Pago?> FindByReservationAsync(string reservaId, CancellationToken ct) => Task.FromResult(payments.Values.FirstOrDefault(item => item.ReservaId == reservaId));
    public Task UpdateAsync(Pago pago, CancellationToken ct) { payments[pago.Id] = pago; return Task.CompletedTask; }
}