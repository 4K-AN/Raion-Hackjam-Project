using UnityEngine;

public class ExitButton : MonoBehaviour
{
   
    public void ExitGame()
    {
        Debug.Log("[ExitButton] Quit game called.");

#if UNITY_EDITOR
       
        UnityEditor.EditorApplication.isPlaying = false;
#else
        
        Application.Quit();
#endif
    }
}
