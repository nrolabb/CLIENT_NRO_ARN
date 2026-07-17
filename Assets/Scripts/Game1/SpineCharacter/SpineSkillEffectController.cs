using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// Phát hiệu ứng Spine skill tạm thời gắn theo nhân vật (biến hình, v.v.)
/// </summary>
public static class SpineSkillEffectController
{
    private const int SPINE_LAYER = 31;

    public class ActiveEffect
    {
        public int charId;
        public GameObject go;
        public long endTime;
        public int oldHead;
    }

    public static readonly Dictionary<int, ActiveEffect> activeEffects = new Dictionary<int, ActiveEffect>();
    private static readonly Dictionary<string, SkeletonDataAsset> skeletonCache = new Dictionary<string, SkeletonDataAsset>();

    public static bool HasEffect(int charId)
    {
        return activeEffects.ContainsKey(charId);
    }

    public static void Play(int charId, string serverPath, string animation, int durationMs)
    {
        Remove(charId);

        SkeletonDataAsset data = LoadSkeleton(serverPath);
        if (data == null)
        {
            Debug.LogWarning("[SpineSkillEffect] Failed to load skeleton: " + serverPath);
            return;
        }

        GameObject go = new GameObject("SpineSkillEffect_" + charId);
        go.layer = SPINE_LAYER;
        Object.DontDestroyOnLoad(go);

        SkeletonAnimation skeletonAnimation = SkeletonAnimation.AddToGameObject(go, data);
        skeletonAnimation.Initialize(true);

        string animName = ResolveAnimationName(skeletonAnimation, animation);
        if (animName == null)
        {
            Object.Destroy(go);
            return;
        }

        var track = skeletonAnimation.AnimationState.SetAnimation(0, animName, false);
        if (track != null && track.Animation != null && track.Animation.Duration > 0f)
        {
            skeletonAnimation.timeScale = track.Animation.Duration / (durationMs / 1000f);
        }

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 32766;
        }

        Char c = GetChar(charId);

        activeEffects[charId] = new ActiveEffect
        {
            charId = charId,
            go = go,
            endTime = mSystem.currentTimeMillis() + durationMs + 2000, // Thêm 2 giây chờ server phản hồi skin mới
            oldHead = c != null ? c.head : -1
        };

        if (c != null)
        {
            c.isWaitBienHinh = true;
            c.lastWaitBienHinh = mSystem.currentTimeMillis();
            c.isLockMove = true;
        }
    }

    public static void Update()
    {
        if (activeEffects.Count == 0)
        {
            return;
        }

        long now = mSystem.currentTimeMillis();
        float zoom = mGraphics.zoomLevel;
        List<int> toRemove = new List<int>();

        foreach (KeyValuePair<int, ActiveEffect> kvp in activeEffects)
        {
            ActiveEffect effect = kvp.Value;
            Char c = GetChar(effect.charId);
            if (c == null || effect.go == null || now >= effect.endTime)
            {
                toRemove.Add(kvp.Key);
                continue;
            }
            // Chỉ kiểm tra thay đổi skin khi effect đã chạy gần xong (tránh lỗi ngắt sớm)
            if (c.head != effect.oldHead && effect.oldHead != -1 && now > (effect.endTime - 2000 - 1000))
            {
                toRemove.Add(kvp.Key);
                continue;
            }

            float screenX = (c.cx - GameScr.cmx - 3) * zoom;
            float screenY = Screen.height - (c.cy - GameScr.cmy + GameCanvas.transY) * zoom;
            effect.go.transform.position = new Vector3(screenX, screenY, 0f);
            // kích thước spine skill
            float scale = 8.5f * zoom; // Cập nhật lại kích thước (trước đây là 22f)
            effect.go.transform.localScale = new Vector3(scale * c.cdir, scale, 1f);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            Remove(toRemove[i]);
        }
    }

    public static void Remove(int charId)
    {
        if (!activeEffects.TryGetValue(charId, out ActiveEffect effect))
        {
            return;
        }

        if (effect.go != null)
        {
            Object.Destroy(effect.go);
        }
        activeEffects.Remove(charId);

        Char c = GetChar(charId);
        if (c != null && c.isWaitBienHinh)
        {
            c.isWaitBienHinh = false;
            if (c.me)
            {
                c.isLockMove = false;
            }
        }
    }

    private static SkeletonDataAsset LoadSkeleton(string serverPath)
    {
        if (skeletonCache.TryGetValue(serverPath, out SkeletonDataAsset cached))
        {
            return cached;
        }

        string resourcePath = "Spine/" + serverPath + "_SkeletonData";
        SkeletonDataAsset asset = Resources.Load<SkeletonDataAsset>(resourcePath);
        if (asset != null)
        {
            skeletonCache[serverPath] = asset;
        }
        return asset;
    }

    private static string ResolveAnimationName(SkeletonAnimation skeletonAnimation, string animation)
    {
        if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
        {
            return null;
        }

        var skeletonData = skeletonAnimation.Skeleton.Data;
        if (skeletonData == null)
        {
            return null;
        }

        if (skeletonData.FindAnimation(animation) != null)
        {
            return animation;
        }

        if (skeletonData.Animations.Count > 0)
        {
            return skeletonData.Animations.Items[0].Name;
        }

        return null;
    }

    private static Char GetChar(int charId)
    {
        Char c = GameScr.findCharInMap(charId);
        if (c != null)
        {
            return c;
        }
        if (Char.myCharz() != null && Char.myCharz().charID == charId)
        {
            return Char.myCharz();
        }
        return null;
    }
}
