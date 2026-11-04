using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{

    public LayoutGroup healthUI;
    List<Image> heart = new();
    public Sprite[] healthSprite = new Sprite[2];
    public Image leftWeapon,rightWeapon;
    public LayoutGroup skillSlot;
    public List<Image> skills = new();
    public List<Image> skillsReload = new();
    // public Image leftWepSymbol, rightWepSymbol;
    // public Image[] wepSymbol => new[]{leftWepSymbol,rightWepSymbol};
    public Image[] wepImages; //get by correct index
    public GameObject d_textBox;
    TMP_Text d_textLabel;
    Player player => GameManager.Instance.player;

    // string d_txt; // d mean dirty, dirty code
    public void ShowText(string txt){
        // d_txt=txt;
        d_textLabel.SetText( txt);
        StartCoroutine(d_foo());
    }
    IEnumerator d_foo(){
        d_textBox.SetActive(true);
        yield return new WaitForSeconds(2);
        d_textBox.SetActive(false);
    }

    void Awake(){
        heart.Clear();
        foreach(Transform o in healthUI.transform)heart.Add(o.GetComponent<Image>());
        skills.Clear();
        foreach(Transform o in skillSlot.transform)skills.Add(o.GetComponent<Image>());
        skillsReload.Clear();
        foreach(Image i in skills)skillsReload.Add(i.transform.GetChild(0).GetComponent<Image>());
        d_textLabel = d_textBox.GetComponentInChildren<TMP_Text>();
    }

    public void UpdateMaxHP(int maxHP){
        int diff = maxHP-heart.Count+1;
        Transform _t = healthUI.transform;
        while(diff-->0)heart.Add(Instantiate(heart[0],_t.position,Quaternion.identity,_t));
        for(int i=0;++i<heart.Count;){
            heart[i].enabled = (i<maxHP);
        }
    }

    public void UpdateHP(int HP){
        int cnt=0;
        foreach(var i in heart){
            if(cnt++>HP-1)i.sprite = healthSprite[1];
            else i.sprite = healthSprite[0];
        }
    }

    private void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = GameManager.Instance.uiCam;
        UpdateWeap();
    }

    void Update(){
        leftWeapon.transform.Find("Fill").GetComponent<Image>().fillAmount=player.lWeapon.getReloadPercent();
        rightWeapon.transform.Find("Fill").GetComponent<Image>().fillAmount=player.rWeapon.getReloadPercent();
        UpdateSkill();
    }
    public void UpdateSkill(){
        if(skillsReload.Count>0)skillsReload[0].fillAmount=GameManager.Instance.summondb.spawnSkill.getReloadPercent();
    }
    public void UpdateWeap(){
        if(player.lWeapon.weapon!=null)wepImages[0].sprite = player.lWeapon.weapon.uiWep;
        if(player.rWeapon.weapon!=null)wepImages[1].sprite = player.rWeapon.weapon.uiWep;
    }
}
