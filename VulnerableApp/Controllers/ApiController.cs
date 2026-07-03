using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using VulnerableApp.Data;
using System.Diagnostics;

namespace VulnerableApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ApiController> _logger;

        public ApiController(AppDbContext db, ILogger<ApiController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("user/{id}")]
        public IActionResult GetUser(int id)
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";

            _logger.LogInformation("Inicio Api.GetUser. IP: {IP}, Usuario de sesión: {User}, Parámetro ID Solicitado: {Id}", ip, sessionUser, id);

            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (!currentUserId.HasValue)
                {
                    _logger.LogWarning("Intento de acceso no autorizado (401) a la API para el ID {Id} desde la IP {IP}", id, ip);
                    return Unauthorized();
                }

                if (id != currentUserId.Value)
                {
                    _logger.LogWarning("Intento de acceso prohibido (403): El usuario {User} intentó acceder a los datos del ID {Id}", sessionUser, id);
                    return Forbid();
                }

                var user = _db.Users.Find(id);
                if (user == null)
                {
                    _logger.LogWarning("Usuario con ID {Id} no encontrado en la base de datos (404)", id);
                    return NotFound();
                }

                _logger.LogInformation("Datos del usuario {Id} consultados correctamente por {User}", id, sessionUser);
                return Ok(new { user.Id, user.Username, user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Api.GetUser al intentar buscar el ID {Id}", id);
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Fin Api.GetUser. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }

        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";

            _logger.LogInformation("Inicio Api.GetAllUsers. IP: {IP}, Usuario solicitante: {User}", ip, sessionUser);

            try
            {
                var users = _db.Users.ToList();
                _logger.LogInformation("Consulta exitosa de todos los usuarios. Total devuelto: {Count}", users.Count);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar obtener la lista completa de usuarios");
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Fin Api.GetAllUsers. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }
    }
}