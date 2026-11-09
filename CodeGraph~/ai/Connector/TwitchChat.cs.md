# Connector/TwitchChat.cs <!-- a1bfbc -->
Reads Twitch chat through the Lexone Unity-Twitch-Chat package. The commented-out code at the end is an older IRC client, kept for reference (not drawn).

## TwitchChat <!-- 14ddcb -->
Chat reader using Lexone's IRC: joins, forwards messages.
`Connect` joins `channel` and subscribes to the package's chat event; each message goes to `Chat.addToDB` as (display name, message). This is the backup when Experimental.unifiedChat is off.
