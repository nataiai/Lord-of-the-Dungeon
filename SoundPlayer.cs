using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public AudioSource musicSource, soundSource;
    public static SoundPlayer instance;
    public AudioClip sd_enemy_hit, sd_pickup_wep, sd_door_open;
    public RoomType[] roomTypeList; // roomMusicList[i] plays in rooms of type roomTypeList[i]
    public AudioClip[] roomMusicList;

    void Awake(){instance = this;} // Awake, so sounds work from any Start
    public static void PlaySound(AudioClip sound)=>instance.soundSource.PlayOneShot(sound);
    public void UpdateMusic(){
        var clip = getClip(GameManager.Instance.currentRoom.roomType);
        if(musicSource.clip==clip)return;
        musicSource.clip = clip;
        if(clip==null){musicSource.Stop();return;} // no music for this room type
        musicSource.Play();
        print("Play:"+clip.name);
    }
    AudioClip getClip(RoomType roomType){
        for(int i=0;i<roomMusicList.Length;i++){
            if(roomType==roomTypeList[i])return roomMusicList[i];
        }return null;
    }
}
