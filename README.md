# Prueba Técnica Backend — Catálogo de Marcas de Autos

API REST en **.NET 8 + Entity Framework Core + PostgreSQL** que expone un catálogo
de marcas de autos, con migraciones, data seed, pruebas unitarias (XUnit) y
entorno completo por **Docker Compose**.

## Requisitos de la prueba y cómo se cumplen

| Requisito | Implementación |
|---|---|
| `DbContext` conectado a PostgreSQL con EF | `Data/MarcasAutosDbContext.cs` + provider Npgsql, cadena por variables de entorno |
| Migración de la tabla `MarcasAutos` | `Migrations/..._CreateMarcasAutos.cs` (tabla + `__EFMigrationsHistory`) |
| Data seed con ≥ 3 marcas | `HasData` en `MarcaAutoConfiguration` (Toyota, Ford, Chevrolet) versionado **dentro** de la migración |
| `MarcasAutosController` con listado, filtro, paginación y consulta por id | `GET /api/marcas-autos` y `GET /api/marcas-autos/{id}` |
| Pruebas XUnit con contexto en memoria | Pruebas unitarias (EF InMemory) + integración (`WebApplicationFactory`) |
| Cobertura ≥ 70% | **98.65% líneas / 95.83% ramas**, umbral 70% aplicado en cada `dotnet test` |
| `docker-compose.yml` (PostgreSQL + API) | Healthcheck, volumen, credenciales y connection string por variables de entorno |

## Stack tecnológico

- **.NET 8** (LTS) — ASP.NET Core Web API con controladores
- **Entity Framework Core 8** + **Npgsql** — PostgreSQL 16
- **XUnit** + **FluentAssertions no requeridas** (aserts nativos) + **Coverlet**
- **Swagger / Swashbuckle** — documentación interactiva
- **Docker / Docker Compose** — entorno ejecutable reproducible
- **GitHub Actions** — CI con umbral de cobertura

## Requisitos previos

- [Docker](https://docs.docker.com/get-docker/) con Docker Compose v2 (recomendado), **o**
- .NET SDK 8.0.x para ejecutar en local sin Docker

## Ejecutar con Docker Compose (recomendado)

```bash
docker compose up --build
```

La API aplica sus migraciones automáticamente al arrancar y deja la tabla sembrada.
Al terminar:

| Recurso | URL |
|---|---|
| API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| Endpoint | `GET http://localhost:8080/api/marcas-autos` |

Detener (elimina contenedores; `-v` elimina también el volumen de datos):

```bash
docker compose down      # conserva los datos
docker compose down -v   # limpia por completo
```

### Configuración por variables de entorno

Todos los valores son opcionales (hay defaults). Copia `.env.example` a `.env`
para ajustarlos:

| Variable | Default | Descripción |
|---|---|---|
| `POSTGRES_HOST_PORT` | `5432` | Puerto de PostgreSQL en el host. Si el 5432 ya está ocupado por otro servidor (p. ej. un PostgreSQL del sistema), usa `5433` en `.env` |
| `API_HOST_PORT` | `8080` | Puerto de la API en el host |
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | `prueba_backend` / `postgres` / `postgres` | Credenciales del contenedor (solo local) |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Entorno de la API |
| `ConnectionStrings__Postgres` | — | Cadena de conexión (ya la define compose hacia el servicio `postgres`). Acepta el formato clave=valor **y** URL `postgres://` (la que entregan proveedores como Render), que se normaliza automáticamente al arrancar |

### Imagen publicada en GitHub Container Registry

Cada push a `main` ejecuta el workflow `.github/workflows/docker-publish.yml`,
que construye la imagen multi-stage y la publica en **`ghcr.io`** (con el token
del propio repositorio: no requiere cuentas ni secretos adicionales). En
cualquier máquina con Docker:

```bash
docker pull ghcr.io/dgalfaro-dev/carbrands:latest

docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Postgres="Host=<host-del-postgres>;Port=5432;Database=prueba_backend;Username=postgres;Password=postgres" \
  ghcr.io/dgalfaro-dev/carbrands:latest
```

| Recurso | URL |
|---|---|
| Swagger UI | http://localhost:8080/swagger |
| Endpoint | `GET http://localhost:8080/api/marcas-autos` |
| Página de la imagen | https://github.com/DgAlfaro-dev/CarBrands/pkgs/container/carbrands |

- La imagen contiene **solo la API** (aplica sus migraciones al arrancar); para
  levantar API y PostgreSQL juntos en una máquina nueva, usa
  `docker compose up --build`.
- Etiquetas disponibles: `latest` (último push a `main`) y `sha-<commit>`
  (versión exacta y reproducible).

## Ejecutar en local (sin Docker)

1. Levanta un PostgreSQL (por ejemplo con este fragmento suelto):

   ```bash
   docker run -d --name pg-local -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=prueba_backend -p 5432:5432 postgres:16-alpine
   ```

2. Ajusta `ConnectionStrings:Postgres` en `appsettings.json` o define la
   variable `ConnectionStrings__Postgres` si usas otro puerto.

3. Ejecuta la API (aplica las migraciones al arrancar):

   ```bash
   dotnet run --project src/PruebaBackend.Api
   ```

## Ejecutar las pruebas

```bash
dotnet test
```

- **36 tests** en verde (unitarios con contexto **en memoria** + integración con la API real).
- Genera el reporte en `tests/PruebaBackend.Api.Tests/coverage/coverage.cobertura.xml`.
- **Cobertura verificada: 98.65% líneas · 95.83% ramas · 100% métodos.**
- El `csproj` de tests define umbrales (**70% líneas y ramas**): si la cobertura
  baja del mínimo, `dotnet test` **falla** (igual que en CI).
- Exclusiones: solo las migraciones (código generado) y el propio assembly de tests.

> Para comprobar que el umbral realmente bloquea:
> `dotnet test -p:Threshold=99` → debe terminar con error.

## Endpoints

### `GET /api/marcas-autos`

Devuelve las marcas ordenadas por id. Admite filtro por nombre y paginación:

| Parámetro | Tipo | Descripción |
|---|---|---|
| `nombre` | texto, opcional | Busca coincidencias parciales sin distinguir mayúsculas. Por ejemplo, `?nombre=toy` encuentra `Toyota`. Los espacios al inicio y al final se ignoran. |
| `page` | entero, opcional | Número de página, empezando en `1`. Si se envía sin `pageSize`, el tamaño de página predeterminado es `10`. |
| `pageSize` | entero, opcional | Resultados por página; rango permitido: `1` a `100`. Si se envía sin `page`, se usa la página `1`. |

Si no se envía `page` ni `pageSize`, se conserva el listado completo. Se pueden combinar el filtro y la paginación, por ejemplo:

```http
GET /api/marcas-autos?nombre=to&page=1&pageSize=10
```

Cuando se solicita paginación, la respuesta incluye estas cabeceras. `X-Total-Count` cuenta los resultados después del filtro y antes de paginar:

| Cabecera | Descripción |
|---|---|
| `X-Total-Count` | Cantidad total de resultados que coinciden con el filtro. |
| `X-Page` | Página devuelta. |
| `X-Page-Size` | Tamaño aplicado a cada página. |
| `X-Total-Pages` | Cantidad total de páginas. |

`page` menor que `1`, o `pageSize` fuera del rango permitido, devuelve `400 Bad Request` con `ValidationProblemDetails`. Una página que supera la última disponible devuelve `200 OK` con una lista vacía y las cabeceras de paginación.

```json
[
  { "id": 1, "nombre": "Toyota" },
  { "id": 2, "nombre": "Ford" },
  { "id": 3, "nombre": "Chevrolet" }
]
```

| Código | Cuando |
|---|---|
| `200 OK` | Siempre que la base responda (lista vacía si no hay filas) |
| `400 Bad Request` + ValidationProblemDetails | `page` o `pageSize` no válido |
| `500` + ProblemDetails | Error inesperado de base de datos (RFC 7807) |
| `404` | Ruta desconocida |

### `GET /api/marcas-autos/{id}`

Devuelve una marca por su identificador. Por ejemplo, `GET /api/marcas-autos/1` devuelve `200 OK` con `{ "id": 1, "nombre": "Toyota" }`. Si no existe una marca con ese id, responde `404 Not Found` con `ProblemDetails`.

Swagger UI: http://localhost:8080/swagger (habilitado en todos los entornos para
que la API pueda evaluarse donde se despliegue).

## Estructura del proyecto

```text
prueba-backend/
├── .github/workflows/ci.yml      CI: restore → build → test con umbrales
├── .env.example                  Variables opcionales de compose
├── Dockerfile                    Build multi-stage (sdk:8.0 → aspnet:8.0)
├── docker-compose.yml            PostgreSQL 16 + API
├── PruebaBackend.sln
├── src/PruebaBackend.Api/
│   ├── Controllers/MarcasAutosController.cs   GET /api/marcas-autos
│   ├── Data/MarcasAutosDbContext.cs           DbContext (PostgreSQL)
│   ├── Data/Configurations/MarcaAutoConfiguration.cs  Tabla + seed HasData
│   ├── Dtos/MarcaAutoResponse.cs              Contrato de salida
│   ├── Entities/MarcaAuto.cs                  Entidad del dominio
│   ├── Migrations/                            Migración versionada
│   ├── Program.cs                             DI, ProblemDetails, Swagger, auto-migración
│   └── appsettings.json                       Config local (cadena por defecto)
└── tests/PruebaBackend.Api.Tests/
    ├── Controllers/MarcasAutosControllerTests.cs   4 tests unitarios
    ├── Data/MarcasAutosSeedTests.cs                2 tests del seed
    ├── Integration/MarcasAutosEndpointTests.cs     3 tests por HTTP
    ├── Integration/StartupConfigurationTests.cs    1 test de configuración
    └── Infrastructure/                             Fábricas de contexto y de API
```

## Decisiones técnicas

- **Alcance acotado**: la prueba es de una tabla y un endpoint; no se introdujeron
  DDD, MassTransit, CQRS ni microservicios porque añadirían estructura sin
  consumidores reales (las prácticas del equipo prohíben incorporar arquitectura
  sin justificación).
- **Seed vía `HasData`**: queda versionado en la migración, por lo que cualquier
  base que ejecute las migraciones queda poblada de forma reproducible (IDs fijos
  → idempotente).
- **Auto-migración al arrancar** con guarda `IsRelational()`: `docker compose up`
  deja la base lista sin pasos manuales; en tests (proveedor InMemory) se omite.
- **ProblemDetails (RFC 7807)** en lugar de `try/catch` vacíos: los errores no
  controlados responden JSON estructurado y quedan en logs.
- **DTO de salida** desacoplado de la entidad: el contrato HTTP no depende del
  modelo de persistencia.
- **Cobertura con `coverlet.msbuild`** (no el collector): permite aplicar
  umbrales que fallan la ejecución local y el CI. Las migraciones se excluyen por
  ser código generado.
- **Puerto de PostgreSQL configurable** (`POSTGRES_HOST_PORT`): permite convivir
  con un servidor local que ya use el 5432 sin editar el compose.

## Integración continua

`.github/workflows/ci.yml` ejecuta en cada push/PR a `main`:

1. `dotnet restore`
2. `dotnet build --no-restore` (cero errores/advertencias esperados)
3. `dotnet test --no-build` → aplica los umbrales de cobertura (70/70); si no se
   cumplen, **el paso falla**
4. Sube `coverage/` como artefacto

> La secuencia completa se validó localmente con los mismos comandos; el workflow
> se ejecutará en GitHub al publicar el repositorio.

## Convenciones

- **Código, commits y comentarios en inglés** (Conventional Commits: `feat:`, `fix:`, `test:`…),
  **documentación en español**.
- Sin secretos en el repositorio: las credenciales viajan por variables de entorno
  (`.env` está en `.gitignore`; `.env.example` documenta las claves).
