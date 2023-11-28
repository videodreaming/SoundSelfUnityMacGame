using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Text modeText; // Reference to the UI text displaying the current mode
    private int currentMode = 0; // 0: Preperation Session, 1: Integration Session, 2: Adjunctive Sessions, 3: Wisdom Session, 4: Passive Session 
    public string sceneToLoad; 

    void Start()
    {
        UpdateModeText();
    }

    void Update()
    {
        HandleModeSwitchingInput();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (currentMode == 0)
        {
            sceneToLoad = "PreperationSession"; // Set the correct scene name
            SceneManager.LoadScene(sceneToLoad);
        }
        else if (currentMode == 1)
        {
            sceneToLoad = "IntegrationSession"; // Set the correct scene name
            SceneManager.LoadScene(sceneToLoad);
        }
        else if (currentMode == 2)
        {
            sceneToLoad = "AdjunctiveSession"; // Set the correct scene name
            SceneManager.LoadScene(sceneToLoad);
        }
        else if (currentMode == 3)
        {
            sceneToLoad = "WisdomSession"; // Set the correct scene name
            SceneManager.LoadScene(sceneToLoad);
        }
        else if (currentMode == 4)
        {
            sceneToLoad = "PassiveSession"; // Set the correct scene name
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    void UpdateModeText()
    {
        // Update the UI text to display the current game mode
        modeText.text = "Mode: " + GetModeName(currentMode);
    }

    string GetModeName(int mode)
    {
        switch (mode)
        {
            case 0: return "Preperation Session";
            case 1: return "Integration Session";
            case 2: return "Adjunctive Session";
            case 3: return "Wisdom Session";
            case 4: return "Passive Session";
            default: return "Unknown Session";
        }
    }

    void HandleModeSwitchingInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SwitchMode(-1); // Switch to the previous mode
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SwitchMode(1); // Switch to the next mode
        }
    }

    private void SwitchMode(int direction)
    {
        currentMode = (currentMode + direction) % 5; // Assumes three game modes (easy, medium, hard)
        if (currentMode < 0)
        {
            currentMode = 4; // Wrap around to the last mode if moving left from the first mode
        }

        UpdateModeText();
    }
}

