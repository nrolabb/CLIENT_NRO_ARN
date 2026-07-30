using System;

/// <summary>
/// Gửi Spine actions (di chuyển, tấn công, nhảy) lên server.
/// Tất cả messages dùng command = -104 với sub-type byte.
/// </summary>
public static class SpineInputSender
{
    /// <summary>
    /// Gửi request bật Spine skin
    /// </summary>
    public static void RequestActivateSpineSkin(int spineSkinId)
    {
        Message message = null;
        try
        {
            message = new Message(SpineCommand.SPINE_CMD);
            message.writer().writeByte(SpineCommand.SPINE_REQUEST_ACTIVATE);
            message.writer().writeInt(spineSkinId);
            Session_ME.gI().sendMessage(message);
        }
        catch (Exception) { }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi request tắt Spine skin
    /// </summary>
    public static void RequestDeactivateSpineSkin()
    {
        Message message = null;
        try
        {
            message = new Message(SpineCommand.SPINE_CMD);
            message.writer().writeByte(SpineCommand.SPINE_REQUEST_DEACTIVATE);
            Session_ME.gI().sendMessage(message);
        }
        catch (Exception) { }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi action di chuyển
    /// </summary>
    public static void SendMove(int direction, short x, short y)
    {
        Message message = null;
        try
        {
            message = new Message(SpineCommand.SPINE_CMD);
            message.writer().writeByte(SpineCommand.SPINE_ACTION_MOVE);
            message.writer().writeByte(direction);
            message.writer().writeShort(x);
            message.writer().writeShort(y);
            Session_ME.gI().sendMessage(message);
        }
        catch (Exception) { }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi action tấn công
    /// </summary>
    public static void SendAttack(int targetId, int skillId)
    {
        Message message = null;
        try
        {
            message = new Message(SpineCommand.SPINE_CMD);
            message.writer().writeByte(SpineCommand.SPINE_ACTION_ATTACK);
            message.writer().writeInt(targetId);
            message.writer().writeByte(skillId);
            Session_ME.gI().sendMessage(message);
        }
        catch (Exception) { }
        finally
        {
            if (message != null) message.cleanup();
        }
    }

    /// <summary>
    /// Gửi action nhảy
    /// </summary>
    public static void SendJump()
    {
        Message message = null;
        try
        {
            message = new Message(SpineCommand.SPINE_CMD);
            message.writer().writeByte(SpineCommand.SPINE_ACTION_JUMP);
            Session_ME.gI().sendMessage(message);
        }
        catch (Exception) { }
        finally
        {
            if (message != null) message.cleanup();
        }
    }
}
