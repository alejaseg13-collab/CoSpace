using CoSpace.Application.Authentication;
using CoSpace.Domain;
using Google.Cloud.Firestore;

namespace CoSpace.Infrastructure.Firebase;

public sealed class FirebaseUserRepository(FirestoreContext context) : IUserRepository
{
    private CollectionReference Users => context.Db.Collection("users");

    public async Task<Usuario?> FindByEmailAsync(string correo, CancellationToken ct)
    {
        var snapshot = await Users.WhereEqualTo("correo", correo.Trim().ToLowerInvariant()).Limit(1).GetSnapshotAsync(ct);
        return snapshot.Documents.FirstOrDefault() is { } document ? FromDocument(document) : null;
    }

    public async Task<Usuario?> FindByIdAsync(string id, CancellationToken ct)
    {
        var document = await Users.Document(id).GetSnapshotAsync(ct);
        return document.Exists ? FromDocument(document) : null;
    }

    public async Task AddAsync(Usuario usuario, CancellationToken ct)
    {
        if (await FindByEmailAsync(usuario.Correo, ct) is not null) throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        await Users.Document(usuario.Id).SetAsync(ToDocument(usuario), cancellationToken: ct);
    }

    public Task UpdateAsync(Usuario usuario, CancellationToken ct) => Users.Document(usuario.Id).SetAsync(ToDocument(usuario), cancellationToken: ct);

    private static Dictionary<string, object?> ToDocument(Usuario user) => new()
    {
        ["id"] = user.Id, ["nombre"] = user.Nombre, ["correo"] = user.Correo.ToLowerInvariant(), ["telefono"] = user.Telefono,
        ["contrasena"] = user.Contrasena, ["rol"] = (int)user.Rol, ["planId"] = user.PlanId, ["sedePreferidaId"] = user.SedePreferidaId,
        ["fechaRegistro"] = Timestamp.FromDateTime(user.FechaRegistro.ToUniversalTime())
    };

    private static Usuario FromDocument(DocumentSnapshot document)
    {
        var data = document.ToDictionary();
        return new Usuario(document.Id, (string)data["nombre"], (string)data["correo"], (string)data["telefono"], (string)data["contrasena"],
            (Rol)Convert.ToInt32(data["rol"]), data.GetValueOrDefault("planId") as string, data.GetValueOrDefault("sedePreferidaId") as string,
            ((Timestamp)data["fechaRegistro"]).ToDateTime());
    }
}
