using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

// One chat reader for Twitch (desktop and WebGL) and YouTube (desktop), no package needed.
// Messages go to GameManager.Instance.chat.addToDB, the same place the Lexone TwitchChat sends them.
// Backup: Lexone (chat.Connect in GameManager._Connect) is used whenever this is off (Experimental.unifiedChat).
// Keep keeps every path connected (Lexone too): no internet or a drop only means it tries again.
public class UnifiedChat : MonoBehaviour
{
    ConcurrentQueue<string> inbox = new(); // desktop Twitch lines arrive on another thread
    TcpClient tcp;
    TextWriter send; // desktop Twitch: the reader thread (PONG) and Ping both write here
    Coroutine yt;
    float heard = -60; // realtime of chat's last answer (or Keep's last try); -60 = connect now

    public static bool TryConnect(string channel){ // Twitch: name or link. YouTube (desktop only): link or @handle
        if(!Experimental.I.unifiedChat || string.IsNullOrWhiteSpace(channel))return false;
        channel = channel.Trim();
        var c = new GameObject("UnifiedChat").AddComponent<UnifiedChat>();
        bool youtube = channel.Contains("youtube.com")||channel.Contains("youtu.be")||channel.StartsWith("@");
        if(!youtube)c.StartCoroutine(c.Keep(()=>c.Twitch(TwitchName(channel)), c.Ping));
        else if(Application.platform==RuntimePlatform.WebGLPlayer)Debug.LogWarning("YouTube chat works in desktop builds only");
        else c.StartCoroutine(c.Keep(()=>c.YouTubeAgain(channel), null));
        return true;
    }
    // Lexone (TwitchChat.Connect) joins irc.channel on every connect, but gives up when it can't connect
    public static void KeepLexone(Lexone.UnityTwitchChat.IRC irc, string channel){
        if(irc==null || string.IsNullOrWhiteSpace(channel))return;
        if(Application.platform==RuntimePlatform.WebGLPlayer)return; // no sockets: use unifiedChat
        irc.channel = TwitchName(channel);
        var c = new GameObject("UnifiedChat").AddComponent<UnifiedChat>();
        irc.OnConnectionAlert += a=>c.Heard(); // any alert: Lexone is up, or busy retrying itself
        c.StartCoroutine(c.Keep(()=>c.StartCoroutine(c.Probe(irc)), irc.Ping));
    }
    static string TwitchName(string channel)=>(Find(channel, @"twitch\.tv/(?:popout/)?(\w+)") ?? channel.Trim().TrimStart('#')).ToLowerInvariant();
    void Update(){while(inbox.TryDequeue(out var l))OnIrcLine(l);}
    void OnDestroy()=>tcp?.Close();
    static void Add(string user, string msg)=>GameManager.Instance.chat.addToDB(user,msg);
    static string Find(string s, string pattern){var m = Regex.Match(s ?? "", pattern); return m.Success?m.Groups[1].Value:null;}

    void Heard()=>heard = Time.realtimeSinceStartup;
    // every path: ping when chat is quiet, reconnect when nothing answers
    IEnumerator Keep(System.Action connect, System.Action ping){
        while(true){
            float quiet = Time.realtimeSinceStartup - heard;
            if(quiet>20){Heard(); connect();} // no internet, dropped, Twitch down: try again
            else if(quiet>10)ping?.Invoke(); // Twitch answers PONG
            yield return new WaitForSecondsRealtime(5);
        }
    }
    // Lexone connects on the main thread (no internet = game freezes), so test on a thread first
    IEnumerator Probe(Lexone.UnityTwitchChat.IRC irc){
        irc.Disconnect(); // or Lexone notices the dead socket and reconnects on the main thread itself
        yield return new WaitForSecondsRealtime(1); // its threads stop
        float asked = heard;
        var t = System.Threading.Tasks.Task.Run(()=>new TcpClient(irc.address, irc.port).Close());
        yield return new WaitUntil(()=>t.IsCompleted);
        if(t.Exception==null && heard==asked)irc.Connect(); // changed: Lexone is back, or a newer try
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void UnifiedChatTwitch(string nick, string channel, string unityObject); // UnifiedChat.jslib
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void UnifiedChatPing();
#endif
    void Twitch(string ch){ // anonymous, read-only login; every chat line goes to OnIrcLine
        string nick = "justinfan"+Random.Range(1000,99999);
#if UNITY_WEBGL && !UNITY_EDITOR
        UnifiedChatTwitch(nick, ch, name); // browsers have no sockets or threads, so a WebSocket in JavaScript
#else
        tcp?.Close(); // reconnect: the old reader stops
        var t = tcp = new TcpClient(); // made here, so Close reaches it even while connecting
        new Thread(()=>{try{
            t.Connect("irc.chat.twitch.tv", 6667);
            var r = new StreamReader(t.GetStream());
            var w = send = TextWriter.Synchronized(new StreamWriter(t.GetStream()){AutoFlush=true, NewLine="\r\n"});
            w.WriteLine("NICK "+nick); w.WriteLine("JOIN #"+ch);
            for(string l; (l = r.ReadLine())!=null;){
                if(l.StartsWith("PING"))w.WriteLine("PONG"+l.Substring(4));
                else inbox.Enqueue(l);
            }
        }catch(System.Exception e){Debug.LogWarning("Twitch chat: "+e.Message);}}){IsBackground=true}.Start();
#endif
    }
    void Ping(){
#if UNITY_WEBGL && !UNITY_EDITOR
        UnifiedChatPing();
#else
        try{send?.WriteLine("PING :tmi.twitch.tv");}catch{} // a dead socket: Keep reconnects
#endif
    }
    void OnIrcLine(string l){ // ":user!user@user.tmi.twitch.tv PRIVMSG #channel :message"
        Heard(); // any line, PONG too
        int i = l.IndexOf(" PRIVMSG #");
        if(i>0)Add(l.Substring(1,l.IndexOf('!')-1), l.Substring(l.IndexOf(" :",i)+2));
    }

    void YouTubeAgain(string link){if(yt!=null)StopCoroutine(yt); yt = StartCoroutine(YouTube(link));}
    IEnumerator YouTube(string link){ // the API YouTube's own chat page uses, no key needed
        string id = Find(link, @"(?:v=|youtu\.be/|/live/)([\w-]{11})");
        if(id==null){ // a channel: find its live stream
            using var page = Web("https://www.youtube.com/"+Find(link, @"(@[\w.-]+|(?:channel|c|user)/[\w-]+)")+"/live");
            yield return page.SendWebRequest();
            id = Find(page.downloadHandler.text, @"rel=""canonical"" href=""[^""]*v=([\w-]{11})");
        }
        using var chatPage = Web("https://www.youtube.com/live_chat?v="+id);
        yield return chatPage.SendWebRequest();
        string html = chatPage.downloadHandler.text, cont = Find(html, @"""continuation"":""([^""]+)""");
        string body = "{\"context\":{\"client\":{\"clientName\":\"WEB\",\"clientVersion\":\""+(Find(html, @"""INNERTUBE_CLIENT_VERSION"":""([^""]+)""") ?? "2.20240101")+"\"}},\"continuation\":\"";
        if(cont==null)Debug.LogWarning("YouTube chat not found (is the stream live?): "+link);
        while(cont!=null){
            using var req = Web("https://www.youtube.com/youtubei/v1/live_chat/get_live_chat?prettyPrint=false", body+cont+"\"}");
            yield return req.SendWebRequest();
            string json = req.downloadHandler.text;
            foreach(Match m in Regex.Matches(json, @"liveChatTextMessageRenderer"":\{""message"":\{""runs"":\[(.*?)\]\},""authorName"":\{""simpleText"":""((?:[^""\\]|\\.)*)""")){
                string text = "";
                foreach(Match t in Regex.Matches(m.Groups[1].Value, @"""text"":""((?:[^""\\]|\\.)*)"""))text += Regex.Unescape(t.Groups[1].Value);
                Add(Regex.Unescape(m.Groups[2].Value), text);
            }
            string next = Find(json, @"""continuation"":""([^""]+)""");
            if(next!=null){cont = next; Heard();} // YouTube answered
            yield return new WaitForSeconds(2);
        }
    }
    static UnityWebRequest Web(string url, string json=null){
        var r = json==null?UnityWebRequest.Get(url):new UnityWebRequest(url, "POST", new DownloadHandlerBuffer(), new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)));
        r.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        r.SetRequestHeader("Cookie", "SOCS=CAI"); // skip the EU cookie-consent page
        if(json!=null)r.SetRequestHeader("Content-Type", "application/json");
        return r;
    }
}
