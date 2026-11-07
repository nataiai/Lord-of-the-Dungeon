using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour // the camera follows the middle of all players
{
    [SerializeField] CinemachineVirtualCamera cinCam;
    CinemachineTargetGroup group;
    void Start(){
        group = new GameObject("Camera Target (players)").AddComponent<CinemachineTargetGroup>();
        cinCam.Follow = group.transform;
    }
    void LateUpdate(){ // players can join, leave or be cloned at any time
        foreach(var p in Player.all)if(group.FindMember(p.transform)<0)group.AddMember(p.transform,1,1);
        foreach(var t in group.m_Targets)if(t.target==null||!Player.all.Exists(p=>p.transform==t.target))group.RemoveMember(t.target);
    }
}
