using UnityEngine;
using Spine.Unity;
using Spine;

/// <summary>
/// Component render Spine cho 1 nhân vật sử dụng SkeletonAnimation (3D).
/// Được thiết kế để chạy với SpineCamera riêng biệt để đè lên OnGUI.
/// </summary>
public class SpineCharacterRenderer : MonoBehaviour
{
    [Header("Spine Components")]
    public SkeletonAnimation skeletonAnimation;
    
    [Header("State")]
    public string currentSkeletonName = "";
    public string currentSkin = "default";
    public string currentAnimation = "";
    public bool isLoop = true;
    public int direction = 1; // 1 = phải, -1 = trái
    public float timeScale = 0.6f; // Tốc độ animation (mặc định 0.6f cho NRO)

    private bool isInitialized;

    /// <summary>
    /// Khởi tạo Spine renderer với SkeletonDataAsset
    /// </summary>
    public void Initialize(SkeletonDataAsset skeletonDataAsset, string skeletonName, string skinName = "default")
    {
        if (skeletonDataAsset == null)
        {
            Debug.LogError("[SpineCharacterRenderer] SkeletonDataAsset is null!");
            return;
        }

        skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null)
        {
            skeletonAnimation = gameObject.AddComponent<SkeletonAnimation>();
        }

        skeletonAnimation.skeletonDataAsset = skeletonDataAsset;
        currentSkeletonName = skeletonName;
        
        // Cấu hình mesh renderer để hiển thị đúng
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 32767; // Ưu tiên hiển thị
        }

        // Đăng ký sự kiện để ghi đè các material bị lỗi (thiếu texture) thành trong suốt
        skeletonAnimation.OnMeshAndMaterialsUpdated += OnMeshAndMaterialsUpdated;

        skeletonAnimation.Initialize(true);

        // Set skin nếu có
        if (!string.IsNullOrEmpty(skinName) && skinName != "default")
        {
            try
            {
                skeletonAnimation.Skeleton.SetSkin(skinName);
                skeletonAnimation.Skeleton.SetSlotsToSetupPose();
                currentSkin = skinName;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SpineCharacterRenderer] Skin '{skinName}' not found: {e.Message}");
            }
        }

        // Animation event callback
        skeletonAnimation.AnimationState.Complete += OnAnimationComplete;

        // Phải set flag initialized TRƯỚC khi gọi SetAnimation (vì SetAnimation check flag này)
        isInitialized = true;

        // Play Idle by default
        SetAnimation("Idle", true);

        Debug.Log($"[SpineCharacterRenderer] Initialized with SkeletonAnimation, skin: {skinName}");
    }

    public void SetAnimation(string animName, bool loop, float speed = -1f)
    {
        if (!isInitialized || skeletonAnimation == null) return;
        if (string.IsNullOrEmpty(animName)) return;

        // Ánh xạ các animation chung chung từ Server sang đúng tên bộ skin hỗ trợ
        animName = GetMappedAnimationName(animName);

        if (currentAnimation == animName && isLoop == loop) return;

        // Kiểm tra animation có tồn tại trong skeleton không
        string resolvedAnim = ResolveAnimationName(animName);
        if (resolvedAnim == null)
        {
            Debug.LogWarning($"[SpineCharacterRenderer] Animation '{animName}' not found in skeleton, skipping");
            return;
        }

        currentAnimation = animName;
        isLoop = loop;

        try
        {
            bool isShip = !string.IsNullOrEmpty(currentSkeletonName) && currentSkeletonName.StartsWith("ship_");
            if (isShip)
            {
                // Reset về SetupPose để tránh hiện tượng giữ trạng thái của animation cũ (bleeding)
                skeletonAnimation.Skeleton.SetToSetupPose();
            }

            if (isShip)
            {
                // Tăng tốc độ animation của ship (1.2f) để luồng khí bập bùng nhanh và đẹp mắt hơn
                skeletonAnimation.timeScale = 1.2f;
            }
            else
            {
                skeletonAnimation.timeScale = (speed > 0) ? speed : timeScale;
            }
            var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, resolvedAnim, loop);

            if (isShip && trackEntry != null)
            {
                // Đặt MixDuration = 0 để chuyển đổi ngay lập tức, tránh các thành phần bị nội suy (bay từ ngoài vào)
                trackEntry.MixDuration = 0f;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SpineCharacterRenderer] Failed to set animation '{resolvedAnim}': {e.Message}");
        }
    }

    /// <summary>
    /// Tìm tên animation thực sự trong skeleton data, hỗ trợ case-insensitive và alias.
    /// </summary>
    private string ResolveAnimationName(string animName)
    {
        if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return null;
        
        var skeletonData = skeletonAnimation.Skeleton.Data;
        if (skeletonData == null) return null;

        // 1. Thử tên gốc trước
        if (skeletonData.FindAnimation(animName) != null) return animName;

        // 2. Thử các alias phổ biến
        string[] aliases = GetAnimationAliases(animName);
        if (aliases != null)
        {
            foreach (string alias in aliases)
            {
                if (skeletonData.FindAnimation(alias) != null) return alias;
            }
        }

        // 3. Tìm case-insensitive
        foreach (var anim in skeletonData.Animations)
        {
            if (string.Equals(anim.Name, animName, System.StringComparison.OrdinalIgnoreCase))
                return anim.Name;
        }

        // 4. Nếu là Idle mà không tìm thấy, tìm animation đầu tiên (đảm bảo không đứng im)
        if (animName == "Idle" || animName == "idle")
        {
            if (skeletonData.Animations.Count > 0)
            {
                string firstAnim = skeletonData.Animations.Items[0].Name;
                Debug.Log($"[SpineCharacterRenderer] 'Idle' not found, using first animation: '{firstAnim}'");
                return firstAnim;
            }
        }

        return null;
    }

    private string[] GetAnimationAliases(string animName)
    {
        bool isShip = !string.IsNullOrEmpty(currentSkeletonName) && currentSkeletonName.StartsWith("ship_");
        switch (animName)
        {
            case "Idle": 
                if (isShip) return new[] { "action_2", "idle", "stand" };
                return new[] { "idle", "IDLE", "standby", "stand", "Stand", "animation" };
            case "Run": 
            case "Walk":
                if (isShip) return new[] { "action_1", "run", "walk", "move" };
                return new[] { "run", "RUN", "Walk", "walk", "move" };
            case "Jump": 
                if (isShip) return new[] { "action_1", "jump" };
                return new[] { "jump", "JUMP" };
            case "Fall": 
                if (isShip) return new[] { "action_1", "fall" };
                return new[] { "fall", "FALL" };
            case "Fly": 
                if (isShip) return new[] { "action_1", "fly" };
                return new[] { "fly", "FLY" };
            case "Die": 
            case "Death":
                return new[] { "die", "DIE", "dead", "Dead", "death" };
            case "Hit": return new[] { "hit", "HIT", "Injured", "injured", "hurt" };
            case "Win": return new[] { "win", "WIN", "victory", "Victory" };
            default: return null;
        }
    }

    private string GetMappedAnimationName(string animName)
    {
        switch (animName)
        {
            case "Attack":
                return "Combo1"; // Mặc định chuyển Attack sang Combo1
            case "Injured":
            case "Hit":
                return "Hit";    // Broly dùng Hit
            case "Die":
                return "Die";    // Broly dùng Die
            case "Run":
                return "Run";
            case "Jump":
                return "Jump";
            case "Fall":
                return "Fall";
            case "Fly":
                return "Fly";
            default:
                // Nếu là dải Skill2_X thì giữ nguyên để SpineManager xử lý
                return animName;
        }
    }

    public void SetDirection(int dir)
    {
        if (!isInitialized || skeletonAnimation == null) return;
        direction = dir;
        skeletonAnimation.Skeleton.ScaleX = dir;
    }

    public void SetVisible(bool visible)
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visible;
        }
        
        // Cũng áp dụng cho các con nếu có (ví dụ các hiệu ứng đính kèm)
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = visible;
        }
    }

    public bool IsVisible()
    {
        return gameObject != null && gameObject.activeSelf;
    }

    private void OnAnimationComplete(TrackEntry trackEntry)
    {
        if (trackEntry.TrackIndex == 0 && !trackEntry.Loop)
        {
            SetAnimation("Idle", true);
        }
    }

    private Material transparentMaterialFallback;

    private void OnMeshAndMaterialsUpdated(SkeletonRenderer skeletonRenderer)
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material[] mats = meshRenderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null || mat.mainTexture == null || (mat.shader != null && mat.shader.name.Contains("Error")))
                {
                    if (transparentMaterialFallback == null)
                    {
                        Shader shader = Shader.Find("Spine/Skeleton");
                        if (shader == null) shader = Shader.Find("Sprites/Default");
                        if (shader == null) shader = Shader.Find("Unlit/Transparent");
                        
                        transparentMaterialFallback = new Material(shader);
                        transparentMaterialFallback.color = new Color(0, 0, 0, 0); // Trong suốt
                    }
                    mats[i] = transparentMaterialFallback;
                    changed = true;
                }
            }
            if (changed)
            {
                meshRenderer.sharedMaterials = mats;
            }
        }
    }

    private void OnDestroy()
    {
        if (skeletonAnimation != null)
        {
            if (skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
            }
            skeletonAnimation.OnMeshAndMaterialsUpdated -= OnMeshAndMaterialsUpdated;
        }
    }

    public void ChangeSkin(string skinName)
    {
        if (!isInitialized || skeletonAnimation == null) return;
        if (!string.IsNullOrEmpty(skinName) && currentSkin != skinName)
        {
            try
            {
                skeletonAnimation.Skeleton.SetSkin(skinName);
                skeletonAnimation.Skeleton.SetSlotsToSetupPose();
                currentSkin = skinName;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SpineCharacterRenderer] Skin '{skinName}' not found: {e.Message}");
            }
        }
    }
}
