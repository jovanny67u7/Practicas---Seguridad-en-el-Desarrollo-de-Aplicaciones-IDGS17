using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using VulnerableApp.Data;
using VulnerableApp.Models;
using System.Diagnostics; // Necesario para el cronómetro

namespace VulnerableApp.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<SearchController> _logger;

        public SearchController(AppDbContext db, ILogger<SearchController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index(string search)
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";

            // Registro de entrada, IP, Usuario y Parámetro
            _logger.LogInformation("Inicio Search.Index. IP: {IP}, Usuario: {User}, Parámetro: {Search}", ip, sessionUser, search);

            try
            {
                if (string.IsNullOrEmpty(search))
                {
                    _logger.LogWarning("Búsqueda vacía realizada por el usuario {User} desde la IP {IP}", sessionUser, ip);
                    return View(new List<User>());
                }

                var users = _db.Users.Where(u => u.Username.Contains(search)).ToList();
                _logger.LogInformation("Búsqueda exitosa. Parámetro: {Search}, Resultados encontrados: {Count}", search, users.Count);
                
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la búsqueda con el parámetro '{Search}' para el usuario {User}", search, sessionUser);
                throw;
            }
            finally
            {
                sw.Stop();
                // Registro de salida y tiempo
                _logger.LogInformation("Fin Search.Index. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }
    }
}