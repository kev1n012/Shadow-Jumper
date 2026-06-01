using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mainMenuBeha : MonoBehaviour
{
    public void changeScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("TowerScene");
    }
}
