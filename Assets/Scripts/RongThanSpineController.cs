using Spine.Unity;
using UnityEngine;
using Game1;

public static class RongThanSpineController
{
    private const string AssetPath = "Spine/RongThanTraiDat/rong_than_traidat";
    private const int OverlayLayer = 31;
    private const float SkeletonScale = 0.385f;

    private static GameObject instance;
    private static SkeletonAnimation animation;
    private static Camera overlayCamera;
    private static RenderTexture overlayTexture;
    private static int textureWidth;
    private static int textureHeight;
    private static int mapX;
    private static int mapY;
    private static bool loaded;
    private static bool failed;
    private static bool hiding;

    public static bool IsAvailable()
    {
        EnsureLoaded();
        return loaded && !failed;
    }

    public static void Show(int x, int y)
    {
        mapX = x;
        mapY = y;
        hiding = false;
        if (!IsAvailable()) return;
        if (instance == null) CreateInstance();
        if (instance == null || animation == null) return;

        instance.SetActive(true);
        UpdatePosition();
        animation.AnimationState.ClearTracks();
        var entry = animation.AnimationState.SetAnimation(0, "rong_than_start", false);
        entry.TimeScale = 0.7f;
        animation.AnimationState.AddAnimation(0, "rong_than_loop", true, 0f);
    }

    public static void Hide()
    {
        if (instance == null || animation == null) return;
        hiding = true;
        animation.AnimationState.ClearTracks();
        var entry = animation.AnimationState.SetAnimation(0, "rong_end", false);
        entry.TimeScale = 0.7f;
        entry.Complete += delegate
        {
            if (hiding && instance != null) instance.SetActive(false);
        };
    }

    public static void Update()
    {
        if (instance != null && instance.activeSelf) UpdatePosition();
    }

    private static Material drawMaterial;

    public static void DrawOverlay()
    {
        if (instance == null || !instance.activeSelf || overlayCamera == null) return;
        EnsureOverlayTexture();
        overlayCamera.Render();
        
        if (Event.current.type == EventType.Repaint)
        {
            if (drawMaterial == null)
            {
                Shader shader = Shader.Find("Spine/Skeleton");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                drawMaterial = new Material(shader);
            }
            Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, drawMaterial);
        }
    }

    private static void EnsureLoaded()
    {
        if (loaded || failed) return;
        TextAsset skel = Resources.Load<TextAsset>(AssetPath + ".skel");
        TextAsset atlas = Resources.Load<TextAsset>(AssetPath + ".atlas");
        Texture2D texture = Resources.Load<Texture2D>(AssetPath);
        if (skel == null || atlas == null || texture == null)
        {
            Debug.LogWarning("[RongThanSpine] Khong tim thay asset Spine; dung hieu ung rong cu.");
            failed = true;
            return;
        }

        Shader shader = Shader.Find("Spine/Skeleton") ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader) { mainTexture = texture };
        SpineAtlasAsset atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(atlas, new Texture2D[] { texture }, material, true);
        SkeletonDataAsset data = SkeletonDataAsset.CreateRuntimeInstance(skel, atlasAsset, true, SkeletonScale);
        if (data == null || data.GetSkeletonData(true) == null)
        {
            failed = true;
            return;
        }

        loaded = true;
        EnsureOverlayCamera();
        instance = new GameObject("RongThanTraiDatSpine");
        Object.DontDestroyOnLoad(instance);
        instance.layer = OverlayLayer;
        animation = SkeletonAnimation.AddToGameObject(instance, data);
        animation.Initialize(true);
        MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sortingOrder = 100;
        instance.SetActive(false);
    }

    private static void CreateInstance()
    {
        EnsureLoaded();
    }

    private static void EnsureOverlayCamera()
    {
        if (overlayCamera != null) return;
        GameObject cameraObject = new GameObject("RongThanSpineOverlayCamera");
        Object.DontDestroyOnLoad(cameraObject);
        overlayCamera = cameraObject.AddComponent<Camera>();
        overlayCamera.enabled = false;
        overlayCamera.orthographic = true;
        overlayCamera.clearFlags = CameraClearFlags.SolidColor;
        overlayCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        overlayCamera.cullingMask = 1 << OverlayLayer;
        overlayCamera.nearClipPlane = -1000f;
        overlayCamera.farClipPlane = 1000f;
        overlayCamera.transform.position = new Vector3(0f, 0f, -10f);
        EnsureOverlayTexture();
    }

    private static void EnsureOverlayTexture()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (overlayTexture != null && textureWidth == width && textureHeight == height) return;
        if (overlayTexture != null)
        {
            overlayTexture.Release();
            Object.Destroy(overlayTexture);
        }
        textureWidth = width;
        textureHeight = height;
        overlayTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
        overlayTexture.filterMode = FilterMode.Point;
        overlayTexture.Create();
        overlayCamera.targetTexture = overlayTexture;
        overlayCamera.orthographicSize = height * 0.5f;
        overlayCamera.allowMSAA = false;
    }

    private static void UpdatePosition()
    {
        float zoom = mGraphics.zoomLevel;
        int cx = mapX;
        int cy = mapY;
        float x = Mathf.Round((cx - GameScr.cmx) * zoom - Screen.width * 0.5f);
        float y = Mathf.Round(Screen.height * 0.5f - (cy - GameScr.cmy + GameCanvas.transY) * zoom);
        instance.transform.position = new Vector3(x, y, 0f);
    }
}
