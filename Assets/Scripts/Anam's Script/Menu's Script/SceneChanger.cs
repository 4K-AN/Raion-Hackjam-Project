// File: SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader : MonoBehaviour
{
    [Header("Runtime settings (used at runtime)")]
    [Tooltip("Jika non-empty, Load() akan menggunakan nama scene ini.")]
    [SerializeField] private string sceneName = "";

    [Tooltip("Jika >= 0, LoadByIndex() akan menggunakan index ini.")]
    [SerializeField] private int sceneBuildIndex = -1;

    #if UNITY_EDITOR
    [Header("Editor: drag a scene from Project here (editor-only)")]
    [SerializeField] private SceneAsset sceneAsset = null;
    #endif

    // -----------------------------
    // Methods meant to be assigned to Button OnClick()
    // -----------------------------

    // Panggil ini di Button -> On Click()
    public void Load()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            TryLoadByName(sceneName);
            return;
        }

        if (sceneBuildIndex >= 0)
        {
            TryLoadByIndex(sceneBuildIndex);
            return;
        }

        Debug.LogError("[SceneLoader] No scene specified. Set SceneAsset (Editor) or sceneName/sceneBuildIndex in Inspector.");
    }

    // Async version (loads in background)
    public void LoadAsync()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            StartCoroutine(LoadAsyncCoroutine(sceneName));
            return;
        }

        if (sceneBuildIndex >= 0)
        {
            StartCoroutine(LoadAsyncCoroutine(sceneBuildIndex));
            return;
        }

        Debug.LogError("[SceneLoader] No scene specified for async load.");
    }

    // Load next scene in build order
    public void LoadNextScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            TryLoadByIndex(next);
        else
            Debug.LogWarning("[SceneLoader] This is the last scene in Build Settings.");
    }

    // -----------------------------
    // Helpers
    // -----------------------------
    private void TryLoadByName(string name)
    {
        if (!IsSceneInBuildSettings(name))
        {
            Debug.LogWarning($"[SceneLoader] Scene '{name}' might not be in Build Settings. It may still load in editor but will fail in a build.");
        }
        Debug.Log($"[SceneLoader] Loading scene by name: {name}");
        SceneManager.LoadScene(name);
    }

    private void TryLoadByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[SceneLoader] Build index {index} is out of range (0 .. {SceneManager.sceneCountInBuildSettings - 1}).");
            return;
        }
        Debug.Log($"[SceneLoader] Loading scene by build index: {index}");
        SceneManager.LoadScene(index);
    }

    private System.Collections.IEnumerator LoadAsyncCoroutine(string name)
    {
        if (!IsSceneInBuildSettings(name))
            Debug.LogWarning($"[SceneLoader] Scene '{name}' may not be in Build Settings.");

        var op = SceneManager.LoadSceneAsync(name);
        while (!op.isDone) yield return null;
    }

    private System.Collections.IEnumerator LoadAsyncCoroutine(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[SceneLoader] Build index {index} is out of range for async load.");
            yield break;
        }
        var op = SceneManager.LoadSceneAsync(index);
        while (!op.isDone) yield return null;
    }

    private bool IsSceneInBuildSettings(string name)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName == name) return true;
        }
        return false;
    }

    #if UNITY_EDITOR
    // Auto-update sceneName and sceneBuildIndex when you drag a SceneAsset in Inspector
    private void OnValidate()
    {
        if (sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            // update build index if present
            sceneBuildIndex = -1;
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == path)
                {
                    sceneBuildIndex = i;
                    break;
                }
            }

            // if scene not in build settings, prompt to add it
            if (sceneBuildIndex == -1)
            {
                if (EditorUtility.DisplayDialog("Add scene to Build Settings?",
                    $"Scene '{sceneName}' is not in Build Settings. Add it now?", "Yes", "No"))
                {
                    var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                    list.Add(new EditorBuildSettingsScene(path, true));
                    EditorBuildSettings.scenes = list.ToArray();
                    sceneBuildIndex = EditorBuildSettings.scenes.Length - 1;
                    Debug.Log($"[SceneLoader] Added '{sceneName}' to Build Settings at index {sceneBuildIndex}.");
                }
            }

            EditorUtility.SetDirty(this);
        }
    }
    #endif
}
