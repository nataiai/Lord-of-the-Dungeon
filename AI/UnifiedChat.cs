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
// Backup: Lexone (chat.Connect in GameManager._Connect) is used whenever this is off.
public class UnifiedChat : MonoBehaviour
{
    public static bool use = false; // ON/OFF: false = Lexone TwitchChat reads chat instead
    ConcurrentQueue<string> inbox = new(); // desktop Twitch lines arrive on another thread
    TcpClient tcp;

    public static bool TryConnect(string channel){ // Twitch: name or link. YouTube (desktop only): link or @handle
        if(!use || string.IsNullOrWhiteSpace(channel))return false;
        channel = channel.Trim();
        var c = new GameObject("UnifiedChat").AddComponent<UnifiedChat>();
        bool youtube = channel.Contains("youtube.com")||channel.Contains("youtu.be")||channel.StartsWith("@");
        if(!youtube)c.Twitch((Find(channel, @"twitch\.tv/(?:popout/)?(\w+)") ?? channel.TrimStart('#')).ToLowerInvariant());
        else if(Application.platform==RuntimePlatform.WebGLPlayer)Debug.LogWarning("YouTube chat works in desktop builds only");
        else c.StartCoroutine(c.YouTube(channel));
        return true;
    }
    void Update(){while(inbox.TryDequeue(out var l))OnIrcLine(l);}
    void OnDestroy()=>tcp?.Close();
    static void Add(string user, string msg)=>GameManager.Instance.chat.addToDB(user,msg);
    static string Find(string s, string pattern){var m = Regex.Match(s ?? "", pattern); return m.Success?m.Groups[1].Value:null;}

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void UnifiedChatTwitch(string nick, string channel, string unityObject); // UnifiedChat.jslib
#endif
    void Twitch(string ch){ // anonymous, read-only login; every chat line goes to OnIrcLine
        string nick = "justinfan"+Random.Range(1000,99999);
#if UNITY_WEBGL && !UNITY_EDITOR
        UnifiedChatTwitch(nick, ch, name); // browsers have no sockets or threads, so a WebSocket in JavaScript
#else
        new Thread(()=>{try{
            tcp = new TcpClient("irc.chat.twitch.tv", 6667);
            var r = new StreamReader(tcp.GetStream());
            var w = new StreamWriter(tcp.GetStream()){AutoFlush=true, NewLine="\r\n"};
            w.WriteLine("NICK "+nick); w.WriteLine("JOIN #"+ch);
            for(string l; (l = r.ReadLine())!=null;){
                if(l.StartsWith("PING"))w.WriteLine("PONG"+l.Substring(4));
                else inbox.Enqueue(l);
            }
        }catch(System.Exception e){Debug.LogWarning("Twitch chat: "+e.Message);}}){IsBackground=true}.Start();
#endif
    }
    void OnIrcLine(string l){ // ":user!user@user.tmi.twitch.tv PRIVMSG #channel :message"
        int i = l.IndexOf(" PRIVMSG #");
        if(i>0)Add(l.Substring(1,l.IndexOf('!')-1), l.Substring(l.IndexOf(" :",i)+2));
    }

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
            cont = Find(json, @"""continuation"":""([^""]+)""") ?? cont;
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
