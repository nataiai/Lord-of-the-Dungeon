using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mini map: a small map in the top right all the time. M (gamepad Select): the big map, moved with the move keys / stick.
// Shows rooms you've been in, doors, a ? behind each door to a room you haven't entered, and you. Made by UI.Start.
// The room you're in is filmed live into its picture; a room you left keeps its last one.
// A camera can't see a hidden room: one you haven't entered is only a ? past its door.
// Experimental.bigMapPauses: the big map pauses the game.
public class MiniMap : MonoBehaviour
{
    [Tooltip("Pixels per world unit on the corner map")]
    public float miniScale = 4;
    [Tooltip("Pixels per world unit on the big map")]
    public float bigScale = 10;
    [Tooltip("Big map: world units per second the move keys move it")]
    public float panSpeed = 20;
    public Vector2 miniSize = new(220,160);
    [Tooltip("Pixels per world unit of a room's picture")]
    public float pixelsPerUnit = 8;
    public Color current = new(1,1,1,.9f), visited = new(.6f,.6f,.6f,.8f);
    public Color door = new(1,.8f,.2f), doorDone = new(.5f,.5f,.5f), you = Color.red;

    RectTransform mini, big, content, me;
    readonly HashSet<DungeonRoom> been = new();
    readonly Dictionary<DungeonRoom,UnityEngine.UI.Image> rooms = new();
    readonly Dictionary<RoomTrigger,UnityEngine.UI.Image> doors = new();
    readonly Dictionary<RoomTrigger,GameObject> unknown = new(); // a ? on the far side of a door
    readonly Dictionary<DungeonRoom,RenderTexture> pictures = new();
    Camera cam;
    DungeonRoom filming;
    Vector2 look; // big map: how far it moved from the player
    bool open;

    void Start(){
        var canvas = new GameObject("MiniMap", typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        mini = Panel(canvas.transform, Vector2.one, Vector2.one, Vector2.one, miniSize, new Vector2(-10,-10)); // top right
        big = Panel(canvas.transform, Vector2.one*.1f, Vector2.one*.9f, Vector2.one*.5f, Vector2.zero, Vector2.zero);
        big.gameObject.SetActive(false);
        content = new GameObject("content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(mini, false);
        me = Box(Vector2.zero, Vector2.one*1.5f, you, "you").rectTransform;
    }
    void OnDestroy(){
        if(open)Time.timeScale = 1;
        foreach(var rt in pictures.Values)rt.Release();
        if(cam)Destroy(cam.gameObject);
    }

    void Update(){
        var gm = GameManager.Instance;
        var map = gm.map;
        if(map==null || map.currentRoom==null || map.roomTrigger==null)return;
        if(been.Add(map.currentRoom))Refresh(map);
        if(filming!=map.currentRoom)Film(map.currentRoom);
        if(gm.controller.map.isPressedOnce("minimap"))Open(!open);
        if(open){
            look += gm.controller.movement * panSpeed * Time.unscaledDeltaTime;
            gm.player.froze(); // no walking or attacks, like a dialogue
            gm.player.lWeapon.lastUse = gm.player.rWeapon.lastUse = Time.time;
        }
        foreach(var r in rooms)r.Value.color = r.Key==map.currentRoom ? current : visited;
        foreach(var u in unknown)u.Value.SetActive(!been.Contains(Room(u.Key)));
        foreach(var d in doors)d.Value.color = map.triggerMap.TryGetValue(d.Key, out var to) && to && been.Contains(Room(to)) ? doorDone : door;
        Vector2 p = gm.player.transform.position;
        me.anchoredPosition = p;
        float scale = open ? bigScale : miniScale;
        content.localScale = Vector3.one*scale;
        content.anchoredPosition = -(p+look)*scale;
    }
    void Open(bool on){
        open = on;
        look = Vector2.zero;
        big.gameObject.SetActive(on);
        mini.gameObject.SetActive(!on);
        content.SetParent(on ? big : mini, false);
        if(Experimental.I.bigMapPauses)Time.timeScale = on ? 0 : 1;
    }

    // a new room entered: it, its doors, and the rooms behind them
    void Refresh(DungeonMap map){
        AddRoom(map.currentRoom);
        foreach(var t in map.roomTrigger){
            if(t==null || doors.ContainsKey(t) || !been.Contains(Room(t)))continue;
            doors[t] = Box(t.transform.position, Vector2.one, door, "door");
            if(map.triggerMap.TryGetValue(t, out var to) && to && !unknown.ContainsKey(to))unknown[to] = Mark(to.transform.position);
        }
        me.SetAsLastSibling();
    }
    void Film(DungeonRoom r){ // the camera films this room into its picture
        filming = r;
        var box = rooms[r].rectTransform;
        if(!pictures.TryGetValue(r, out var rt)){
            Vector2 size = box.sizeDelta*pixelsPerUnit;
            rt = pictures[r] = new RenderTexture(Mathf.Max(1,(int)size.x), Mathf.Max(1,(int)size.y), 16);
            var pic = new GameObject(r.name+" picture", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
            pic.transform.SetParent(content, false);
            pic.rectTransform.anchoredPosition = box.anchoredPosition;
            pic.rectTransform.sizeDelta = box.sizeDelta;
            pic.texture = rt;
            pic.raycastTarget = false;
            pic.transform.SetSiblingIndex(rooms.Count); // over the room boxes, under doors and you
        }
        var main = GameManager.Instance.mainCam;
        if(cam==null){
            cam = new GameObject("MiniMap camera").AddComponent<Camera>();
            cam.CopyFrom(main);
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
        }
        cam.targetTexture = rt;
        cam.orthographicSize = box.sizeDelta.y/2;
        cam.transform.position = new Vector3(box.anchoredPosition.x, box.anchoredPosition.y, main.transform.position.z);
    }
    static DungeonRoom Room(RoomTrigger t) => t.transform.parent.GetComponent<DungeonRoom>(); // like DungeonMap.switchRoom
    void AddRoom(DungeonRoom r){
        if(r==null || rooms.ContainsKey(r))return;
        var b = Floor(r);
        rooms[r] = Box(b.center, b.size, visited, r.name);
        rooms[r].transform.SetAsFirstSibling(); // under doors and you
    }
    static Bounds Floor(DungeonRoom r){ // its floor tilemap (the Spawner's): known while the room is hidden too
        var s = r.GetComponentInChildren<Spawner>(true);
        if(s==null)return new Bounds(r.transform.position, Vector3.one*10);
        var tm = s.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        tm.CompressBounds();
        var b = tm.localBounds;
        return new Bounds(tm.transform.TransformPoint(b.center), Vector3.Scale(b.size, tm.transform.lossyScale));
    }

    UnityEngine.UI.Image Box(Vector2 at, Vector2 size, Color c, string name){
        var img = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        img.transform.SetParent(content, false);
        img.rectTransform.anchoredPosition = at;
        img.rectTransform.sizeDelta = size;
        img.color = c;
        img.raycastTarget = false;
        return img;
    }
    GameObject Mark(Vector2 at){ // a ?
        var t = new GameObject("?", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
        t.transform.SetParent(content, false);
        t.rectTransform.anchoredPosition = at;
        t.rectTransform.sizeDelta = Vector2.one*3;
        t.text = "?";
        t.fontSize = 3;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.color = door;
        t.raycastTarget = false;
        return t.gameObject;
    }
    static RectTransform Panel(Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size, Vector2 at){
        var rt = new GameObject("map", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.RectMask2D)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
        rt.sizeDelta = size; rt.anchoredPosition = at;
        var bg = rt.GetComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0,0,0,.5f);
        bg.raycastTarget = false;
        return rt;
    }
}
