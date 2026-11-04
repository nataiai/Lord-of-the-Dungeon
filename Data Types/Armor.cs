using UnityEngine;

[CreateAssetMenu(fileName = "ArmorSO", menuName = "Armor")]
public class Armor : ScriptableObject
{
    [SerializeField] bool isMagical;
    [SerializeField] int defensePhysic;
    [SerializeField] int defenseMagic;
}
