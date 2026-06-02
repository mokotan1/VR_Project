# Forwards to the central Codex project harness, defaulting this repo to VR_Project.
param(
    [Parameter(Position = 0)]
    [string]$Command = "help",
    [Parameter(Position = 1)]
    [string]$Project = ""
)

$CentralHarnessRoot = "C:\Users\user\Documents\Codex\project-harness"
$CentralHarnessScript = Join-Path $CentralHarnessRoot "harness.ps1"
$DefaultProject = "VR_Project"
$CommandsWithoutProject = @("help", "list")

if (-not (Test-Path -LiteralPath $CentralHarnessScript)) {
    throw "Central harness not found: $CentralHarnessScript"
}

if ($CommandsWithoutProject -contains $Command) {
    & $CentralHarnessScript -Command $Command
    exit $LASTEXITCODE
}

if (-not $Project) {
    $Project = $DefaultProject
}

& $CentralHarnessScript -Command $Command -Project $Project
exit $LASTEXITCODE
