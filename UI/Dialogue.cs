using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// enum DialogueSequence{
//     EnterDungeon = {"wow","hey"},
//     FinishFirstRoom={"oh"},
//     EnterOuterHotSpring={"oh"}
// }
enum TriggerType{OnStart,OnCall}
public class Dialogue : MonoBehaviour
{
    // [SerializeField] DialogueSequence d;
    [SerializeField] TriggerType tt=TriggerType.OnStart;
    [SerializeField] string[] dialogueText;
    UI ui;
    // Start is called before the first frame update
    void Start()
    {
        ui = GameManager.Instance.ui;
        if(tt==TriggerType.OnStart)Call();
    }

    public void Call(){
        ui.Dialogue(dialogueText);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
