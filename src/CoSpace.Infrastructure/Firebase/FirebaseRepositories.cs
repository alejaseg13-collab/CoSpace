using CoSpace.Application.Reservations;
using CoSpace.Domain;
using Google.Cloud.Firestore;

namespace CoSpace.Infrastructure.Firebase;

public sealed class FirebaseBookingRepository(FirestoreContext context) : IBookingRepository
{
    private CollectionReference Reservations => context.Db.Collection("reservations");

    public async Task<IReadOnlyList<Reserva>> FindBySpaceAndDateAsync(string espacioId, DateOnly fecha, CancellationToken ct)
    {
        var snapshot = await Reservations.WhereEqualTo("espacioId", espacioId).WhereEqualTo("fecha", fecha.ToString("yyyy-MM-dd")).GetSnapshotAsync(ct);
        return snapshot.Documents.Select(FromDocument).ToList();
    }

    public async Task<Reserva?> FindByIdAsync(string reservaId, CancellationToken ct)
    {
        var document = await Reservations.Document(reservaId).GetSnapshotAsync(ct);
        return document.Exists ? FromDocument(document) : null;
    }

    public async Task<IReadOnlyList<Reserva>> AllAsync(CancellationToken ct)
    {
        var snapshot = await Reservations.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(FromDocument).ToList();
    }

    public Task AddAsync(Reserva reserva, CancellationToken ct) => Reservations.Document(reserva.Id).SetAsync(ToDocument(reserva), cancellationToken: ct);
    public Task UpdateAsync(Reserva reserva, CancellationToken ct) => Reservations.Document(reserva.Id).SetAsync(ToDocument(reserva), cancellationToken: ct);

    private static Dictionary<string, object?> ToDocument(Reserva item) => new()
    {
        ["usuarioId"] = item.UsuarioId, ["espacioId"] = item.EspacioId, ["fecha"] = item.Fecha.ToString("yyyy-MM-dd"),
        ["horaInicio"] = item.HoraInicio.ToString("HH:mm"), ["horaFin"] = item.HoraFin.ToString("HH:mm"), ["estado"] = (int)item.Estado,
        ["precioTotal"] = (double)item.PrecioTotal, ["creadoEn"] = Timestamp.FromDateTime(item.CreadoEn.ToUniversalTime())
    };

    private static Reserva FromDocument(DocumentSnapshot document)
    {
        var data = document.ToDictionary();
        return new Reserva(document.Id, (string)data["usuarioId"], (string)data["espacioId"], DateOnly.Parse((string)data["fecha"]),
            TimeOnly.Parse((string)data["horaInicio"]), TimeOnly.Parse((string)data["horaFin"]), (EstadoReserva)Convert.ToInt32(data["estado"]),
            Convert.ToDecimal(data["precioTotal"]), ((Timestamp)data["creadoEn"]).ToDateTime());
    }
}

public sealed class FirebasePaymentRepository(FirestoreContext context) : IPaymentRepository
{
    private CollectionReference Payments => context.Db.Collection("payments");

    public async Task<string> NextInvoiceNumberAsync(CancellationToken ct)
    {
        var counter = context.Db.Collection("settings").Document("invoice");
        return await context.Db.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(counter);
            var next = snapshot.Exists ? Convert.ToInt32(snapshot.GetValue<long>("value")) + 1 : 1246;
            transaction.Set(counter, new Dictionary<string, object> { ["value"] = next });
            return $"FAC-{next:000000}";
        }, cancellationToken: ct);
    }

    public Task AddAsync(Pago pago, CancellationToken ct) => Payments.Document(pago.Id).SetAsync(ToDocument(pago), cancellationToken: ct);

    public async Task<Pago?> FindByReservationAsync(string reservaId, CancellationToken ct)
    {
        var snapshot = await Payments.WhereEqualTo("reservaId", reservaId).Limit(1).GetSnapshotAsync(ct);
        return snapshot.Documents.FirstOrDefault() is { } document ? FromDocument(document) : null;
    }

    public Task UpdateAsync(Pago pago, CancellationToken ct) => Payments.Document(pago.Id).SetAsync(ToDocument(pago), cancellationToken: ct);

    private static Dictionary<string, object?> ToDocument(Pago item) => new()
    {
        ["reservaId"] = item.ReservaId, ["numeroFactura"] = item.NumeroFactura, ["monto"] = (double)item.Monto,
        ["metodo"] = (int)item.Metodo, ["estado"] = (int)item.Estado, ["fecha"] = Timestamp.FromDateTime(item.Fecha.ToUniversalTime())
    };

    private static Pago FromDocument(DocumentSnapshot document)
    {
        var data = document.ToDictionary();
        return new Pago(document.Id, (string)data["reservaId"], (string)data["numeroFactura"], Convert.ToDecimal(data["monto"]),
            (MetodoPago)Convert.ToInt32(data["metodo"]), (EstadoPago)Convert.ToInt32(data["estado"]), ((Timestamp)data["fecha"]).ToDateTime());
    }
}
