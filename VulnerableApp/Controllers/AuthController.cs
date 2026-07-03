using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnerableApp.Data;
using System.Diagnostics; // Necesario para el cronómetro (Stopwatch)

namespace VulnerableApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuthController> _logger; // Inyección de ILogger

        public AuthController(AppDbContext db, ILogger<AuthController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var sw = Stopwatch.StartNew(); // Inicia el cronómetro
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            _logger.LogInformation("Inicio Auth.Login (GET). IP: {IP}", ip);

            try
            {
                return View();
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Fin Auth.Login (GET). Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            
            // Registramos los parámetros permitidos (usuario) pero NUNCA la contraseña
            _logger.LogInformation("Inicio Auth.Login (POST). IP: {IP}, Usuario intentando ingresar: {Username}", ip, username);

            try
            {
                var user = _db.Users.FirstOrDefault(u => u.Username == username);

                if (user == null || !user.PasswordHash.StartsWith("$") || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    // Generar Warning en intento fallido
                    _logger.LogWarning("Evento de Autenticación Fallido: Credenciales inválidas para {Username} desde la IP {IP}", username, ip);
                    ViewBag.Error = "Credenciales inválidas";
                    return View();
                }

                // Generar Information en evento exitoso
                _logger.LogInformation("Evento de Autenticación Exitoso: El usuario {Username} inició sesión correctamente desde la IP {IP}", username, ip);
                
                HttpContext.Session.SetString("User", user.Username);
                HttpContext.Session.SetInt32("UserId", user.Id);
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                // Generar Error si algo explota
                _logger.LogError(ex, "Error crítico durante Auth.Login (POST) para el usuario {Username}", username);
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Fin Auth.Login (POST). Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }

        public IActionResult Dashboard()
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";
            
            _logger.LogInformation("Inicio Auth.Dashboard. IP: {IP}, Usuario de sesión: {User}", ip, sessionUser);

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Intento de acceso no autorizado al Dashboard desde la IP {IP}", ip);
                    return RedirectToAction("Login");
                }

                var user = _db.Users.Find(userId.Value);
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el Dashboard para el usuario {User}", sessionUser);
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Fin Auth.Dashboard. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }

        public IActionResult Logout()
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";
            
            _logger.LogInformation("Inicio Auth.Logout. IP: {IP}, Usuario: {User}", ip, sessionUser);

            try
            {
                HttpContext.Session.Clear();
                _logger.LogInformation("Evento de Autenticación: El usuario {User} cerró sesión desde la IP {IP}", sessionUser, ip);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar cerrar sesión para el usuario {User}", sessionUser);
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("Fin Auth.Logout. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
        }
    }
}