# ProductApi

API REST de gestión de productos construida con ASP.NET Core,
arquitectura en capas y principios SOLID.

## Arquitectura

```
ProductApi (API)
    ↓ depende de
ProductApi.Application (casos de uso, DTOs, interfaces de servicio)
    ↓ depende de
ProductApi.Domain (entidades, interfaces de repositorio, excepciones)
    ↑ implementa
ProductApi.Infrastructure (repositorios, acceso a datos)
```

## Principios SOLID aplicados

| Principio | Dónde | Cómo |
|-----------|-------|------|
| **SRP** | `ProductService` | Solo orquesta casos de uso. No persiste ni serializa. |
| **SRP** | `ValidationFilter` | Solo valida el modelo de entrada. |
| **OCP** | `IProductService` | Nuevos casos de uso se añaden sin modificar los existentes. |
| **LSP** | `InMemoryProductRepository` | Sustituible por `EfProductRepository` sin cambiar comportamiento. |
| **ISP** | `IProductRepository` | Expone solo las operaciones necesarias para los consumidores actuales. |
| **DIP** | Toda la aplicación | `ProductService` depende de `IProductRepository`, nunca de la implementación concreta. |

## Patrones de diseño aplicados

| Patrón | Dónde | Por qué |
|--------|-------|---------|
| **Repository** | `IProductRepository` / `InMemoryProductRepository` | Desacopla el acceso a datos del dominio. |
| **Service Layer** | `ProductService` | Centraliza la lógica de negocio fuera del controlador. |
| **Middleware** | `ExceptionMiddleware` | Captura excepciones globalmente y devuelve JSON consistente. |
| **Action Filter** | `ValidationFilter` | Validación transversal sin repetir código en cada endpoint. |
| **DTO** | `CreateProductDto`, `ProductResponseDto`... | Desacopla el contrato HTTP de la entidad de dominio. |

## Cambiar a EF Core

Solo hay que tocar **una línea** en `Program.cs`:

```csharp
// Actual
builder.Services.AddScoped<IProductRepository, InMemoryProductRepository>();

// Con EF Core
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
```

El resto del código (servicio, controlador, tests) no necesita ningún cambio.

## Endpoints

| Método | Ruta | Descripción | Respuesta |
|--------|------|-------------|-----------|
| GET | `/api/v1/products` | Lista paginada con filtro y ordenación | 200 |
| GET | `/api/v1/products/{id}` | Detalle de producto | 200 / 404 |
| POST | `/api/v1/products` | Crear producto | 201 / 400 |
| PUT | `/api/v1/products/{id}` | Reemplazar producto completo | 200 / 400 / 404 / 409 |
| PATCH | `/api/v1/products/{id}` | Actualización parcial | 200 / 400 / 404 / 409 |
| DELETE | `/api/v1/products/{id}` | Eliminar producto | 204 / 404 |
| GET | `/health` | Estado de la aplicación | 200 |

## Ejecutar los tests

```bash
dotnet test
```

45 tests · 0 errores · xUnit + Moq