using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader : MonoBehaviour
{
    [Header("Pengaturan Runtime")]
    [Tooltip("Jika diisi, Load() akan menggunakan nama scene ini.")]
    [SerializeField] private string sceneName = "";

    [Tooltip("Jika >= 0, Load() akan menggunakan index ini.")]
    [SerializeField] private int sceneBuildIndex = -1;

    #if UNITY_EDITOR
    [Header("Editor: Drag Scene dari Project ke sini")]
    [SerializeField] private SceneAsset sceneAsset = null;
    #endif

    // =============================================
    // FUNGSI-FUNGSI UTAMA UNTUK TOMBOL UI
    // =============================================

    /// <summary>
    /// Memuat scene secara normal (mengganti scene saat ini).
    /// </summary>
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

        Debug.LogError("[SceneLoader] Tidak ada scene yang ditentukan. Atur di Inspector.");
    }

    /// <summary>
    /// Memuat scene secara aditif (menumpuk di atas scene saat ini).
    /// </summary>
    public void LoadAdditive()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] sceneName belum di-set untuk LoadAdditive!");
            return;
        }
        Debug.Log($"[SceneLoader] Memuat scene '{sceneName}' secara aditif.");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// Menutup/unload scene yang namanya ditentukan di Inspector.
    /// </summary>
    public void Unload()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] sceneName belum di-set untuk Unload!");
            return;
        }
        Debug.Log($"[SceneLoader] Menutup scene '{sceneName}'.");
        SceneManager.UnloadSceneAsync(sceneName);
    }

    /// <summary>
    /// Kembali ke Main Menu dan me-reset sistem BGM (musik).
    /// </summary>
    public void ExitToMainMenuAndReset()
    {
        if (BGMController.instance != null)
        {
            // Hancurkan BGM_Manager yang lama
            Destroy(BGMController.instance.gameObject);
            // Kosongkan referensi instance agar yang baru bisa dibuat
            BGMController.instance = null;
        }
        
        // Pindah ke scene Main Menu
        Load(); 
    }

    // =============================================
    // FUNGSI BANTUAN
    // =============================================

    private void TryLoadByName(string name)
    {
        if (!IsSceneInBuildSettings(name))
        {
            Debug.LogWarning($"[SceneLoader] Scene '{name}' mungkin belum ada di Build Settings.");
        }
        Debug.Log($"[SceneLoader] Memuat scene dengan nama: {name}");
        SceneManager.LoadScene(name);
    }

    private void TryLoadByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[SceneLoader] Build index {index} di luar jangkauan.");
            return;
        }
        Debug.Log($"[SceneLoader] Memuat scene dengan build index: {index}");
        SceneManager.LoadScene(index);
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
    
    // =============================================
    // KHUSUS EDITOR
    // =============================================
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

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
        }
    }
    #endif
}