# Dialogue.cs <!-- 43d253 -->
All story text in one table, and the component that shows a piece of it.

## Dialogues <!-- e63b31 -->
The story lines, by sequence name.
Add a value to `DialogueSequence` and its lines here; any scene can then show it by picking it in a Dialogue component.

## Dialogue <!-- 59bf89 -->
Shows a dialogue sequence, on start or when called.
Its own `dialogueText` is used only when Sequence is None.

## Dialogue.Start <!-- 848a39 -->
Finds the UI. With trigger OnStart the dialogue shows right away; with OnCall it waits for something (a UnityEvent, for example) to call `Call`.
