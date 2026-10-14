# Pomodoro

A tiny Windows interval timer that you can't ignore. Set work/break minutes (default 30/10), press Start.
When a period ends, a dark full-screen popup covers every monitor and an alarm repeats until you click to start the next period.

No dependencies: a ~10 KB exe built from one C# file with the compiler that ships with Windows.

## Install

In PowerShell:

```powershell
irm https://raw.githubusercontent.com/davidramsay/pomodoro/main/install.ps1 | iex
```

Approve the admin prompt. It installs to `C:\Program Files\Pomodoro` and adds **Pomodoro** to the Start Menu. Run it again to update.

## Uninstall

In an admin PowerShell:

```powershell
Remove-Item -Recurse "$env:ProgramFiles\Pomodoro", "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Pomodoro.lnk"
```
