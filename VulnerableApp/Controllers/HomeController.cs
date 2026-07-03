using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using VulnerableApp.Models;

namespace VulnerableApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger; // Inyección de ILogger

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var sw = Stopwatch.StartNew(); // Inicia cronómetro
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
        var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";

        _logger.LogInformation("Inicio Home.Index. IP: {IP}, Usuario: {User}", ip, sessionUser);

        try
        {
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Home.Index para el usuario {User}", sessionUser);
            throw;
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("Fin Home.Index. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }
    }

    public IActionResult Privacy()
    {
        var sw = Stopwatch.StartNew();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
        var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";

        _logger.LogInformation("Inicio Home.Privacy. IP: {IP}, Usuario: {User}", ip, sessionUser);

        try
        {
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Home.Privacy para el usuario {User}", sessionUser);
            throw;
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("Fin Home.Privacy. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var sw = Stopwatch.StartNew();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
        var sessionUser = HttpContext.Session.GetString("User") ?? "Anónimo";

        _logger.LogInformation("Inicio Home.Error. IP: {IP}, Usuario: {User}", ip, sessionUser);

        try
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico al cargar la vista de error para el usuario {User}", sessionUser);
            throw;
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("Fin Home.Error. Tiempo de ejecución: {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }
    }
}