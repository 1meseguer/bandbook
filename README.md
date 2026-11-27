# Bandbook

Sistema de cotizaciones y agenda para una agencia de management musical.
ASP.NET Core Razor Pages (.NET 10) y MySQL 8.

## Carpetas
- `Sql/`: Scripts de la base de datos.
    - esquema
    - datos
    - consultas y vistas
    - rutinas
    - pruebas
    - algebra relacional

- `BandFlow/`: Aplicación web.

## Cómo ejecutarlo
1. Instalar MySQL 8 y el SDK de .NET 10.
2. Ejecutar los scripts de `Sql/` en orden (01 a 05), o `ejecutar_todo.sh`.
3. Crear el usuario de la app y guardar la conexión en user-secrets (ver `BandFlow/README.md`).
4. Dentro de `BandFlow/`: `dotnet run`.
