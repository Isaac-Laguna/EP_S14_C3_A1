# Registro de videojuegos

Aplicación de consola en C# que usa ADO.NET y SQLite para administrar un registro de videojuegos.

## Requisitos

- .NET 8 SDK.
- El proyecto ya incluye `Microsoft.Data.Sqlite`, el proveedor necesario para conectar C# con SQLite mediante `SqliteConnection`.

## Ejecución

Desde la carpeta del proyecto, ejecuta:

```powershell
dotnet run
```

La aplicación crea el archivo `games.db` en la carpeta desde la que se ejecuta. Al iniciar una base de datos vacía, crea la tabla `Games` y agrega tres videojuegos de ejemplo. Después muestra un menú para agregar, eliminar, actualizar, listar todos o buscar juegos por ID, título, plataforma o año.

## Estructura de datos

La tabla `Games` contiene estas columnas:

| Columna | Tipo | Descripción |
| --- | --- | --- |
| `Id` | `INTEGER` | Identificador único autoincremental |
| `Title` | `TEXT` | Título del videojuego |
| `Platform` | `TEXT` | Plataforma del videojuego |
| `ReleaseYear` | `INTEGER` | Año de lanzamiento |

Las operaciones usan comandos SQL con parámetros para los valores ingresados. Las conexiones, comandos y lectores se administran con `using`.
