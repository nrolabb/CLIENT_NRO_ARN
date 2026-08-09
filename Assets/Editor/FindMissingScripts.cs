using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    public static void FindInScene()
    {
        GameObject[] go = Object.FindObjectsOfType<GameObject>(true);
        int go_count = 0, components_count = 0, missing_count = 0;
        string resultStr = "";
        foreach (GameObject g in go)
        {
            go_count++;
            Component[] components = g.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                components_count++;
                if (components[i] == null)
                {
                    missing_count++;
                    string s = g.name;
                    Transform t = g.transform;
                    while (t.parent != null) 
                    {
                        s = t.parent.name + "/" + s;
                        t = t.parent;
                    }
                    resultStr += "\n- " + s;
                }
            }
        }
        
        if (missing_count > 0)
        {
            Debug.LogError(string.Format("Searched {0} GameObjects, {1} components, found {2} missing scripts on the following objects:{3}", go_count, components_count, missing_count, resultStr));
        }
        else
        {
            Debug.Log("No missing scripts found.");
        }
    }

    [MenuItem("Tools/Remove Missing Scripts In Scene")]
    public static void RemoveMissing()
    {
        GameObject[] go = Object.FindObjectsOfType<GameObject>(true);
        int totalRemoved = 0;
        foreach (GameObject g in go)
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
            if (count > 0)
            {
                totalRemoved += count;
                Debug.Log($"Removed {count} missing script(s) from {g.name}", g);
            }
        }
        Debug.Log($"Cleanup complete. Removed a total of {totalRemoved} missing scripts.");
    }
}
