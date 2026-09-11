using CoSpace.Application.Reservations;
using CoSpace.Application.Authentication;
using CoSpace.Application.Members;
using CoSpace.Application.Payments;
using CoSpace.Domain;
using CoSpace.Infrastructure.Firebase;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:10000");
builder.Services.AddSingleton<FirestoreContext>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddSingleton<IBookingRepository, FirebaseBookingRepository>();
builder.Services.AddSingleton<IPaymentRepository, FirebasePaymentRepository>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddSingleton<IUserRepository, FirebaseUserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<IMemberStore, FirebaseMemberStore>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<PaymentService>();
var allowedOrigin = builder.Configuration["FRONTEND_ORIGIN"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (string.IsNullOrWhiteSpace(allowedOrigin)) policy.AllowAnyOrigin();
    else policy.WithOrigins(allowedOrigin).AllowAnyHeader().AllowAnyMethod();
}));
var app = builder.Build();
app.UseCors();
if (builder.Configuration.GetValue("SEED_DEMO_USERS", true))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(CancellationToken.None);
}

app.MapPost("/api/auth/register", async (RegisterRequest request, AuthService auth, CancellationToken ct) =>
{
    if (!request.AceptaTerminos)
        return Results.BadRequest(new { error = "Debes aceptar los términos y condiciones." });
    if (request.Contrasena != request.ConfirmarContrasena)
        return Results.BadRequest(new { error = "Las contraseñas no coinciden." });
    try
    {
        var user = await auth.RegisterAsync(request.Nombre, request.Correo, request.Telefono, request.Contrasena, request.SedePreferidaId, ct);
        return Results.Created("/api/auth/me", PublicUser.From(user));
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
{
    try
    {
        var user = await auth.LoginAsync(request.Correo, request.Contrasena, ct);
        return Results.Ok(PublicUser.From(user));
    }
    catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapGet("/api/sedes", () => new[]
{
    new SedeMapResponse("sede-centro-mayor", "Centro Mayor", "Calle 38A Sur # 34D-51, Antonio Nariño, Bogotá.", 4.5948, -74.1434),
    new SedeMapResponse("sede-santa-fe", "Centro Comercial Santa Fe", "Autopista Norte con Calle 183, Bogotá.", 4.7612, -74.0451),
    new SedeMapResponse("sede-plaza-central", "Plaza Central", "Carrera 65 # 11-50, Puente Aranda, Bogotá.", 4.6277, -74.1307),
    new SedeMapResponse("sede-mallplaza-nqs", "Mallplaza NQS", "Carrera 30 # 19, Los Mártires, Bogotá.", 4.6198, -74.0772),
    new SedeMapResponse("sede-nuestro-bogota", "Nuestro Bogotá", "Av. Carrera 86 # 55A-75, Engativá, Bogotá.", 4.6958, -74.1064)
});

app.MapGet("/api/reservas/disponibilidad", async (string sedeId, DateOnly fecha, string? tipo, [FromServices] IBookingRepository bookings, CancellationToken ct) =>
{
    var type = tipo switch { "desk" => TipoEspacio.EscritorioFlexible, "office" or "cubicle" => TipoEspacio.OficinaPrivada, "meeting" => TipoEspacio.SalaReunion, _ => (TipoEspacio?)null };
    var spaces = CatalogSeed.Espacios.Where(space => space.SedeId == sedeId && (type is null || space.Tipo == type));
    var occupied = new List<AvailabilityResponse>();
    foreach (var space in spaces)
        foreach (var reservation in await bookings.FindBySpaceAndDateAsync(space.Id, fecha, ct))
            if (reservation.Estado != EstadoReserva.Cancelada)
                occupied.Add(new AvailabilityResponse(space.Id, reservation.HoraInicio.ToString("HH:mm"), reservation.HoraFin.ToString("HH:mm")));
    return Results.Ok(occupied);
});

app.MapGet("/api/perfil/{userId}", async (string userId, MemberService members, CancellationToken ct) =>
    await members.ProfileAsync(userId, ct) is { } profile ? Results.Ok(profile) : Results.NotFound());

app.MapPost("/api/pagos/confirmar", async (PayRequest request, PaymentService payments, CancellationToken ct) =>
{
    try { return Results.Ok(await payments.PayAsync(request.UsuarioId, request.ReservaId, request.Metodo, ct)); }
    catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
});
app.MapGet("/api/pagos/{userId}", async (string userId, PaymentService payments, CancellationToken ct) => Results.Ok(await payments.HistoryAsync(userId, ct)));
app.MapGet("/api/pagos/{userId}/{paymentId}/factura", async (string userId, string paymentId, PaymentService payments, CancellationToken ct) =>
{
    var history = await payments.HistoryAsync(userId, ct);
    var item = history.FirstOrDefault(entry => entry.Pago.Id == paymentId);
    if (item is null) return Results.NotFound();
    var text = $"Factura {item.Pago.NumeroFactura}\nCoSpace\nConcepto: {item.Espacio.Nombre} - {item.Espacio.Ubicacion}\nFecha: {item.Pago.Fecha:dd/MM/yyyy}\nMonto: {item.Pago.Monto:C0}\nEstado: {item.Pago.Estado}";
    return Results.File(System.Text.Encoding.UTF8.GetBytes(text), "text/plain", $"{item.Pago.NumeroFactura}.txt");
});
app.MapGet("/api/perfil/{userId}/reservas", async (string userId, MemberService members, CancellationToken ct) => Results.Ok(await members.ReservationsAsync(userId, ct)));
app.MapGet("/api/perfil/{userId}/pago-ultimo", async (string userId, MemberService members, CancellationToken ct) =>
    await members.LastPaymentAsync(userId, ct) is { } payment ? Results.Ok(payment) : Results.NoContent());
app.MapPost("/api/perfil/{userId}/reservas/{reservationId}/cancelar", async (string userId, string reservationId, MemberService members, CancellationToken ct) =>
{
    try { await members.CancelAsync(userId, reservationId, ct); return Results.NoContent(); }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
});
app.MapGet("/api/perfil/{userId}/reservas/{reservationId}/factura", async (string userId, string reservationId, MemberService members, CancellationToken ct) =>
{
    try { return Results.File(System.Text.Encoding.UTF8.GetBytes(await members.InvoiceAsync(userId, reservationId, ct)), "text/plain", $"factura-{reservationId}.txt"); }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
});
app.MapPatch("/api/perfil/{userId}", async (string userId, UpdateProfileRequest request, IUserRepository users, CancellationToken ct) =>
{
    var user = await users.FindByIdAsync(userId, ct);
    if (user is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Telefono) || string.IsNullOrWhiteSpace(request.SedePreferidaId)) return Results.BadRequest(new { error = "Completa los campos del perfil." });
    var updated = user with { Nombre = request.Nombre.Trim(), Telefono = request.Telefono.Trim(), SedePreferidaId = request.SedePreferidaId };
    await users.UpdateAsync(updated, ct);
    return Results.Ok(PublicUser.From(updated));
});

app.MapPost("/api/reservas/confirmar", async (ConfirmBookingRequest request, BookingService service, CancellationToken ct) =>
{
    try
    {
        var result = await service.ConfirmAsync(request.UsuarioId, request.EspacioId, request.Fecha, request.Inicio, request.Fin, request.Subtotal, request.Metodo, ct);
        return Results.Ok(new { reserva = result.Reserva, pago = result.Pago });
    }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
});
app.Run();

public sealed record ConfirmBookingRequest(string UsuarioId, string EspacioId, DateOnly Fecha, TimeOnly Inicio, TimeOnly Fin, decimal Subtotal, MetodoPago Metodo);
public sealed record SedeMapResponse(string Id, string Name, string Address, double Latitude, double Longitude);
public sealed record RegisterRequest(string Nombre, string Correo, string Telefono, string Contrasena, string ConfirmarContrasena, string SedePreferidaId, bool AceptaTerminos);
public sealed record LoginRequest(string Correo, string Contrasena, bool Recuerdame);
public sealed record PublicUser(string Id, string Nombre, string Correo, Rol Rol, string? PlanId, string? SedePreferidaId)
{
    public static PublicUser From(Usuario user) => new(user.Id, user.Nombre, user.Correo, user.Rol, user.PlanId, user.SedePreferidaId);
}
public sealed record UpdateProfileRequest(string Nombre, string Telefono, string SedePreferidaId);
public sealed record AvailabilityResponse(string EspacioId, string Inicio, string Fin);
public sealed record PayRequest(string UsuarioId, string ReservaId, MetodoPago Metodo);