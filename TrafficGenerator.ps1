<#
TrafficGenerator.ps1
Genera trafico de prueba masivo contra VulnerableApp (http://localhost:5282)
para poblar Seq / los logs de Serilog con evidencia de: navegacion normal,
busquedas (validas, vacias, caracteres especiales, SQL Injection),
autenticacion (exitosa, fallida, usuarios inexistentes), comentarios
(normales y XSS), consumo de la API y excepciones no controladas forzadas
para validar el Exception Middleware global.

Notas de preparacion realizadas antes de este script (una sola vez, en la
base de datos local VulnerableAppDb):
  - El usuario "user1" tiene un hash BCrypt valido para la contrasena
    "Test1234!" (los datos sembrados originalmente traian contrasenas en
    texto plano, que el AuthController rechaza porque no empiezan con "$").
  - El script crea y elimina un usuario "fantasma" (ghost_test) para forzar
    de forma realista una NullReferenceException en /Auth/Dashboard cuando
    la sesion apunta a un usuario que ya no existe en la base de datos.
#>

$ErrorActionPreference = 'Stop'

$baseUrl = "http://localhost:5282"
$connectionString = "Server=(localdb)\mssqllocaldb;Database=VulnerableAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
$ghostPasswordHash = '$2a$11$HNvRX/2fKZJOiNvS6p2bsOTczVr9zG926bvnn9VasNMbykCTcBMBW' # bcrypt de "Ghost1234!"

Add-Type -AssemblyName "System.Data"

$statusCounts = @{}

function Add-StatusCount {
    param([int]$Code)
    $key = "$Code"
    if ($statusCounts.ContainsKey($key)) {
        $statusCounts[$key] = $statusCounts[$key] + 1
    } else {
        $statusCounts[$key] = 1
    }
}

function Invoke-Traffic {
    param(
        [string]$Method = "GET",
        [string]$Url,
        [hashtable]$Body = $null,
        $Session = $null
    )
    try {
        $params = @{
            Uri            = $Url
            Method         = $Method
            UseBasicParsing = $true
            TimeoutSec     = 15
        }
        if ($Body) { $params.Body = $Body }
        if ($Session) { $params.WebSession = $Session }

        $resp = Invoke-WebRequest @params
        Add-StatusCount -Code ([int]$resp.StatusCode)
        return [int]$resp.StatusCode
    } catch {
        $code = -1
        if ($_.Exception.Response) {
            try { $code = [int]$_.Exception.Response.StatusCode } catch { $code = -1 }
        }
        Add-StatusCount -Code $code
        return $code
    }
}

function Invoke-SqlNonQuery {
    param([string]$Sql, [hashtable]$Params = @{})
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        foreach ($key in $Params.Keys) {
            $cmd.Parameters.AddWithValue($key, $Params[$key]) | Out-Null
        }
        return $cmd.ExecuteNonQuery()
    } finally {
        $conn.Close()
    }
}

function Invoke-SqlScalar {
    param([string]$Sql, [hashtable]$Params = @{})
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        foreach ($key in $Params.Keys) {
            $cmd.Parameters.AddWithValue($key, $Params[$key]) | Out-Null
        }
        return $cmd.ExecuteScalar()
    } finally {
        $conn.Close()
    }
}

Write-Host "=== TrafficGenerator.ps1 iniciado contra $baseUrl ===" -ForegroundColor Cyan

# ---------------------------------------------------------------------------
# 1. NAVEGACION
# ---------------------------------------------------------------------------
Write-Host "`n[1/7] Generando 30 peticiones GET a / (Home)..." -ForegroundColor Yellow
for ($i = 1; $i -le 30; $i++) {
    Invoke-Traffic -Method GET -Url "$baseUrl/" | Out-Null
}

Write-Host "Generando 20 peticiones aleatorias a otros controladores..." -ForegroundColor Yellow
$otherRoutes = @("/Home/Privacy", "/Auth/Login", "/Comment/Index", "/Search?search=")
for ($i = 1; $i -le 20; $i++) {
    $route = Get-Random -InputObject $otherRoutes
    Invoke-Traffic -Method GET -Url "$baseUrl$route" | Out-Null
}

# ---------------------------------------------------------------------------
# 2. BUSQUEDAS
# ---------------------------------------------------------------------------
$validTerms = @("admin","user1","user2","john","maria","carlos","test","report","2024","sales","support","david","laura","proyecto","cliente","factura","pedro","ana","luis","sofia")
$specialChars = @("@#$!","!@#$%^&*()","<<>>","%%%%","&&&&","???!!!")
$sqliPayloads = @("' OR 1=1 --","' OR '1'='1","'; DROP TABLE Users --","admin'--","' UNION SELECT NULL--","1' OR '1'='1' /*")

Write-Host "`n[2/7] Generando 100 busquedas con texto valido..." -ForegroundColor Yellow
for ($i = 1; $i -le 100; $i++) {
    $term = Get-Random -InputObject $validTerms
    $encoded = [uri]::EscapeDataString($term)
    Invoke-Traffic -Method GET -Url "$baseUrl/Search?search=$encoded" | Out-Null
}

Write-Host "Generando 20 busquedas con parametro vacio..." -ForegroundColor Yellow
for ($i = 1; $i -le 20; $i++) {
    Invoke-Traffic -Method GET -Url "$baseUrl/Search?search=" | Out-Null
}

Write-Host "Generando 20 busquedas con caracteres especiales..." -ForegroundColor Yellow
for ($i = 1; $i -le 20; $i++) {
    $payload = Get-Random -InputObject $specialChars
    $encoded = [uri]::EscapeDataString($payload)
    Invoke-Traffic -Method GET -Url "$baseUrl/Search?search=$encoded" | Out-Null
}

Write-Host "Generando 20 busquedas simulando SQL Injection..." -ForegroundColor Yellow
for ($i = 1; $i -le 20; $i++) {
    $payload = Get-Random -InputObject $sqliPayloads
    $encoded = [uri]::EscapeDataString($payload)
    Invoke-Traffic -Method GET -Url "$baseUrl/Search?search=$encoded" | Out-Null
}

# ---------------------------------------------------------------------------
# 3. AUTENTICACION
# ---------------------------------------------------------------------------
$mainSession = $null
Invoke-WebRequest -Uri "$baseUrl/" -SessionVariable mainSession -UseBasicParsing | Out-Null

Write-Host "`n[3/7] Generando 50 logins exitosos (user1 / Test1234!)..." -ForegroundColor Yellow
for ($i = 1; $i -le 50; $i++) {
    Invoke-Traffic -Method POST -Url "$baseUrl/Auth/Login" -Session $mainSession -Body @{ username = "user1"; password = "Test1234!" } | Out-Null
}

Write-Host "Generando 100 logins fallidos (usuarios validos, password incorrecto)..." -ForegroundColor Yellow
$validUsernames = @("admin","user1","user2")
$wrongPasswords = @("wrongpass","12345","letmein","password1","incorrecto","000000","qwerty","abc123")
for ($i = 1; $i -le 100; $i++) {
    $u = Get-Random -InputObject $validUsernames
    $p = Get-Random -InputObject $wrongPasswords
    Invoke-Traffic -Method POST -Url "$baseUrl/Auth/Login" -Session $mainSession -Body @{ username = $u; password = $p } | Out-Null
}

Write-Host "Generando 30 intentos de login con usuarios inexistentes..." -ForegroundColor Yellow
$fakeUsernames = @("hacker1","noexiste","testuser","ghost99","fakeuser","intruso","x_admin","desconocido")
for ($i = 1; $i -le 30; $i++) {
    $u = Get-Random -InputObject $fakeUsernames
    Invoke-Traffic -Method POST -Url "$baseUrl/Auth/Login" -Session $mainSession -Body @{ username = "$u$i"; password = "cualquiera" } | Out-Null
}

# Dejamos la sesion principal autenticada como user1 para las siguientes fases
Invoke-Traffic -Method POST -Url "$baseUrl/Auth/Login" -Session $mainSession -Body @{ username = "user1"; password = "Test1234!" } | Out-Null

# ---------------------------------------------------------------------------
# 4. COMENTARIOS
# ---------------------------------------------------------------------------
Write-Host "`n[4/7] Generando 100 comentarios de texto normal..." -ForegroundColor Yellow
$normalPhrases = @(
    "Excelente articulo, gracias por compartir",
    "Muy util esta informacion",
    "No estoy de acuerdo con este punto",
    "Podrian ampliar el tema en un proximo post",
    "Gran trabajo del equipo",
    "Tuve un problema similar la semana pasada",
    "Interesante perspectiva",
    "Gracias, me ayudo mucho"
)
for ($i = 1; $i -le 100; $i++) {
    $phrase = Get-Random -InputObject $normalPhrases
    Invoke-Traffic -Method POST -Url "$baseUrl/Comment/AddComment" -Session $mainSession -Body @{ comment = "$phrase #$i" } | Out-Null
}

Write-Host "Generando 30 comentarios simulando cargas XSS..." -ForegroundColor Yellow
$xssPayloads = @(
    "<script>alert('xss')</script>",
    "<img src=x onerror=alert(1)>",
    "<svg/onload=alert('xss')>",
    "`"><script>alert(1)</script>",
    "<iframe src=javascript:alert(1)>",
    "<body onload=alert('xss')>"
)
for ($i = 1; $i -le 30; $i++) {
    $payload = Get-Random -InputObject $xssPayloads
    Invoke-Traffic -Method POST -Url "$baseUrl/Comment/AddComment" -Session $mainSession -Body @{ comment = $payload } | Out-Null
}

# ---------------------------------------------------------------------------
# 5. API
# ---------------------------------------------------------------------------
Write-Host "`n[5/7] Generando 100 peticiones GET /api/users..." -ForegroundColor Yellow
for ($i = 1; $i -le 100; $i++) {
    if ($i % 2 -eq 0) {
        Invoke-Traffic -Method GET -Url "$baseUrl/api/users" -Session $mainSession | Out-Null
    } else {
        Invoke-Traffic -Method GET -Url "$baseUrl/api/users" | Out-Null
    }
}

Write-Host "Generando 90 peticiones GET /api/user/{id} (propio, ajeno, sin sesion)..." -ForegroundColor Yellow
$validIds = @(1,2,3)
for ($i = 1; $i -le 90; $i++) {
    $id = Get-Random -InputObject $validIds
    if ($i % 3 -eq 0) {
        Invoke-Traffic -Method GET -Url "$baseUrl/api/user/$id" | Out-Null            # sin sesion -> 401
    } else {
        Invoke-Traffic -Method GET -Url "$baseUrl/api/user/$id" -Session $mainSession | Out-Null  # con sesion -> 200 propio / 403 ajeno
    }
}

Write-Host "Generando 20 peticiones con identificadores invalidos e ids inexistentes..." -ForegroundColor Yellow
for ($i = 1; $i -le 10; $i++) {
    Invoke-Traffic -Method GET -Url "$baseUrl/api/user/99999" -Session $mainSession | Out-Null
}
for ($i = 1; $i -le 10; $i++) {
    Invoke-Traffic -Method GET -Url "$baseUrl/api/user/abc" -Session $mainSession | Out-Null
}

Write-Host "Generando 10 peticiones a recursos inexistentes (/api/rutafalsa)..." -ForegroundColor Yellow
for ($i = 1; $i -le 10; $i++) {
    Invoke-Traffic -Method GET -Url "$baseUrl/api/rutafalsa" | Out-Null
}

# ---------------------------------------------------------------------------
# 6. EXCEPCIONES NO CONTROLADAS
# ---------------------------------------------------------------------------
Write-Host "`n[6/7] Forzando excepciones no controladas..." -ForegroundColor Yellow

Write-Host "  Preparando usuario 'fantasma' para forzar NullReferenceException en /Auth/Dashboard..." -ForegroundColor DarkYellow
Invoke-SqlNonQuery -Sql "DELETE FROM Users WHERE Username = @u" -Params @{ "@u" = "ghost_test" } | Out-Null
Invoke-SqlNonQuery -Sql "INSERT INTO Users (Username, PasswordHash, Email, Balance, CreatedAt) VALUES (@u, @p, @e, @b, @c)" -Params @{
    "@u" = "ghost_test"
    "@p" = $ghostPasswordHash
    "@e" = "ghost_test@test.com"
    "@b" = 0
    "@c" = (Get-Date)
} | Out-Null

$ghostSession = $null
Invoke-WebRequest -Uri "$baseUrl/" -SessionVariable ghostSession -UseBasicParsing | Out-Null
Invoke-Traffic -Method POST -Url "$baseUrl/Auth/Login" -Session $ghostSession -Body @{ username = "ghost_test"; password = "Ghost1234!" } | Out-Null

# Se elimina el usuario ya autenticado: la sesion sigue apuntando a un Id que ya no existe en la BD
Invoke-SqlNonQuery -Sql "DELETE FROM Users WHERE Username = @u" -Params @{ "@u" = "ghost_test" } | Out-Null

Write-Host "  Generando 15 excepciones via /Auth/Dashboard (usuario de sesion ya no existe en la BD)..." -ForegroundColor DarkYellow
for ($i = 1; $i -le 15; $i++) {
    Invoke-Traffic -Method GET -Url "$baseUrl/Auth/Dashboard" -Session $ghostSession | Out-Null
}

Write-Host "  Generando 5 peticiones adicionales con tipos de datos incorrectos..." -ForegroundColor DarkYellow
Invoke-Traffic -Method GET -Url "$baseUrl/api/user/99999999999999999999" -Session $mainSession | Out-Null
Invoke-Traffic -Method GET -Url "$baseUrl/api/user/-1" -Session $mainSession | Out-Null
Invoke-Traffic -Method GET -Url "$baseUrl/api/user/%00" -Session $mainSession | Out-Null
Invoke-Traffic -Method GET -Url "$baseUrl/api/user/true" -Session $mainSession | Out-Null
Invoke-Traffic -Method GET -Url "$baseUrl/api/user/1.5" -Session $mainSession | Out-Null

# ---------------------------------------------------------------------------
# 7. RESUMEN
# ---------------------------------------------------------------------------
Write-Host "`n[7/7] Trafico generado. Resumen de codigos de estado HTTP recibidos:" -ForegroundColor Cyan
foreach ($key in ($statusCounts.Keys | Sort-Object)) {
    Write-Host ("  {0,5} -> {1} peticiones" -f $key, $statusCounts[$key])
}

Write-Host "`n=== TrafficGenerator.ps1 finalizado. Revisa Seq (http://localhost:8081) y Logs/log-*.txt ===" -ForegroundColor Cyan
