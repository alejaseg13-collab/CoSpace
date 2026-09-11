# Informe de pruebas unitarias de CoSpace

**Fecha:** 2026-09-10  
**Proyecto:** CoSpace  
**Commit base probado:** `b04e7c9` más los cambios locales de pruebas  
**Framework:** .NET 8, xUnit 2.9.2, Microsoft.NET.Test.Sdk 17.11.1  
**Entorno:** Windows, .NET SDK 9.0.101, runtime de pruebas .NET 8.0.11

## 1. Objetivo

Verificar las reglas de negocio críticas de la aplicación sin depender de Firebase, Render, Vercel, red ni datos externos. Las pruebas se enfocan en los servicios de aplicación:

- `AuthService`: registro, normalización, hash de contraseña, login y validaciones.
- `BookingService`: creación de reservas, IVA del 19 %, horarios inválidos y traslapes.
- `PaymentService`: autorización por usuario, confirmación de pago, factura y pagos ya realizados.

## 2. Archivos creados

- `tests/CoSpace.Tests/CoSpace.Tests.csproj`: proyecto de pruebas .NET 8.
- `tests/CoSpace.Tests/ApplicationServiceTests.cs`: 11 pruebas xUnit y repositorios falsos en memoria.

El proyecto de pruebas referencia únicamente:

```xml
<ProjectReference Include="../../src/CoSpace.Domain/CoSpace.Domain.csproj" />
<ProjectReference Include="../../src/CoSpace.Application/CoSpace.Application.csproj" />
```

Los paquetes utilizados son:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="coverlet.collector" Version="6.0.2" />
```

## 3. Código utilizado para probar

Las pruebas se encuentran completas en `tests/CoSpace.Tests/ApplicationServiceTests.cs`. Este es el patrón principal de una prueba de reserva:

```csharp
[Fact]
public async Task ConfirmAsync_creates_booking_and_pending_payment_with_iva()
{
    var bookings = new FakeBookingRepository();
    var payments = new FakePaymentRepository();
    var service = new BookingService(bookings, payments);

    var result = await service.ConfirmAsync(
        "user-1", SpaceId, BookingDate,
        new TimeOnly(9), new TimeOnly(11),
        100000, MetodoPago.Tarjeta, CancellationToken.None);

    Assert.Equal(119000m, result.Reserva.PrecioTotal);
    Assert.Equal(EstadoReserva.Confirmada, result.Reserva.Estado);
    Assert.Equal(EstadoPago.Pendiente, result.Pago.Estado);
    Assert.Equal(result.Reserva.Id, result.Pago.ReservaId);
    Assert.Single(bookings.Items);
    Assert.Single(payments.Items);
}
```

Los repositorios reales no se usan en estas pruebas. Se implementaron `FakeUserRepository`, `FakeBookingRepository` y `FakePaymentRepository` dentro del archivo de pruebas. Esto permite probar exclusivamente la lógica de los servicios, sin crear datos reales en Firestore.

Ejemplo del repositorio falso de reservas:

```csharp
private sealed class FakeBookingRepository : IBookingRepository
{
    public List<Reserva> Items { get; } = [];

    public Task<IReadOnlyList<Reserva>> FindBySpaceAndDateAsync(
        string espacioId, DateOnly fecha, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Reserva>>(
            Items.Where(item => item.EspacioId == espacioId && item.Fecha == fecha).ToList());

    public Task<Reserva?> FindByIdAsync(string reservaId, CancellationToken ct) =>
        Task.FromResult(Items.FirstOrDefault(item => item.Id == reservaId));

    public Task<IReadOnlyList<Reserva>> AllAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Reserva>>(Items.ToList());

    public Task AddAsync(Reserva reserva, CancellationToken ct)
    {
        Items.Add(reserva);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Reserva reserva, CancellationToken ct)
    {
        Items.RemoveAll(item => item.Id == reserva.Id);
        Items.Add(reserva);
        return Task.CompletedTask;
    }
}
```

## 4. Casos ejecutados y resultados

| # | Prueba | Qué verifica | Resultado |
|---:|---|---|---|
| 1 | `RegisterAsync_creates_normalized_member_with_hashed_password` | Recorta nombre/teléfono, normaliza correo, crea miembro y no guarda la contraseña en texto plano. | Correcta |
| 2 | `RegisterAsync_rejects_duplicate_email` | Impide registrar dos usuarios con el mismo correo sin importar mayúsculas. | Correcta |
| 3 | `LoginAsync_accepts_correct_password_and_rejects_wrong_password` | Permite login válido y rechaza contraseña incorrecta con `UnauthorizedAccessException`. | Correcta |
| 4 | `RegisterAsync_rejects_short_password` | Rechaza contraseñas de menos de 8 caracteres. | Correcta |
| 5 | `ConfirmAsync_creates_booking_and_pending_payment_with_iva` | Crea reserva confirmada, pago pendiente y calcula `100000 * 1.19 = 119000`. | Correcta |
| 6 | `ConfirmAsync_rejects_overlapping_active_booking` | Rechaza un horario que se cruza con una reserva activa. | Correcta |
| 7 | `ConfirmAsync_allows_overlap_with_cancelled_booking` | Permite reservar un horario ocupado únicamente por una reserva cancelada. | Correcta |
| 8 | `ConfirmAsync_rejects_invalid_time_range` | Rechaza cuando la hora final no es posterior a la inicial. | Correcta |
| 9 | `PayAsync_marks_pending_payment_as_paid_and_assigns_invoice` | Cambia pago pendiente a pagado, asigna método y número de factura. | Correcta |
| 10 | `PayAsync_rejects_payment_by_different_user` | Impide que otro usuario pague o modifique una reserva ajena. | Correcta |
| 11 | `PayAsync_returns_existing_paid_payment_without_new_invoice` | Evita duplicar el pago o generar otra factura si ya está pagado. | Correcta |

## 5. Comandos ejecutados

### Ejecución principal

```powershell
dotnet test .\tests\CoSpace.Tests\CoSpace.Tests.csproj --configuration Release --logger "console;verbosity=normal"
```

### Resultado final

```text
Pruebas totales: 11
Correcto: 11
Errores: 0
Omitido: 0
duración: 5,8 s
Compilación realizado correctamente
```

### Validación adicional de la API

También se validó la compilación del backend completo:

```powershell
dotnet build .\src\CoSpace.Api\CoSpace.Api.csproj --no-restore
```

Resultado:

```text
CoSpace.Domain realizado correctamente
CoSpace.Application realizado correctamente
CoSpace.Infrastructure realizado correctamente
CoSpace.Api realizado correctamente
Compilación realizado correctamente
```

La API desplegada se comprobó mediante:

```powershell
Invoke-WebRequest -UseBasicParsing `
  'https://co-space-nine.vercel.app/api/sedes'
```

Resultado observado:

```text
HTTP 200
```

El proxy de Vercel devolvió las cinco sedes y confirmó la conexión Vercel -> Render -> API.

## 6. Fallo encontrado y corrección

La primera ejecución de `dotnet test` no llegó a ejecutar las pruebas. Falló la compilación con 22 errores `CS0246` porque `Fact` y `Assert` no estaban importados.

Error representativo:

```text
CS0246: El nombre del tipo o del espacio de nombres 'FactAttribute' no se encontró
CS0246: El nombre del tipo o del espacio de nombres 'Fact' no se encontró
```

Corrección aplicada en `ApplicationServiceTests.cs`:

```csharp
using Xunit;
```

Después de esa corrección se repitió el mismo comando y las 11 pruebas pasaron correctamente.

## 7. Cobertura y límites

Estas son pruebas unitarias: no llaman Firestore ni realizan pagos reales. Por diseño, no prueban:

- Reglas de seguridad de Firestore.
- Validez de la cuenta de servicio en Render.
- Conexión real de Render con Firestore.
- Navegación visual completa en distintos navegadores.
- Procesamiento real con una pasarela de pagos.
- Pruebas de carga o concurrencia de reservas simultáneas.

El siguiente nivel recomendado es agregar pruebas de integración contra una base Firebase de pruebas o un emulador de Firestore, y pruebas end-to-end con Playwright sobre la URL de Vercel.

## 8. Conclusión

Las reglas principales de autenticación, reservas y pagos están cubiertas por 11 pruebas unitarias y todas terminaron correctamente. Se corrigió el único fallo encontrado, que era la importación faltante de xUnit. La API también compila y el endpoint público a través del proxy de Vercel respondió `HTTP 200`.
