using System;
using System.Collections.Generic;
/// <summary>
/// Constants cho hệ thống Cloud Garden phía Client
/// </summary>
public class FarmConstants
{
    // ===================== CROP TEMPLATES =====================
    public class CropTemplateInfo
    {
        public int id;           // Dùng int để tránh signed/unsigned mismatch với Java byte (0-255)
        public string name;
        public short seedItemId;
        public short harvestItemId;
        public short imgYoung;
        public short imgMature;
        public short imgWithered;
    }

    // Key là int (0-255) để tránh Java byte (unsigned) vs C# sbyte (signed) mismatch
    // Khi cropType từ server là sbyte, dùng helper CropKey() để convert
    public static Dictionary<int, CropTemplateInfo> cropTemplates = new Dictionary<int, CropTemplateInfo>();

    /// <summary>
    /// Convert sbyte cropType từ server → int key an toàn (0–255)
    /// Java byte 128 → wire 0x80 → C# readByte() = sbyte -128 → CropKey = 128
    /// </summary>
    public static int CropKey(sbyte cropType)
    {
        return (int)(byte)cropType; // cast sbyte→byte→int: -128→128, -1→255, 0→0
    }

    public static void AddCropTemplate(CropTemplateInfo info)
    {
        cropTemplates[info.id] = info;
    }

    public static int GetCropTypeCount()
    {
        return cropTemplates.Count;
    }
    // ===================== GROWTH STAGES =====================
    public const sbyte STAGE_EMPTY = 0;      // Đất trống
    public const sbyte STAGE_SEED = 1;       // Hạt giống
    public const sbyte STAGE_SPROUT_1 = 2;   // Mầm 1
    public const sbyte STAGE_SPROUT_2 = 3;   // Mầm 2
    public const sbyte STAGE_YOUNG = 4;      // Cây non
    public const sbyte STAGE_MATURE = 5;     // Trưởng thành
    public const sbyte STAGE_WITHERED = 6;   // Héo
    public const int STAGE_COUNT = 6;

    // ===================== MAP IDs =====================
    // Sử dụng lại map nhà theo gender
    public const int MAP_CLOUD_GARDEN_TD = 39;  // Trái Đất (gender 0)
    public const int MAP_CLOUD_GARDEN_NM = 40;  // Namếc (gender 1)
    public const int MAP_CLOUD_GARDEN_XD = 41;  // Xayda (gender 2)

    // ===================== PLOT CONFIG =====================
    public const int INITIAL_PLOTS = 5; // Số ô mở khóa ban đầu
    public const int MAX_PLOTS = 10;

    // ===================== MESSAGE TYPES =====================
    public const sbyte MSG_FARM_ASSET = -58;   // Đã đổi sang -58 để tránh xung đột với Offline Map (-33) và Mabu Power (-115)
    public const sbyte MSG_FARM_DATA = -34;
    
    // Sub-types for MSG_FARM_ASSET (-115)
    public const sbyte SUBTYPE_FARM_ASSET = 10;
    public const sbyte SUBTYPE_CROP_ASSET = 11;
    public const sbyte SUBTYPE_FARM_ICON = 12;
    public const sbyte SUBTYPE_CROP_TEMPLATE = 13;
    
    // Sub-types for MSG_FARM_DATA (-34)
    public const sbyte SUBTYPE_PLOT_UPDATE = 10;
    public const sbyte DATA_UPDATE_SINGLE = 0;
    public const sbyte DATA_UPDATE_FULL = 1;
    public const sbyte DATA_OPEN_SEED_PANEL = 2;   // Server yêu cầu mở panel chọn hạt
    public const sbyte DATA_CLOSE_DIALOG = 3;      // Server yêu cầu đóng dialog
    public const sbyte DATA_HARVEST_SUCCESS = 4;   // Hiệu ứng thu hoạch
    public const sbyte DATA_FERTILIZE_SUCCESS = 5; // Bón phân thành công
    public const sbyte DATA_PESTICIDE_SUCCESS = 6; // Phun thuốc trừ sâu thành công
    public const sbyte DATA_OPEN_FERTILIZE_PANEL = 7; // Server yêu cầu mở panel bón phân

    // ===================== FERTILIZER / PESTICIDE ITEM IDs =====================
    // Phân bón: giảm thời gian chờ
    public const short ITEM_FERTILIZER_5M  = 2148; // Phân bón 5 phút
    public const short ITEM_FERTILIZER_10M = 2149; // Phân bón 10 phút
    public const short ITEM_FERTILIZER_20M = 2150; // Phân bón 20 phút
    public const short ITEM_FERTILIZER_30M = 2151; // Phân bón 30 phút
    // Thuốc trừ sâu: chữa cây héo
    public const short ITEM_PESTICIDE      = 2152; // Thuốc trừ sâu

    // Số giây giảm tương ứng mỗi loại phân
    public const int FERTILIZER_5M_SECS  = 5  * 60;
    public const int FERTILIZER_10M_SECS = 10 * 60;
    public const int FERTILIZER_20M_SECS = 20 * 60;
    public const int FERTILIZER_30M_SECS = 30 * 60;

    // Farm panel action IDs (dùng trong Panel.perform())
    public const int ACTION_FARM_PLANT     = 14001; // Gieo hạt
    public const int ACTION_FARM_FERTILIZE = 14002; // Bón phân
    public const int ACTION_FARM_PESTICIDE = 14003; // Phun thuốc trừ sâu

    // ===================== HELPER METHODS =====================

    /// <summary>
    /// Kiểm tra map có phải Cloud Garden không
    /// </summary>
    public static bool IsCloudGardenMap(int mapId)
    {
        return mapId >= MAP_CLOUD_GARDEN_TD && mapId <= MAP_CLOUD_GARDEN_XD;
    }

    /// <summary>
    /// Lấy tên giai đoạn
    /// </summary>
    public static string GetStageName(sbyte stage)
    {
        switch (stage)
        {
            case STAGE_EMPTY: return "Đất trống";
            case STAGE_SEED: return "Hạt giống";
            case STAGE_SPROUT_1: return "Mầm 1";
            case STAGE_SPROUT_2: return "Mầm 2";
            case STAGE_YOUNG: return "Cây non";
            case STAGE_MATURE: return "Thu hoạch";
            case STAGE_WITHERED: return "Héo";
            default: return "Không xác định";
        }
    }

    // ===================== ICONS =====================
    public const short ICON_PLOT_EMPTY = 21946;
    public const short ICON_PLOT_SELECTED = 21947;
    public const short ICON_KHUNG_RAUCU = 21945;
    public const short ICON_HARVEST = 21948;
    public const short ICON_QUESTION_1 = 21941;
    public const short ICON_QUESTION_2 = 21942;
    public const short ICON_LOCK_1 = 21943;
    public const short ICON_LOCK_2 = 21944;
    public const short ICON_SEED = 21911; // ID tam thoi (neu co)
    public const short ICON_SPROUT_1 = 21912; // ID tam thoi (neu co)
    public const short ICON_SPROUT_2 = 21913; // ID tam thoi (neu co)
    
    public static short GetCropIconId(sbyte cropType, sbyte stage)
    {
        switch (stage)
        {
            case STAGE_SEED: return ICON_SEED;
            case STAGE_SPROUT_1: return ICON_SPROUT_1;
            case STAGE_SPROUT_2: return ICON_SPROUT_2;
            case STAGE_YOUNG:
            case STAGE_MATURE:
            case STAGE_WITHERED:
                int key = CropKey(cropType);
                if (cropTemplates.ContainsKey(key))
                {
                    CropTemplateInfo info = cropTemplates[key];
                    if (stage == STAGE_YOUNG) return info.imgYoung;
                    if (stage == STAGE_MATURE) return info.imgMature;
                    if (stage == STAGE_WITHERED) return info.imgWithered;
                }
                break;
        }
        return -1;
    }

    // ===================== SEED/HARVEST IDs (Removed hardcoded constants) =====================

    public static short GetSeedItemId(sbyte cropType)
    {
        int key = CropKey(cropType);
        if (cropTemplates.ContainsKey(key))
        {
            return cropTemplates[key].seedItemId;
        }
        return -1;
    }

    public static bool IsSeedItem(short itemId)
    {
        // Fallback hardcode cho các hạt giống ban đầu
        if ((itemId >= 1832 && itemId <= 1839)
            || itemId == 1889
            || itemId == 1890
            || itemId == 1891
            || itemId == 2147)
        {
            return true;
        }

        foreach (var crop in cropTemplates.Values)
        {
            if (crop.seedItemId == itemId)
                return true;
        }
        return false;
    }

    public static short GetHarvestItemId(sbyte cropType)
    {
        int key = CropKey(cropType);
        if (cropTemplates.ContainsKey(key))
        {
            return cropTemplates[key].harvestItemId;
        }
        return -1;
    }

    public static string GetCropName(sbyte cropType)
    {
        int key = CropKey(cropType);
        if (cropTemplates.ContainsKey(key))
        {
            return cropTemplates[key].name;
        }
        return "Không xác định";
    }

    // ===================== FERTILIZER / PESTICIDE HELPERS =====================

    /// <summary>
    /// Kiểm tra item có phải phân bón không
    /// </summary>
    public static bool IsFertilizerItem(short itemId)
    {
        return itemId == ITEM_FERTILIZER_5M
            || itemId == ITEM_FERTILIZER_10M
            || itemId == ITEM_FERTILIZER_20M
            || itemId == ITEM_FERTILIZER_30M;
    }

    /// <summary>
    /// Kiểm tra item có phải thuốc trừ sâu không
    /// </summary>
    public static bool IsPesticideItem(short itemId)
    {
        return itemId == ITEM_PESTICIDE;
    }

    /// <summary>
    /// Kiểm tra item có phải vật phẩm farm (phân bón hoặc thuốc) không
    /// </summary>
    public static bool IsFarmConsumable(short itemId)
    {
        return IsFertilizerItem(itemId) || IsPesticideItem(itemId);
    }

    /// <summary>
    /// Lấy số giây giảm của một loại phân bón
    /// </summary>
    public static int GetFertilizerSeconds(short itemId)
    {
        switch (itemId)
        {
            case ITEM_FERTILIZER_5M:  return FERTILIZER_5M_SECS;
            case ITEM_FERTILIZER_10M: return FERTILIZER_10M_SECS;
            case ITEM_FERTILIZER_20M: return FERTILIZER_20M_SECS;
            case ITEM_FERTILIZER_30M: return FERTILIZER_30M_SECS;
            default: return 0;
        }
    }

    /// <summary>
    /// Lấy tên hiển thị của phân bón / thuốc trừ sâu
    /// </summary>
    public static string GetFarmConsumableName(short itemId)
    {
        switch (itemId)
        {
            case ITEM_FERTILIZER_5M:  return "Phân 5p";
            case ITEM_FERTILIZER_10M: return "Phân 10p";
            case ITEM_FERTILIZER_20M: return "Phân 20p";
            case ITEM_FERTILIZER_30M: return "Phân 30p";
            case ITEM_PESTICIDE:      return "Thuốc trừ sâu";
            default: return "Không rõ";
        }
    }
}
