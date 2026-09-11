# Guion 1: Enfoque, arquitectura y despliegue de CoSpace

## Introducción

Hola. En esta presentación vamos a explicar el enfoque general del proyecto CoSpace, su arquitectura, la conexión con Firebase, el despliegue de la API en Render y la publicación del frontend en Vercel.

CoSpace es una plataforma para administrar espacios de coworking. Permite consultar sedes y espacios disponibles, registrar usuarios, iniciar sesión, crear reservas, gestionar pagos y generar información de facturación.

El objetivo principal fue pasar de una aplicación que solamente se podía visualizar de forma local con Live Server a una solución publicada en internet, con una API funcional y una base de datos persistente.

## 1. Enfoque del proyecto

El proyecto se organizó separando la interfaz, la lógica de negocio y la infraestructura.

La idea central es que cada parte tenga una responsabilidad clara:

- El frontend se encarga de la experiencia visual y de recoger las acciones del usuario.
- La API recibe las solicitudes y expone los endpoints del sistema.
- La capa de aplicación contiene las reglas del negocio.
- La capa de dominio define las entidades y enumeraciones.
- La infraestructura conecta la aplicación con Firebase Firestore.

Esta separación permite cambiar la base de datos o el proveedor de despliegue sin tener que reescribir toda la aplicación.

## 2. Arquitectura por capas

### Capa de presentación

La presentación está construida con HTML, CSS y JavaScript sin un framework pesado. Esto permite que el frontend sea estático y fácil de publicar.

Entre las pantallas principales están:

- Inicio y presentación de CoSpace.
- Registro e inicio de sesión.
- Consulta de sedes.
- Disponibilidad y reserva de espacios.
- Perfil del usuario.
- Pagos y facturación.
- Panel administrativo.

Los archivos JavaScript realizan solicitudes HTTP a la API. En desarrollo se utiliza `http://localhost:5050/api`, mientras que en producción el frontend utiliza `/api`.

Esto es importante porque el usuario final no necesita conocer la dirección interna del backend.

### Capa de dominio

El proyecto de dominio contiene las entidades principales:

- `Usuario`.
- `Sede`.
- `Espacio`.
- `Reserva`.
- `Pago`.
- `Plan`.

También contiene enumeraciones para representar estados, por ejemplo:

- Estado de una reserva: confirmada, completada o cancelada.
- Estado de un pago: pagado, pendiente o vencido.
- Métodos de pago: tarjeta, PSE o billetera digital.

Esta capa no depende de Firebase, Render ni Vercel. Solo define el modelo del negocio.

### Capa de aplicación

La capa de aplicación contiene los servicios que aplican las reglas del sistema.

`AuthService` controla el registro y el inicio de sesión. Valida el correo, exige una contraseña mínima de ocho caracteres, normaliza los datos y almacena un hash de la contraseña en lugar de guardarla directamente.

`BookingService` crea reservas y evita los traslapes. Antes de confirmar una reserva consulta las reservas existentes para el mismo espacio y fecha. Si encuentra un cruce de horario con una reserva activa, rechaza la operación.

También calcula el IVA del 19 %. Por ejemplo, si el subtotal es 100.000 pesos, el total almacenado es:

```text
100.000 × 1,19 = 119.000
```

`PaymentService` confirma pagos, comprueba que el usuario sea dueño de la reserva, genera el número de factura y evita volver a cobrar un pago que ya está confirmado.

### Capa de infraestructura

La infraestructura contiene los adaptadores que conectan la aplicación con servicios externos.

En este caso se implementó `FirestoreContext`, que crea una conexión con Google Cloud Firestore utilizando las credenciales de una cuenta de servicio.

Los repositorios principales son:

- `FirebaseUserRepository`: usuarios.
- `FirebaseBookingRepository`: reservas.
- `FirebasePaymentRepository`: pagos y consecutivos de factura.
- `FirebaseMemberStore`: historial, perfil, cancelaciones e invoices.

La aplicación no conoce los detalles de Firestore. Solo utiliza interfaces como `IUserRepository`, `IBookingRepository` e `IPaymentRepository`.

## 3. Firebase Firestore

Antes de este cambio, los repositorios llamados Firebase guardaban información en memoria usando estructuras como `ConcurrentDictionary` y `ConcurrentBag`. Eso permitía hacer demostraciones, pero todos los datos se perdían cuando se reiniciaba la API.

Para resolverlo se reemplazó esa implementación por Firestore.

Firestore es una base de datos NoSQL de documentos. CoSpace utiliza estas colecciones:

- `users`: información de usuarios y autenticación.
- `reservations`: reservas realizadas.
- `payments`: pagos y facturas.
- `settings/invoice`: contador para el consecutivo de facturas.

La API convierte cada entidad de C# en un documento de Firestore y vuelve a convertir el documento en una entidad cuando consulta la información.

Para generar facturas se utiliza una transacción de Firestore. Esto evita que dos solicitudes simultáneas obtengan el mismo número de factura.

## 4. Seguridad de las credenciales

La cuenta de servicio de Firebase contiene una clave privada y nunca debe subirse a GitHub.

Por eso se configuraron variables de entorno en Render:

```text
FIREBASE_PROJECT_ID
FIREBASE_SERVICE_ACCOUNT_JSON
```

`FIREBASE_PROJECT_ID` contiene el identificador del proyecto Firebase.

`FIREBASE_SERVICE_ACCOUNT_JSON` contiene el JSON completo de la cuenta de servicio, guardado como secreto en Render.

El JSON no se incluye en el repositorio, no se almacena en el frontend y no se publica en Vercel.

## 5. API en Render

La API es una aplicación ASP.NET Core .NET 8. Vercel es excelente para frontend estático y funciones JavaScript, pero no ejecuta esta API .NET como un servidor tradicional.

Por eso la API se desplegó en Render.

El repositorio incluye un `Dockerfile` que describe cómo construir y ejecutar la API:

1. Utiliza el SDK de .NET 8 para compilar.
2. Publica la aplicación en modo Release.
3. Utiliza la imagen runtime de ASP.NET 8.
4. Expone el puerto `10000`, que es el puerto utilizado por Render.
5. Inicia `CoSpace.Api.dll`.

Render ejecuta la API y entrega una URL pública:

```text
https://cospace-api.onrender.com
```

La API se comprobó con el endpoint:

```text
https://cospace-api.onrender.com/api/sedes
```

La respuesta fue `HTTP 200` y devolvió las cinco sedes de CoSpace.

## 6. Frontend en Vercel

El frontend se publicó en Vercel desde el repositorio de GitHub.

La URL pública es:

```text
https://co-space-nine.vercel.app/
```

Para que el navegador pueda utilizar la API, se configuró un proxy en `vercel.json`:

```json
{
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://cospace-api.onrender.com/api/:path*"
    }
  ]
}
```

Esto significa que cuando el frontend solicita:

```text
https://co-space-nine.vercel.app/api/sedes
```

Vercel redirige internamente la solicitud a:

```text
https://cospace-api.onrender.com/api/sedes
```

Para el usuario todo funciona desde una única dirección de Vercel.

## 7. CORS y comunicación entre servicios

La API permite configurar el dominio del frontend mediante la variable:

```text
FRONTEND_ORIGIN
```

Cuando se conoce la URL definitiva de Vercel, Render puede permitir solamente ese origen. Esto evita aceptar solicitudes desde cualquier sitio.

El flujo final es:

```text
Usuario
  ↓
Frontend en Vercel
  ↓ /api
Proxy de Vercel
  ↓
API ASP.NET en Render
  ↓
Firestore en Firebase
```

## 8. GitHub y control de versiones

El código quedó publicado en:

```text
https://github.com/alejaseg13-collab/CoSpace
```

Los cambios se organizaron en commits. Entre ellos están:

- Preparación inicial para despliegue.
- Integración de Firestore y Docker.
- Configuración del proxy de Vercel.
- Incorporación de pruebas unitarias e informe.
- Incorporación del informe HTML.

Esto permite mantener una historia de cambios y volver a desplegar automáticamente cuando se actualiza la rama `main`.

## Cierre

En conclusión, CoSpace pasó de ser una interfaz local a una aplicación distribuida:

- El frontend está en Vercel.
- La API está en Render.
- La persistencia está en Firestore.
- El código está versionado en GitHub.
- El proxy de Vercel permite que el usuario vea una sola URL.

La arquitectura mantiene separadas las responsabilidades y deja una base preparada para agregar nuevas funciones, como autenticación más avanzada, pasarela de pagos real, panel administrativo conectado a Firestore y pruebas end-to-end.
