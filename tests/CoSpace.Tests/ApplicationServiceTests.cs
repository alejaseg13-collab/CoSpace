using CoSpace.Application.Authentication;
using CoSpace.Application.Payments;
using CoSpace.Application.Reservations;
using CoSpace.Domain;
using Xunit;

namespace CoSpace.Tests;

public sealed class ApplicationServiceTests
{
    private static readonly DateOnly BookingDate = new(2026, 9, 20);
    private static readonly string SpaceId = CatalogSeed.Espacios[0].Id;

    [Fact]
    public async Task RegisterAsync_creates_normalized_member_with_hashed_password()
    {
        var users = new FakeUserRepository();
        var service = new AuthService(users);

        var user = await service.RegisterAsync("  Ana Pérez ", " ANA@EXAMPLE.COM ", " 3001234567 ", "password-123", "sede-centro-mayor", CancellationToken.None);

        Assert.Equal("Ana Pérez", user.Nombre);
        Assert.Equal("ana@example.com", user.Correo);
        Assert.Equal("3001234567", user.Telefono);
        Assert.Equal(Rol.Miembro, user.Rol);
        Assert.NotEqual("password-123", user.Contrasena);
        Assert.Single(users.Items);
    }

    [Fact]
    public async Task RegisterAsync_rejects_duplicate_email()
    {
        var users = new FakeUserRepository();
        var service = new AuthService(users);
        await service.RegisterAsync("Ana", "ana@example.com", "3001234567", "password-123", "sede-centro-mayor", CancellationToken.None);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync("Otra", "ANA@example.com", "3000000000", "password-456", "sede-centro-mayor", CancellationToken.None));

        Assert.Equal("Ya existe un usuario con ese correo.", error.Message);
    }

    [Fact]
    public async Task LoginAsync_accepts_correct_password_and_rejects_wrong_password()
    {
        var users = new FakeUserRepository();
        var service = new AuthService(users);
        await service.RegisterAsync("Ana", "ana@example.com", "3001234567", "password-123", "sede-centro-mayor", CancellationToken.None);

        var authenticated = await service.LoginAsync("ana@example.com", "password-123", CancellationToken.None);
        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync("ana@example.com", "wrong-pass", CancellationToken.None));

        Assert.Equal("ana@example.com", authenticated.Correo);
        Assert.Equal("Correo o contraseña incorrectos.", error.Message);
    }

    [Fact]
    public async Task RegisterAsync_rejects_short_password()
    {
        var service = new AuthService(new FakeUserRepository());

        var error = await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync("Ana", "ana@example.com", "3001234567", "short", "sede-centro-mayor", CancellationToken.None));

        Assert.Contains("mínimo 8 caracteres", error.Message);
    }

    [Fact]
    public async Task ConfirmAsync_creates_booking_and_pending_payment_with_iva()
    {
        var bookings = new FakeBookingRepository();
        var payments = new FakePaymentRepository();
        var service = new BookingService(bookings, payments);

        var result = await service.ConfirmAsync("user-1", SpaceId, BookingDate, new TimeOnly(9), new TimeOnly(11), 100000, MetodoPago.Tarjeta, CancellationToken.None);

        Assert.Equal(119000m, result.Reserva.PrecioTotal);
        Assert.Equal(EstadoReserva.Confirmada, result.Reserva.Estado);
        Assert.Equal(EstadoPago.Pendiente, result.Pago.Estado);
        Assert.Equal(result.Reserva.Id, result.Pago.ReservaId);
        Assert.Single(bookings.Items);
        Assert.Single(payments.Items);
    }

    [Fact]
    public async Task ConfirmAsync_rejects_overlapping_active_booking()
    {
        var bookings = new FakeBookingRepository();
        bookings.Items.Add(new Reserva("existing", "user-2", SpaceId, BookingDate, new TimeOnly(10), new TimeOnly(12), EstadoReserva.Confirmada, 23800, DateTime.UtcNow));
        var service = new BookingService(bookings, new FakePaymentRepository());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmAsync("user-1", SpaceId, BookingDate, new TimeOnly(11), new TimeOnly(13), 100000, MetodoPago.Pse, CancellationToken.None));

        Assert.Equal("El espacio ya tiene una reserva en ese horario.", error.Message);
    }

    [Fact]
    public async Task ConfirmAsync_allows_overlap_with_cancelled_booking()
    {
        var bookings = new FakeBookingRepository();
        bookings.Items.Add(new Reserva("cancelled", "user-2", SpaceId, BookingDate, new TimeOnly(10), new TimeOnly(12), EstadoReserva.Cancelada, 23800, DateTime.UtcNow));
        var service = new BookingService(bookings, new FakePaymentRepository());

        var result = await service.ConfirmAsync("user-1", SpaceId, BookingDate, new TimeOnly(11), new TimeOnly(13), 100000, MetodoPago.Pse, CancellationToken.None);

        Assert.Equal(EstadoReserva.Confirmada, result.Reserva.Estado);
    }

    [Fact]
    public async Task ConfirmAsync_rejects_invalid_time_range()
    {
        var service = new BookingService(new FakeBookingRepository(), new FakePaymentRepository());

        var error = await Assert.ThrowsAsync<ArgumentException>(() => service.ConfirmAsync("user-1", SpaceId, BookingDate, new TimeOnly(13), new TimeOnly(12), 100000, MetodoPago.Tarjeta, CancellationToken.None));

        Assert.Equal("La hora final debe ser posterior a la inicial.", error.Message);
    }

    [Fact]
    public async Task PayAsync_marks_pending_payment_as_paid_and_assigns_invoice()
    {
        var reservation = new Reserva("reservation-1", "user-1", SpaceId, BookingDate, new TimeOnly(9), new TimeOnly(10), EstadoReserva.Confirmada, 119000, DateTime.UtcNow);
        var bookings = new FakeBookingRepository();
        bookings.Items.Add(reservation);
        var payments = new FakePaymentRepository { NextInvoice = "FAC-001246" };
        payments.Items.Add(new Pago("payment-1", reservation.Id, string.Empty, 119000, MetodoPago.Tarjeta, EstadoPago.Pendiente, DateTime.UtcNow));
        var service = new PaymentService(bookings, payments);

        var paid = await service.PayAsync("user-1", reservation.Id, MetodoPago.Pse, CancellationToken.None);

        Assert.Equal(EstadoPago.Pagado, paid.Estado);
        Assert.Equal("FAC-001246", paid.NumeroFactura);
        Assert.Equal(MetodoPago.Pse, paid.Metodo);
        Assert.Same(paid, payments.Updated);
    }

    [Fact]
    public async Task PayAsync_rejects_payment_by_different_user()
    {
        var reservation = new Reserva("reservation-1", "owner", SpaceId, BookingDate, new TimeOnly(9), new TimeOnly(10), EstadoReserva.Confirmada, 119000, DateTime.UtcNow);
        var bookings = new FakeBookingRepository();
        bookings.Items.Add(reservation);
        var payments = new FakePaymentRepository();
        payments.Items.Add(new Pago("payment-1", reservation.Id, string.Empty, 119000, MetodoPago.Tarjeta, EstadoPago.Pendiente, DateTime.UtcNow));
        var service = new PaymentService(bookings, payments);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.PayAsync("attacker", reservation.Id, MetodoPago.Tarjeta, CancellationToken.None));
        Assert.Null(payments.Updated);
    }

    [Fact]
    public async Task PayAsync_returns_existing_paid_payment_without_new_invoice()
    {
        var reservation = new Reserva("reservation-1", "user-1", SpaceId, BookingDate, new TimeOnly(9), new TimeOnly(10), EstadoReserva.Confirmada, 119000, DateTime.UtcNow);
        var bookings = new FakeBookingRepository();
        bookings.Items.Add(reservation);
        var paid = new Pago("payment-1", reservation.Id, "FAC-000001", 119000, MetodoPago.Tarjeta, EstadoPago.Pagado, DateTime.UtcNow);
        var payments = new FakePaymentRepository();
        payments.Items.Add(paid);
        var service = new PaymentService(bookings, payments);

        var result = await service.PayAsync("user-1", reservation.Id, MetodoPago.Pse, CancellationToken.None);

        Assert.Same(paid, result);
        Assert.Null(payments.Updated);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<Usuario> Items { get; } = [];
        public Task<Usuario?> FindByEmailAsync(string correo, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(item => item.Correo.Equals(correo.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task<Usuario?> FindByIdAsync(string id, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
        public Task AddAsync(Usuario usuario, CancellationToken ct) { Items.Add(usuario); return Task.CompletedTask; }
        public Task UpdateAsync(Usuario usuario, CancellationToken ct) { Items.RemoveAll(item => item.Id == usuario.Id); Items.Add(usuario); return Task.CompletedTask; }
    }

    private sealed class FakeBookingRepository : IBookingRepository
    {
        public List<Reserva> Items { get; } = [];
        public Task<IReadOnlyList<Reserva>> FindBySpaceAndDateAsync(string espacioId, DateOnly fecha, CancellationToken ct) => Task.FromResult<IReadOnlyList<Reserva>>(Items.Where(item => item.EspacioId == espacioId && item.Fecha == fecha).ToList());
        public Task<Reserva?> FindByIdAsync(string reservaId, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(item => item.Id == reservaId));
        public Task<IReadOnlyList<Reserva>> AllAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Reserva>>(Items.ToList());
        public Task AddAsync(Reserva reserva, CancellationToken ct) { Items.Add(reserva); return Task.CompletedTask; }
        public Task UpdateAsync(Reserva reserva, CancellationToken ct) { Items.RemoveAll(item => item.Id == reserva.Id); Items.Add(reserva); return Task.CompletedTask; }
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public List<Pago> Items { get; } = [];
        public Pago? Updated { get; private set; }
        public string NextInvoice { get; init; } = "FAC-001000";
        public Task<string> NextInvoiceNumberAsync(CancellationToken ct) => Task.FromResult(NextInvoice);
        public Task AddAsync(Pago pago, CancellationToken ct) { Items.Add(pago); return Task.CompletedTask; }
        public Task<Pago?> FindByReservationAsync(string reservaId, CancellationToken ct) => Task.FromResult(Items.FirstOrDefault(item => item.ReservaId == reservaId));
        public Task UpdateAsync(Pago pago, CancellationToken ct) { Updated = pago; Items.RemoveAll(item => item.Id == pago.Id); Items.Add(pago); return Task.CompletedTask; }
    }
}
