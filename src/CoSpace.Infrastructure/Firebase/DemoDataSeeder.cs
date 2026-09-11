using CoSpace.Application.Authentication;
using CoSpace.Domain;
using Google.Cloud.Firestore;

namespace CoSpace.Infrastructure.Firebase;

public sealed class DemoDataSeeder(FirestoreContext context, IUserRepository users)
{
    private const string DemoPasswordHash = "7DBB7F051B44D7D54584A7BC6C32F00DA5A3B5E6973485B9FB176FE56346B3E3";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedUsersAsync(cancellationToken);
        await SeedCollectionAsync("sedes", CatalogSeed.Sedes.ToDictionary(item => item.Id, ToDocument), cancellationToken);
        await SeedCollectionAsync("plans", CatalogSeed.Planes.ToDictionary(item => item.Id, ToDocument), cancellationToken);
        await SeedCollectionAsync("spaces", CatalogSeed.Espacios.ToDictionary(item => item.Id, ToDocument), cancellationToken);
        await SeedReservationsAndPaymentsAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var demoUsers = new[]
        {
            new Usuario("demo-member", "Sebastián Gil", "sebastian.gil@example.com", "3000000000", DemoPasswordHash, Rol.Miembro, "plan-pro", "sede-centro-mayor", DateTime.UtcNow),
            new Usuario("demo-admin", "Laura Gómez", "admin@cospace.co", "3000000001", DemoPasswordHash, Rol.Administrador, null, "sede-centro-mayor", DateTime.UtcNow)
        };
        foreach (var user in demoUsers)
            if (await users.FindByEmailAsync(user.Correo, cancellationToken) is null)
                await users.AddAsync(user, cancellationToken);
    }

    private async Task SeedReservationsAndPaymentsAsync(CancellationToken cancellationToken)
    {
        var reservations = context.Db.Collection("reservations");
        var existing = await reservations.Limit(1).GetSnapshotAsync(cancellationToken);
        if (existing.Count > 0) return;

        var payments = context.Db.Collection("payments");
        var batch = context.Db.StartBatch();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var demoSpaces = CatalogSeed.Espacios.Take(6).ToArray();
        for (var index = 0; index < demoSpaces.Length; index++)
        {
            var reservationId = $"seed-rsv-{index + 1:000}";
            var date = today.AddDays(index - 2);
            var reservation = new Reserva(reservationId, "demo-member", demoSpaces[index].Id, date, new TimeOnly(9), new TimeOnly(11),
                index == 3 ? EstadoReserva.Cancelada : index == 0 ? EstadoReserva.Confirmada : EstadoReserva.Completada,
                demoSpaces[index].PrecioHora * 2 * 1.19m, DateTime.UtcNow.AddDays(index - 2));
            batch.Set(reservations.Document(reservationId), ToDocument(reservation));

            var payment = new Pago($"seed-pay-{index + 1:000}", reservationId, index == 0 ? string.Empty : $"FAC-SEED-{index + 1:0000}",
                reservation.PrecioTotal, index % 2 == 0 ? MetodoPago.Tarjeta : MetodoPago.Pse,
                index == 0 ? EstadoPago.Pendiente : index == 3 ? EstadoPago.Vencido : EstadoPago.Pagado,
                reservation.CreadoEn);
            batch.Set(payments.Document(payment.Id), ToDocument(payment));
        }
        await batch.CommitAsync(cancellationToken);
    }

    private async Task SeedCollectionAsync(string name, Dictionary<string, Dictionary<string, object?>> documents, CancellationToken cancellationToken)
    {
        var collection = context.Db.Collection(name);
        var existing = await collection.Limit(1).GetSnapshotAsync(cancellationToken);
        if (existing.Count > 0) return;

        var batch = context.Db.StartBatch();
        foreach (var (id, data) in documents)
            batch.Set(collection.Document(id), data);
        await batch.CommitAsync(cancellationToken);
    }

    private static Dictionary<string, object?> ToDocument(Sede item) => new()
    {
        ["nombre"] = item.Nombre, ["direccion"] = item.Direccion, ["horario"] = item.Horario,
        ["servicios"] = item.Servicios.ToArray(), ["foto"] = item.Foto
    };

    private static Dictionary<string, object?> ToDocument(Plan item) => new()
    {
        ["nombre"] = item.Nombre, ["precioMes"] = (double)item.PrecioMes, ["beneficios"] = item.Beneficios.ToArray()
    };

    private static Dictionary<string, object?> ToDocument(Espacio item) => new()
    {
        ["nombre"] = item.Nombre, ["tipo"] = (int)item.Tipo, ["capacidad"] = item.Capacidad, ["sedeId"] = item.SedeId,
        ["ubicacion"] = item.Ubicacion, ["precioHora"] = (double)item.PrecioHora, ["precioDia"] = item.PrecioDia is null ? null : (double?)item.PrecioDia.Value,
        ["estado"] = (int)item.Estado, ["foto"] = item.Foto
    };

    private static Dictionary<string, object?> ToDocument(Reserva item) => new()
    {
        ["usuarioId"] = item.UsuarioId, ["espacioId"] = item.EspacioId, ["fecha"] = item.Fecha.ToString("yyyy-MM-dd"),
        ["horaInicio"] = item.HoraInicio.ToString("HH:mm"), ["horaFin"] = item.HoraFin.ToString("HH:mm"), ["estado"] = (int)item.Estado,
        ["precioTotal"] = (double)item.PrecioTotal, ["creadoEn"] = Timestamp.FromDateTime(item.CreadoEn.ToUniversalTime())
    };

    private static Dictionary<string, object?> ToDocument(Pago item) => new()
    {
        ["reservaId"] = item.ReservaId, ["numeroFactura"] = item.NumeroFactura, ["monto"] = (double)item.Monto,
        ["metodo"] = (int)item.Metodo, ["estado"] = (int)item.Estado, ["fecha"] = Timestamp.FromDateTime(item.Fecha.ToUniversalTime())
    };
}
