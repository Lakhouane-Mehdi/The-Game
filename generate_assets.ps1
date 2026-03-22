$contentDir = "c:\Users\roto2\The Game\Content"
$assets = @()

Get-ChildItem -Path $contentDir -Recurse -File | Where-Object { $_.Extension -match "\.(png|wav|ogg|json)$" -and $_.Name -ne "assets.json" } | ForEach-Object {
    $rel = $_.FullName.Substring($contentDir.Length + 1).Replace("\", "/")
    
    $type = "image"
    if ($_.Extension -eq ".wav" -or $_.Extension -eq ".ogg") { $type = "audio" }
    elseif ($_.Extension -eq ".json") { $type = "json" }
    
    if ($rel -match "(?i)tileset") { $type = "tileset" }
    elseif ($rel -match "(?i)(Monsters|Player|Weapon|woodcutter)") { $type = "spritesheet" }

    $key = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    if ($key -match "^[0-9]+$" -or $key -match "^(idle|walk|attack|death|jump)$") {
        $parent = [System.IO.Path]::GetFileName($_.DirectoryName)
        $key = "${parent}_${key}"
    }

    $assets += @{
        key = $key
        type = $type
        url = $rel
    }
}

$outputData = @{ assets = $assets }
$outputData | ConvertTo-Json -Depth 10 | Out-File "$contentDir\assets.json" -Encoding utf8
Write-Host "Generated assets.json at $contentDir\assets.json"
