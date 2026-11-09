$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$editorData = 'C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Data'
$outputRoot = Join-Path $env:TEMP 'LandmassE21-CompileCheck'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$references = @(Get-ChildItem "$editorData\NetStandard\ref\2.1.0" -Filter '*.dll') +
    @(Get-ChildItem "$editorData\Managed\UnityEngine" -Filter '*.dll' | Where-Object { $_.Name -ne 'UnityEditor.dll' })
$compilerOptions = @('-nologo', '-target:library', '-nostdlib+')
foreach ($reference in $references) { $compilerOptions += '-reference:' + $reference.FullName }
$sourceFiles = @(Get-ChildItem "$projectRoot\Assets" -Recurse -Filter '*.cs')
$runtimeFiles = @($sourceFiles | Where-Object { $_.FullName -notmatch '[\\/]Editor[\\/]' } | Select-Object -ExpandProperty FullName)
$runtimeAssembly = Join-Path $outputRoot 'Landmass.Runtime.dll'
& dotnet "$editorData\DotNetSdkRoslyn\csc.dll" @compilerOptions "-out:$runtimeAssembly" @runtimeFiles
if ($LASTEXITCODE -ne 0) { throw 'Player compilation failed.' }
# Editor build also checks the UNITY_EDITOR portions of the runtime scripts.
$editorOptions = $compilerOptions + @('-define:UNITY_EDITOR,UNITY_2022_3_OR_NEWER', "-out:$outputRoot\Landmass.EditorCheck.dll")
& dotnet "$editorData\DotNetSdkRoslyn\csc.dll" @editorOptions @($sourceFiles.FullName)
if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed.' }
Write-Output 'PASS: Player and Editor C# compilation against Unity 2022.3.51f1 references.'
Write-Output 'This does not validate Unity asset import, shaders, scene references or Play Mode.'
