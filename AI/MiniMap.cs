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
    [Tooltip("Tint of the room you're in / rooms you've left (white = the room's own colors)")]
    public Color current = Color.white, visited = Color.white;
    public Color door = new(1,.8f,.2f), doorDone = new(.5f,.5f,.5f), you = Color.red;

    RectTransform mini, big, content, me;
    readonly HashSet<DungeonRoom> been = new();
    readonly Dictionary<DungeonRoom,UnityEngine.UI.Image> rooms = new();
    readonly Dictionary<RoomTrigger,UnityEngine.UI.Image> doors = new();
    readonly Dictionary<RoomTrigger,GameObject> unknown = new(); // a ? on the far side of a door
    readonly Dictionary<DungeonRoom,UnityEngine.UI.RawImage> pictures = new();
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
        if(open){Time.timeScale = 1; if(GameManager.Instance && GameManager.Instance.player)GameManager.Instance.player.enabled = true;}
        foreach(var p in pictures.Values)((RenderTexture)p.texture).Release();
        if(cam)Destroy(cam.gameObject);
    }

    void Update(){
        var gm = GameManager.Instance;
        var map = gm.map;
        if(map==null || map.currentRoom==null || map.roomTrigger==null)return;
        mini.gameObject.SetActive(!open && Experimental.I.miniMap);
        if(been.Add(map.currentRoom))Refresh(map);
        if(filming!=map.currentRoom)Film(map.currentRoom);
        if(gm.controller.map.isPressedOnce("minimap"))Open(!open);
        if(open)look += gm.controller.movement * panSpeed * Time.unscaledDeltaTime;
        foreach(var r in pictures)r.Value.color = r.Key==map.currentRoom ? current : visited;
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
        content.SetParent(on ? big : mini, false);
        if(Experimental.I.bigMapPauses)Time.timeScale = on ? 0 : 1;
        var player = GameManager.Instance.player;
        player.enabled = !on; // no walking or attacks; weapon cooldowns untouched
        if(on)player.rb.velocity = Vector2.zero;
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
        if(!pictures.TryGetValue(r, out var pic)){
            Vector2 size = box.sizeDelta*pixelsPerUnit;
            pic = pictures[r] = new GameObject(r.name+" picture", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
            pic.transform.SetParent(content, false);
            pic.rectTransform.anchoredPosition = box.anchoredPosition;
            pic.rectTransform.sizeDelta = box.sizeDelta;
            pic.texture = new RenderTexture(Mathf.Max(1,(int)size.x), Mathf.Max(1,(int)size.y), 16);
            pic.raycastTarget = false;
            Sort();
        }
        var main = GameManager.Instance.mainCam;
        if(cam==null){
            cam = new GameObject("MiniMap camera").AddComponent<Camera>();
            cam.CopyFrom(main);
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
        }
        cam.targetTexture = (RenderTexture)pic.texture;
        cam.ResetAspect(); // the picture's shape, not the screen's
        cam.orthographicSize = box.sizeDelta.y/2;
        cam.transform.position = new Vector3(box.anchoredPosition.x, box.anchoredPosition.y, main.transform.position.z);
    }
    static DungeonRoom Room(RoomTrigger t) => t.transform.parent.GetComponent<DungeonRoom>(); // like DungeonMap.switchRoom
    void AddRoom(DungeonRoom r){
        if(r==null || rooms.ContainsKey(r))return;
        var b = Area(r);
        rooms[r] = Box(b.center, b.size, Color.clear, r.name); // only its place: the picture shows the room
        Sort();
    }
    // under doors and you; a bigger room first, so a room inside a room shows on top of it
    void Sort(){
        var list = new List<DungeonRoom>(rooms.Keys);
        list.Sort((a,b)=>Size(b).CompareTo(Size(a)));
        int i = 0;
        foreach(var r in list){
            rooms[r].transform.SetSiblingIndex(i++);
            if(pictures.TryGetValue(r, out var p))p.transform.SetSiblingIndex(i++);
        }
    }
    float Size(DungeonRoom r){var s = rooms[r].rectTransform.sizeDelta; return s.x*s.y;}
    // everything it draws (tall wall tiles, sprites; not a room inside it). Made when you walk in, so the room is active
    static Bounds Area(DungeonRoom r){
        Bounds area = new(r.transform.position, Vector3.one*10);
        bool any = false;
        foreach(var rend in r.GetComponentsInChildren<Renderer>()){
            if(!rend.enabled || Owner(rend.transform)!=r || rend.GetComponentInParent<Character>())continue; // enemies and chests can stand at the edge
            if(any)area.Encapsulate(rend.bounds);
            else area = rend.bounds;
            any = true;
        }
        if(any)return area;
        foreach(var tm in r.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)){ // nothing drawn: its tilemap cells
            if(Owner(tm.transform)!=r)continue;
            tm.CompressBounds();
            var b = tm.localBounds;
            if(b.size==Vector3.zero)continue; // no tiles
            var w = new Bounds(tm.transform.TransformPoint(b.center), Vector3.Scale(b.size, tm.transform.lossyScale));
            if(any)area.Encapsulate(w);
            else area = w;
            any = true;
        }
        return area;
    }
    static DungeonRoom Owner(Transform t){ // the nearest room above it
        for(;t!=null;t=t.parent)if(t.TryGetComponent(out DungeonRoom r))return r;
        return null;
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
