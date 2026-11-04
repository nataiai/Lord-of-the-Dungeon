using Cinemachine;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera cinCam;
    void Start()=>cinCam.Follow = GameManager.Instance.player.transform;
}
