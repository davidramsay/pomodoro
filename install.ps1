# Builds Pomodoro from source with Windows' built-in C# compiler, installs it to
# C:\Program Files\Pomodoro and adds a Start Menu shortcut. Re-run to update.
#
#   irm https://raw.githubusercontent.com/davidramsay/pomodoro/main/install.ps1 | iex
#
# or from a clone:  powershell -ExecutionPolicy Bypass -File install.ps1
$ErrorActionPreference = 'Stop'
$Raw = 'https://raw.githubusercontent.com/davidramsay/pomodoro/main'

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $admin) {
    # Program Files needs admin: relaunch this same install elevated.
    $run = if ($PSCommandPath) { "& '$PSCommandPath'" } else { "irm $Raw/install.ps1 | iex" }
    Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command',
        "try { $run } catch { Write-Host `$_ -ForegroundColor Red }; Read-Host 'Press Enter to close'"
}
else {
    $src = Join-Path $env:TEMP 'Pomodoro.cs'
    $local = if ($PSScriptRoot) { Join-Path $PSScriptRoot 'Pomodoro.cs' }
    if ($local -and (Test-Path $local)) { Copy-Item $local $src -Force }
    else { Invoke-WebRequest "$Raw/Pomodoro.cs" -OutFile $src -UseBasicParsing }

    $dir = Join-Path $env:ProgramFiles 'Pomodoro'
    New-Item -ItemType Directory -Force $dir | Out-Null
    Get-Process Pomodoro -ErrorAction SilentlyContinue | Stop-Process -Force  # free the exe for an update

    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    & $csc /nologo /target:winexe /optimize "/out:$dir\Pomodoro.exe" $src
    if ($LASTEXITCODE) { throw 'Compile failed.' }
    Remove-Item $src

    $lnk = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Pomodoro.lnk'
    $s = (New-Object -ComObject WScript.Shell).CreateShortcut($lnk)
    $s.TargetPath = "$dir\Pomodoro.exe"
    $s.Save()

    Write-Host "Installed to $dir. Open 'Pomodoro' from the Start Menu." -ForegroundColor Green
}
