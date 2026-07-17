using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý việc mapping tự động giữa ID từ Server và folder chứa Skin Spine.
/// Hệ thống sẽ tự động tìm skeleton dựa trên ID folder mà không cần khai báo cứng.
/// </summary>
public static class SpineSkinManager
{
    public class SpineSkinData
    {
        public string skeletonName;
        public string skinName;

        public SpineSkinData(string skeleton, string skin)
        {
            this.skeletonName = skeleton;
            this.skinName = skin;
        }
    }

    /// <summary>
    /// Lấy thông tin Skin từ ID (ID này tương ứng với tên folder trong Resources/Spine/Skins)
    /// </summary>
    public static SpineSkinData GetSkinData(int folderId)
    {
        // Chuyển ID thành chuỗi để làm tên skeleton/folder
        string idStr = folderId.ToString();
        
        // Mặc định sử dụng skin "default". 
        // Nếu sau này server gửi thêm skinName, có thể bổ sung tham số vào hàm này.
        return new SpineSkinData(idStr, "default");
    }

    /// <summary>
    /// Tìm đường dẫn Resource dựa trên tên skeleton (folder ID)
    /// </summary>
    public static string GetResourcePath(string skeletonName)
    {
        if (skeletonName.StartsWith("ship_"))
        {
            string pathShip = $"Spine/Ships/{skeletonName}/{skeletonName}_SkeletonData";
            if (Resources.Load(pathShip) != null)
            {
                return pathShip;
            }
            string pathShipFallback = $"Spine/Ships/{skeletonName}/{skeletonName}_41_SkeletonData";
            if (Resources.Load(pathShipFallback) != null)
            {
                return pathShipFallback;
            }
        }
        if (skeletonName.StartsWith("character_"))
        {
            string pathChar = $"Spine/CharactersOnePiece/{skeletonName}/{skeletonName}_SkeletonData";
            if (Resources.Load(pathChar) != null)
            {
                return pathChar;
            }
            string pathCharFallback = $"Spine/CharactersOnePiece/{skeletonName}/{skeletonName}_41_SkeletonData";
            if (Resources.Load(pathCharFallback) != null)
            {
                return pathCharFallback;
            }
        }
        // 1. Kiểm tra trong folder Skins (Mapping tự động theo ID)
        // Cấu trúc: Resources/Spine/Skins/{ID}/{ID}_SkeletonData hoặc Resources/Spine/Skins/{ID}/skin_SkeletonData
        string pathSkins = $"Spine/Skins/{skeletonName}/{skeletonName}_SkeletonData";
        if (Resources.Load(pathSkins) != null)
        {
            return pathSkins;
        }

        string pathSkinsGeneric = $"Spine/Skins/{skeletonName}/skin_SkeletonData";
        if (Resources.Load(pathSkinsGeneric) != null)
        {
            return pathSkinsGeneric;
        }

        // 2. Kiểm tra trong folder SpineAssets/Player (Cấu trúc cũ)
        // Cấu trúc: Resources/Spine/SpineAssets/Player/{Name}_SkeletonData
        string pathOld = $"Spine/SpineAssets/Player/{skeletonName}_SkeletonData";
        if (Resources.Load(pathOld) != null)
        {
            return pathOld;
        }

        // 3. Dự phòng: Thử tìm trực tiếp trong folder Skins nếu cấu trúc khác
        string pathSkinsDirect = $"Spine/Skins/{skeletonName}_SkeletonData";
        if (Resources.Load(pathSkinsDirect) != null)
        {
            return pathSkinsDirect;
        }

        return pathOld; // Fallback về path cũ
    }
}
