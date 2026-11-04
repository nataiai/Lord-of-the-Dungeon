using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public AudioSource musicSource, soundSource;
    public static SoundPlayer instance;
    public AudioClip sd_enemy_hit;
    public RoomType[] roomTypeList; // roomMusicList[i] plays in rooms of type roomTypeList[i]
    public AudioClip[] roomMusicList;

    void Start(){instance = this;}
    public static void PlaySound(AudioClip sound)=>instance.soundSource.PlayOneShot(sound);
    public void UpdateMusic(){
        var clip = getClip(GameManager.Instance.currentRoom.roomType);
        if(musicSource.clip==clip)return;
        musicSource.clip = clip;
        musicSource.Play();
        print("Play:"+clip.name);
    }
    AudioClip getClip(RoomType roomType){
        int i = System.Array.IndexOf(roomTypeList, roomType);
        return i>=0 && i<roomMusicList.Length ? roomMusicList[i] : null;
    }
}
