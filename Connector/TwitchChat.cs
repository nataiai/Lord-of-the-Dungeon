using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lexone.UnityTwitchChat;


public class TwitchChat : Chat
{
    // public List<Chatter> chatters;
    // public List<ChatMsg> chatMsgs;
    public Chatter sampleTwitchChatter;
    public string channel;
    public IRC irc;

    public void Start(){
        irc = IRC.Instance;
        // irc.Connect();
        // chatters = new List<Chatter>();
    }
    
    public void Connect() => Connect(channel);
    public void Connect(string channel){
        irc.JoinChannel(channel);
        irc.OnChatMessage += OnChatMessage;
    }

    private void OnChatMessage(Chatter chatter)
    {
        // chatters.Add(chatter);
        // if(chatters.Count>100)chatters.Clear();
        addToDB(chatter);
    }

    private void addToDB(Chatter chatter){
        var name = chatter.tags.displayName;
        var msg = chatter.message;
        addToDB(name,msg);
    }
}


// public class TwitchChat : Chat
// {

//     [SerializeField] public string address = "irc.chat.twitch.tv";
//     [SerializeField] public int port = 6667;
//     string username;
//     TcpClient tcpClient;
//     int connectionFailCount;

//     void Connect(){


//         connectionFailCount = 0;

//         StartCoroutine(StartConnection());
//         IEnumerator StartConnection()
//         {
            
//             try
//             {
//                 tcpClient = new TcpClient(address, port);
//                 // SendCommand("PASS oauth:" + oauth.ToLower(), true);
//                 username = "justinfan" + UnityEngine.Random.Range(1000000, 9999999);
//                 SendCommand("NICK " + username.ToLower(), true);
//                 SendCommand("CAP REQ :twitch.tv/tags twitch.tv/commands", true);
//             }
//             catch
//             {
//                 tcpClient = null;
//                 connectionFailCount+=1;
//             }
//             if (connectionFailCount >= 2)
//             {
//                 int delay = 1 << (connectionFailCount - 2); // -> 0s, 1s, 2s, 4s, 8s, 16s, ...
//                 if (showIRCDebug)
//                     Debug.Log($"{Tags.alert} Reconnecting in {delay} seconds");

//                 yield return new WaitForSecondsRealtime(delay);
//             }

//         }
        
//     }
//     private IEnumerator NonBlockingDisconnect()
//     {
//         yield return StartCoroutine(connection.End());
//         connection = null;
//         if (showIRCDebug) Debug.Log($"{Tags.alert} Disconnected from Twitch IRC");
//     }

//     public void JoinChannel(string channel)
//     {
//         if (channel == "")Debug.LogError("Failed joining channel. Channel name is empty");
//         else connection.SendCommand("JOIN #" + channel.ToLower(), true);
//     }
// }

// // https://github.com/lexonegit/Unity-Twitch-Chat/blob/main/Unity-Twitch-Chat/Assets/Package/Runtime/IRCMessages.cs
// // https://github.com/lexonegit/Unity-Twitch-Chat/blob/main/Unity-Twitch-Chat/Assets/Package/Runtime/IRC.cs
// // https://github.com/lexonegit/Unity-Twitch-Chat/blob/main/Unity-Twitch-Chat/Assets/Package/Runtime/Chatter.cs


// [System.Serializable]
// public class Chatter
// {
//     public Chatter(string login, string channel, string message, IRCTags tags)
//     {
//         this.login = login;
//         this.channel = channel;
//         this.message = message;
//         this.tags = tags;
//     }

//     public string login, channel, message;
//     public IRCTags tags = null;

//     /// <summary>
//     /// <para>Returns the RGBA color of the chatter's name (tags.colorHex)</para>
//     /// <param name="normalize">Should the name color be normalized, if needed?</param>
//     /// </summary>
//     public Color GetNameColor(bool normalize = true)
//     {
//         if (ColorUtility.TryParseHtmlString(tags.colorHex, out Color color))
//         {
//             if (normalize)
//                 return ChatColors.NormalizeColor(color);
//             else
//                 return color;
//         }
//         else
//             return Color.white; // Parsing failed somehow, return default white
//     }

//     /// <summary>
//     /// <para>Returns true if displayName is "font-safe" 
//     /// meaning that it only contains characters: a-z, A-Z, 0-9, _</para>
//     /// <para>Useful because most fonts do not support unusual characters</para>
//     /// </summary>
//     public bool IsDisplayNameFontSafe()
//     {
//         return ParseHelper.CheckNameRegex(tags.displayName);
//     }

//     /// <summary>
//     /// <para>Returns true if the chatter's message contains a given emote (by emote ID)</para>
//     /// <para>You can find emote IDs by using the Twitch API, or 3rd party sites</para>
//     /// </summary>
//     public bool ContainsEmote(string emoteId) => tags.ContainsEmote(emoteId);

//     /// <summary>
//     /// Returns true if the chatter has a given badge.
//     /// </summary>
//     public bool HasBadge(string badgeName) => tags.HasBadge(badgeName);
// }
// [System.Serializable]
// public struct ChatterEmote
// {
//     [System.Serializable]
//     public struct Index
//     {
//         public int startIndex, endIndex;
//     }

//     public string id;
//     public Index[] indexes;
// }

// [System.Serializable]
// public struct ChatterBadge
// {
//     public string id;
//     public string version;
// }

// [System.Serializable]
// public class IRCTags
// {
//     public string colorHex = string.Empty;
//     public string displayName = string.Empty;
//     public string channelId = string.Empty;
//     public string userId = string.Empty;

//     public ChatterBadge[] badges = new ChatterBadge[0];
//     public ChatterEmote[] emotes = new ChatterEmote[0];

//     public bool ContainsEmote(string emoteId)
//     {
//         foreach (ChatterEmote e in emotes)
//         {
//             if (e.id == emoteId)
//                 return true;
//         }

//         return false;
//     }

//     public bool HasBadge(string badge)
//     {
//         foreach (ChatterBadge b in badges)
//         {
//             if (b.id == badge)
//                 return true;
//         }

//         return false;
//     }
// }
