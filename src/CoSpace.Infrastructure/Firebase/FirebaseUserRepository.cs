using System.Collections.Concurrent;
using CoSpace.Application.Authentication;
using CoSpace.Domain;

namespace CoSpace.Infrastructure.Firebase;

public sealed class FirebaseUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, Usuario> users = new(StringComparer.OrdinalIgnoreCase);

    public FirebaseUserRepository()
    {
        var admin = new Usuario("admin-demo", "Laura Gómez", "admin@cospace.co", "3000000000", "60FE74406E7F353ED979F350F2FBB6A2E8690A5FA7D1B0C32983D1D8B3F95F67", Rol.Administrador, null, "sede-centro-mayor", DateTime.UtcNow);
        users.TryAdd(admin.Correo, admin);
        var seeds = new[] { ("Sebastián Gil", "sebastian.gil@example.com", "plan-pro", "sede-centro-mayor"), ("Mariana Torres", "mariana@example.com", "plan-basico", "sede-santa-fe"), ("Daniel Rojas", "daniel@example.com", "plan-empresarial", "sede-plaza-central"), ("Sofía Martínez", "sofia@example.com", "plan-pro", "sede-mallplaza-nqs"), ("Camilo Pérez", "camilo@example.com", "plan-basico", "sede-nuestro-bogota"), ("Valentina Ruiz", "valentina@example.com", "plan-pro", "sede-centro-mayor"), ("Nicolás León", "nicolas@example.com", "plan-basico", "sede-santa-fe") };
        var seedIndex = 0;
        foreach (var (name, email, plan, sede) in seeds)
            users.TryAdd(email, new Usuario(seedIndex++ == 0 ? "member-demo" : $"member-{seedIndex}", name, email, "3000000000", "", Rol.Miembro, plan, sede, DateTime.UtcNow.AddDays(-20)));
    }

    public Task<Usuario?> FindByEmailAsync(string correo, CancellationToken ct) => Task.FromResult(users.TryGetValue(correo.Trim(), out var user) ? user : null);
    public Task<Usuario?> FindByIdAsync(string id, CancellationToken ct) => Task.FromResult(users.Values.FirstOrDefault(user => user.Id == id));
    public Task AddAsync(Usuario usuario, CancellationToken ct) => Task.FromResult(users.TryAdd(usuario.Correo, usuario) ? usuario : throw new InvalidOperationException("Ya existe un usuario con ese correo."));
    public Task UpdateAsync(Usuario usuario, CancellationToken ct) { users[usuario.Correo] = usuario; return Task.CompletedTask; }
}
