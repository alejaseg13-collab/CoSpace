using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using CoSpace.Domain;

namespace CoSpace.Application.Authentication;

public interface IUserRepository
{
    Task<Usuario?> FindByEmailAsync(string correo, CancellationToken ct);
    Task<Usuario?> FindByIdAsync(string id, CancellationToken ct);
    Task AddAsync(Usuario usuario, CancellationToken ct);
    Task UpdateAsync(Usuario usuario, CancellationToken ct);
}

public sealed class AuthService(IUserRepository users)
{
    public async Task<Usuario> RegisterAsync(string nombre, string correo, string telefono, string contrasena, string sedePreferidaId, CancellationToken ct)
    {
        ValidateEmail(correo);
        ValidatePassword(contrasena);
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(sedePreferidaId))
            throw new ArgumentException("Todos los campos obligatorios deben estar completos.");
        if (await users.FindByEmailAsync(correo, ct) is not null)
            throw new InvalidOperationException("Ya existe un usuario con ese correo.");

        var user = new Usuario(Guid.NewGuid().ToString("N"), nombre.Trim(), correo.Trim().ToLowerInvariant(), telefono.Trim(), Hash(contrasena), Rol.Miembro, "plan-basico", sedePreferidaId, DateTime.UtcNow);
        await users.AddAsync(user, ct);
        return user;
    }

    public async Task<Usuario> LoginAsync(string correo, string contrasena, CancellationToken ct)
    {
        ValidateEmail(correo);
        if (string.IsNullOrWhiteSpace(contrasena)) throw new ArgumentException("La contraseña es obligatoria.");
        var user = await users.FindByEmailAsync(correo, ct);
        if (user is null || user.Contrasena != Hash(contrasena)) throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        return user;
    }

    private static void ValidateEmail(string correo)
    {
        try { _ = new MailAddress(correo); }
        catch { throw new ArgumentException("El correo electrónico no tiene un formato válido."); }
    }

    private static void ValidatePassword(string contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length < 8) throw new ArgumentException("La contraseña debe tener mínimo 8 caracteres.");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
