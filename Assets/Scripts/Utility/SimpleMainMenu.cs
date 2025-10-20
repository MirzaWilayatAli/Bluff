using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleMainMenu : MonoBehaviour
{
    public void LoadLevel(string levelName)
    {
           SceneManager.LoadScene(levelName);
    }
    
    public void QuitGame() 
    { 
        Application.Quit();
        
        // For testing in editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
