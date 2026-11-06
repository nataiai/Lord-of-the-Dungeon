using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    public LayoutGroup healthUI;
    List<Image> heart = new();
    public Sprite[] healthSprite = new Sprite[2]; // full, empty
    public Image leftWeapon,rightWeapon;
    public LayoutGroup skillSlot;
    public List<Image> skills = new();
    public List<Image> skillsReload = new();
    public Image[] wepImages; //get by correct index
    [UnityEngine.Serialization.FormerlySerializedAs("d_textBox")]
    public GameObject enemyNameTextbox; 
    public GameObject dialogueTextbox;
    TMP_Text dialogueTextLabel;
    string[] dialogueTextToShow;
    public float dialogueSpeed = 30; // typewriter: letters per second
    float dialogueStart;
    TMP_Text enemyNameTextLabel;
    Coroutine d_hide;
    Image leftFill, rightFill;
    Player player => GameManager.Instance.player;

    void Awake(){
        heart.Clear(); skills.Clear(); skillsReload.Clear();
        foreach(Transform o in healthUI.transform)heart.Add(o.GetComponent<Image>());
        foreach(Transform o in skillSlot.transform){var img = o.GetComponent<Image>(); skills.Add(img); skillsReload.Add(img.transform.GetChild(0).GetComponent<Image>());}
        enemyNameTextLabel = enemyNameTextbox.GetComponentInChildren<TMP_Text>();
        dialogueTextLabel = dialogueTextbox.GetComponentInChildren<TMP_Text>();
        leftFill = leftWeapon.transform.Find("Fill").GetComponent<Image>();
        rightFill = rightWeapon.transform.Find("Fill").GetComponent<Image>();
    }
    void Start(){
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = GameManager.Instance.uiCam;
        UpdateWeap();
    }
    void Update(){
        leftFill.fillAmount=player.lWeapon.getReloadPercent();
        rightFill.fillAmount=player.rWeapon.getReloadPercent();
        UpdateSkill();
        UpdateDialogue();
    }

    public void ShowText(string txt){ // for 2 seconds; a new text restarts the timer
        enemyNameTextLabel.SetText(txt);
        if(d_hide!=null)StopCoroutine(d_hide);
        d_hide = StartCoroutine(d_foo());
    }
    IEnumerator d_foo(){
        enemyNameTextbox.SetActive(true);
        yield return new WaitForSeconds(2);
        enemyNameTextbox.SetActive(false);
    }
    public void UpdateMaxHP(int maxHP){ // heart[0] is the template and always shown
        Transform t = healthUI.transform;
        while(heart.Count<=maxHP)heart.Add(Instantiate(heart[0],t.position,Quaternion.identity,t));
        for(int i=1;i<heart.Count;i++)heart[i].enabled = i<maxHP;
    }
    public void UpdateHP(int HP){for(int i=0;i<heart.Count;i++)heart[i].sprite = healthSprite[i<HP?0:1];}
    public void UpdateSkill(){if(skillsReload.Count>0)skillsReload[0].fillAmount=GameManager.Instance.summondb.spawnSkill.getReloadPercent();}
    public void UpdateWeap(){
        if(player.lWeapon.weapon!=null)wepImages[0].sprite = player.lWeapon.weapon.uiWep;
        if(player.rWeapon.weapon!=null)wepImages[1].sprite = player.rWeapon.weapon.uiWep;
    }
    public void Dialogue(string[] text){
        dialogueTextToShow=text;
        dialogueStart=Time.time;
        GameManager.Instance.controller.attackLeft.isPressedOnce("dialogue");
    }
    void UpdateDialogue(){
        if(dialogueTextToShow==null||dialogueTextToShow.Length==0){
            dialogueTextbox.SetActive(false);
            return;
        }
        player.froze();
        player.invincibleTime = Time.time+1;
        player.lWeapon.lastUse = player.rWeapon.lastUse = Time.time; // clicking through dialogue doesn't attack
        dialogueTextbox.SetActive(true);
        dialogueTextLabel.SetText(dialogueTextToShow[0]);
        int shown = (int)((Time.time-dialogueStart)*dialogueSpeed); // typewriter
        dialogueTextLabel.maxVisibleCharacters = shown;
        if(GameManager.Instance.controller.attackLeft.isPressedOnce("dialogue")){
            if(shown<dialogueTextToShow[0].Length){dialogueStart-=9999;return;} // still typing: show the whole line
            dialogueStart=Time.time;
            if(dialogueTextToShow.Length==1)dialogueTextToShow=null;
            else dialogueTextToShow = dialogueTextToShow[1..];
        }
    }
}
