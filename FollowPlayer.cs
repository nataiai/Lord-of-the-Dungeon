using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera cinCam;
    void Start()=>cinCam.Follow = GameManager.Instance.player.transform;
}
