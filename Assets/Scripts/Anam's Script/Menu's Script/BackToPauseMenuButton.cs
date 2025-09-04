using UnityEngine;

public class BackToPauseMenuButton : MonoBehaviour
{
    public SceneLoader sceneLoader; 
    public string sceneToUnload; 

    public void OnClick()
    {
       
        if (PauseManager.instance != null)
        {
            PauseManager.instance.ShowPausePanel();
        }

      
        sceneLoader.Unload();
    }
}