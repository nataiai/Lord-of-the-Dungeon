using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "PrefabDatabase", menuName = "Game/Prefab Database")]
public class PrefabDatabase : ScriptableObject
{
    static PrefabDatabase _instance;
    public static PrefabDatabase Instance =>
        _instance ??= Resources.Load<PrefabDatabase>("PrefabDatabase");

    public Weapon fist;
}