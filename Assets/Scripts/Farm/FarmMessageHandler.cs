using System;
using Game1;
using Char = Game1.Char;

/// <summary>
/// Xử lý message farm từ Server
/// Gọi các method này trong Controller.onMessage()
/// </summary>
public class FarmMessageHandler
{
    private static FarmMessageHandler instance;

    public static FarmMessageHandler GI()
    {
        if (instance == null)
        {
            instance = new FarmMessageHandler();
        }
        return instance;
    }

    /// <summary>
    /// Xử lý message -33 (Farm Assets)
    /// </summary>
    public void HandleFarmAssetMessage(Message msg, sbyte subType)
    {
        try
        {
            switch (subType)
            {


                case FarmConstants.SUBTYPE_CROP_TEMPLATE: // 13
                    ReadCropTemplate(msg);
                    break;

                default:
                    Res.outz("FarmMessageHandler: Unknown sub-type " + subType);
                    break;
            }
        }
        catch (Exception ex)
        {
            Res.outz("FarmMessageHandler: Error handling asset message - " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý message -34 (Farm Data)
    /// </summary>
    public void HandleFarmDataMessage(Message msg)
    {
        try
        {
            sbyte subType = msg.reader().readByte();

            if (subType == FarmConstants.SUBTYPE_PLOT_UPDATE) // 10
            {
                ProcessFarmData(msg);
            }
        }
        catch (Exception ex)
        {
            Res.outz("FarmMessageHandler: Error handling data message - " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý message -34 khi subType (10) đã được đọc ở Controller
    /// Gọi từ Controller.cs khi b12 == 10
    /// </summary>
    public void HandleFarmDataDirect(Message msg)
    {
        try
        {
            ProcessFarmData(msg);
        }
        catch (Exception ex)
        {
            Res.outz("FarmMessageHandler: Error handling farm data direct - " + ex.Message);
        }
    }

    /// <summary>
    /// Logic xử lý farm data chung (đọc dataType và dispatch)
    /// </summary>
    private void ProcessFarmData(Message msg)
    {
        int available = msg.reader().available();
        sbyte dataType = msg.reader().readByte();
        Res.outz("FarmMessageHandler: ProcessFarmData dataType=" + dataType + " available=" + available);

        switch (dataType)
        {
            case FarmConstants.DATA_UPDATE_SINGLE: // 0
                ReadSinglePlotUpdate(msg);
                break;

            case FarmConstants.DATA_UPDATE_FULL: // 1
                ReadFullGardenUpdate(msg);
                break;

            case FarmConstants.DATA_OPEN_SEED_PANEL: // 2
                int plotId = msg.reader().readInt();
                // Mở panel chọn hạt giống, Client tự lọc từ inventory
                GameScr.info1.addInfo("chieu.lq Nhận lệnh mở kho từ Server!", 0);
                GameCanvas.panel.setTypeFarmSeed(plotId);
                GameCanvas.panel.show();
                Res.outz("FarmMessageHandler: Open seed panel for plot " + plotId);
                break;

            case 3: // DATA_CLOSE_DIALOG - Server yêu cầu đóng dialog
                CloseCurrentDialog();
                Res.outz("FarmMessageHandler: Server requested close dialog");
                break;

            case FarmConstants.DATA_HARVEST_SUCCESS: // 4 - Hiệu ứng thu hoạch
                int hPlotId = msg.reader().readInt();
                sbyte hCropType = msg.reader().readByte();
                int hQuantity = msg.reader().readInt();
                CloudGarden.GI().ShowHarvestEffect(hPlotId, hCropType, hQuantity);
                break;

            case FarmConstants.DATA_FERTILIZE_SUCCESS: // 5 - Bón phân thành công
                ReadFertilizeSuccess(msg);
                break;

            case FarmConstants.DATA_PESTICIDE_SUCCESS: // 6 - Phun thuốc trừ sâu thành công
                ReadPesticideSuccess(msg);
                break;

            case FarmConstants.DATA_OPEN_FERTILIZE_PANEL: // 7 - Server yêu cầu mở panel bón phân
                int fPlotId = msg.reader().readInt();
                GameCanvas.panel.setTypeFarmFertilize(fPlotId);
                GameCanvas.panel.show();
                Res.outz("FarmMessageHandler: Open fertilize panel for plot " + fPlotId);
                break;

            default:
                Res.outz("FarmMessageHandler: Unknown data type " + dataType);
                break;
        }
    }



    /// <summary>
    /// Đọc crop template
    /// </summary>
    private void ReadCropTemplate(Message msg)
    {
        sbyte count = msg.reader().readByte();
        for (int i = 0; i < count; i++)
        {
            FarmConstants.CropTemplateInfo info = new FarmConstants.CropTemplateInfo();
            // Server Java gửi byte (unsigned 0-255), readByte() trả sbyte
            // Dùng CropKey() để convert sang int key 0-255
            sbyte rawId = msg.reader().readByte();
            info.id = FarmConstants.CropKey(rawId); // int key
            info.name = msg.reader().readUTF();
            info.seedItemId = msg.reader().readShort();
            info.harvestItemId = msg.reader().readShort();
            info.imgYoung = msg.reader().readShort();
            info.imgMature = msg.reader().readShort();
            info.imgWithered = msg.reader().readShort();
            FarmConstants.AddCropTemplate(info);

        }
        Res.outz("FarmMessageHandler: Loaded " + count + " crop templates");
    }

    /// <summary>
    /// Đọc update cho một ô
    /// </summary>
    private void ReadSinglePlotUpdate(Message msg)
    {
        int plotId = msg.reader().readInt();
        sbyte stage = msg.reader().readByte();
        sbyte cropType = msg.reader().readByte();
        int timeToHarvest = msg.reader().readInt();
        bool watered = msg.reader().readBool();

        CloudGarden.GI().UpdateSinglePlot(plotId, true, stage, cropType, timeToHarvest, watered);
        Res.outz("FarmMessageHandler: Updated plot " + plotId);
        Res.outz("chieu.lq: [Client] Received UpdateSinglePlot: plotId=" + plotId + " stage=" + stage + " time=" + timeToHarvest);

        // KHÔNG tự động CloseCurrentDialog() ở đây.
        // Lý do: Client mở panel (seed/fertilize) sau khi click, rồi ngay sau đó server gửi
        // DATA_UPDATE_SINGLE xác nhận trạng thái ô → nếu đóng dialog ở đây sẽ đóng panel vừa mở.
        // Server sẽ gửi DATA_CLOSE_DIALOG (3) riêng khi thực sự muốn đóng dialog.
    }

    /// <summary>
    /// Đọc update toàn bộ garden
    /// </summary>
    private void ReadFullGardenUpdate(Message msg)
    {
        CloudGarden.GI().UpdateFullGarden(msg);
        Res.outz("FarmMessageHandler: Full garden updated");
        Res.outz("chieu.lq: [Client] Received FullGardenUpdate");
        // Không đóng dialog ở đây - sẽ gây đóng menu khi vào map
        // Server sẽ gửi DATA_CLOSE_DIALOG (3) khi cần đóng dialog
    }

    /// <summary>
    /// Đóng menu và ChatPopup hiện tại
    /// Gọi sau khi thu hoạch hoặc gieo hạt thành công
    /// </summary>
    private void CloseCurrentDialog()
    {
        try
        {
            // Đóng menu
            if (GameCanvas.menu != null)
            {
                GameCanvas.menu.showMenu = false;
            }
            
            // Đóng ChatPopup an toàn
            ChatPopup.currChatPopup = null;
            ChatPopup.serverChatPopUp = null;
            Char.chatPopup = null;
            
            // Ẩn InfoDlg và reset dialog state
            InfoDlg.hide();
            GameCanvas.endDlg();
            
            Res.outz("FarmMessageHandler: Closed current dialog safely");
        }
        catch (Exception ex)
        {
            Res.outz("FarmMessageHandler: Error closing dialog - " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý phản hồi bón phân thành công từ server
    /// Packet: [plotId:int][newTimeToHarvest:int]
    /// Server trả về thời gian còn lại mới sau khi bón phân
    /// </summary>
    private void ReadFertilizeSuccess(Message msg)
    {
        try
        {
            int plotId = msg.reader().readInt();
            int newTimeToHarvest = msg.reader().readInt();

            FarmPlot plot = CloudGarden.GI().GetPlot(plotId);
            if (plot != null)
            {
                // Cập nhật thời gian còn lại mới từ server
                plot.serverTimeToHarvest = newTimeToHarvest;
                plot.lastUpdateTime = mSystem.currentTimeMillis();
                Res.outz("FarmMessageHandler: Fertilize success plot=" + plotId + " newTime=" + newTimeToHarvest);
            }

            // Hiệu ứng thông báo
            GameScr.info1.addInfo("Bón phân thành công!", 0);

            // Đóng panel bón phân
            CloseCurrentDialog();
            if (GameCanvas.panel != null) GameCanvas.panel.hide();
        }
        catch (Exception ex)
        {
            Res.outz("FarmMessageHandler: ReadFertilizeSuccess error - " + ex.Message);
        }
    }

    /// <summary>
    /// Xử lý phản hồi phun thuốc trừ sâu thành công từ server
    /// Packet: [plotId:int][newStage:byte][newTimeToHarvest:int]
    /// Server trả về stage mới sau khi chữa héo
    /// </summary>
    private void ReadPesticideSuccess(Message msg)
    {
        try
        {
            int plotId = msg.reader().readInt();
            sbyte newStage = msg.reader().readByte();
            int newTimeToHarvest = msg.reader().readInt();

            FarmPlot plot = CloudGarden.GI().GetPlot(plotId);
            if (plot != null)
            {
                plot.currentStage = newStage;
                plot.serverTimeToHarvest = newTimeToHarvest;
                plot.lastUpdateTime = mSystem.currentTimeMillis();
                Res.outz("FarmMessageHandler: Pesticide success plot=" + plotId + " newStage=" + newStage);
            }

            // Hiệu ứng thông báo
            GameScr.info1.addInfo("Phun thuốc thành công! Cây đã hồi phục.", 0);

            // Đóng dialog
            CloseCurrentDialog();
            if (GameCanvas.panel != null) GameCanvas.panel.hide();
        }
        catch (Exception ex)
        {
            Res.outz("FarmMessageHandler: ReadPesticideSuccess error - " + ex.Message);
        }
    }
}
