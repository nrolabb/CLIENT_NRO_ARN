$source = 'Game1'
$targets = @('Game2', 'Game3')

Set-Location -Path "d:\NRO\NROKHACH\CLIENT_NRO_ARN\Assets\Scripts"

foreach ($target in $targets) {
    Write-Host "Processing $target..."
    if (Test-Path $target) {
        Remove-Item -Recurse -Force $target
    }
    
    Copy-Item -Recurse $source $target
    
    $files = Get-ChildItem -Path $target -Filter *.cs -Recurse
    foreach ($file in $files) {
        $content = Get-Content $file.FullName -Raw
        $content = $content -replace '\bGame1\b', $target
        Set-Content -Path $file.FullName -Value $content -NoNewline
    }
    Write-Host "$target created and namespace updated."
}
