$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..\\..")).Path
$src = Join-Path $root "goose-cursor-agent-windows\\wrapper\\cursor-agent-wrapper.cs"

$csc = Join-Path $env:WINDIR "Microsoft.NET\\Framework64\\v4.0.30319\\csc.exe"
if (!(Test-Path $csc)) {
  throw "未找到 csc.exe：$csc"
}

$buildExe = Join-Path $root "goose-cursor-agent-windows\\wrapper\\cursor-agent.exe"

Write-Host "Building: $buildExe"
& $csc /nologo /target:exe /out:$buildExe $src

if (!(Test-Path $buildExe)) {
  throw "编译失败：未生成 $buildExe"
}

$windowsApps = Join-Path $env:LOCALAPPDATA "Microsoft\\WindowsApps"
if (!(Test-Path $windowsApps)) {
  throw "未找到 WindowsApps 目录：$windowsApps"
}

$dst = Join-Path $windowsApps "cursor-agent.exe"

Write-Host "Installing to: $dst"

# 如果被占用，建议先退出 Goose；这里做一次尽力的释放和重试
Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -in @("cursor-agent","goose") } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

for ($i = 0; $i -lt 10; $i++) {
  try {
    Copy-Item -Force $buildExe $dst
    break
  } catch {
    Start-Sleep -Milliseconds 300
    if ($i -eq 9) { throw }
  }
}

Write-Host "Done. Test:"
Write-Host "  cursor-agent --version"

