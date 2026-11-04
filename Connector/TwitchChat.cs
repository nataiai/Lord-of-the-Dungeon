using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

// reads Twitch or YouTube chat, no package needed (still named TwitchChat so the scene keeps its link)
public class TwitchChat : Chat
{
    public string channel; // Twitch: name or link. YouTube: link or @handle
    ConcurrentQueue<(string user,string msg)> inbox = new(); // Twitch messages arrive on another thread
    TcpClient tcp;

    public void Connect() => Connect(channel);
    public void Connect(string channel){
        if(string.IsNullOrWhiteSpace(channel))return;
        channel = channel.Trim();
        if(channel.Contains("youtube.com")||channel.Contains("youtu.be")||channel.StartsWith("@"))StartCoroutine(YouTube(channel));
        else Twitch((Find(channel, @"twitch\.tv/(?:popout/)?(\w+)") ?? channel.TrimStart('#')).ToLowerInvariant());
    }
    void Update(){while(inbox.TryDequeue(out var m))addToDB(m.user,m.msg);}
    void OnDestroy()=>tcp?.Close();
    static string Find(string s, string pattern){var m = Regex.Match(s ?? "", pattern); return m.Success?m.Groups[1].Value:null;}

    void Twitch(string ch){ // anonymous, read-only IRC login
        string nick = "justinfan"+Random.Range(1000,99999);
        new Thread(()=>{try{
            tcp = new TcpClient("irc.chat.twitch.tv", 6667);
            var r = new StreamReader(tcp.GetStream());
            var w = new StreamWriter(tcp.GetStream()){AutoFlush=true, NewLine="\r\n"};
            w.WriteLine("NICK "+nick); w.WriteLine("JOIN #"+ch);
            for(string l; (l = r.ReadLine())!=null;){ // ":user!user@user.tmi.twitch.tv PRIVMSG #channel :message"
                if(l.StartsWith("PING"))w.WriteLine("PONG"+l.Substring(4));
                int i = l.IndexOf(" PRIVMSG #");
                if(i>0)inbox.Enqueue((l.Substring(1,l.IndexOf('!')-1), l.Substring(l.IndexOf(" :",i)+2)));
            }
        }catch(System.Exception e){Debug.LogWarning("Twitch chat: "+e.Message);}}){IsBackground=true}.Start();
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
                addToDB(Regex.Unescape(m.Groups[2].Value), text);
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
