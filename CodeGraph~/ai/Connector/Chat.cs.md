# Connector/Chat.cs <!-- c9c7be -->
Turns chat messages into game commands (summon, teleport, vote, work).

## Chat <!-- 05a1fa -->
Base of the chat readers: message → command.
A message's first word can be any start of a command word ("s", "sum", "summon"), followed by the allowed number of words. TwitchChat (Lexone) and AI/UnifiedChat both send their messages to `addToDB`.

## Chat.Categorize <!-- be4d49 -->
Finds the first command whose word count fits and whose word starts with the first word typed. Because summon comes first in the table, "s" means summon. Anything else is `invalid`.

## Chat.addToDB <!-- 648768 -->
Summon queues the named enemy for its sender; teleport moves the player when both numbers parse. Vote and work are recognized but nothing handles them yet.
