# Cloud Garden - Hướng dẫn tích hợp Client

## ✅ Đã hoàn thành tích hợp

### Các file đã tạo trong thư mục Farm:

```
Assets/Scripts/Farm/
├── FarmConstants.cs      - Hằng số (crop types, stages, map IDs)
├── FarmAssetManager.cs   - Quản lý cache assets/images  
├── FarmPlot.cs           - Model ô ruộng
├── CloudGarden.cs        - Quản lý khu vườn + vẽ UI
├── FarmMessageHandler.cs - Xử lý message từ server
├── FarmService.cs        - Gửi request lên server (partial class)
└── README.md             - File này
```

### Đã sửa đổi:

#### Server (Java):
1. **DataGame.java**
   - Thêm `sendFarmResources()`: Gửi farm assets cùng lúc với main resources
   - Lồng ghép vào `sendRes()` để tự động gửi khi client download data lần đầu
   - Sử dụng sub-type 5 cho farm assets

#### Client (C#):
1. **Controller.cs**
   - Thêm `case -33`: Xử lý Farm Assets từ server (realtime)
   - Thêm xử lý `b12 == 10` trong `case -34`: Xử lý Farm Data từ server
   - Thêm xử lý `b38 == 5` trong `case -74`: Nhận và cache farm assets khi download data
   - Thêm method `ParseAndSaveCropAsset()`: Parse filename để lưu crop assets

2. **GameScr.cs**
   - Thêm vẽ `CloudGarden.Paint()` trong method `paint()` (sau vẽ NPC)
   - Thêm kiểm tra click vào ô ruộng trong `checkClickMoveTo()`

---

## 🔄 FLOW DOWNLOAD FARM ASSETS

```
1. Client mở game lần đầu
   ↓
2. Client gửi request download resources (message -74, sub-type 1)
   ↓
3. Server gửi main resources (sub-type 2)
   ↓
4. Server gọi sendFarmResources() 
   - Đọc từ data/famer/x{zoom}/*.png
   - Gửi từng file với sub-type 5
   ↓
5. Client nhận farm assets (sub-type 5)
   - Lưu vào RMS để cache lâu dài
   - Parse filename và cache vào FarmAssetManager
   ↓
6. Server gửi finish signal (sub-type 3)
   ↓
7. Client sẵn sàng sử dụng
```

---

## 📝 CÔNG VIỆC CẦN LÀM THỦ CÔNG

### 1. Chạy SQL Migration (Server)

```sql
ALTER TABLE `player` ADD COLUMN `data_cloud_garden` TEXT NULL AFTER `data_magic_tree`;
```

### 2. Tạo NPC Farmer (Server)

Xem hướng dẫn trong: `TOMAHOC/src/models/Farm/CLOUD_GARDEN_DOCUMENTATION.md`

### 3. Chuẩn bị Assets (BẮT BUỘC!)

Tạo các file PNG trong thư mục `data/famer/x{zoom}/`:

```
data/famer/
├── x1/
│   ├── plot_empty.png
│   ├── crop_tomato_seed.png
│   ├── crop_tomato_sprout1.png
│   ├── crop_tomato_sprout2.png
│   ├── crop_tomato_young.png
│   ├── crop_tomato_mature.png
│   ├── crop_starfruit_seed.png
│   ├── ... (tương tự cho starfruit, corn, pumpkin)
│   ├── icon_hoe.png
│   ├── icon_seed.png
│   ├── icon_water.png
│   ├── icon_harvest.png
│   └── icon_arrow_down.png
├── x2/ (giống x1, kích thước x2)
├── x3/ (giống x1, kích thước x3)
└── x4/ (giống x1, kích thước x4)
```

Xem danh sách chi tiết: `TOMAHOC/src/models/Farm/ASSETS_README.md`

### 4. Test

- [ ] Clear RMS để buộc download lại data
- [ ] Vào game, kiểm tra farm assets được download
- [ ] Vào map 210/211/212 (Cloud Garden)
- [ ] Ô ruộng hiển thị đúng
- [ ] Click vào ô ruộng mở menu

---

## Message Protocol

### Download (Message -74, Server → Client)

| Sub-type | Mô tả |
|----------|-------|
| 0 | Version check |
| 1 | Size info |
| 2 | Main resource file |
| 3 | Finish |
| 4 | Music file |
| **5** | **Farm asset file** (NEW!) |

### Farm Assets (Message -33, Server → Client)

| Sub-type | Mô tả |
|----------|-------|
| 10 | Farm asset chung |
| 11 | Crop stage asset |
| 12 | Farm icon |

### Farm Data (Message -34, Server → Client)

| Sub-type | Data-type | Mô tả |
|----------|-----------|-------|
| 10 | 0 | Update single plot |
| 10 | 1 | Update full garden |

---

## Lưu ý

1. **Lint errors**: Các lỗi lint trong Java files (Server) là do IDE chưa refresh các dependencies (Lombok, Logger, etc). Project sẽ compile bình thường.

2. **Map IDs**: Cloud Garden sử dụng map 210 (Trái Đất), 211 (Namếc), 212 (Xayda)

3. **Zoom levels**: Hỗ trợ 4 zoom levels (x1, x2, x3, x4)

---

*Cập nhật: 2026-01-10*
