using System;

namespace Game1
{
/// <summary>
/// Service gửi request farm lên Server
/// </summary>
public partial class Service
{
    /// <summary>
    /// Gửi tương tác với ô ruộng
    /// Protocol: [cmd=-34][sub-type=10][action=0][plotId:int]
    /// </summary>
    public void farmPlotInteraction(int plotId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)FarmConstants.MSG_FARM_DATA); // -34
            message.writer().writeByte(FarmConstants.SUBTYPE_PLOT_UPDATE); // 10 - farm sub-type
            message.writer().writeByte(0); // action: interact
            message.writer().writeInt(plotId);
            session.sendMessage(message);
            Res.outz("Service: Sent farm plot interaction for plot " + plotId);
            Res.outz("chieu.lq: [Client] Sending farmPlotInteraction for plot " + plotId);
        }
        catch (Exception ex)
        {
            Res.outz("Service farmPlotInteraction error: " + ex.Message);
        }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi request gieo hạt
    /// </summary>
    public void farmPlantSeed(int plotId, short seedItemId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)FarmConstants.MSG_FARM_DATA); // -34
            message.writer().writeByte(FarmConstants.SUBTYPE_PLOT_UPDATE); // 10
            message.writer().writeByte(1); // action: plant
            message.writer().writeInt(plotId);
            message.writer().writeShort(seedItemId);
            session.sendMessage(message);
            Res.outz("Service: Sent plant seed request plot=" + plotId + " seed=" + seedItemId);
            Res.outz("chieu.lq: [Client] Sending farmPlantSeed plot=" + plotId + " seed=" + seedItemId);
        }
        catch (Exception ex)
        {
            Res.outz("Service farmPlantSeed error: " + ex.Message);
        }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi request thu hoạch
    /// </summary>
    public void farmHarvest(int plotId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)FarmConstants.MSG_FARM_DATA);
            message.writer().writeByte(FarmConstants.SUBTYPE_PLOT_UPDATE); // 10
            message.writer().writeByte(2); // action: harvest
            message.writer().writeInt(plotId);
            session.sendMessage(message);
            Res.outz("Service: Sent harvest request for plot " + plotId);
            Res.outz("chieu.lq: [Client] Sending farmHarvest for plot " + plotId);
        }
        catch (Exception ex)
        {
            Res.outz("Service farmHarvest error: " + ex.Message);
        }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi request tưới nước
    /// </summary>
    public void farmWater(int plotId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)FarmConstants.MSG_FARM_DATA);
            message.writer().writeByte(FarmConstants.SUBTYPE_PLOT_UPDATE); // 10
            message.writer().writeByte(3); // action: water
            message.writer().writeInt(plotId);
            session.sendMessage(message);
            Res.outz("Service: Sent water request for plot " + plotId);
        }
        catch (Exception ex)
        {
            Res.outz("Service farmWater error: " + ex.Message);
        }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi request mở khóa ô mới
    /// </summary>
    public void farmUnlockPlot(bool useGem)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)FarmConstants.MSG_FARM_DATA);
            message.writer().writeByte(FarmConstants.SUBTYPE_PLOT_UPDATE); // 10
            message.writer().writeByte(4); // action: unlock
            message.writer().writeByte((sbyte)(useGem ? 1 : 0)); // 1 = gem, 0 = gold
            session.sendMessage(message);
            Res.outz("Service: Sent unlock plot request, useGem=" + useGem);
        }
        catch (Exception ex)
        {
            Res.outz("Service farmUnlockPlot error: " + ex.Message);
        }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi request lấy data garden
    /// </summary>
    public void farmGetGardenData()
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)FarmConstants.MSG_FARM_DATA);
            message.writer().writeByte(FarmConstants.SUBTYPE_PLOT_UPDATE); // 10
            message.writer().writeByte(5); // action: get data
            session.sendMessage(message);
            Res.outz("Service: Sent get garden data request");
        }
        catch (Exception ex)
        {
            Res.outz("Service farmGetGardenData error: " + ex.Message);
        }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi request bón phân cho ô ruộng
    /// Protocol: [cmd=-34][sub=10][action=6][plotId:int][itemId:short]
    /// itemId: 2148=5p, 2149=10p, 2150=20p, 2151=30p
    /// </summary>
    public void farmFertilize(int plotId, short itemId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)FarmConstants.MSG_FARM_DATA);
            message.writer().writeByte(FarmConstants.SUBTYPE_PLOT_UPDATE); // 10
            message.writer().writeByte(6); // action: fertilize
            message.writer().writeInt(plotId);
            message.writer().writeShort(itemId);
            session.sendMessage(message);
            Res.outz("Service: Sent fertilize request plot=" + plotId + " item=" + itemId);
        }
        catch (Exception ex)
        {
            Res.outz("Service farmFertilize error: " + ex.Message);
        }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi request phun thuốc trừ sâu cho ô ruộng (chữa cây héo)
    /// Protocol: [cmd=-34][sub=10][action=7][plotId:int]
    /// itemId cố định = 2152 (ITEM_PESTICIDE), server tự trừ từ inventory
    /// </summary>
    public void farmPesticide(int plotId)
    {
        Message message = null;
        try
        {
            message = new Message((sbyte)FarmConstants.MSG_FARM_DATA);
            message.writer().writeByte(FarmConstants.SUBTYPE_PLOT_UPDATE); // 10
            message.writer().writeByte(7); // action: pesticide
            message.writer().writeInt(plotId);
            message.writer().writeShort(FarmConstants.ITEM_PESTICIDE); // 2152
            session.sendMessage(message);
            Res.outz("Service: Sent pesticide request plot=" + plotId);
        }
        catch (Exception ex)
        {
            Res.outz("Service farmPesticide error: " + ex.Message);
        }
        finally
        {
            if (message != null) message.cleanup();
        }
    }
}
}
