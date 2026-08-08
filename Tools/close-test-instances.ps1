# close-test-instances.ps1
#
# Closes leftover Unity Editor/Player test-instance windows before starting a fresh
# multiplayer test round (Host/Player2/Player3, etc.) - run this any time "Could not
# start hosting" / "Only one usage of each socket address" shows up, or just as a
# habit before starting a new round of local multiplayer testing.
#
# Only targets windows whose title contains "Player" (matches "Player 2 (Client And
# Server)", "Player 3 (Client And Server)", etc.) - the main Editor window's title is
# the project/scene name and never matches this, and Unity Hub isn't a Unity.exe
# process at all, so neither gets touched. Adjust the -match pattern below if your
# window-naming convention changes.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File Tools\close-test-instances.ps1

$targets = Get-Process -Name "Unity" -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowTitle -match "Player" }

if (-not $targets) {
    Write-Output "No leftover Player test instances found."
    exit 0
}

foreach ($proc in $targets) {
    Write-Output "Closing PID $($proc.Id): '$($proc.MainWindowTitle)'"
    Stop-Process -Id $proc.Id -Force
}

Write-Output "Done. $($targets.Count) leftover instance(s) closed."
