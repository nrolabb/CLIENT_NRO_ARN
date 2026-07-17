using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Xử lý các message Spine từ server.
/// Tất cả Spine messages dùng command = -48, byte đầu tiên là sub-type.
/// Được gọi từ Controller.cs khi nhận message với command = -48.
/// </summary>
public static class SpineMessageHandler
{
    /// <summary>
    /// Kiểm tra command có phải Spine command không
    /// </summary>
    public static bool IsSpineCommand(int command)
    {
        return command == SpineCommand.SPINE_CMD;
    }

    /// <summary>
    /// Xử lý message Spine từ server
    /// </summary>
    public static void HandleMessage(Message message)
    {
        try
        {
            int subType = message.reader().readByte();
            Debug.Log($"[SpineMessageHandler] Received message subType: {subType}");
            switch (subType)
            {
                case SpineCommand.SPINE_INIT_DATA:
                    HandleSpineInitData(message);
                    break;
                case SpineCommand.SPINE_ANIMATION:
                    HandleSpineAnimation(message);
                    break;
                case SpineCommand.SPINE_DIRECTION:
                    HandleSpineDirection(message);
                    break;
                case SpineCommand.SPINE_MOVE:
                    HandleSpineMove(message);
                    break;
                case SpineCommand.SPINE_ATTACK:
                    HandleSpineAttack(message);
                    break;
                case SpineCommand.SPINE_HIT:
                    HandleSpineHit(message);
                    break;
                case SpineCommand.SPINE_DIE:
                    HandleSpineDie(message);
                    break;
                case SpineCommand.SPINE_TOGGLE:
                    HandleSpineToggle(message);
                    break;
                case SpineCommand.SPINE_CHANGE_SKIN:
                    HandleSpineChangeSkin(message);
                    break;
                case SpineCommand.SPINE_SKILL_EFFECT:
                    HandleSpineSkillEffect(message);
                    break;
                default:
                    Debug.LogWarning("[SpineMessageHandler] Unknown Spine sub-type: " + subType);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SpineMessageHandler] Error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ==================== HANDLERS ====================

    private static void HandleSpineInitData(Message msg)
    {
        int playerId = msg.reader().readInt();
        bool useSpine = msg.reader().readBoolean();

        if (useSpine)
        {
            int skinId = msg.reader().readInt();
            bool loop = msg.reader().readBoolean();
            int scaleX = msg.reader().readByte();

            SpineSkinManager.SpineSkinData data = SpineSkinManager.GetSkinData(skinId);
            Res.outz($"[Spine] Init: player={playerId}, skinId={skinId} -> skeleton={data.skeletonName}");

            Char c = GetChar(playerId);
            Vector2 pos = Vector2.zero;
            if (c != null)
            {
                pos = new Vector2(c.cx, c.cy);
                c.useSpine = true;
            }

            SpineCharacterRenderer renderer = SpineCharacterManager.Instance.AddOrUpdateCharacter(
                playerId, data.skeletonName, data.skinName, pos);

            if (renderer != null)
            {
                renderer.SetAnimation("Idle", loop);
                renderer.SetDirection(scaleX);
            }
        }
    }

    private static void HandleSpineAnimation(Message msg)
    {
        int playerId = msg.reader().readInt();
        string animation = msg.reader().readUTF();
        bool loop = msg.reader().readBoolean();

        SpineCharacterManager.Instance.SetCharacterAnimation(playerId, animation, loop);
    }

    private static void HandleSpineDirection(Message msg)
    {
        int playerId = msg.reader().readInt();
        int scaleX = msg.reader().readByte();

        SpineCharacterRenderer renderer = SpineCharacterManager.Instance.GetRenderer(playerId);
        if (renderer != null)
        {
            renderer.SetDirection(scaleX);
        }
    }

    private static void HandleSpineMove(Message msg)
    {
        int playerId = msg.reader().readInt();
        short x = msg.reader().readShort();
        short y = msg.reader().readShort();
        string animation = msg.reader().readUTF();
        bool loop = msg.reader().readBoolean();
        int scaleX = msg.reader().readByte();

        SpineCharacterRenderer renderer = SpineCharacterManager.Instance.GetRenderer(playerId);
        if (renderer != null)
        {
            renderer.transform.position = new Vector3(x, y, 0);
            renderer.SetDirection(scaleX);
            renderer.SetAnimation(animation, loop);
        }

        // Cập nhật vị trí trên Char
        Char c = GetChar(playerId);
        if (c != null)
        {
            c.cx = x;
            c.cy = y;
        }
    }

    private static void HandleSpineAttack(Message msg)
    {
        int playerId = msg.reader().readInt();
        int targetId = msg.reader().readInt();
        string animation = msg.reader().readUTF();
        int damage = msg.reader().readInt();

        SpineCharacterRenderer renderer = SpineCharacterManager.Instance.GetRenderer(playerId);
        if (renderer != null)
        {
            renderer.SetAnimation(animation, false);
        }
    }

    private static void HandleSpineHit(Message msg)
    {
        int playerId = msg.reader().readInt();
        string animation = msg.reader().readUTF();
        long currentHp = msg.reader().readLong();
        int damage = msg.reader().readInt();

        SpineCharacterRenderer renderer = SpineCharacterManager.Instance.GetRenderer(playerId);
        if (renderer != null)
        {
            renderer.SetAnimation(animation, false);
        }
    }

    private static void HandleSpineDie(Message msg)
    {
        int playerId = msg.reader().readInt();
        string animation = msg.reader().readUTF();

        SpineCharacterRenderer renderer = SpineCharacterManager.Instance.GetRenderer(playerId);
        if (renderer != null)
        {
            renderer.SetAnimation(animation, false);
        }
    }

    // Lưu trữ thông tin skin của các player để tự động áp dụng khi họ xuất hiện trong map
    public static Dictionary<int, int> playerSkinCache = new Dictionary<int, int>();

    private static void HandleSpineToggle(Message msg)
    {
        int playerId = msg.reader().readInt();
        bool useSpine = msg.reader().readBoolean();

        if (useSpine)
        {
            int skinId = msg.reader().readInt();
            playerSkinCache[playerId] = skinId; // Lưu vào cache
            
            Char c = GetChar(playerId);
            ApplySkinToChar(c, skinId);
        }
        else
        {
            if (playerSkinCache.ContainsKey(playerId)) playerSkinCache.Remove(playerId);
            SpineCharacterManager.Instance.RemoveCharacter(playerId);

            Char c = GetChar(playerId);
            if (c != null) c.useSpine = false;
        }

        Res.outz("[Spine] Toggle: player=" + playerId + ", active=" + useSpine);
    }

    public static void ApplySkinToChar(Char c, int skinId)
    {
        if (c == null) return;
        
        c.useSpine = true;
        Vector2 pos = new Vector2(c.cx, c.cy);
        
        SpineSkinManager.SpineSkinData data = SpineSkinManager.GetSkinData(skinId);
        if (data != null)
        {
            SpineCharacterManager.Instance.AddOrUpdateCharacter(c.charID, data.skeletonName, data.skinName, pos);
        }
    }

    // Hàm này sẽ được gọi từ Char để kiểm tra skin khi nhân vật mới xuất hiện hoặc load map
    public static void CheckAndApplySpine(Char c)
    {
        if (c == null) return;
        
        // Nếu c.useSpine là true nhưng renderer bị mất (do lỗi map/UI), thì vẫn cần apply lại
        bool hasRenderer = SpineCharacterManager.Instance.GetRenderer(c.charID) != null;
        
        if (c.useSpine && !hasRenderer)
        {
            // Ưu tiên dùng cache, fallback sang spineId trên Char
            int skinId = 0;
            if (playerSkinCache.TryGetValue(c.charID, out int cachedId))
            {
                skinId = cachedId;
            }
            else if (c.spineId > 0)
            {
                skinId = c.spineId;
                playerSkinCache[c.charID] = skinId; // Populate cache
            }
            
            if (skinId > 0)
            {
                ApplySkinToChar(c, skinId);
            }
        }
        else if (!c.useSpine && !hasRenderer)
        {
            // Trường hợp useSpine chưa set nhưng cache có (nhân vật khác xuất hiện trong map)
            if (playerSkinCache.TryGetValue(c.charID, out int skinId))
            {
                ApplySkinToChar(c, skinId);
            }
        }
    }

    private static void HandleSpineChangeSkin(Message msg)
    {
        int playerId = msg.reader().readInt();
        int skinId = msg.reader().readInt();
        playerSkinCache[playerId] = skinId;

        Res.outz($"[Spine] ChangeSkin: player={playerId}, skinId={skinId}");

        Char c = GetChar(playerId);
        ApplySkinToChar(c, skinId);
    }

    private static void HandleSpineSkillEffect(Message msg)
    {
        int playerId = msg.reader().readInt();
        string skeletonPath = msg.reader().readUTF();
        string animation = msg.reader().readUTF();
        short durationMs = msg.reader().readShort();

        Res.outz($"[Spine] SkillEffect: player={playerId}, skeleton={skeletonPath}, anim={animation}, duration={durationMs}");
        SpineSkillEffectController.Play(playerId, skeletonPath, animation, durationMs);
    }

    private static Char GetChar(int playerId)
    {
        Char c = GameScr.findCharInMap(playerId);
        if (c != null) return c;
        if (Char.myCharz() != null)
        {
            if (playerId == Char.myCharz().charID) return Char.myCharz();
            if (playerId == -Char.myCharz().charID && Char.myPetz() != null)
            {
                Char.myPetz().charID = playerId;
                return Char.myPetz();
            }
        }
        return null;
    }
}
