using UnityEngine;

[DisallowMultipleComponent]
public class TrapConfig : MonoBehaviour
{
    [Tooltip("Se true, quando la puddle viene consumata genera una trappola")]
    public bool enableTrapFromPuddle = true;
    [Tooltip("Lista di prefab da cui pescare la trappola")]
    public GameObject[] trapPrefabs;
}
