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

El frontend es estático y puede publicarse en Vercel. La API ASP.NET debe publicarse como un servicio separado compatible con .NET, por ejemplo Azure App Service, Render o Railway. Después coloca su URL pública en `api-config.js` y configura CORS para permitir únicamente el dominio final de Vercel.