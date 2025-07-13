using UnityEngine;

public class RotazioneY : MonoBehaviour
{
    [SerializeField] private float velocitaRotazione = 45f; // gradi al secondo

    private void Update()
    {
        transform.Rotate(0f, velocitaRotazione * Time.deltaTime, 0f);
    }
}
