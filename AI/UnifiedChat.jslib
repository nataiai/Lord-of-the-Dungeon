// WebGL only: Twitch chat over a WebSocket, because browsers have no sockets or threads (called by UnifiedChat.cs)
mergeInto(LibraryManager.library, {
  UnifiedChatTwitch: function (nick, channel, unityObject) {
    nick = UTF8ToString(nick); channel = UTF8ToString(channel); unityObject = UTF8ToString(unityObject);
    if (Module.unifiedChatWs) Module.unifiedChatWs.close(); // reconnect: the old socket stops
    var ws = Module.unifiedChatWs = new WebSocket("wss://irc-ws.chat.twitch.tv:443");
    ws.onopen = function () { ws.send("NICK " + nick); ws.send("JOIN #" + channel); };
    ws.onmessage = function (e) {
      e.data.split("\r\n").forEach(function (l) {
        if (l.indexOf("PING") == 0) ws.send("PONG" + l.substring(4));
        else if (l) SendMessage(unityObject, "OnIrcLine", l);
      });
    };
  },
  UnifiedChatPing: function () { // UnifiedChat.Ping when chat is quiet: Twitch answers PONG
    var ws = Module.unifiedChatWs;
    if (ws && ws.readyState == 1) ws.send("PING :tmi.twitch.tv");
  },
});
