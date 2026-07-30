using UnityEngine;
using UnityEngine.UI;

public class SpineDebugInfo : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[SpineDebug] Debug Starter Active");
    }

    void Update()
    {
        if (Time.frameCount % 600 == 0)
        {
            Camera[] cameras = Camera.allCameras;
            Debug.Log($"[SpineDebug] Total Cameras: {cameras.Length}");
            foreach (var cam in cameras)
            {
                Debug.Log($"[SpineDebug] Camera: {cam.name}, renderingPath: {cam.actualRenderingPath}, cullingMask: {cam.cullingMask}");
            }

            Canvas[] canvases = FindObjectsOfType<Canvas>();
            Debug.Log($"[SpineDebug] Total Canvases: {canvases.Length}");
            foreach (var canv in canvases)
            {
                Debug.Log($"[SpineDebug] Canvas: {canv.name}, renderMode: {canv.renderMode}, enabled: {canv.enabled}, alpha: {canv.GetComponent<CanvasGroup>()?.alpha ?? 1f}");
            }
        }
    }
}
