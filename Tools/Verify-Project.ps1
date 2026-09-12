param(
    # Built-in packages such as com.unity.ugui live in the Editor install until Unity copies them to Library/PackageCache.
    [string]$EditorPath = $(if ($env:UNITY_EDITOR_PATH) { $env:UNITY_EDITOR_PATH } else { 'E:/Unity/Editors/6000.0.65f1/Editor' })
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$assetsRoot = Join-Path $projectRoot 'Assets'
$guidPaths = @{}
$assetFiles = Get-ChildItem -LiteralPath $assetsRoot -Recurse -File

foreach ($assetFile in $assetFiles) {
    if ($assetFile.Extension -eq '.meta') {
        $metaText = Get-Content -Raw -LiteralPath $assetFile.FullName
        $guidMatch = [regex]::Match($metaText, '(?m)^guid: ([0-9a-f]{32})\r?$')
        if (!$guidMatch.Success) { throw "Invalid GUID in $($assetFile.FullName)" }
        $assetGuid = $guidMatch.Groups[1].Value
        if ($guidPaths.ContainsKey($assetGuid)) { throw "Duplicate asset GUID: $assetGuid" }
        $sourcePath = $assetFile.FullName.Substring(0, $assetFile.FullName.Length - 5)
        if (!(Test-Path -LiteralPath $sourcePath)) { throw "Orphan metadata: $($assetFile.FullName)" }
        $guidPaths[$assetGuid] = $sourcePath
    }
    elseif (!(Test-Path -LiteralPath ($assetFile.FullName + '.meta'))) {
        throw "Missing metadata: $($assetFile.FullName)"
    }
}

foreach ($folder in Get-ChildItem -LiteralPath $assetsRoot -Recurse -Directory) {
    if (!(Test-Path -LiteralPath ($folder.FullName + '.meta'))) {
        throw "Missing folder metadata: $($folder.FullName)"
    }
}

# GUIDs of scripts shipped in the manifest's packages (for example uGUI components referenced by the scene).
$manifest = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'Packages/manifest.json') | ConvertFrom-Json
$packageGuids = @{}
$packageRoots = @()
foreach ($packageName in $manifest.dependencies.PSObject.Properties.Name) {
    $cached = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Directory -Filter "$packageName@*" -ErrorAction SilentlyContinue
    if ($cached) { $packageRoots += $cached.FullName; continue }
    $builtIn = Join-Path $EditorPath "Data/Resources/PackageManager/BuiltInPackages/$packageName"
    if (Test-Path -LiteralPath $builtIn) { $packageRoots += $builtIn }
}
foreach ($packageRoot in $packageRoots) {
    foreach ($packageMeta in Get-ChildItem -LiteralPath $packageRoot -Recurse -File -Filter '*.meta') {
        $guidMatch = [regex]::Match((Get-Content -Raw -LiteralPath $packageMeta.FullName), '(?m)^guid: ([0-9a-f]{32})\r?$')
        if ($guidMatch.Success) { $packageGuids[$guidMatch.Groups[1].Value] = $true }
    }
}

# Unity's built-in default and extra resources (materials, legacy font).
$builtInResourceGuids = @('0000000000000000f000000000000000', '0000000000000000e000000000000000')

foreach ($serializedFile in $assetFiles | Where-Object Extension -In '.unity', '.asset', '.prefab') {
    $serializedText = Get-Content -Raw -LiteralPath $serializedFile.FullName
    foreach ($reference in [regex]::Matches($serializedText, 'guid: ([0-9a-f]{32})')) {
        $assetGuid = $reference.Groups[1].Value
        if ($builtInResourceGuids -contains $assetGuid -or $guidPaths.ContainsKey($assetGuid) -or $packageGuids.ContainsKey($assetGuid)) { continue }
        throw "Unresolved GUID $assetGuid in $($serializedFile.FullName). If it belongs to a package, pass -EditorPath or open the project in Unity once."
    }

    foreach ($component in [regex]::Split($serializedText, '(?m)^--- !u!')) {
        $scriptMatch = [regex]::Match($component, 'm_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}')
        # Field names are only checked for project scripts; package components are validated by Unity import.
        if (!$scriptMatch.Success -or !$guidPaths.ContainsKey($scriptMatch.Groups[1].Value)) { continue }
        $sourceText = Get-Content -Raw -LiteralPath $guidPaths[$scriptMatch.Groups[1].Value]
        foreach ($field in [regex]::Matches($component, '(?m)^  (_\w+):')) {
            if ($sourceText -notmatch ('\b' + [regex]::Escape($field.Groups[1].Value) + '\b')) {
                throw "Unknown serialized field $($field.Groups[1].Value) in $($serializedFile.Name)"
            }
        }
    }
}

$scenePath = Join-Path $assetsRoot '_Project/Scenes/Gameplay/Gameplay.unity'
$sceneText = Get-Content -Raw -LiteralPath $scenePath
$sceneIds = @{}
foreach ($sceneId in [regex]::Matches($sceneText, '(?m)^--- !u!\d+ &(\d+)')) {
    $idValue = $sceneId.Groups[1].Value
    if ($sceneIds.ContainsKey($idValue)) { throw "Duplicate scene fileID: $idValue" }
    $sceneIds[$idValue] = $true
}
foreach ($reference in [regex]::Matches($sceneText, '\{fileID: (\d+)\}')) {
    $idValue = $reference.Groups[1].Value
    if ($idValue -ne '0' -and !$sceneIds.ContainsKey($idValue)) { throw "Unresolved scene fileID: $idValue" }
}

foreach ($assemblyFile in $assetFiles | Where-Object Extension -EQ '.asmdef') {
    $null = Get-Content -Raw -LiteralPath $assemblyFile.FullName | ConvertFrom-Json
}
$buildSettings = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'ProjectSettings/EditorBuildSettings.asset')
$sceneGuid = [regex]::Match((Get-Content -Raw -LiteralPath ($scenePath + '.meta')), 'guid: ([0-9a-f]{32})').Groups[1].Value
if ($buildSettings -notmatch $sceneGuid) { throw 'Gameplay scene is missing from build settings.' }

Write-Output "PASS: $($guidPaths.Count) unique asset/folder GUIDs, $($packageGuids.Count) package GUIDs indexed, $($sceneIds.Count) scene objects/components, resolved references and valid JSON."
Write-Output 'This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor.'
