using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum DialogueSequence{None, EnterDungeon, FinishFirstRoom, EnterOuterHotSpring}
public static class Dialogues{
    public static readonly Dictionary<DialogueSequence,string[]> text = new(){
        [DialogueSequence.EnterDungeon] = new[]{
            "Went out of the dungeon to eat currywurst with a girl and fell in love...",
            "Then came back to find all my warriors turned into skeletons by a lizard my sworn brother adopted!",
            "Havelagandr!",
        },
        [DialogueSequence.FinishFirstRoom] = new[]{
            "Now those roaming soul of my subordinate are around our dungeon. They should help me in reclaiming my throne! ",
            "I've been nice to them ever since. I even shorten their shift to 23hrs a day! There's no way they will turn against me!",
        },
        [DialogueSequence.EnterOuterHotSpring] = new[]{
            "good thing I built a personal spa room here. those souls should thank me I turn their village to something more useful.",
        },
    };
}
enum TriggerType{OnStart,OnCall}
public class Dialogue : MonoBehaviour
{
    [SerializeField] DialogueSequence sequence=DialogueSequence.None;
    [SerializeField] TriggerType tt=TriggerType.OnStart;
    [Tooltip("Used only when Sequence is None")]
    [SerializeField] string[] dialogueText;
    UI ui;
    // Start is called before the first frame update
    void Start()
    {
        ui = GameManager.Instance.ui;
        if(tt==TriggerType.OnStart)Call();
    }

    public void Call(){
        ui.Dialogue(sequence==DialogueSequence.None?dialogueText:Dialogues.text[sequence]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
