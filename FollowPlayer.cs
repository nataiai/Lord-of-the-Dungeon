using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera cinCam;
    // Start is called before the first frame update
    void Start()
    {
        cinCam.Follow = GameManager.Instance.player.transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
