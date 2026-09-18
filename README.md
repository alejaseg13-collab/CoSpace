# CoSpace

Panel de gestión de reservas y pagos para cinco sedes de coworking en Bogotá.

## Ejecutar la interfaz

Abre `index.html` directamente en el navegador. Es la página pública de CoSpace, con hero, categorías, sedes, planes, contacto y enlaces funcionales a autenticación y reservas. `admin.html` conserva el panel administrativo con navegación responsive, búsqueda y filtro de espacios, creación de reservas, validación de traslapes, IVA del 19% y factura consecutiva simulada. `reservas.html` muestra el catálogo filtrable por tipo y sede.

## Ejecutar la API

Requiere .NET 8 o superior. Desde la raíz del proyecto:

```powershell
dotnet run --project .\src\CoSpace.Api\CoSpace.Api.csproj --urls http://localhost:5050
```

Luego abre `index.html` con Live Server. La URL del backend se configura en `api-config.js`; en producción reemplaza `http://localhost:5050/api` por la URL pública de la API.

## Backend

La solución está organizada por capas en `src/`:

- `CoSpace.Domain`: entidades y enumeraciones del modelo.
- `CoSpace.Application`: casos de uso y regla de no doble reserva.
- `CoSpace.Infrastructure`: repositorios en memoria con datos semilla; todavía no es una conexión persistente a Firebase.
- `CoSpace.Api`: endpoint mínimo para confirmar una reserva y pago.

La API compila con .NET 8, pero los repositorios llamados `Firebase*` todavía usan `ConcurrentDictionary`/`ConcurrentBag`. Los datos se pierden al reiniciar. Para conectar Firebase de verdad hay que agregar Firebase Admin SDK, configurar la cuenta de servicio mediante variables de entorno y reemplazar estas implementaciones por lecturas/escrituras de Firestore.

## Despliegue

### Firebase/Firestore

En Firebase Console crea un proyecto, habilita Firestore y crea una cuenta de servicio en **Project settings > Service accounts**. Conserva el JSON fuera de Git. La API usa las colecciones `users`, `reservations`, `payments` y `settings/invoice`.

### API en Render

Este repositorio incluye `Dockerfile` y `render.yaml`. Crea un Web Service desde GitHub con runtime Docker y configura estas variables:

- `FIREBASE_PROJECT_ID`: ID del proyecto Firebase.
- `FIREBASE_SERVICE_ACCOUNT_JSON`: contenido completo del JSON de la cuenta de servicio.
- `FRONTEND_ORIGIN`: dominio final de Vercel, por ejemplo `https://cospace.vercel.app`.

Render entregará una URL similar a `https://cospace-api.onrender.com`.

### Frontend y proxy en Vercel

El archivo `vercel.json` funciona como proxy `/api/*` hacia Render. Importa el repositorio en Vercel con la raíz del proyecto como Root Directory. El frontend usa `/api` automáticamente cuando se abre fuera de localhost, por lo que el navegador solo verá el dominio de Vercel.

La cuenta de servicio nunca debe subirse al repositorio. Si cambia el dominio de Vercel, actualiza `FRONTEND_ORIGIN` en Render.

### Usuarios de ejemplo

Para habilitar las cuentas demo en Firestore, configura explícitamente `SEED_DEMO_USERS=true` en Render. En local el valor predeterminado es `false`, para que la API pueda iniciar sin credenciales de Firebase; la pantalla de login muestra botones para cargar estas credenciales cuando el backend está configurado:

- Miembro: `sebastian.gil@example.com` / `Demo1234!`
- Administradora: `admin@cospace.co` / `Demo1234!`

Son cuentas únicamente para demostración; cambia o desactiva `SEED_DEMO_USERS` en un entorno real.

El frontend es estático y puede publicarse en Vercel. La API ASP.NET debe publicarse como un servicio separado compatible con .NET, por ejemplo Azure App Service, Render o Railway. Después coloca su URL pública en `api-config.js` y configura CORS para permitir únicamente el dominio final de Vercel.