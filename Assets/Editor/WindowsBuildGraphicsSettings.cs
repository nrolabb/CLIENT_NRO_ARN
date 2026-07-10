#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;

[InitializeOnLoad]
internal static class WindowsBuildGraphicsSettings
{
    static WindowsBuildGraphicsSettings()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.StandaloneWindows64,
            new[] { GraphicsDeviceType.Direct3D11 });
    }
}
#endif
