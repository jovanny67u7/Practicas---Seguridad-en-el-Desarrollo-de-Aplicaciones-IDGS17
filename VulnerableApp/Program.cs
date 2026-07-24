using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using VulnerableApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSession();


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) 
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341") 
    .Enrich.FromLogContext()
    .Enrich.WithMachineName() 
    .CreateLogger();

builder.Host.UseSerilog();


var app = builder.Build();

var _logger = app.Services.GetRequiredService<ILogger<Program>>();

app.Use(async (context, next) =>
{
    var cid = Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-ID"] = cid;

    using (LogContext.PushProperty("CorrelationId", cid))
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled");
            context.Response.StatusCode = 500;
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();