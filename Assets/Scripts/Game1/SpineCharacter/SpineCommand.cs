/// <summary>
/// Định nghĩa command và sub-command IDs cho Spine protocol.
/// Mirror của SpineCommand.java trên server.
/// 
/// Sử dụng DUY NHẤT 1 command ID = -48 cho tất cả Spine messages.
/// Byte đầu tiên sau command là sub-type để phân biệt loại message.
/// </summary>
public static class SpineCommand
{
    /// <summary>
    /// Command ID duy nhất cho tất cả Spine protocol messages.
    /// Đã xác nhận CHƯA DÙNG trên cả server và client.
    /// </summary>
    public const int SPINE_CMD = -48;

    // ==================== SUB-TYPES ====================

    // --- Server → Client ---
    public const int SPINE_INIT_DATA = 0;
    public const int SPINE_ANIMATION = 1;
    public const int SPINE_DIRECTION = 2;
    public const int SPINE_MOVE = 3;
    public const int SPINE_ATTACK = 4;
    public const int SPINE_HIT = 5;
    public const int SPINE_DIE = 6;
    public const int SPINE_TOGGLE = 7;
    public const int SPINE_CHANGE_SKIN = 8;
    public const int SPINE_SKILL_EFFECT = 9;

    // --- Client → Server ---
    public const int SPINE_REQUEST_ACTIVATE = 10;
    public const int SPINE_REQUEST_DEACTIVATE = 11;
    public const int SPINE_ACTION_MOVE = 12;
    public const int SPINE_ACTION_ATTACK = 13;
    public const int SPINE_ACTION_JUMP = 14;
}
