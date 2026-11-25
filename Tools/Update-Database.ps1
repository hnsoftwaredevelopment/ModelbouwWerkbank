<#
.SYNOPSIS
    Brings a ModelbouwWerkbank database up to date with the one-time scripts in Database\ (KAN-99).

.DESCRIPTION
    Runs these scripts, in this order, against one database:
      1. KAN-136-usage-to-stocklog.sql       usage on projects lowers the stock
      2. KAN-137-remove-orphan-receipts.sql  removes receipts of orders that no longer exist
      3. KAN-99-drop-unused-objects.sql      removes tables, views and procedures nothing uses
    Every script is safe to run again, so it does not matter which of them already ran on this database.

    Steps:
    1. Checks the connection.
    2. Asks for confirmation (unless -Force).
    3. Makes a full backup (structure, data, views, procedures, functions).
    4. Runs the scripts and stops at the first error, with the restore command.

    Default target is the local database (localhost), for example the database on a second laptop.
    Use -Server for another server (the NAS).

    Passwords never live in this script:
      localhost: MODELBOUWER_DB_PASSWORD, user MODELBOUWER_DB_USER (default root)
      other:     MODELBOUWER_PROD_DB_PASSWORD, user MODELBOUWER_PROD_DB_USER (default root)
      If the variable is not set, the script asks for the password.

.EXAMPLE
    .\Tools\Update-Database.ps1
.EXAMPLE
    .\Tools\Update-Database.ps1 -Server 87.210.244.108
#>
[CmdletBinding()]
param(
    [string] $Server = '127.0.0.1',
    [int]    $Port = 3306,
    [string] $Database = 'modelbuilder',
    [string] $BackupDir = (Join-Path $env:USERPROFILE 'Documents\ModelbouwWerkbank\db-backups'),
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path $PSScriptRoot -Parent
$Scripts = @(
    'KAN-136-usage-to-stocklog.sql',
    'KAN-137-remove-orphan-receipts.sql',
    'KAN-99-drop-unused-objects.sql'
)

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
        Select-Object -First 1
    if (-not $found) { Fail "$exe niet gevonden. Installeer MySQL Server of de MySQL client tools." }
    return $found
}

function ConvertTo-PlainText([securestring] $secure) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

# Runs a MySQL client tool with the password in MYSQL_PWD, so it never appears on a command line.
function Invoke-MySql([string] $exe, [string[]] $arguments) {
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

# Feeds a .sql file to mysql through standard input ("source" is not allowed in --execute).
function Invoke-MySqlFile([string] $file) {
    $outFile = [IO.Path]::GetTempFileName()
    $errFile = [IO.Path]::GetTempFileName()
    $env:MYSQL_PWD = $password
    try {
        # Start-Process joins arguments with spaces, so quote each one to keep it a single argument.
        $arguments = ($conn + @('--table', '--default-character-set=utf8mb4', $Database)) |
            ForEach-Object { '"' + ($_ -replace '"', '\"') + '"' }
        $p = Start-Process -FilePath $mysql -NoNewWindow -Wait -PassThru `
            -ArgumentList $arguments `
            -RedirectStandardInput $file -RedirectStandardOutput $outFile -RedirectStandardError $errFile
        $errors = (Get-Content $errFile -ErrorAction SilentlyContinue) |
            Where-Object { $_ -and $_ -notmatch '^(mysql)(\.exe)?: \[Warning\]|^Warning: ' }
        return [pscustomobject]@{ ExitCode = $p.ExitCode; Output = (Get-Content $outFile); Errors = ($errors -join "`n") }
    }
    finally {
        Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
        Remove-Item $outFile, $errFile -ErrorAction SilentlyContinue
    }
}

# ---------------------------------------------------------------- settings
$mysql = Find-MySqlTool 'mysql.exe'
$mysqldump = Find-MySqlTool 'mysqldump.exe'

$isLocal = $Server -in @('localhost', '127.0.0.1', '::1')
$prefix = if ($isLocal) { 'MODELBOUWER_DB' } else { 'MODELBOUWER_PROD_DB' }
$user = Get-Variable2 "${prefix}_USER"; if (-not $user) { $user = 'root' }
$password = Get-Variable2 "${prefix}_PASSWORD"
if (-not $password) {
    $password = ConvertTo-PlainText (Read-Host "Wachtwoord ($user@$Server)" -AsSecureString)
}
$conn = @("--user=$user", "--host=$Server", "--port=$Port", '--protocol=TCP')

foreach ($s in $Scripts) {
    if (-not (Test-Path (Join-Path $RepoRoot "Database\$s"))) { Fail "Script Database\$s niet gevonden. Haal eerst de laatste versie van master op." }
}

# ---------------------------------------------------------------- 1. connection
Write-Step "Verbinding controleren"
$probe = "SELECT VERSION(), (SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME='$Database')"
$r = Invoke-MySql $mysql ($conn + @('--connect-timeout=10', '-N', '-B', "--execute=$probe"))
if ($r.ExitCode -ne 0) { Fail "Database op $Server niet bereikbaar: $($r.Errors)" }
$info = "$($r.Output)".Split("`t")
if ($info[1] -ne '1') { Fail "Database '$Database' bestaat niet op $Server." }
Write-Host "Server: $Server, MySQL $($info[0]), database '$Database'"

# ---------------------------------------------------------------- 2. confirmation
if (-not $Force) {
    Write-Host "`nDeze scripts worden uitgevoerd op '$Database' op $Server :" -ForegroundColor Yellow
    $Scripts | ForEach-Object { Write-Host "  - $_" }
    Write-Host "Vooraf wordt een volledige back-up gemaakt."
    $answer = Read-Host 'Doorgaan? (j/n)'
    if ($answer -notin @('j', 'J', 'ja', 'Ja', 'y', 'Y')) { Write-Host 'Afgebroken, er is niets gewijzigd.'; exit 0 }
}

# ---------------------------------------------------------------- 3. backup
Write-Step "Back-up maken"
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
$serverName = $Server -replace '[^\w\.-]', '_'
$backup = Join-Path $BackupDir ("before-update-$serverName-$Database-{0}.sql" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
$dumpOptions = @('--single-transaction', '--routines', '--triggers', '--hex-blob', '--no-tablespaces', '--default-character-set=utf8mb4')
$r = Invoke-MySql $mysqldump ($conn + $dumpOptions + @("--result-file=$backup", $Database))
if ($r.ExitCode -ne 0 -or -not (Test-Path $backup) -or (Get-Item $backup).Length -lt 1000) {
    Fail "Back-up mislukt, er is niets gewijzigd: $($r.Errors)"
}
Write-Host ("Back-up: {0} ({1:N1} MB)" -f $backup, ((Get-Item $backup).Length / 1MB))

# ---------------------------------------------------------------- 4. scripts
foreach ($s in $Scripts) {
    Write-Step $s
    $r = Invoke-MySqlFile (Join-Path $RepoRoot "Database\$s")
    $r.Output | ForEach-Object { Write-Host $_ }
    if ($r.ExitCode -ne 0 -or $r.Errors) {
        Write-Host "`nFout in $s : $($r.Errors)" -ForegroundColor Red
        Write-Host "Terugzetten van de back-up (in een opdrachtprompt):"
        Write-Host "  mysql -u $user -p -h $Server $Database < `"$backup`""
        exit 1
    }
}

Write-Host "`nKlaar. De database '$Database' op $Server is bijgewerkt." -ForegroundColor Green
Write-Host "Back-up van daarvoor: $backup"
