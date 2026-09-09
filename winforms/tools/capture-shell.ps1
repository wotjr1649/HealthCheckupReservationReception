# WF-00 실행 화면을 떠서 PNG 로 남긴다.
#
# 왜 필요한가: VS 디자인 표면은 실행 화면이 아니다. references/designer.md 가 명시한다 —
# Program.cs 가 디자인타임에 돌지 않으므로 굴림 9pt 도, Presenter 가 채우는 업무 Tab·상태바도
# 디자이너에는 나타나지 않는다. 배치를 눈으로 판정하려면 실행본을 봐야 한다.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File winforms/tools/capture-shell.ps1
#
# 결과: D:\tmp\wf00_run.png

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinShot {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$root = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$exe = Join-Path $root 'src\HealthCheckupReservationReception.WinForms\bin\Debug\HealthCheckupReservationReception.WinForms.exe'
if (-not (Test-Path $exe)) { throw "빌드본이 없다: $exe" }

$out = 'D:\tmp'
if (-not (Test-Path $out)) { New-Item -ItemType Directory -Path $out | Out-Null }
$png = Join-Path $out 'wf00_run.png'

$p = Start-Process -FilePath $exe -PassThru
try {
  Start-Sleep -Seconds 6
  $p.Refresh()
  if ($p.MainWindowHandle -eq [IntPtr]::Zero) { throw '창이 뜨지 않았다 — 시작 실패이거나 모달이 떠 있다' }

  [void][WinShot]::SetForegroundWindow($p.MainWindowHandle)
  Start-Sleep -Seconds 1

  $r = New-Object WinShot+RECT
  [void][WinShot]::GetWindowRect($p.MainWindowHandle, [ref]$r)
  $w = $r.Right - $r.Left
  $h = $r.Bottom - $r.Top
  Write-Output ("창 {0}x{1}  제목='{2}'" -f $w, $h, $p.MainWindowTitle)

  $bmp = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
  $bmp.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose()
  $bmp.Dispose()
  Write-Output "저장: $png"
}
finally {
  if (-not $p.HasExited) { $p.Kill() }
}
