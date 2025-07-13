using UnityEngine;

public class Galleggiamento : MonoBehaviour
{
    [SerializeField] private float ampiezza = 0.1f; // altezza dell'oscillazione
    [SerializeField] private float frequenza = 1f;  // velocità dell'oscillazione
    [SerializeField] private float rotazioneAmpiezza = 2f; // gradi di inclinazione
    [SerializeField] private float rotazioneFrequenza = 0.5f;

    private Vector3 posizioneIniziale;
    private Quaternion rotazioneIniziale;

    private void Start()
    {
        posizioneIniziale = transform.position;
        rotazioneIniziale = transform.rotation;
    }

    private void Update()
    {
        // Movimento su/giù
        float offsetY = Mathf.Sin(Time.time * frequenza) * ampiezza;
        transform.position = posizioneIniziale + new Vector3(0f, offsetY, 0f);

        // Oscillazione di rollio/pitch
        float rotX = Mathf.Sin(Time.time * rotazioneFrequenza) * rotazioneAmpiezza;
        float rotZ = Mathf.Cos(Time.time * rotazioneFrequenza) * rotazioneAmpiezza;

        transform.rotation = rotazioneIniziale * Quaternion.Euler(rotX, 0f, rotZ);
    }
}
