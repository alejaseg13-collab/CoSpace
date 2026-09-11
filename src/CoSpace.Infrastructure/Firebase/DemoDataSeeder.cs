using CoSpace.Application.Authentication;
using CoSpace.Domain;

namespace CoSpace.Infrastructure.Firebase;

public sealed class DemoDataSeeder(IUserRepository users)
{
    private const string DemoPasswordHash = "7DBB7F051B44D7D54584A7BC6C32F00DA5A3B5E6973485B9FB176FE56346B3E3";

    public async Task SeedAsync(CancellationToken cancellationToken)
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
}
