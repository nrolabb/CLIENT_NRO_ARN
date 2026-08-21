using Spine.Unity;
using UnityEngine;
using Game1;

public static class RongThanSpineController
{
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

    private static SkeletonDataAsset dataTraiDat;
    private static SkeletonDataAsset dataNamek;

    private static bool loadedTraiDat;
    private static bool loadedNamek;
    private static bool failedTraiDat;
    private static bool failedNamek;

    private static bool hiding;
    private static bool isCurrentNamek;

    public static bool IsAvailable(bool isNamek = false)
    {
        EnsureLoaded(isNamek);
        return isNamek ? (loadedNamek && !failedNamek) : (loadedTraiDat && !failedTraiDat);
    }

    public static void Show(int x, int y, bool isNamek = false)
    {
        mapX = x;
        mapY = y;
        hiding = false;
        
        if (!IsAvailable(isNamek)) return;
        
        EnsureOverlayCamera();
        
        if (instance == null)
        {
            instance = new GameObject("RongThanSpine");
            Object.DontDestroyOnLoad(instance);
            instance.layer = OverlayLayer;
            animation = instance.AddComponent<SkeletonAnimation>();
            MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = 100;
        }

        if (animation == null) return;

        SkeletonDataAsset targetData = isNamek ? dataNamek : dataTraiDat;
        if (animation.skeletonDataAsset != targetData || isCurrentNamek != isNamek || !animation.valid)
        {
            animation.skeletonDataAsset = targetData;
            animation.Initialize(true);
            isCurrentNamek = isNamek;
        }

        instance.SetActive(true);
        UpdatePosition();
        animation.AnimationState.ClearTracks();
        var entry = animation.AnimationState.SetAnimation(0, "rong_than_start", false);
        if (entry != null)
        {
            entry.TimeScale = 0.7f;
        }
        animation.AnimationState.AddAnimation(0, "rong_than_loop", true, 0f);
    }

    public static void Hide()
    {
        if (instance == null || animation == null) return;
        hiding = true;
        animation.AnimationState.ClearTracks();
        var entry = animation.AnimationState.SetAnimation(0, "rong_end", false);
        if (entry != null)
        {
            entry.TimeScale = 0.7f;
            entry.Complete += delegate
            {
                if (hiding && instance != null) instance.SetActive(false);
            };
        }
        else
        {
            if (instance != null) instance.SetActive(false);
        }
    }

    public static void Update()
    {
        if (instance != null && instance.activeSelf) UpdatePosition();
    }

    public static void DrawOverlay()
    {
        if (instance == null || !instance.activeSelf || overlayCamera == null) return;
        EnsureOverlayTexture();
        overlayCamera.Render();
        
        if (Event.current.type == EventType.Repaint)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture, ScaleMode.StretchToFill, true);
        }
    }

    private static void EnsureLoaded(bool isNamek)
    {
        if (isNamek)
        {
            if (loadedNamek || failedNamek) return;
            dataNamek = Resources.Load<SkeletonDataAsset>("Spine/RongThanNamek/rong_than_namek_SkeletonData");
            if (dataNamek == null || dataNamek.GetSkeletonData(true) == null)
            {
                Debug.LogWarning("[RongThanSpine] Khong tim thay asset Spine Namek; dung hieu ung rong cu.");
                failedNamek = true;
                return;
            }
            loadedNamek = true;
        }
        else
        {
            if (loadedTraiDat || failedTraiDat) return;
            dataTraiDat = Resources.Load<SkeletonDataAsset>("Spine/RongThanTraiDat/rong_than_traidat_SkeletonData");
            if (dataTraiDat == null || dataTraiDat.GetSkeletonData(true) == null)
            {
                Debug.LogWarning("[RongThanSpine] Khong tim thay asset Spine Trai Dat; dung hieu ung rong cu.");
                failedTraiDat = true;
                return;
            }
            loadedTraiDat = true;
        }
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
