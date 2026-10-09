#requires -Version 7
param([string]$UnityEditor='E:/Unity/Editors/6000.0.65f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskTarget=Join-Path $taskRoot 'Assets/_Project/Tests/PlayMode/BlenderFurnaceCapture.cs'
$taskResults=Join-Path $taskRoot 'TestResults/blender-furnace.xml'
if((Get-Process Unity -ErrorAction SilentlyContinue) -or (Test-Path "$taskRoot/Temp/UnityLockfile")){throw 'Unity is in use'}
if((Test-Path $taskTarget) -or (Test-Path "$taskTarget.meta")){throw 'Temporary capture already exists'}
if(!(Test-Path "$taskRoot/ArtDirection/2026-10-09/blender-furnace-01/furnace-blender.png")){throw 'Run BuildBlenderFurnace.py with Blender first'}
Add-Type -AssemblyName System.Drawing
$taskProof=Join-Path $taskRoot 'ArtDirection/2026-10-09/blender-furnace-01'
$taskBitmap=[System.Drawing.Bitmap]::new("$taskProof/furnace-blender.png")
try {
 $taskData=$taskBitmap.LockBits([System.Drawing.Rectangle]::new(0,0,1200,1200),[System.Drawing.Imaging.ImageLockMode]::ReadOnly,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 try {
  $taskBytes=[byte[]]::new(1200*1200*4)
  for($row=0;$row -lt 1200;$row++){
   [Runtime.InteropServices.Marshal]::Copy([IntPtr]::Add($taskData.Scan0,$row*$taskData.Stride),$taskBytes,(1199-$row)*4800,4800)
  }
  [IO.File]::WriteAllBytes("$taskRoot/TestResults/furnace-study.bgra",$taskBytes)
 } finally {$taskBitmap.UnlockBits($taskData)}
} finally {$taskBitmap.Dispose()}
try {
 Copy-Item -LiteralPath "$PSScriptRoot/BlenderFurnaceCapture.cs" -Destination $taskTarget
 Set-Content -LiteralPath "$taskTarget.meta" -Value "fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))"
 if(Test-Path $taskResults){Remove-Item -LiteralPath $taskResults}
 $taskProcess=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-projectPath',"`"$taskRoot`"",'-runTests','-testPlatform','PlayMode','-testFilter','BlenderFurnaceCapture','-testResults',"`"$taskResults`"",'-logFile',"`"$taskRoot/TestResults/blender-furnace-unity.log`"") -PassThru -WindowStyle Hidden
 $taskProcess.WaitForExit()
 if($taskProcess.ExitCode -ne 0 -or !(Test-Path $taskResults)){throw 'Capture failed; see blender-furnace-unity.log'}
 $taskRun=([xml](Get-Content -LiteralPath $taskResults)).'test-run'
 if($taskRun.result -ne 'Passed'){throw "Capture failed: $($taskRun.result)"}
 foreach($taskFile in Get-ChildItem -LiteralPath $taskProof -Filter '*.bmp'){
  $taskImage=[System.Drawing.Bitmap]::new($taskFile.FullName)
  try{$taskImage.Save([IO.Path]::ChangeExtension($taskFile.FullName,'png'),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$taskImage.Dispose()}
  Remove-Item -LiteralPath $taskFile.FullName
 }
 Write-Output "Blender/production actual Unity comparison: $($taskRun.passed) passed"
} finally {
 if(Test-Path $taskTarget){Remove-Item -LiteralPath $taskTarget}
 if(Test-Path "$taskTarget.meta"){Remove-Item -LiteralPath "$taskTarget.meta"}
}
