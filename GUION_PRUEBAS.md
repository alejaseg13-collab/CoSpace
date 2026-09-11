# Guion 2: Explicación de las pruebas unitarias de CoSpace

## Introducción

Hola. En esta parte vamos a explicar las pruebas unitarias realizadas al proyecto CoSpace.

El objetivo de las pruebas fue comprobar que las reglas más importantes del sistema funcionen correctamente antes de confiar en el despliegue.

No se probó solamente si la página se veía bien. Se probaron las reglas que pueden afectar directamente a los usuarios y al negocio:

- Crear cuentas.
- Iniciar sesión.
- Validar contraseñas.
- Evitar correos duplicados.
- Crear reservas.
- Evitar reservas cruzadas.
- Calcular el IVA.
- Confirmar pagos.
- Evitar pagos duplicados.

## 1. ¿Qué es una prueba unitaria?

Una prueba unitaria revisa una parte pequeña y específica del código.

En este proyecto se probaron los servicios de aplicación:

- `AuthService`.
- `BookingService`.
- `PaymentService`.

Cada servicio se probó de forma independiente.

No se utilizaron datos reales de Firebase para estas pruebas. En su lugar, se crearon repositorios falsos en memoria. Esto permite que las pruebas sean rápidas, repetibles y no modifiquen la base de datos real.

## 2. Herramientas utilizadas

Se creó el proyecto:

```text
tests/CoSpace.Tests/CoSpace.Tests.csproj
```

Se utilizó:

- .NET 8.
- xUnit 2.9.2.
- Microsoft.NET.Test.Sdk 17.11.1.
- xUnit Visual Studio Runner.
- Repositorios falsos en memoria.

El comando utilizado para ejecutar las pruebas fue:

```powershell
dotnet test .\tests\CoSpace.Tests\CoSpace.Tests.csproj --configuration Release --logger "console;verbosity=normal"
```

El proyecto completo de pruebas se encuentra en:

```text
tests/CoSpace.Tests/ApplicationServiceTests.cs
```

## 3. Pruebas de autenticación

### Prueba 1: registro correcto

Nombre técnico:

```text
RegisterAsync_creates_normalized_member_with_hashed_password
```

Esta prueba crea un usuario con espacios adicionales y un correo escrito en mayúsculas.

Se verifica que:

- El nombre quede sin espacios innecesarios.
- El teléfono quede limpio.
- El correo se convierta a minúsculas.
- El usuario tenga el rol de miembro.
- La contraseña no quede almacenada directamente.
- El usuario sea guardado en el repositorio.

La idea es comprobar que el registro no solo acepte datos, sino que los guarde de forma consistente.

### Prueba 2: correo duplicado

Nombre técnico:

```text
RegisterAsync_rejects_duplicate_email
```

Primero se registra un usuario. Después se intenta registrar otro con el mismo correo, pero usando mayúsculas diferentes.

El sistema debe rechazarlo con el mensaje:

```text
Ya existe un usuario con ese correo.
```

Esto evita que una misma persona tenga registros duplicados por diferencias entre mayúsculas y minúsculas.

### Prueba 3: inicio de sesión

Nombre técnico:

```text
LoginAsync_accepts_correct_password_and_rejects_wrong_password
```

Esta prueba tiene dos partes:

1. Se utiliza la contraseña correcta y el login debe funcionar.
2. Se utiliza una contraseña incorrecta y el sistema debe rechazarla.

La respuesta esperada para la contraseña incorrecta es una excepción de autorización:

```text
UnauthorizedAccessException
```

### Prueba 4: contraseña corta

Nombre técnico:

```text
RegisterAsync_rejects_short_password
```

Se intenta crear un usuario con una contraseña de menos de ocho caracteres.

El sistema debe rechazarla y mostrar un mensaje que indique que la contraseña debe tener como mínimo ocho caracteres.

## 4. Pruebas de reservas

### Prueba 5: reserva correcta e IVA

Nombre técnico:

```text
ConfirmAsync_creates_booking_and_pending_payment_with_iva
```

Se crea una reserva con:

```text
Subtotal: 100.000
IVA: 19 %
Total esperado: 119.000
```

La prueba verifica que:

- La reserva se cree.
- La reserva quede en estado confirmada.
- Se cree un pago asociado.
- El pago quede pendiente.
- El total sea 119.000.
- La reserva y el pago tengan relación mediante el mismo identificador.

El cálculo probado es:

```text
100.000 × 1,19 = 119.000
```

### Prueba 6: traslape de reservas activas

Nombre técnico:

```text
ConfirmAsync_rejects_overlapping_active_booking
```

Se crea una reserva existente de 10:00 a 12:00. Luego se intenta crear otra de 11:00 a 13:00 para el mismo espacio y fecha.

Como los horarios se cruzan, el sistema debe rechazar la segunda reserva con:

```text
El espacio ya tiene una reserva en ese horario.
```

Esta prueba protege la regla más importante del sistema de reservas: no permitir dos reservas activas en el mismo espacio y horario.

### Prueba 7: reserva cancelada

Nombre técnico:

```text
ConfirmAsync_allows_overlap_with_cancelled_booking
```

Se repite un caso de horario cruzado, pero la reserva anterior está cancelada.

En este caso sí se permite la nueva reserva, porque una reserva cancelada ya no bloquea la disponibilidad.

Esta prueba demuestra que el sistema diferencia entre una reserva activa y una cancelada.

### Prueba 8: horario inválido

Nombre técnico:

```text
ConfirmAsync_rejects_invalid_time_range
```

Se intenta crear una reserva donde la hora inicial es 13:00 y la hora final es 12:00.

La operación debe rechazarse porque la hora final siempre debe ser posterior a la inicial.

El mensaje esperado es:

```text
La hora final debe ser posterior a la inicial.
```

## 5. Pruebas de pagos

### Prueba 9: confirmar pago pendiente

Nombre técnico:

```text
PayAsync_marks_pending_payment_as_paid_and_assigns_invoice
```

Se prepara una reserva y un pago pendiente.

Después se confirma el pago. La prueba verifica que:

- El estado pase de pendiente a pagado.
- Se actualice el método de pago.
- Se genere un número de factura.
- El pago actualizado se guarde en el repositorio.

En la prueba se utiliza una factura de ejemplo:

```text
FAC-001246
```

### Prueba 10: usuario no autorizado

Nombre técnico:

```text
PayAsync_rejects_payment_by_different_user
```

Se crea una reserva perteneciente a un usuario. Después se intenta pagar usando el identificador de otro usuario.

El sistema debe rechazar la operación con `UnauthorizedAccessException`.

Esta prueba evita que un usuario pueda modificar o pagar reservas ajenas.

### Prueba 11: pago ya confirmado

Nombre técnico:

```text
PayAsync_returns_existing_paid_payment_without_new_invoice
```

Se prepara un pago que ya está en estado pagado.

Luego se vuelve a llamar al servicio de pago.

El sistema debe devolver el pago existente y no debe:

- Generar una segunda factura.
- Crear un segundo pago.
- Modificar innecesariamente el registro.

Esta prueba evita cobros o facturas duplicadas.

## 6. Primer error encontrado

La primera vez que se ejecutó la suite apareció un error de compilación, no un error de la lógica del negocio.

El resultado inicial mostraba 22 errores relacionados con `Fact` y `Assert`:

```text
CS0246: El nombre del tipo o del espacio de nombres
'FactAttribute' no se encontró
```

La causa fue que el archivo de pruebas no tenía importado el namespace de xUnit.

La corrección fue agregar:

```csharp
using Xunit;
```

Después se ejecutó nuevamente el mismo comando de pruebas y el problema quedó resuelto.

## 7. Resultado final

El resultado final fue:

```text
Pruebas totales: 11
Correctas: 11
Errores: 0
Omitidas: 0
Duración: 1,4 segundos
```

Todas las pruebas pasaron correctamente.

Esto significa que las reglas principales cubiertas por esta suite se comportan como se esperaba.

## 8. ¿Qué no cubren estas pruebas?

Es importante aclarar que estas son pruebas unitarias. No cubren todo el sistema.

No se probaron directamente:

- La conexión real con Firestore.
- Las reglas de seguridad de Firebase.
- La cuenta de servicio en Render.
- Una pasarela de pagos real.
- La interfaz visual en todos los navegadores.
- La concurrencia de dos reservas creadas exactamente al mismo tiempo.
- Pruebas end-to-end completas con un navegador.

Esas verificaciones corresponden a pruebas de integración, pruebas de carga y pruebas end-to-end.

## 9. Verificaciones adicionales del despliegue

Además de las pruebas unitarias, se compiló la API completa con:

```powershell
dotnet build .\src\CoSpace.Api\CoSpace.Api.csproj --configuration Release --no-restore
```

La compilación fue correcta para:

- `CoSpace.Domain`.
- `CoSpace.Application`.
- `CoSpace.Infrastructure`.
- `CoSpace.Api`.

También se probó el endpoint público:

```text
https://co-space-nine.vercel.app/api/sedes
```

La respuesta fue:

```text
HTTP 200
```

Esto confirmó que Vercel puede comunicarse con el proxy y que el proxy puede llegar a la API desplegada en Render.

## Cierre

En conclusión, se implementó una suite de 11 pruebas unitarias que revisa las partes más importantes del negocio.

Todas las pruebas terminaron correctamente después de corregir la importación de xUnit.

Las pruebas demostraron que CoSpace:

- Valida correctamente los usuarios.
- Protege el inicio de sesión.
- Evita reservas cruzadas.
- Respeta las reservas canceladas.
- Calcula el IVA.
- Protege los pagos de otros usuarios.
- Evita duplicar facturas y pagos.

Esto proporciona una base confiable para continuar con pruebas de integración y pruebas end-to-end en el futuro.
