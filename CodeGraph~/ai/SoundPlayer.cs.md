# SoundPlayer.cs <!-- 9d7763 -->
Sound effects and the music for each room type.

## SoundPlayer <!-- c44d4e -->
Plays sound effects and each room type's music.
`PlaySound` is static and uses `instance`, set in Awake so sounds work from any Start. `roomMusicList[i]` plays in rooms of type `roomTypeList[i]`.

## SoundPlayer.UpdateMusic <!-- 680af9 -->
Called when the room changes: plays the current room type's music, unless it is already playing; stops the music if the type has none.

## SoundPlayer.getClip <!-- 27d316 -->
The music listed for a room type, or none.
