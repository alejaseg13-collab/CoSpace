using CoSpace.Application.Members;
using CoSpace.Domain;
using Google.Cloud.Firestore;

namespace CoSpace.Infrastructure.Firebase;

public sealed class FirebaseMemberStore(FirestoreContext context) : IMemberStore
{
    private CollectionReference Reservations => context.Db.Collection("reservations");
    private CollectionReference Payments => context.Db.Collection("payments");

    public async Task<IReadOnlyList<MemberReservation>> ReservationsAsync(string usuarioId, CancellationToken ct)
    {
        var snapshot = await Reservations.WhereEqualTo("usuarioId", usuarioId).GetSnapshotAsync(ct);
        var result = new List<MemberReservation>();
        foreach (var document in snapshot.Documents)
        {
            var data = document.ToDictionary();
            var space = CatalogSeed.Espacios.FirstOrDefault(item => item.Id == (string)data["espacioId"]);
            if (space is null) continue;
            var payment = await PaymentForAsync(document.Id, ct);
            result.Add(new MemberReservation(document.Id, space, DateOnly.Parse((string)data["fecha"]), TimeOnly.Parse((string)data["horaInicio"]),
                TimeOnly.Parse((string)data["horaFin"]), (EstadoReserva)Convert.ToInt32(data["estado"]), payment?.NumeroFactura,
                Convert.ToDecimal(data["precioTotal"])));
        }
        return result.OrderByDescending(item => item.Fecha).ToList();
    }

    public async Task<MemberPayment?> LastPaymentAsync(string usuarioId, CancellationToken ct)
    {
        var reservations = await ReservationsAsync(usuarioId, ct);
        foreach (var reservation in reservations)
        {
            var payment = await PaymentForAsync(reservation.Id, ct);
            if (payment is not null && payment.Estado == EstadoPago.Pagado)
                return new MemberPayment(payment.NumeroFactura, payment.Monto, payment.Metodo, payment.Estado, payment.Fecha, reservation.Id);
        }
        return null;
    }

    public async Task CancelAsync(string usuarioId, string reservaId, CancellationToken ct)
    {
        var document = await Reservations.Document(reservaId).GetSnapshotAsync(ct);
        if (!document.Exists || (string)document.ToDictionary()["usuarioId"] != usuarioId) throw new KeyNotFoundException("Reserva no encontrada.");
        var data = document.ToDictionary();
        if (DateOnly.Parse((string)data["fecha"]) < DateOnly.FromDateTime(DateTime.UtcNow)) throw new InvalidOperationException("Solo puedes cancelar reservas futuras.");
        if (Convert.ToInt32(data["estado"]) == (int)EstadoReserva.Cancelada) return;
        await Reservations.Document(reservaId).UpdateAsync("estado", (int)EstadoReserva.Cancelada, cancellationToken: ct);
    }

    public async Task<string> InvoiceAsync(string usuarioId, string reservaId, CancellationToken ct)
    {
        var document = await Reservations.Document(reservaId).GetSnapshotAsync(ct);
        if (!document.Exists || (string)document.ToDictionary()["usuarioId"] != usuarioId) throw new KeyNotFoundException("Reserva no encontrada.");
        var data = document.ToDictionary();
        var payment = await PaymentForAsync(reservaId, ct);
        var space = CatalogSeed.Espacios.FirstOrDefault(item => item.Id == (string)data["espacioId"]);
        if (payment is null || string.IsNullOrWhiteSpace(payment.NumeroFactura) || space is null) throw new KeyNotFoundException("La reserva no tiene factura.");
        return $"Factura {payment.NumeroFactura}\nCoSpace\nReserva: {space.Nombre}\nUbicación: {space.Ubicacion}\nFecha: {DateOnly.Parse((string)data["fecha"]):dd/MM/yyyy}\nTotal pagado: {Convert.ToDecimal(data["precioTotal"]):C0}\nEstado: Pagado";
    }

    private async Task<Pago?> PaymentForAsync(string reservaId, CancellationToken ct)
    {
        var snapshot = await Payments.WhereEqualTo("reservaId", reservaId).Limit(1).GetSnapshotAsync(ct);
        if (snapshot.Documents.FirstOrDefault() is not { } document) return null;
        var data = document.ToDictionary();
        return new Pago(document.Id, reservaId, (string)data["numeroFactura"], Convert.ToDecimal(data["monto"]), (MetodoPago)Convert.ToInt32(data["metodo"]),
            (EstadoPago)Convert.ToInt32(data["estado"]), ((Timestamp)data["fecha"]).ToDateTime());
    }
}
