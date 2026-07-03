using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace VulnerableApp.Controllers
{
    public class CommentController : Controller
    {
        private static List<string> _comments = new();
        private readonly ILogger<CommentController> _logger; // Inyección de Serilog

        public CommentController(ILogger<CommentController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";

            _logger.LogInformation("Inicio Comment.Index. IP: {IP}, Usuario: {User}", ip, sessionUser);

            try
            {
                return View(_comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al cargar los comentarios para el usuario {User}", sessionUser);
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Fin Comment.Index. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        public IActionResult AddComment(string comment)
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";

            _logger.LogInformation("Inicio Comment.AddComment. IP: {IP}, Usuario: {User}, Parámetro: {Comment}", ip, sessionUser, comment);

            try
            {
                if (!string.IsNullOrEmpty(comment))
                {
                    _comments.Add(comment);
                    _logger.LogInformation("Comentario agregado exitosamente por el usuario {User}", sessionUser);
                }
                else
                {
                    _logger.LogWarning("El usuario {User} intentó agregar un comentario vacío desde la IP {IP}", sessionUser, ip);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar agregar el comentario '{Comment}' del usuario {User}", comment, sessionUser);
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Fin Comment.AddComment. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }
    }
}