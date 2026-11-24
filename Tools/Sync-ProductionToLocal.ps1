<#
.SYNOPSIS
    Copies the production database (NAS) to the local test database (KAN-108).

.DESCRIPTION
    Reads ONLY from production and writes ONLY to localhost.
    1. Checks both connections.
    2. Asks for confirmation (unless -Force).
    3. Backs up the local database.
    4. Dumps production (structure, data, views, procedures, functions, triggers).
    5. Replaces DEFINER=`root`@`%` by CURRENT_USER with SQL SECURITY INVOKER, so views and
       procedures also work locally where that account does not exist.
    6. Replaces the local database with the dump. Restores the backup automatically if this fails.
    7. Shows a row count of the main tables.

    Passwords never live in this script:
      production: MODELBOUWER_PROD_DB_PASSWORD (else a prompt), user MODELBOUWER_PROD_DB_USER (default root)
      local:      MODELBOUWER_DB_PASSWORD (same variable as the app), user MODELBOUWER_DB_USER (default root)

.EXAMPLE
    .\Tools\Sync-ProductionToLocal.ps1
.EXAMPLE
    .\Tools\Sync-ProductionToLocal.ps1 -Force -KeepBackups 3
#>
[CmdletBinding()]
param(
    [string] $ProdServer,
    [int]    $ProdPort = 3306,
    [string] $Database = 'modelbuilder',
    [string] $BackupDir = (Join-Path $env:USERPROFILE 'Documents\ModelbouwWerkbank\db-backups'),
    [int]    $KeepBackups = 5,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$LocalServer = '127.0.0.1'
$LocalPort = 3306
$RepoRoot = Split-Path $PSScriptRoot -Parent

function Write-Step([string] $text) { Write-Host "`n== $text" -ForegroundColor Cyan }
function Fail([string] $text) { Write-Host "`nFOUT: $text" -ForegroundColor Red; exit 1 }

function Get-Variable2([string] $name) {
    foreach ($scope in 'Process', 'User', 'Machine') {
        $value = [Environment]::GetEnvironmentVariable($name, $scope)
        if (-not [string]::IsNullOrWhiteSpace($value)) { return $value }
    }
    return $null
}

function Find-MySqlTool([string] $exe) {
    $cmd = Get-Command $exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    # Prefer the tools of the running local MySQL service, so client and server versions match.
    $service = Get-CimInstance Win32_Service -Filter "Name LIKE 'MySQL%' AND State='Running'" -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($service -and $service.PathName -match '"?([^"]+\\bin)\\mysqld\.exe') {
        $candidate = Join-Path $Matches[1] $exe
        if (Test-Path $candidate) { return $candidate }
    }

    $found = Get-ChildItem 'C:\Program Files\MySQL' -Directory -ErrorAction SilentlyContinue |
        ForEach-Object { Join-Path $_.FullName "bin\$exe" } |
        Where-Object { Test-Path $_ } |
        Sort-Object { $v = ((Split-Path (Split-Path $_ -Parent) -Parent) -replace '^.*?(\d+(\.\d+)*)\D*$', '$1'); try { [version]"$v.0" } catch { [version]'0.0' } } -Descending |
        Select-Object -First 1
    if (-not $found) { Fail "$exe niet gevonden. Installeer MySQL Server of de MySQL client tools." }
    return $found
}

function Get-ProdServerFromConfig {
    # Same encoding as the app uses (IpHelper), so the address does not have to be typed in.
    $config = Join-Path $RepoRoot 'Modelbouwer\Resources\Config\modelbouwer.config.external'
    if (-not (Test-Path $config)) { return $null }
    $parts = (Get-Content $config -Raw).Trim().Split(',') | ForEach-Object { (([int]$_.Trim() / 2) - 1200) / 6 }
    return ($parts -join '.')
}

# Runs a MySQL client tool with the password in MYSQL_PWD, so it never appears on a command line.
function Invoke-MySql([string] $exe, [string[]] $arguments, [string] $password) {
    # Windows PowerShell 5.1 turns stderr of a native tool into a terminating error under 'Stop'.
    $ErrorActionPreference = 'Continue'
    $env:MYSQL_PWD = $password
    try {
        $output = & $exe @arguments 2>&1
        $code = $LASTEXITCODE
    }
    finally {
        Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
    }
    $errors = $output | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } |
        ForEach-Object { $_.ToString() } | Where-Object { $_ -notmatch '^(mysqldump|mysql)(\.exe)?: \[Warning\]|^Warning: ' }
    $text = $output | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }
    return [pscustomobject]@{ ExitCode = $code; Output = $text; Errors = ($errors -join "`n") }
}

function ConvertTo-PlainText([securestring] $secure) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

# ---------------------------------------------------------------- settings
$mysql = Find-MySqlTool 'mysql.exe'
$mysqldump = Find-MySqlTool 'mysqldump.exe'

if (-not $ProdServer) { $ProdServer = Get-Variable2 'MODELBOUWER_PROD_DB_SERVER' }
if (-not $ProdServer) { $ProdServer = Get-ProdServerFromConfig }
if (-not $ProdServer) { Fail 'Geen productieserver bekend. Geef -ProdServer op of zet MODELBOUWER_PROD_DB_SERVER.' }
if ($ProdServer -in @('localhost', '127.0.0.1', '::1')) { Fail 'De productieserver mag niet localhost zijn.' }

$prodUser = Get-Variable2 'MODELBOUWER_PROD_DB_USER'; if (-not $prodUser) { $prodUser = 'root' }
$localUser = Get-Variable2 'MODELBOUWER_DB_USER'; if (-not $localUser) { $localUser = 'root' }

$prodPassword = Get-Variable2 'MODELBOUWER_PROD_DB_PASSWORD'
if (-not $prodPassword) {
    $prodPassword = ConvertTo-PlainText (Read-Host "Wachtwoord productie ($prodUser@$ProdServer)" -AsSecureString)
}
$localPassword = Get-Variable2 'MODELBOUWER_DB_PASSWORD'
if (-not $localPassword) {
    $localPassword = ConvertTo-PlainText (Read-Host "Wachtwoord lokaal ($localUser@localhost)" -AsSecureString)
}

$prodConn = @("--user=$prodUser", "--host=$ProdServer", "--port=$ProdPort", '--protocol=TCP')
$prodProbeConn = $prodConn + @('--connect-timeout=10')   # mysql only; mysqldump does not know this option
$localConn = @("--user=$localUser", "--host=$LocalServer", "--port=$LocalPort", '--protocol=TCP')

# ---------------------------------------------------------------- 1. connections
Write-Step "Verbindingen controleren"
$probe = "SELECT VERSION(), (SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME='$Database')"
$prod = Invoke-MySql $mysql ($prodProbeConn + @('-N', '-B', "--execute=$probe")) $prodPassword
if ($prod.ExitCode -ne 0) { Fail "Productie ($ProdServer) niet bereikbaar: $($prod.Errors)`nTip: vanaf het Voortman-netwerk is de NAS niet bereikbaar." }
$prodInfo = "$($prod.Output)".Split("`t")
if ($prodInfo[1] -ne '1') { Fail "Database '$Database' bestaat niet op productie." }
Write-Host "Productie: $ProdServer, MySQL $($prodInfo[0])"

$local = Invoke-MySql $mysql ($localConn + @('-N', '-B', "--execute=$probe")) $localPassword
if ($local.ExitCode -ne 0) { Fail "Lokale database niet bereikbaar: $($local.Errors)" }
$localInfo = "$($local.Output)".Split("`t")
Write-Host "Lokaal:    localhost, MySQL $($localInfo[0])"

# The collation must exist locally before anything is dropped, otherwise CREATE (and the restore) would fail.
$Collation = 'utf8mb4_0900_ai_ci'
$check = Invoke-MySql $mysql ($localConn + @('-N', '-B', "--execute=SELECT COUNT(*) FROM information_schema.COLLATIONS WHERE COLLATION_NAME='$Collation'")) $localPassword
if ($check.ExitCode -ne 0 -or "$($check.Output)".Trim() -ne '1') {
    Fail "De lokale server kent collation $Collation niet. Er is niets gewijzigd."
}

# ---------------------------------------------------------------- 2. confirmation
if (-not $Force) {
    Write-Host "`nDe LOKALE database '$Database' wordt vervangen door de gegevens van productie." -ForegroundColor Yellow
    Write-Host "Productie wordt alleen gelezen. Vooraf wordt een back-up van de lokale database gemaakt."
    $answer = Read-Host 'Doorgaan? (j/n)'
    if ($answer -notin @('j', 'J', 'ja', 'Ja', 'y', 'Y')) { Write-Host 'Afgebroken, er is niets gewijzigd.'; exit 0 }
}

New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$localBackup = Join-Path $BackupDir "local-$Database-$stamp.sql"
$prodDump = Join-Path $BackupDir "prod-$Database-$stamp.sql"
$prodImport = Join-Path $BackupDir "prod-$Database-$stamp-import.sql"
$dumpOptions = @('--single-transaction', '--routines', '--triggers', '--events', '--hex-blob',
    '--set-gtid-purged=OFF', '--no-tablespaces', '--default-character-set=utf8mb4')

# ---------------------------------------------------------------- 3. local backup
if ($localInfo[1] -eq '1') {
    Write-Step "Back-up lokale database"
    $r = Invoke-MySql $mysqldump ($localConn + $dumpOptions + @("--result-file=$localBackup", $Database)) $localPassword
    if ($r.ExitCode -ne 0) { Fail "Back-up van de lokale database mislukt, er is niets gewijzigd: $($r.Errors)" }
    Write-Host ("Back-up: {0} ({1:N1} MB)" -f $localBackup, ((Get-Item $localBackup).Length / 1MB))
}
else {
    $localBackup = $null
    Write-Host "Lokale database '$Database' bestaat nog niet, geen back-up nodig."
}

# ---------------------------------------------------------------- 4. production dump
Write-Step "Productie uitlezen"
$r = Invoke-MySql $mysqldump ($prodConn + $dumpOptions + @("--result-file=$prodDump", $Database)) $prodPassword
if ($r.ExitCode -ne 0) { Fail "Uitlezen van productie mislukt, er is niets gewijzigd: $($r.Errors)" }
Write-Host ("Dump: {0:N1} MB" -f ((Get-Item $prodDump).Length / 1MB))

# ---------------------------------------------------------------- 5. definers
Write-Step "Dump geschikt maken voor lokaal (definers, MariaDB sql_mode)"
$utf8 = New-Object System.Text.UTF8Encoding $false
$reader = New-Object System.IO.StreamReader($prodDump, $utf8)
$writer = New-Object System.IO.StreamWriter($prodImport, $false, $utf8)
$changed = 0
try {
    while ($null -ne ($line = $reader.ReadLine())) {
        # Only object definitions and sql_mode settings are rewritten; data rows (INSERT) stay byte-identical.
        if (-not $line.StartsWith('INSERT INTO') -and ($line.Contains('DEFINER') -or $line.Contains('NO_AUTO_CREATE_USER'))) {
            $new = $line -replace 'DEFINER=`[^`]*`@`[^`]*`', 'DEFINER=CURRENT_USER' -replace 'SQL SECURITY DEFINER', 'SQL SECURITY INVOKER'
            # Production runs MariaDB 10.3; its routines carry sql_mode NO_AUTO_CREATE_USER, which MySQL 8+ rejects.
            $new = $new -replace ',NO_AUTO_CREATE_USER', '' -replace 'NO_AUTO_CREATE_USER,?', ''
            if ($new -ne $line) { $changed++ }
            $line = $new
        }
        $writer.WriteLine($line)
    }
}
finally {
    $reader.Close(); $writer.Close()
}
Remove-Item $prodDump
Write-Host "$changed regels aangepast"

# ---------------------------------------------------------------- 6. replace local database
function Import-IntoLocal([string] $file) {
    $recreate = "DROP DATABASE IF EXISTS ``$Database``; CREATE DATABASE ``$Database`` CHARACTER SET utf8mb4 COLLATE $Collation;"
    $r = Invoke-MySql $mysql ($localConn + @("--execute=$recreate")) $localPassword
    if ($r.ExitCode -ne 0) { return $r }
    return Invoke-MySqlFile $file
}

# Feeds a .sql file to mysql through standard input ("source" is not allowed in --execute).
function Invoke-MySqlFile([string] $file) {
    $errFile = "$file.err"
    $env:MYSQL_PWD = $localPassword
    try {
        # Start-Process joins arguments with spaces, so quote each one to keep it a single argument.
        $arguments = ($localConn + @('--default-character-set=utf8mb4', $Database)) |
            ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }
        $p = Start-Process -FilePath $mysql -NoNewWindow -Wait -PassThru `
            -ArgumentList $arguments `
            -RedirectStandardInput $file -RedirectStandardError $errFile
        $errors = (Get-Content $errFile -ErrorAction SilentlyContinue) |
            Where-Object { $_ -and $_ -notmatch '^(mysql)(\.exe)?: \[Warning\]|^Warning: ' }
        return [pscustomobject]@{ ExitCode = $p.ExitCode; Output = $null; Errors = ($errors -join "`n") }
    }
    finally {
        Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
        Remove-Item $errFile -ErrorAction SilentlyContinue
    }
}

function Import-IntoLocalSafe([string] $file) {
    # Any exception becomes a failed result, so the restore path always runs.
    try { return Import-IntoLocal $file }
    catch { return [pscustomobject]@{ ExitCode = 1; Output = $null; Errors = $_.Exception.Message } }
}

Write-Step "Lokale database vervangen"
$r = Import-IntoLocalSafe $prodImport
if ($r.ExitCode -ne 0 -or $r.Errors) {
    Write-Host "Import mislukt: $($r.Errors)" -ForegroundColor Red
    if ($localBackup) {
        Write-Step "Back-up terugzetten"
        $restore = Import-IntoLocalSafe $localBackup
        if ($restore.ExitCode -ne 0 -or $restore.Errors) {
            Fail "Terugzetten mislukt: $($restore.Errors)`nZet handmatig terug met: mysql -u $localUser -p $Database < `"$localBackup`""
        }
        Fail "De lokale database is teruggezet naar de toestand van voor de synchronisatie."
    }
    Fail "Er was geen lokale back-up; de lokale database is mogelijk onvolledig."
}
Remove-Item $prodImport

# ---------------------------------------------------------------- 7. check
Write-Step "Controle"
$count = @"
SELECT 'tabellen', COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA='$Database' AND TABLE_TYPE='BASE TABLE'
UNION ALL SELECT 'views', COUNT(*) FROM information_schema.VIEWS WHERE TABLE_SCHEMA='$Database'
UNION ALL SELECT 'procedures/functies', COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA='$Database'
UNION ALL SELECT 'projecten', COUNT(*) FROM ``$Database``.project
UNION ALL SELECT 'producten', COUNT(*) FROM ``$Database``.product
UNION ALL SELECT 'urenregels', COUNT(*) FROM ``$Database``.time
UNION ALL SELECT 'voorraadmutaties', COUNT(*) FROM ``$Database``.stocklog
UNION ALL SELECT 'inkooporders', COUNT(*) FROM ``$Database``.supplyorder;
"@
function Get-Counts($conn, [string] $password, [string] $name) {
    $r = Invoke-MySql $mysql ($conn + @('-N', '-B', "--execute=$count")) $password
    if ($r.ExitCode -ne 0) {
        Write-Host "Telling op $name mislukt: $($r.Errors)" -ForegroundColor Yellow
        return @{}
    }
    $rows = @{}
    foreach ($row in $r.Output) { $p = "$row".Split("`t"); $rows[$p[0]] = $p[1] }
    return $rows
}
$rows_productie = Get-Counts $prodConn $prodPassword 'productie'
$rows_lokaal = Get-Counts $localConn $localPassword 'lokaal'

$allEqual = $true
'{0,-22}{1,12}{2,12}' -f '', 'productie', 'lokaal' | Write-Host
foreach ($key in 'tabellen', 'views', 'procedures/functies', 'projecten', 'producten', 'urenregels', 'voorraadmutaties', 'inkooporders') {
    $p = $rows_productie[$key]; $l = $rows_lokaal[$key]
    # A missing count is never a success.
    if ($null -eq $p -or $null -eq $l -or $p -ne $l) { $allEqual = $false }
    if ($null -eq $p) { $p = '?' }
    if ($null -eq $l) { $l = '?' }
    '{0,-22}{1,12}{2,12}' -f $key, $p, $l | Write-Host -ForegroundColor ($(if ($p -eq $l) { 'Gray' } else { 'Yellow' }))
}

# ---------------------------------------------------------------- 8. keep the newest backups
Get-ChildItem $BackupDir -Filter "local-$Database-*.sql" | Sort-Object LastWriteTime -Descending |
    Select-Object -Skip $KeepBackups | Remove-Item

if ($allEqual) { Write-Host "`nKlaar. De lokale database is gelijk aan productie." -ForegroundColor Green }
else { Write-Host "`nKlaar, maar niet alle tellingen zijn gelijk. Controleer de gele regels." -ForegroundColor Yellow }
if ($localBackup) { Write-Host "Back-up van de vorige lokale database: $localBackup" }
