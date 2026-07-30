$basePath = "Assets/Resources/Spine/CharactersOnePiece"
if (Test-Path $basePath) {
    Get-ChildItem -Path $basePath -Directory | ForEach-Object {
        $dir = $_.FullName
        $name = $_.Name
        
        $json41File = Join-Path $dir "${name}_41.json"
        $skelFile = Join-Path $dir "$name.skel.bytes"
        $skelMeta = Join-Path $dir "$name.skel.meta"
        $targetJsonFile = Join-Path $dir "$name.json"
        
        # Nếu tồn tại file JSON bản 4.1 (*_41.json)
        if (Test-Path $json41File) {
            # 1. Xóa file skel.bytes phiên bản 3.8 cũ và file meta tương ứng
            if (Test-Path $skelFile) {
                Remove-Item -Path $skelFile -Force
                Write-Host "Removed 3.8 skel in $name"
            }
            if (Test-Path $skelMeta) {
                Remove-Item -Path $skelMeta -Force
            }
            
            # 2. Dọn dẹp các asset cũ có thể bị lỗi do import nhầm bản 3.8 trước đó
            Get-ChildItem -Path $dir -Include "*.asset", "*.asset.meta", "*.mat", "*.mat.meta" -Recurse | ForEach-Object {
                Remove-Item -Path $_.FullName -Force
            }
            
            # 3. Đổi tên *_41.json thành *.json để Unity tự động sinh ra *_SkeletonData.asset bản 4.1
            if (Test-Path $targetJsonFile) {
                Remove-Item -Path $targetJsonFile -Force
            }
            Rename-Item -Path $json41File -NewName "$name.json" -Force
            Write-Host "Renamed ${name}_41.json to $name.json"
        }
    }
    Write-Host "Done processing all characters!"
} else {
    Write-Warning "Directory $basePath not found!"
}

