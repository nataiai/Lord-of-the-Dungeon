using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager: MonoBehaviour{
    public bool testWithoutChat;
    public float testWithoutChatSpawnCooldown = 5f;
    public float spawnRangeFromPlayer=3;
    public int FPS = 60;
    public static GameManager Instance { get; private set; }
    public Player player;
    public UI ui;
    public Enemy mockEnemy;
    public Controller controller;
    public Vector2 playerPos => player.transform.position;
    public Camera uiCam;
    public Camera mainCam;
    public UI uiPrefab;
    public TwitchChat chat;
    public SimpleSummonDB summondb => chat.summondb;
    public List<Enemy> summonableEnemy => summondb.summonableEnemy;
    public DungeonMap map;
    public DungeonMap dungeonMap => map;
    public DungeonRoom currentRoom => map.currentRoom;
    public List<ChatType> availableSkill;
    SoundPlayer soundPlayer;
    public void UpdateMusic()=>soundPlayer.UpdateMusic();
    public DungeonRoom initRoom;
    float tmp;
    bool _connectOnce=false;

    void Awake(){
        if(Instance != null && Instance != this){Destroy(this);return;}
        Instance = this;
        DontDestroyOnLoad(this);
        mainCam = Camera.main;
        uiCam = mainCam.transform.Find("UI Cam").GetComponent<Camera>();
        ui = FindFirstObjectByType<UI>(); // use the UI in the scene, or make one
        if(ui==null)ui = Instantiate(uiPrefab, new Vector2(0,0), Quaternion.identity);
        QualitySettings.vSyncCount = 0; // vSync off, so targetFrameRate works
        Application.targetFrameRate = FPS;
    }
    void Start(){
        ui.UpdateMaxHP(player.maxHP);
        ui.UpdateHP(player.HP);
        soundPlayer = FindFirstObjectByType<SoundPlayer>();
    }
    void Update(){
        if(testWithoutChat && !Input.GetKeyDown(KeyCode.L) && tmp<Time.time){Spawn(mockEnemy);tmp = Time.time+testWithoutChatSpawnCooldown;}
        var summon = summondb.GetToSpawn();
        if(!testWithoutChat && summon!=null)Spawn(summon);
    }
    void Spawn(Enemy enemy)=>currentRoom.Spawn(enemy,playerPos,spawnRangeFromPlayer);

    public void _Connect(){ // join chat (Twitch or YouTube), once
        if(_connectOnce)return;
        _connectOnce = true;
        chat.channel = Data.cn;
        if(!testWithoutChat)chat.Connect();
    }
    public void _re(){ // back to the first room
        map.currentRoom.gameObject.SetActive(false);
        initRoom.gameObject.SetActive(true);
        map.currentRoom = initRoom;
    }
    public void TeleportSkill(int x, int y){
        if(true){ // TODO: check cooldown
            Vector2 pos = new Vector2(x,y);
            if(map._findRoom(pos)==map.currentRoom&&Physics2D.OverlapPoint(pos)==null)player.transform.position = pos;
        }
    }
}
