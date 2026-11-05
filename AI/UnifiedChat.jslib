// WebGL only: Twitch chat over a WebSocket, because browsers have no sockets or threads (called by UnifiedChat.cs)
mergeInto(LibraryManager.library, {
  UnifiedChatTwitch: function (nick, channel, unityObject) {
    nick = UTF8ToString(nick); channel = UTF8ToString(channel); unityObject = UTF8ToString(unityObject);
    var ws = new WebSocket("wss://irc-ws.chat.twitch.tv:443");
    ws.onopen = function () { ws.send("NICK " + nick); ws.send("JOIN #" + channel); };
    ws.onmessage = function (e) {
      e.data.split("\r\n").forEach(function (l) {
        if (l.indexOf("PING") == 0) ws.send("PONG" + l.substring(4));
        else if (l) SendMessage(unityObject, "OnIrcLine", l);
      });
    };
  },
});
