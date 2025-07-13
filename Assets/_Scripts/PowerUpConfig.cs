using UnityEngine;

[CreateAssetMenu(menuName = "Game/PowerUpConfig")]
public class PowerUpConfig : ScriptableObject
{
    [Header("Speed Boost")]
    public GameObject speedVFXPrefab;

    [Header("Damage Boost")]
    public GameObject damageVFXPrefab;

    [Header("Poison Leak")]
    public GameObject poisonPuddlePrefab;
    public GameObject poisonVFXPrefab;
    public GameObject[] poisonTrapPrefabs;
}
