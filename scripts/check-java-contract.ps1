[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$output = [IO.Path]::Combine($repoRoot, 'build', 'java-contract')
$source = [IO.Path]::Combine($repoRoot, 'examples', 'java', 'OverpunchContract.java')
$fixtures = [IO.Path]::Combine($repoRoot, 'examples', 'contracts', 'signed-overpunch.txt')

Get-Command java, javac -ErrorAction Stop | Out-Null
New-Item -ItemType Directory -Path $output -Force | Out-Null
java --version
if ($LASTEXITCODE -ne 0) { throw 'Java could not start. Install JDK 25 or use the documented container.' }
javac --release 25 -encoding UTF-8 -Xlint:all -Werror -d $output $source
if ($LASTEXITCODE -ne 0) { throw 'Java signed-overpunch example compilation failed.' }
java --class-path $output OverpunchContract $fixtures
if ($LASTEXITCODE -ne 0) { throw 'Java signed-overpunch contract failed.' }
