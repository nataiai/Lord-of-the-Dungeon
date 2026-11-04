using Lexone.UnityTwitchChat;

public class TwitchChat : Chat // reads chat only, never sends
{
    public Chatter sampleTwitchChatter;
    public string channel;
    public IRC irc;

    public void Start(){irc = IRC.Instance;}
    public void Connect() => Connect(channel);
    public void Connect(string channel){
        irc.JoinChannel(channel);
        irc.OnChatMessage += OnChatMessage;
    }
    void OnChatMessage(Chatter chatter)=>addToDB(chatter.tags.displayName,chatter.message);
}
