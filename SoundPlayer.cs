using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [System.Serializable]
// public class RoomMusic{RoomType r;AudioClip a;}

public class SoundPlayer : MonoBehaviour
{
    public AudioSource musicSource, soundSource;
    public static SoundPlayer instance;
    public AudioClip sd_enemy_hit;

    // public Dictionary<RoomType,AudioClip> roomMusicDict;
    public RoomType[] roomTypeList;
    public AudioClip[] roomMusicList;

    void Start()
    {
        instance = this;
    }


    void Update()
    {
        
    }

    public static void PlaySound(AudioClip sound)
    {
        instance.soundSource.PlayOneShot(sound);
    }

    public void UpdateMusic(){
        RoomType roomType = GameManager.Instance.currentRoom.roomType;
        var clip = getClip(roomType);
        if(musicSource.clip!=clip){
            musicSource.clip = clip;
            musicSource.Play();
            print("Play:"+clip.name);
        }
    }
    AudioClip getClip(RoomType roomType){
        for(int i=0;i<roomMusicList.Length;i++){
            if(roomType==roomTypeList[i])return roomMusicList[i];
        }return null;
    }

}
