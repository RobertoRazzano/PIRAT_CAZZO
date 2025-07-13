using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [SerializeField] private Transform puntoIniziale;
    [SerializeField] private Transform puntoFinale;
    [SerializeField] private float durata = 2f;

    private void Start()
    {
        if (puntoIniziale != null && puntoFinale != null)
        {
            transform.position = puntoIniziale.position;
            transform.rotation = puntoIniziale.rotation;
            StartCoroutine(MuoviCamera());
        }
    }

    private System.Collections.IEnumerator MuoviCamera()
    {
        float tempoTrascorso = 0f;

        Vector3 posizioneIniziale = puntoIniziale.position;
        Quaternion rotazioneIniziale = puntoIniziale.rotation;

        Vector3 posizioneFinale = puntoFinale.position;
        Quaternion rotazioneFinale = puntoFinale.rotation;

        while (tempoTrascorso < durata)
        {
            float t = tempoTrascorso / durata;
            transform.position = Vector3.Lerp(posizioneIniziale, posizioneFinale, t);
            transform.rotation = Quaternion.Slerp(rotazioneIniziale, rotazioneFinale, t);

            tempoTrascorso += Time.deltaTime;
            yield return null;
        }

        transform.position = posizioneFinale;
        transform.rotation = rotazioneFinale;
    }
}
