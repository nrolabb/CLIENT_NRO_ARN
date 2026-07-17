using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// Manager quản lý tất cả SpineCharacterRenderer sử dụng SkeletonAnimation (3D).
/// Tạo một Camera riêng biệt để render Spine đè lên trên OnGUI.
/// </summary>
public class SpineCharacterManager : MonoBehaviour
{
    private static SpineCharacterManager instance;
    public static SpineCharacterManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SpineCharacterManager");
                instance = go.AddComponent<SpineCharacterManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private Dictionary<string, SkeletonDataAsset> skeletonCache =
        new Dictionary<string, SkeletonDataAsset>();
    private Dictionary<int, SpineCharacterRenderer> spineCharacters =
        new Dictionary<int, SpineCharacterRenderer>();
    private Dictionary<int, SpineCharacterRenderer> petFollowCharacters =
        new Dictionary<int, SpineCharacterRenderer>();
    // Kênh Spine độc lập cho Tàu bay (slot 8 - type 95), tách khỏi petFollowCharacters
    // (slot 11) để hai trang bị có thể hiển thị đồng thời mà không ảnh hưởng nhau.
    private Dictionary<int, SpineCharacterRenderer> shipFollowCharacters =
        new Dictionary<int, SpineCharacterRenderer>();

    private Camera spineCamera;
    private RenderTexture spinetexture;
    private const int SPINE_LAYER = 31; // Lớp dành riêng cho Spine thế giới
    private const int PET_SPINE_LAYER = 29; // Lớp dành riêng cho Spine pet follow (ship)
    private const int PREVIEW_SPINE_LAYER = 30; // Lớp dành riêng cho Spine xem trước (UI)

    private Camera petSpineCamera;
    private RenderTexture petSpineTexture;

    private Camera previewSpineCamera;
    private RenderTexture previewSpineTexture;
    private const int PREVIEW_TEX_SIZE = 256; // Kích thước texture preview (pixels)

    // Renderer riêng cho chế độ xem trước trong UI
    private SpineCharacterRenderer previewRenderer;
    private int currentPreviewCharId = -1;
    private int currentPreviewCharIdRequested = -1;
    private float previewDisplayBuffer = 0; // Thời gian giữ hiển thị preview
    private string lastPreviewSkeleton = "";
    private string lastPreviewSkin = "";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        EnsureCamera();
    }

    private void LateUpdate()
    {
        EnsureCamera();
        UpdatePositions();
        UpdatePetFollowPositions();
        UpdateShipFollowPositions();
    }

    public void PaintSpine(mGraphics g)
    {
        // Vô hiệu hóa hàm vẽ toàn cục cũ để chuyển sang vẽ theo từng nhân vật (interleaved)
        // Nếu muốn vẽ đè lên tất cả như cũ thì để lại, nhưng user muốn nó như skin bình thường.
    }

    /// <summary>
    /// Vẽ phần texture Spine tương ứng với vị trí của nhân vật này.
    /// Giúp Spine nhân vật được vẽ đúng thứ tự Z-order với các nhân vật 2D khác.
    /// </summary>
    public void PaintSpineForChar(mGraphics g, Char c, int x, int y)
    {
        if (spinetexture == null || c == null || ModFunc.isSpineSkinOff)
            return;
            
        bool hasSkillEffect = SpineSkillEffectController.activeEffects.ContainsKey(c.charID);
        if (!c.useSpine && !hasSkillEffect)
            return;

        if (c.isMonkey > 0 || c.isFusion || c.isHide)
            return;

        int zoom = mGraphics.zoomLevel;

        // Vùng clip quanh nhân vật (tọa độ logic mGraphics)
        int drawW = 150;
        int drawH = 150;
        
        if (hasSkillEffect)
        {
            drawW = 400; // Mở rộng vùng vẽ để không cắt mất effect SpineSkill
            drawH = 400;
        }

        int drawX = x - drawW / 2;
        int drawY = y - drawH + 20;

        // Lưu lại clip cũ
        int oldCX = g.clipX / zoom;
        int oldCY = g.clipY / zoom;
        int oldCW = g.clipW / zoom;
        int oldCH = g.clipH / zoom;
        bool oldIsClip = g.isClip;

        // Thiết lập vùng hiển thị cho nhân vật này
        g.setClip(drawX, drawY, drawW, drawH);

        // Vẽ toàn bộ texture thế giới tại vị trí bù trừ translation của mGraphics (đơn vị pixel)
        // Truyền tọa độ LOGIC. drawRenderTexture sẽ tự nhân zoom và cộng translateX pixel.
        g.drawRenderTexture(spinetexture, -g.translateX / zoom, -g.translateY / zoom);

        // Khôi phục clip cũ
        if (oldIsClip)
            g.setClip(oldCX, oldCY, oldCW, oldCH);
        else
            g.isClip = false;
    }

    /// <summary>
    /// Vẽ nhân vật Spine xem trước như một element UI bình thường.
    /// Tọa độ x,y là vị trí trung tâm nhân vật trong hệ tọa độ mGraphics (content space).
    /// Tự động tuân thủ translate và setClip của mGraphics, giống fillRect.
    /// </summary>
    public void PaintPreviewSpine(mGraphics g, int x, int y, int charId)
    {
        currentPreviewCharIdRequested = charId;
        if (previewSpineTexture == null || previewSpineCamera == null || ModFunc.isSpineSkinOff)
            return;
        if (previewRenderer == null || !previewRenderer.IsVisible())
            return;

        int zoom = mGraphics.zoomLevel;
        int pw = PREVIEW_TEX_SIZE;
        int ph = PREVIEW_TEX_SIZE;

        // Tọa độ truyền vào x, y là logic trung tâm chân
        // Chúng ta tính logicX, logicY sao cho tâm chân texture khớp với x, y
        // drawRenderTexture sẽ tự nhân zoom
        int logicX = x - (pw / 2) / zoom;
        int logicY = (y - 70);
        int logicW = pw / zoom;
        int logicH = ph / zoom;

        g.drawRenderTexture(previewSpineTexture, logicX, logicY, logicW, logicH);
    }

    private void EnsureCamera()
    {
        float halfH = UnityEngine.Screen.height / 2f;
        float halfW = UnityEngine.Screen.width / 2f;

        // 1. Khởi tạo Camera World
        if (spineCamera == null)
        {
            GameObject camObj = GameObject.Find("SpineCamera");
            if (camObj == null)
            {
                camObj = new GameObject("SpineCamera");
                spineCamera = camObj.AddComponent<Camera>();
                spineCamera.orthographic = true;
                spineCamera.clearFlags = CameraClearFlags.SolidColor;
                spineCamera.backgroundColor = new Color(0, 0, 0, 0);
                spineCamera.depth = 100;
                spineCamera.cullingMask = 1 << SPINE_LAYER; // Chỉ nhìn thấy layer 31
                spineCamera.nearClipPlane = 0.1f;
                spineCamera.farClipPlane = 100f;

                spinetexture = new UnityEngine.RenderTexture(
                    UnityEngine.Screen.width,
                    UnityEngine.Screen.height,
                    24,
                    UnityEngine.RenderTextureFormat.ARGB32
                );
                spinetexture.Create();
                spineCamera.targetTexture = spinetexture;

                camObj.transform.position = new UnityEngine.Vector3(halfW, halfH, -10);
                DontDestroyOnLoad(camObj);
            }
            else
                spineCamera = camObj.GetComponent<Camera>();
        }

        // 2. Khởi tạo Camera UI Preview
        if (previewSpineCamera == null)
        {
            GameObject pCamObj = GameObject.Find("SpinePreviewCamera");
            if (pCamObj == null)
            {
                pCamObj = new GameObject("SpinePreviewCamera");
                previewSpineCamera = pCamObj.AddComponent<Camera>();
                previewSpineCamera.orthographic = true;
                previewSpineCamera.clearFlags = CameraClearFlags.SolidColor;
                previewSpineCamera.backgroundColor = new Color(0, 0, 0, 0);
                previewSpineCamera.depth = -100;
                previewSpineCamera.cullingMask = 1 << PREVIEW_SPINE_LAYER;
                previewSpineCamera.nearClipPlane = 0.1f;
                previewSpineCamera.farClipPlane = 100f;

                previewSpineTexture = new UnityEngine.RenderTexture(
                    PREVIEW_TEX_SIZE,
                    PREVIEW_TEX_SIZE,
                    24,
                    UnityEngine.RenderTextureFormat.ARGB32
                );
                previewSpineTexture.Create();
                previewSpineCamera.targetTexture = previewSpineTexture;

                // Đặt camera ở một vị trí rất xa để không bị camera chính vô tình nhìn thấy
                pCamObj.transform.position = new UnityEngine.Vector3(-5000, -5000, -10);
                previewSpineCamera.orthographicSize = PREVIEW_TEX_SIZE / 2f;
                DontDestroyOnLoad(pCamObj);
            }
            else
                previewSpineCamera = pCamObj.GetComponent<Camera>();
        }

        // 3. Khởi tạo Camera Pet Follow (tách riêng khỏi spine chính để ship không đè lên player)
        if (petSpineCamera == null)
        {
            GameObject petCamObj = GameObject.Find("SpinePetCamera");
            if (petCamObj == null)
            {
                petCamObj = new GameObject("SpinePetCamera");
                petSpineCamera = petCamObj.AddComponent<Camera>();
                petSpineCamera.orthographic = true;
                petSpineCamera.clearFlags = CameraClearFlags.SolidColor;
                petSpineCamera.backgroundColor = new Color(0, 0, 0, 0);
                petSpineCamera.depth = 99;
                petSpineCamera.cullingMask = 1 << PET_SPINE_LAYER;
                petSpineCamera.nearClipPlane = 0.1f;
                petSpineCamera.farClipPlane = 100f;

                petSpineTexture = new UnityEngine.RenderTexture(
                    UnityEngine.Screen.width,
                    UnityEngine.Screen.height,
                    24,
                    UnityEngine.RenderTextureFormat.ARGB32
                );
                petSpineTexture.Create();
                petSpineCamera.targetTexture = petSpineTexture;

                petCamObj.transform.position = new UnityEngine.Vector3(halfW, halfH, -10);
                DontDestroyOnLoad(petCamObj);
            }
            else
                petSpineCamera = petCamObj.GetComponent<Camera>();
        }

        // ĐẢM BẢO CÁC CAMERA KHÁC KHÔNG NHÌN THẤY LAYER SPINE
        ExcludeSpineLayersFromOtherCameras();

        // Luôn đảm bảo camera có nền trong suốt
        if (spineCamera != null)
        {
            spineCamera.clearFlags = CameraClearFlags.SolidColor;
            spineCamera.backgroundColor = new Color(0, 0, 0, 0);
        }
        if (petSpineCamera != null)
        {
            petSpineCamera.clearFlags = CameraClearFlags.SolidColor;
            petSpineCamera.backgroundColor = new Color(0, 0, 0, 0);
        }
        if (previewSpineCamera != null)
        {
            previewSpineCamera.clearFlags = CameraClearFlags.SolidColor;
            previewSpineCamera.backgroundColor = new Color(0, 0, 0, 0);
        }

        spineCamera.orthographicSize = halfH;
        spineCamera.transform.position = new UnityEngine.Vector3(halfW, halfH, -10);
        petSpineCamera.orthographicSize = halfH;
        petSpineCamera.transform.position = new UnityEngine.Vector3(halfW, halfH, -10);
        previewSpineCamera.orthographicSize = PREVIEW_TEX_SIZE / 2f;
        previewSpineCamera.transform.position = new UnityEngine.Vector3(-5000, -5000, -10);

        // Kiểm tra Resize
        if (
            spinetexture == null
            || spinetexture.width != Screen.width
            || spinetexture.height != Screen.height
        )
        {
            if (spinetexture != null)
                spinetexture.Release();
            spinetexture = new UnityEngine.RenderTexture(
                Screen.width,
                Screen.height,
                24,
                UnityEngine.RenderTextureFormat.ARGB32
            );
            spinetexture.Create();
            spineCamera.targetTexture = spinetexture;
        }
        if (
            petSpineTexture == null
            || petSpineTexture.width != Screen.width
            || petSpineTexture.height != Screen.height
        )
        {
            if (petSpineTexture != null)
                petSpineTexture.Release();
            petSpineTexture = new UnityEngine.RenderTexture(
                Screen.width,
                Screen.height,
                24,
                UnityEngine.RenderTextureFormat.ARGB32
            );
            petSpineTexture.Create();
            petSpineCamera.targetTexture = petSpineTexture;
        }
        if (previewSpineTexture == null)
        {
            previewSpineTexture = new UnityEngine.RenderTexture(
                PREVIEW_TEX_SIZE,
                PREVIEW_TEX_SIZE,
                24,
                UnityEngine.RenderTextureFormat.ARGB32
            );
            previewSpineTexture.Create();
            previewSpineCamera.targetTexture = previewSpineTexture;
        }
    }

    private void ExcludeSpineLayersFromOtherCameras()
    {
        Camera[] allCameras = Camera.allCameras;
        int maskToRemove = (1 << SPINE_LAYER) | (1 << PET_SPINE_LAYER) | (1 << PREVIEW_SPINE_LAYER);

        foreach (Camera cam in allCameras)
        {
            if (cam != spineCamera && cam != petSpineCamera && cam != previewSpineCamera)
            {
                cam.cullingMask &= ~maskToRemove;
            }
        }
    }

    public SpineCharacterRenderer AddOrUpdateCharacter(
        int charId,
        string skeletonName,
        string skinName,
        Vector2 position
    )
    {
        if (spineCharacters.ContainsKey(charId))
        {
            SpineCharacterRenderer existingRenderer = spineCharacters[charId];
            if (existingRenderer.currentSkin != skinName)
            {
                existingRenderer.ChangeSkin(skinName);
            }
            return existingRenderer;
        }

        SkeletonDataAsset skeletonData = LoadSkeletonData(skeletonName);
        if (skeletonData == null)
            return null;

        GameObject charObj = new GameObject($"SpineChar_{charId}");
        SetLayerRecursively(charObj, SPINE_LAYER);

        SpineCharacterRenderer renderer = charObj.AddComponent<SpineCharacterRenderer>();
        renderer.Initialize(skeletonData, skeletonName, skinName);

        // Luôn đảm bảo renderers con cũng đúng layer
        SetLayerRecursively(charObj, SPINE_LAYER);

        spineCharacters[charId] = renderer;
        Debug.Log($"[SpineCharacterManager] Created 3D Spine for {charId} at layer {SPINE_LAYER}");
        return renderer;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null)
            return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public void RemoveCharacter(int charId)
    {
        if (spineCharacters.ContainsKey(charId))
        {
            SpineCharacterRenderer renderer = spineCharacters[charId];
            if (renderer != null)
                Destroy(renderer.gameObject);
            spineCharacters.Remove(charId);
        }
    }

    public SpineCharacterRenderer GetRenderer(int charId)
    {
        spineCharacters.TryGetValue(charId, out SpineCharacterRenderer renderer);
        return renderer;
    }

    public void SetCharacterAnimation(int charId, string animation, bool loop)
    {
        SpineCharacterRenderer renderer = GetRenderer(charId);
        if (renderer != null)
            renderer.SetAnimation(animation, loop);
    }

    private void UpdatePositions()
    {
        int zoom = mGraphics.zoomLevel;
        bool panelVisible =
            (GameCanvas.panel != null && GameCanvas.panel.isShow)
            || (GameCanvas.panel2 != null && GameCanvas.panel2.isShow)
            || CustomInventoryPanel.isShow;

        Char previewChar = null;
        if (currentPreviewCharIdRequested != -1 && panelVisible)
        {
            previewChar = GetCharById(currentPreviewCharIdRequested);
        }

        if (spineCharacters.Count == 0)
        {
            UpdatePreviewRenderer(previewChar, panelVisible, zoom);
            currentPreviewCharIdRequested = -1;
            return;
        }

        List<int> toRemove = new List<int>();

        foreach (var kvp in spineCharacters)
        {
            int charId = kvp.Key;
            SpineCharacterRenderer renderer = kvp.Value;

            Char c = GameScr.findCharInMap(charId);
            if (c == null && Char.myCharz() != null)
            {
                if (charId == Char.myCharz().charID)
                {
                    c = Char.myCharz();
                }
                else if (charId == -Char.myCharz().charID && Char.myPetz() != null)
                {
                    c = Char.myPetz();
                    c.charID = charId;
                }
            }

            if (c != null)
            {
                if (previewChar == null && c.isPreviewSpine && panelVisible)
                {
                    previewChar = c;
                }

                // LUÔN cập nhật vị trí WORLD cho renderer này
                float screenX = (float)(c.cx - GameScr.cmx) * zoom;
                float screenY =
                    (float)Screen.height - (float)(c.cy - GameScr.cmy + GameCanvas.transY) * zoom;

                // Thêm hiệu ứng dao động bập bềnh hình sin cho ship của player
                float floatOffset = 0f;
                if (
                    !string.IsNullOrEmpty(renderer.currentSkeletonName)
                    && renderer.currentSkeletonName.StartsWith("ship_")
                )
                {
                    floatOffset = Mathf.Sin(Time.time * 4f) * 6f * zoom;
                }
                renderer.transform.position = new Vector3(screenX, screenY + floatOffset, 0);

                float finalScale = 16.5f * zoom;
                renderer.transform.localScale = new Vector3(finalScale, finalScale, 1);

                renderer.SetDirection(c.cdir);
                UpdateAnimationByCharState(renderer, c);

                // Ẩn Spine nếu đang biến hình (Khỉ) hoặc Hợp thể hoặc bị ẩn hoàn toàn, hoặc đang chạy skill effect Spine
                bool isPlayingSkillEffect = SpineSkillEffectController.activeEffects.ContainsKey(c.charID);
                bool shouldShowSpine = (c.isMonkey == 0 && !c.isFusion && !c.isHide && !ModFunc.isSpineSkinOff && !isPlayingSkillEffect);
                renderer.SetVisible(shouldShowSpine);

                // Xử lý độ trong suốt cho Tàng hình
                if (shouldShowSpine && renderer.skeletonAnimation != null)
                {
                    float alpha = 1.0f;
                    if (c.me && c.isTanHinh)
                        alpha = 0.4f; // Player tự nhìn mình mờ mờ
                    else if (c.isTanHinh)
                        alpha = 0f; // Đối thủ không nhìn thấy (hoặc mờ tùy server)

                    if (renderer.skeletonAnimation.skeleton != null)
                    {
                        renderer.skeletonAnimation.skeleton.A = alpha;
                    }
                }
            }
            else
            {
                toRemove.Add(charId);
            }
        }

        // Xử lý mô hình xem trước (Preview) riêng biệt
        UpdatePreviewRenderer(previewChar, panelVisible, zoom);
        currentPreviewCharIdRequested = -1;

        // Reset flag cho tất cả nhân vật sau khi đã xử lý xong frame
        foreach (var kvp in spineCharacters)
        {
            Char cObj = GetCharById(kvp.Key);
            if (cObj != null)
                cObj.isPreviewSpine = false;
        }
        if (Char.myCharz() != null)
            Char.myCharz().isPreviewSpine = false;
        Char myPet = Char.myPetz();
        if (myPet != null)
            myPet.isPreviewSpine = false;

        foreach (int id in toRemove)
            RemoveCharacter(id);
    }

    private Char GetCharById(int id)
    {
        Char c = GameScr.findCharInMap(id);
        if (c != null)
            return c;

        Char myChar = Char.myCharz();
        if (myChar != null)
        {
            if (id == myChar.charID)
                return myChar;

            // Check Pet ID (Thường là -MasterID hoặc ID riêng tùy server)
            // Trong game này thường dùng -myChar.charID cho đệ tử
            if (id == -myChar.charID)
                return Char.myPetz();
        }
        return null;
    }

    private void UpdatePreviewRenderer(Char c, bool panelVisible, int zoom)
    {
        // Nếu đang yêu cầu vẽ, reset buffer
        if (c != null && panelVisible && c.useSpine && !ModFunc.isSpineSkinOff)
        {
            previewDisplayBuffer = 0.1f; // Giữ hiển thị trong ít nhất 0.1s (khoảng 6 frames)
        }

        if (previewDisplayBuffer > 0)
        {
            previewDisplayBuffer -= Time.deltaTime;
        }

        // Chỉ thực sự ẩn khi hết buffer
        if (previewDisplayBuffer <= 0)
        {
            if (previewRenderer != null)
                previewRenderer.SetVisible(false);
            if (c != null)
                c.isPreviewSpine = false;
            return;
        }

        // Nếu buffer còn nhưng nhân vật null thì lấy nhân vật cũ hoặc bỏ qua
        if (c == null)
            return;

        // Lấy thông tin skeleton và skin
        string skeletonName = "";
        string skinName = "default";

        SpineCharacterRenderer worldRenderer = GetRenderer(c.charID);
        if (worldRenderer != null)
        {
            skeletonName = worldRenderer.currentSkeletonName;
            skinName = worldRenderer.currentSkin;
        }
        else
        {
            // Nếu không có renderer trong thế giới, thử lấy từ SpineSkinManager dựa trên spineId
            var skinData = SpineSkinManager.GetSkinData(c.spineId);
            if (skinData != null)
            {
                skeletonName = skinData.skeletonName;
                skinName = skinData.skinName;
            }
        }

        if (string.IsNullOrEmpty(skeletonName))
        {
            if (previewRenderer != null)
                previewRenderer.SetVisible(false);
            return;
        }

        // Khởi tạo/Cập nhật previewRenderer
        if (previewRenderer == null)
        {
            GameObject go = new GameObject("SpinePreviewRenderer");
            SetLayerRecursively(go, PREVIEW_SPINE_LAYER);
            previewRenderer = go.AddComponent<SpineCharacterRenderer>();
            DontDestroyOnLoad(go);
        }

        // Đồng bộ Skeleton và Skin nếu thay đổi
        if (lastPreviewSkeleton != skeletonName || lastPreviewSkin != skinName)
        {
            SkeletonDataAsset data = LoadSkeletonData(skeletonName);
            if (data != null)
            {
                previewRenderer.Initialize(data, skeletonName, skinName);
                lastPreviewSkeleton = skeletonName;
                lastPreviewSkin = skinName;
                SetLayerRecursively(previewRenderer.gameObject, PREVIEW_SPINE_LAYER);
            }
        }

        // Đặt nhân vật preview tại vị trí của camera preview (vùng cô lập)
        previewRenderer.transform.position =
            previewSpineCamera.transform.position + new Vector3(0, 0, 10);

        // Scale chuẩn (không âm Y) cho chế độ No-Flip
        float finalScale = 16.5f * zoom;
        previewRenderer.transform.localScale = new Vector3(finalScale, finalScale, 1);

        previewRenderer.SetVisible(true);

        previewRenderer.SetDirection(1);
        UpdateAnimationByCharState(previewRenderer, c);
    }

    private void UpdateAnimationByCharState(SpineCharacterRenderer renderer, Char c)
    {
        string targetAnim = "Idle";
        bool loop = true;
        float animSpeed = 1.0f; // Mặc định tốc độ là 1.0f
        bool isCurrentlyAttacking = false;

        // 1. Chết hoặc Bất động (Ưu tiên cao nhất)
        if (c.statusMe == 14 || c.statusMe == 5 || c.cf == 23)
        {
            targetAnim = "Die";
            loop = false;
        }
        // 2. Gồng năng lượng (Charge)
        else if (c.isCharge || c.isStandAndCharge || c.isFlyAndCharge || c.cf == 17)
        {
            int skillId = -1;
            if (c.myskill != null && c.myskill.template != null)
            {
                skillId = c.myskill.template.id;
            }
            else
            {
                skillId = c.skillTemplateId;
            }

            loop = true;
            animSpeed = 1.0f; // Mặc định tốc độ là 1.0f

            if (skillId == 10) // Quả cầu kênh khí
            {
                if (c.posDisY <= 20)
                {
                    targetAnim = "Skill5_5";
                }
                else
                {
                    targetAnim = "ZSkill6";
                    animSpeed = 0.25f; // Giảm tốc độ để kéo dài thời gian chạy hoạt ảnh thêm 1s
                }
            }
            else if (skillId == 11) // Makankosappo
            {
                targetAnim = "ZSkill8";
            }
            else
            {
                targetAnim = "Skill5_5";
            }
        }
        // 3. Chạy (Run)
        else if (c.statusMe == 2)
        {
            targetAnim = "Run";
            loop = true;
        }
        // 4. Nhảy (Jump)
        else if (c.statusMe == 3 || c.statusMe == 9)
        {
            targetAnim = "Jump";
            loop = false;
        }
        // 5. Rơi (Fall)
        else if (c.statusMe == 4)
        {
            targetAnim = c.isFlyUp ? "Fly" : "Fall";
            loop = !c.isFlyUp;
        }
        // 6. Bay (Fly)
        else if (c.statusMe == 10)
        {
            targetAnim = "Fly";
            loop = true;
        }
        // 6.5. Trói - Đang giữ dây trói (holder)
        else if (c.holder)
        {
            targetAnim = "ZSkill5";
            loop = true;
            animSpeed = 1.0f;
        }
        // 7. Tấn công / Skill (Dựa trên skillPaint, status và skill đặc biệt)
        else if (
            c.isAttack
            || c.statusMe == 7
            || c.isAttFly
            || c.skillPaint != null
            || c.cf == 9
            || c.cf == 10
            || c.cf == 11
            || c.cf == 7
            || c.cf == 12
            || c.cf == 13
            || c.statusMe == 12
            || c.statusMe == 13
            || c.isPaintNewSkill
        )
        {
            isCurrentlyAttacking = true;

            // Kiểm tra xem đây có phải frame đầu tiên của đợt tấn công mới không
            bool wasPreviouslyAttacking = false;
            wasAttacking.TryGetValue(c.charID, out wasPreviouslyAttacking);
            bool isNewAttack = !wasPreviouslyAttacking;

            // Nếu là skill đặc biệt mới
            if (c.isPaintNewSkill)
            {
                if (c.idskillPaint == 24) // Super Kamejoko
                {
                    targetAnim = "ZSkill1";
                    if (c.stt == 0) // Gồng (chỉ 1 loop, thời gian kéo dài)
                    {
                        loop = false;
                        animSpeed = 0.25f;
                    }
                    else // Ra chiêu (stt == 1 hoặc 2)
                    {
                        loop = false;
                        animSpeed = 1.0f;
                    }
                }
                else if (c.idskillPaint == 25) // Cađíc liên hoàn chưởng
                {
                    if (c.stt == 0) // Gồng (chỉ 1 loop, thời gian kéo dài)
                    {
                        targetAnim = "Skill2_4";
                        loop = false;
                        animSpeed = 0.25f;
                    }
                    else // Ra chiêu (stt == 1 hoặc 2)
                    {
                        targetAnim = "Skill2_3";
                        loop = false;
                        animSpeed = 1.0f;
                    }
                }
                else if (c.idskillPaint == 26) // Ma phong ba
                {
                    targetAnim = "ZSkill11";
                    if (c.stt == 0) // Gồng (loop liên tục)
                    {
                        loop = true;
                        animSpeed = 1.0f;
                    }
                    else // Ra chiêu (stt == 1 hoặc 2)
                    {
                        loop = false;
                        animSpeed = 1.0f;
                    }
                }
                else
                {
                    targetAnim = GetAttackAnimationName(c, isNewAttack);
                    loop = false;
                }
            }
            else
            {
                // Skill bình thường
                targetAnim = GetAttackAnimationName(c, isNewAttack);
                loop = false;

                // Điều chỉnh tốc độ animation theo loại skill
                if (targetAnim.StartsWith("Skill2_"))
                {
                    animSpeed = 0.5f; // Bắn chưởng giảm tốc độ
                }
                else
                {
                    animSpeed = 1.0f; // Tất cả animation khác dùng tốc độ gốc
                }
            }
        }
        // 8. Bị thương (Hit)
        else if (c.statusMe == 8 || c.cf == 8)
        {
            targetAnim = "Hit";
            loop = false;
        }

        // Cập nhật trạng thái tấn công cho frame tiếp theo
        wasAttacking[c.charID] = isCurrentlyAttacking;
        // Xóa cache khi không còn tấn công (để lần tấn công sau random mới)
        if (!isCurrentlyAttacking)
        {
            cachedAttackAnim.Remove(c.charID);
        }

        renderer.SetAnimation(targetAnim, loop, animSpeed);
    }


    // Cache animation tấn công đã chọn cho mỗi nhân vật (tránh random mỗi frame)
    private Dictionary<int, string> cachedAttackAnim = new Dictionary<int, string>();
    // Theo dõi trạng thái tấn công frame trước để biết khi nào bắt đầu tấn công mới
    private Dictionary<int, bool> wasAttacking = new Dictionary<int, bool>();

    // Mảng animation cận chiến để random
    private static readonly string[] meleeAnimations = new string[]
    {
        "Combo1", "Combo2", "Combo3", "Combo4", "Combo5"
    };

    // Mảng animation chưởng để random
    private static readonly string[] kiBlastAnimations = new string[]
    {
        "Skill2_1", "Skill2_2"
    };

    // Mảng animation Cađíc liên hoàn chưởng để random
    private static readonly string[] cadicAnimations = new string[]
    {
        "Skill2_4", "Skill2_5"
    };

    /// <summary>
    /// Chọn random animation từ mảng, nhưng cache kết quả cho charId.
    /// Chỉ chọn mới khi chuyển từ non-attack sang attack (isNewAttack = true).
    /// </summary>
    private string PickCachedRandom(int charId, string[] pool, bool isNewAttack)
    {
        if (isNewAttack || !cachedAttackAnim.ContainsKey(charId))
        {
            string picked = pool[Random.Range(0, pool.Length)];
            cachedAttackAnim[charId] = picked;
            return picked;
        }
        return cachedAttackAnim[charId];
    }

    private string GetAttackAnimationName(Char c, bool isNewAttack)
    {
        int charId = c.charID;

        // Lấy skill template ID từ myskill (nếu có)
        int skillTemplateId = -1;
        if (c.myskill != null && c.myskill.template != null)
        {
            skillTemplateId = c.myskill.template.id;
        }

        // 0. Biến khỉ / Hóa hình / Biến hình (Ưu tiên cao nhất)
        if (c.isWaitMonkey || c.isWaitBienHinh)
        {
            return "ZSkill7";
        }
        if (c.skillPaint != null)
        {
            int id = c.skillPaint.id;
            if ((id >= 35 && id <= 41) || id == 105 || id == 165)
            {
                return "ZSkill7";
            }
        }

        // 1. Kiểm tra các skill đặc biệt dựa trên myskill.template.id
        switch (skillTemplateId)
        {
            case 6:  // Thái dương hạ san
                return "Skill5_1";

            case 7:  // Trị thương
                return "Skill5_4";

            case 8:  // Tái tạo năng lượng
                return "ZSkill7";

            case 10: // Quả cầu kênh khí
                return "ZSkill6";

            case 11: // Makankosappo
                return "ZSkill8";

            case 13: // Biến Khỉ
                return "ZSkill7";

            case 28: // Biến hình Super
                return "ZSkill7";

            case 14: // Tự phát nổ
                return "ZSkill7";

            case 19: // Khiên
                return "ZSkill7";

            case 20: // Dịch chuyển tức thời
                return "ZSkill9";

            case 23: // Trói
                return "ZSkill5";

            case 24: // Cađíc liên hoàn chưởng
                return PickCachedRandom(charId, cadicAnimations, isNewAttack);

            case 26: // Ma phong ba
                return "ZSkill11";
        }

        // 2. Nếu không phải skill đặc biệt, phân biệt melee vs chưởng theo skillPaint
        if (c.skillPaint != null)
        {
            int id = c.skillPaint.id;

            // Nhóm đấm cận chiến
            bool isMelee =
                (id >= 0 && id <= 6)
                || (id >= 14 && id <= 20)
                || (id >= 28 && id <= 34)
                || (id >= 63 && id <= 69)
                || (id >= 107 && id <= 109)
                || id == 164
                || id == 183
                || id == 186
                || id == 192;

            if (isMelee)
            {
                return PickCachedRandom(charId, meleeAnimations, isNewAttack);
            }

            // Nhóm bắn chưởng (mặc định)
            return PickCachedRandom(charId, kiBlastAnimations, isNewAttack);
        }

        // 3. Fallback cho frame bắn chưởng cơ bản (cf 12, 13)
        if (c.cf == 12 || c.cf == 13)
        {
            return PickCachedRandom(charId, kiBlastAnimations, isNewAttack);
        }

        // 4. Fallback cho frame đấm cận chiến
        if (c.cf == 9 || c.cf == 10 || c.cf == 11)
        {
            return PickCachedRandom(charId, meleeAnimations, isNewAttack);
        }

        return PickCachedRandom(charId, meleeAnimations, isNewAttack);
    }

    public SpineCharacterRenderer AddOrUpdatePetFollow(int playerId, string spineId)
    {
        string skeletonName = spineId;
        if (!spineId.StartsWith("character_") && !spineId.StartsWith("ship_"))
        {
            skeletonName = "ship_" + spineId;
        }

        if (petFollowCharacters.ContainsKey(playerId))
        {
            SpineCharacterRenderer existingRenderer = petFollowCharacters[playerId];
            if (existingRenderer != null && existingRenderer.currentSkeletonName != skeletonName)
            {
                RemovePetFollow(playerId);
            }
            else
            {
                return existingRenderer;
            }
        }

        SkeletonDataAsset skeletonData = LoadSkeletonData(skeletonName);
        if (skeletonData == null)
            return null;

        GameObject charObj = new GameObject($"SpinePetFollow_{playerId}");
        SetLayerRecursively(charObj, PET_SPINE_LAYER);

        SpineCharacterRenderer renderer = charObj.AddComponent<SpineCharacterRenderer>();
        renderer.Initialize(skeletonData, skeletonName, "default");

        SetLayerRecursively(charObj, PET_SPINE_LAYER);

        petFollowCharacters[playerId] = renderer;
        Debug.Log(
            $"[SpineCharacterManager] Created Spine Pet Follow for player {playerId}, pet: {skeletonName}"
        );
        return renderer;
    }

    public void RemovePetFollow(int playerId)
    {
        if (petFollowCharacters.ContainsKey(playerId))
        {
            SpineCharacterRenderer renderer = petFollowCharacters[playerId];
            if (renderer != null)
                Destroy(renderer.gameObject);
            petFollowCharacters.Remove(playerId);
        }
    }

    public void PaintSpineForPetFollow(mGraphics g, Assets.src.g.PetFollow pet)
    {
        if (petSpineTexture == null || pet == null || !pet.isSpine)
            return;

        int zoom = mGraphics.zoomLevel;
        int drawW = 100;
        int drawH = 100;
        int drawX = pet.cmx - drawW / 2;
        int drawY = pet.cmy - drawH + 20;

        int oldCX = g.clipX / zoom;
        int oldCY = g.clipY / zoom;
        int oldCW = g.clipW / zoom;
        int oldCH = g.clipH / zoom;
        bool oldIsClip = g.isClip;

        g.setClip(drawX, drawY, drawW, drawH);
        g.drawRenderTexture(petSpineTexture, -g.translateX / zoom, -g.translateY / zoom);

        if (oldIsClip)
            g.setClip(oldCX, oldCY, oldCW, oldCH);
        else
            g.isClip = false;
    }

    private void UpdatePetFollowPositions()
    {
        int zoom = mGraphics.zoomLevel;
        if (petFollowCharacters.Count == 0)
            return;

        List<int> toRemove = new List<int>();
        foreach (var kvp in petFollowCharacters)
        {
            int playerId = kvp.Key;
            SpineCharacterRenderer renderer = kvp.Value;

            Char c = GetCharById(playerId);
            if (c != null && c.petFollow != null && c.petFollow.isSpine)
            {
                float screenX = (float)(c.petFollow.cmx - GameScr.cmx) * zoom;
                float screenY =
                    (float)Screen.height
                    - (float)(c.petFollow.cmy - GameScr.cmy + GameCanvas.transY) * zoom;

                if (renderer != null)
                {
                    bool isGroundPet =
                        !string.IsNullOrEmpty(renderer.currentSkeletonName)
                        && renderer.currentSkeletonName.StartsWith("character_");

                    if (isGroundPet)
                    {
                        // Linh thú chạy dưới đất: Không bập bềnh hình sin, không lệch Y, đứng vững trên đất
                        renderer.transform.position = new Vector3(screenX, screenY, 0);

                        // Size pet spine
                        float finalScale = 40f * zoom;
                        renderer.transform.localScale = new Vector3(finalScale, finalScale, 1);

                        // Hướng lật cùng hướng di chuyển của player (không bị ngược như ship)
                        renderer.SetDirection(c.petFollow.dir);

                        // Cập nhật hoạt ảnh cho linh thú đất:
                        // 1. Chết (Die)
                        // 2. Di chuyển (Walk)
                        // 3. Đứng yên: luân phiên standby (Idle) và win (Win) sau mỗi vài giây
                        bool isDead = (
                            c.statusMe == 14
                            || c.statusMe == 5
                            || c.meDead
                            || (c.cHP <= 0 && c.cHP != -1)
                        );

                        if (isDead)
                        {
                            renderer.SetAnimation("Die", true);
                        }
                        else
                        {
                            bool isMoving = (
                                c.statusMe == 2
                                || c.statusMe == 3
                                || c.statusMe == 4
                                || c.statusMe == 9
                                || c.statusMe == 10
                            );
                            if (isMoving)
                            {
                                renderer.SetAnimation("Walk", true);
                            }
                            else
                            {
                                string idStr = renderer.currentSkeletonName.Replace("character_", "");
                                int.TryParse(idStr, out int spineId);
                                int baseId = 1000 + spineId;

                                // Luân phiên các hành động (tổng chu kỳ 18 giây)
                                // Thêm offset playerId để các pet không diễn cùng một lúc
                                float cycleTime = (Time.time + (playerId % 10)) % 18f;
                                string idleAnim = "Idle";

                                if (cycleTime < 6f) idleAnim = "Idle";
                                else if (cycleTime < 9f) idleAnim = "Win";
                                else if (cycleTime < 12f) idleAnim = $"skill_{baseId}00";
                                else if (cycleTime < 15f) idleAnim = $"skill_{baseId}01";
                                else idleAnim = $"skill_{baseId}02";

                                renderer.SetAnimation(idleAnim, true);
                            }
                        }
                    }
                    else
                    {
                        // Đối với phi thuyền Spine bay lơ lửng (Ship)
                        float floatOffset = Mathf.Sin(Time.time * 4f) * 6f * zoom;
                        renderer.transform.position = new Vector3(
                            screenX,
                            screenY + floatOffset,
                            0
                        );

                        float finalScale = 15f * zoom;
                        renderer.transform.localScale = new Vector3(finalScale, finalScale, 1);

                        // Đảo ngược hướng lật vì hướng của ship Spine đang ngược với player
                        renderer.SetDirection((c.petFollow.dir != 1) ? 1 : -1);

                        // Khi player di chuyển (Run, Fly, Jump, Fall, JumpFly) thì chạy action_1, đứng im chạy action_2
                        bool isMoving = (
                            c.statusMe == 2
                            || c.statusMe == 3
                            || c.statusMe == 4
                            || c.statusMe == 9
                            || c.statusMe == 10
                        );
                        string targetAnim = isMoving ? "action_1" : "action_2";
                        renderer.SetAnimation(targetAnim, true);
                    }

                    renderer.SetVisible(true);
                }
            }
            else
            {
                toRemove.Add(playerId);
            }
        }

        foreach (int id in toRemove)
        {
            RemovePetFollow(id);
        }
    }

    // ==================== SHIP FOLLOW (Slot 8 - Type 95) ====================
    // Kênh Spine độc lập cho Tàu bay Spine, song song với petFollow (slot 11).

    public SpineCharacterRenderer AddOrUpdateShipFollow(int playerId, string spineId)
    {
        string skeletonName = spineId;
        if (!spineId.StartsWith("ship_") && !spineId.StartsWith("character_"))
        {
            skeletonName = "ship_" + spineId;
        }

        if (shipFollowCharacters.ContainsKey(playerId))
        {
            SpineCharacterRenderer existingRenderer = shipFollowCharacters[playerId];
            if (existingRenderer != null && existingRenderer.currentSkeletonName != skeletonName)
            {
                RemoveShipFollow(playerId);
            }
            else
            {
                return existingRenderer;
            }
        }

        SkeletonDataAsset skeletonData = LoadSkeletonData(skeletonName);
        if (skeletonData == null)
            return null;

        GameObject charObj = new GameObject($"SpineShipFollow_{playerId}");
        SetLayerRecursively(charObj, PET_SPINE_LAYER);

        SpineCharacterRenderer renderer = charObj.AddComponent<SpineCharacterRenderer>();
        renderer.Initialize(skeletonData, skeletonName, "default");

        SetLayerRecursively(charObj, PET_SPINE_LAYER);

        shipFollowCharacters[playerId] = renderer;
        Debug.Log(
            $"[SpineCharacterManager] Created Spine Ship Follow for player {playerId}, ship: {skeletonName}"
        );
        return renderer;
    }

    public void RemoveShipFollow(int playerId)
    {
        if (shipFollowCharacters.ContainsKey(playerId))
        {
            SpineCharacterRenderer renderer = shipFollowCharacters[playerId];
            if (renderer != null)
                Destroy(renderer.gameObject);
            shipFollowCharacters.Remove(playerId);
        }
    }

    public void PaintSpineForShipFollow(mGraphics g, Assets.src.g.PetFollow ship)
    {
        if (petSpineTexture == null || ship == null || !ship.isSpine)
            return;

        int zoom = mGraphics.zoomLevel;
        int drawW = 100;
        int drawH = 100;
        int drawX = ship.cmx - drawW / 2;
        int drawY = ship.cmy - drawH + 20;

        int oldCX = g.clipX / zoom;
        int oldCY = g.clipY / zoom;
        int oldCW = g.clipW / zoom;
        int oldCH = g.clipH / zoom;
        bool oldIsClip = g.isClip;

        g.setClip(drawX, drawY, drawW, drawH);
        g.drawRenderTexture(petSpineTexture, -g.translateX / zoom, -g.translateY / zoom);

        if (oldIsClip)
            g.setClip(oldCX, oldCY, oldCW, oldCH);
        else
            g.isClip = false;
    }

    private void UpdateShipFollowPositions()
    {
        int zoom = mGraphics.zoomLevel;
        if (shipFollowCharacters.Count == 0)
            return;

        List<int> toRemove = new List<int>();
        foreach (var kvp in shipFollowCharacters)
        {
            int playerId = kvp.Key;
            SpineCharacterRenderer renderer = kvp.Value;

            Char c = GetCharById(playerId);
            if (c != null && c.shipFollow != null && c.shipFollow.isSpine)
            {
                float screenX = (float)(c.shipFollow.cmx - GameScr.cmx) * zoom;
                float screenY =
                    (float)Screen.height
                    - (float)(c.shipFollow.cmy - GameScr.cmy + GameCanvas.transY) * zoom;

                if (renderer != null)
                {
                    // Tàu bay luôn bay lơ lửng với hiệu ứng dao động sin nhẹ
                    float floatOffset = Mathf.Sin(Time.time * 4f) * 6f * zoom;
                    renderer.transform.position = new Vector3(
                        screenX,
                        screenY + floatOffset,
                        0
                    );

                    float finalScale = 15f * zoom;
                    renderer.transform.localScale = new Vector3(finalScale, finalScale, 1);

                    // Hướng tàu ngược với hướng player giống ship spine cũ
                    renderer.SetDirection((c.shipFollow.dir != 1) ? 1 : -1);

                    bool isMoving = (
                        c.statusMe == 2
                        || c.statusMe == 3
                        || c.statusMe == 4
                        || c.statusMe == 9
                        || c.statusMe == 10
                    );
                    string targetAnim = isMoving ? "action_1" : "action_2";
                    renderer.SetAnimation(targetAnim, true);

                    renderer.SetVisible(true);
                }
            }
            else
            {
                toRemove.Add(playerId);
            }
        }

        foreach (int id in toRemove)
        {
            RemoveShipFollow(id);
        }
    }

    public void ClearAll()
    {
        foreach (var r in spineCharacters.Values)
            if (r != null)
                Destroy(r.gameObject);
        spineCharacters.Clear();
        foreach (var r in petFollowCharacters.Values)
            if (r != null)
                Destroy(r.gameObject);
        petFollowCharacters.Clear();
        foreach (var r in shipFollowCharacters.Values)
            if (r != null)
                Destroy(r.gameObject);
        shipFollowCharacters.Clear();
    }

    private SkeletonDataAsset LoadSkeletonData(string skeletonName)
    {
        if (skeletonCache.ContainsKey(skeletonName))
            return skeletonCache[skeletonName];

        // Sử dụng SpineSkinManager để tìm path linh hoạt
        string path = SpineSkinManager.GetResourcePath(skeletonName);
        SkeletonDataAsset asset = Resources.Load<SkeletonDataAsset>(path);

        if (asset != null)
            skeletonCache[skeletonName] = asset;
        else
            Debug.LogError($"[Spine] Failed to load SkeletonData at: {path}");

        return asset;
    }
}
