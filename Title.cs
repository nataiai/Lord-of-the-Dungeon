using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Title : MonoBehaviour
{
    [SerializeField] AudioSource myAudio;
    [SerializeField] AudioClip titleMusic;
    public InputField userInput;
    public Text resultDisplay;

    // Start is called before the first frame update
    public void SubmitInput()
    {
        string enteredText = userInput.text;
        resultDisplay.text = "You entered: " + enteredText;
    }
    public void EnterDungeon()
    {
        SubmitInput();
        SceneManager.LoadScene("DungeonRoom");
    }
}
