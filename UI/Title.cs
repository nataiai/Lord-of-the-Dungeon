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

    public void SubmitInput()=>resultDisplay.text = "You entered: " + userInput.text;
    public void EnterDungeon(){SubmitInput();SceneManager.LoadScene("DungeonRoom");}
}
