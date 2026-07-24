# Contexto del Proyecto: VulnerableApp

## Descripción General
VulnerableApp es una aplicación web desarrollada en ASP.NET Core (MVC y API) que está siendo instrumentada con prácticas de desarrollo seguro, manejo de errores y observabilidad.

## Stack Tecnológico
- **Framework:** .NET / ASP.NET Core
- **Base de datos:** SQL Server LocalDB (`mssqllocaldb`) con Entity Framework Core.
- **Logging:** Serilog (Sinks: Console, File, Seq).
- **Monitoreo Centralizado:** Seq (ejecutándose en Docker en el puerto `5341`, visualización en `8081`).

## Estado Actual
1. Serilog ya está completamente integrado en `Program.cs`, leyendo niveles mínimos desde `appsettings.json` y configurado con un enriquecedor `.Enrich.WithMachineName()`.
2. Los controladores (`HomeController`, `SearchController`, `AuthController`, `CommentController`, `ApiController`) ya fueron instrumentados de forma individual usando `ILogger<T>` y `Stopwatch` para medir tiempos y registrar parámetros, ocultando datos sensibles.
3. El proyecto requiere la implementación de **Middlewares Globales** en el pipeline de `Program.cs` para interceptar todas las peticiones, inyectar trazabilidad e interceptar excepciones a nivel global antes de que lleguen a los controladores.