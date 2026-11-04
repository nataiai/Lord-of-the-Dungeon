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
    // public Spawner currentSpawner => currentRoom.spawner;
    public DungeonRoom initRoom;
    
    float tmp;
    public void _re(){

        map.currentRoom.gameObject.SetActive(false);
        initRoom.gameObject.SetActive(true);
        map.currentRoom = initRoom;
    }
    private void Awake() 
    { 
        if (Instance != null && Instance != this) {
            Destroy(this); 
            return;
        }
         
        Instance = this; 
        DontDestroyOnLoad(this);

        mainCam = Camera.main;

        uiCam = mainCam.transform.Find("UI Cam").GetComponent<Camera>();

        ui = FindFirstObjectByType<UI>(); // support when ui canvas presesnt inside the scene
        if(ui==null)ui = Instantiate(uiPrefab, new Vector2(0,0), Quaternion.identity);
        

        // Limit framerate to cinematic ...fps. https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/application/targetframerate
        QualitySettings.vSyncCount = 0; // Set vSyncCount to 0 so that using .targetFrameRate is enabled.
        //If vSyncCount == 1, rendering is synchronized to the vertical refresh rate of the display.
        Application.targetFrameRate = FPS;
        
    }

    void Start(){
        ui.UpdateMaxHP(player.maxHP);
        ui.UpdateHP(player.HP);
        soundPlayer = FindFirstObjectByType<SoundPlayer>();
    }
    bool _connectOnce=false;
    public void _Connect(){
        if(_connectOnce)return;
        _connectOnce = true;
        chat.channel = Data.cn;
        if(!testWithoutChat)chat.Connect();
    }

    void Update(){
        
        if(testWithoutChat && !Input.GetKeyDown(KeyCode.L)){
            if(tmp<Time.time){
                Spawn(mockEnemy);
                tmp = Time.time+testWithoutChatSpawnCooldown;
            }
        }

        if (Input.GetKeyDown(KeyCode.Semicolon))
            SceneManager.LoadScene("DungeonRoom");
    
        var summon = summondb.GetToSpawn();
        if(!testWithoutChat && summon!=null)Spawn(summon);
        // else print("wrong command");
    }
    void Spawn(Enemy enemy){
        currentRoom.Spawn(enemy,playerPos,spawnRangeFromPlayer);
    }

    public void TeleportSkill(int x, int y){
        if(true){ // TODO: check cooldown 
            Vector2 pos = new Vector2(x,y);
            if(map._findRoom(pos)==map.currentRoom&&Physics2D.OverlapPoint(pos)==null)player.transform.position = pos;
        }
    }

}