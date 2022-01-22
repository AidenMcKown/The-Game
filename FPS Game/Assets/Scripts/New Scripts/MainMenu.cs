using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public MenuLauncher launcher;


    public void JoinMatch()
    {
        //SceneManager.LoadScene("Map");
        launcher.Join();
    }

    
    public void CreateMatch()
    {
        launcher.Create();
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}
