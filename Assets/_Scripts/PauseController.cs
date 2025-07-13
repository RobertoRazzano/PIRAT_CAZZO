using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{

    public GameObject pauseMenu; // Assicurati che questo oggetto sia assegnato nell'Inspector



    void Start()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        else
        {
            Debug.LogError("PauseController: pauseMenu is not set!");
        }

        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pauseGame();

        }
    }

    public void continueGame()
    {
        Time.timeScale = 1;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void pauseGame()
    {
        Time.timeScale = 0;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void exitToMainMenu()
    {
        Time.timeScale = 1; // Ensure the game is not paused when returning to the main menu
        SceneManager.LoadScene(4); // Usa l'indice della scena
    }
}

