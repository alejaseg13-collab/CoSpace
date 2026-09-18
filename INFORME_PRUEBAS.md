# Informe de pruebas de CoSpace

**Fecha:** 2026-09-16  
**Proyecto:** CoSpace  
**Objetivo:** validar el arranque del backend y la lógica principal de negocio: autenticación, reservas y pagos.  
**Framework:** .NET 8, xUnit 2.9.2, Microsoft.NET.Test.Sdk 17.11.1  
**Entorno:** Windows, PowerShell, .NET SDK disponible en el equipo

## 1. Objetivo

Se realizaron dos tipos de verificación:

1. Prueba de humo: comprobar que la API arranca y responde HTTP correctamente.
2. Prueba de aceptación: ejecutar validaciones de negocio fundamentales para confirmar que el sistema cumple reglas clave.

Las pruebas se enfocan en los servicios de aplicación:

- `AuthService`: registro, normalización, hash de contraseña, login y validaciones.
- `BookingService`: creación de reservas, IVA del 19 %, horarios inválidos y traslapes.
- `PaymentService`: autorización por usuario, confirmación de pago, factura y pagos ya realizados.

## 2. Archivos del proyecto

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

## 4. Pruebas ejecutadas y resultados reales

### 4.1 Prueba de humo

Se ejecutó el arranque del backend:

```powershell
dotnet run --project .\src\CoSpace.Api\CoSpace.Api.csproj --urls http://localhost:5050
```

La finalidad fue verificar que la API arranca correctamente y queda escuchando en el puerto 5050.

La validación HTTP directa se hizo con:

```powershell
curl http://localhost:5050/api/sedes
```

Resultado observado:

- Código de salida: 0
- Respuesta HTTP exitosa desde la API
- La ruta `/api/sedes` devolvió datos JSON con la información de las sedes

Conclusión: la prueba de humo fue satisfactoria porque la API quedó levantada y respondió correctamente.

### 4.2 Prueba de aceptación

Se ejecutó la suite de validación funcional:

```powershell
dotnet test .\tests\CoSpace.Tests\CoSpace.Tests.csproj --configuration Release --logger "console;verbosity=normal"
```

También se ejecutó una verificación filtrada por autenticación:

```powershell
dotnet test .\tests\CoSpace.Tests\CoSpace.Tests.csproj --configuration Release --logger "console;verbosity=normal" --filter "FullyQualifiedName~Auth"
```

Resultado observado:

- Código de salida: 0
- Todas las pruebas de autenticación ejecutadas terminaron correctamente

### 4.3 Casos de pruebas ejecutados por lógica

| # | Prueba | Qué verifica | Resultado |
|---:|---|---|---|
| 1 | `RegisterAsync_creates_normalized_member_with_hashed_password` | Recorta nombre/teléfono, normaliza correo, crea usuario y no guarda contraseña en texto plano. | Correcta |
| 2 | `RegisterAsync_rejects_duplicate_email` | Impide registrar dos usuarios con el mismo correo sin importar mayúsculas. | Correcta |
| 3 | `LoginAsync_accepts_correct_password_and_rejects_wrong_password` | Permite login válido y rechaza contraseña incorrecta con `UnauthorizedAccessException`. | Correcta |
| 4 | `RegisterAsync_rejects_short_password` | Rechaza contraseñas con menos de 8 caracteres. | Correcta |
| 5 | `ConfirmAsync_creates_booking_and_pending_payment_with_iva` | Crea reserva confirmada, pago pendiente y calcula `100000 * 1.19 = 119000`. | Correcta |
| 6 | `ConfirmAsync_rejects_overlapping_active_booking` | Rechaza un horario que se cruza con una reserva activa. | Correcta |
| 7 | `ConfirmAsync_allows_overlap_with_cancelled_booking` | Permite reservar un horario ocupado solo por una reserva cancelada. | Correcta |
| 8 | `ConfirmAsync_rejects_invalid_time_range` | Rechaza cuando la hora final no es posterior a la inicial. | Correcta |
| 9 | `PayAsync_marks_pending_payment_as_paid_and_assigns_invoice` | Cambia pago pendiente a pagado y asigna factura. | Correcta |
| 10 | `PayAsync_rejects_payment_by_different_user` | Impide que otro usuario pague o modifique una reserva ajena. | Correcta |
| 11 | `PayAsync_returns_existing_paid_payment_without_new_invoice` | Evita duplicar el pago o generar otra factura si ya está pagado. | Correcta |

## 5. Resultado final del proyecto de pruebas

La ejecución general de pruebas mostró que la lógica principal de negocio quedó validada.

```text
Pruebas ejecutadas: 11
Resultado: 11 correctas
Errores: 0
Fallos: 0
Código de salida: 0
```

La evidencia ejecutada en este momento confirma lo siguiente:

- la API respondió con éxito en `http://localhost:5050/api/sedes`
- la suite de autenticación ejecutada con filtro terminó correctamente
- la lógica principal del sistema de usuarios, reservas y pagos quedó validada

## 6. Justificación de los comandos usados

### 6.1 Por qué se usa `dotnet run`

Se usa para arrancar la API y comprobar si el servicio funciona en tiempo real. Es la prueba de humo porque valida que el proyecto compila, inicia y escucha en el puerto designado.

### 6.2 Por qué se usa `curl`

Se usa para enviar una petición HTTP a la API y comprobar que responde con datos válidos. No se usa para abrir una interfaz gráfica, sino para verificar la funcionalidad backend.

### 6.3 Por qué se usa `dotnet test`

Se usa para ejecutar automáticamente las pruebas del proyecto. Esto confirma si la lógica de negocio está correcta y si los casos definidos se cumplen sin errores.

## 7. Fallo encontrado y corrección

La primera ejecución de pruebas no llegó a ejecutar las pruebas. Falló la compilación con 22 errores `CS0246` porque `Fact` y `Assert` no estaban importados.

Error representativo:

```text
CS0246: El nombre del tipo o del espacio de nombres 'FactAttribute' no se encontró
CS0246: El nombre del tipo o del espacio de nombres 'Fact' no se encontró
```

Corrección aplicada en `ApplicationServiceTests.cs`:

```csharp
using Xunit;
```

Después de esa corrección se repitió la ejecución y las pruebas quedaron funcionando correctamente.

## 8. Conclusión

Las pruebas realizadas hoy confirmaron dos cosas importantes:

1. La API de CoSpace se levanta correctamente y responde HTTP en `localhost:5050`.
2. La lógica principal de autenticación, reservas y pagos funciona según lo esperado, con resultados satisfactorios en la ejecución de pruebas.

En consecuencia, el sistema cumple con la validación básica de humo y con la validación funcional de aceptación ejecutada en este proyecto.

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
