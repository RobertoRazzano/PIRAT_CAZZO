using UnityEngine.VFX;
using UnityEngine;

public class QuickTimeVFXManager : MonoBehaviour
{
    public VisualEffect biteEffect;
    public float autoDisableDelay = 1.5f;

    public void PlayBiteVFX()
    {
        if (biteEffect == null || Camera.main == null) return;

        // Posiziona sempre davanti alla camera
        Transform cam = Camera.main.transform;
        biteEffect.transform.position = cam.position + cam.forward * 3f;
        biteEffect.transform.rotation = Quaternion.LookRotation(-cam.forward);

        // Riavvia il VFX
        biteEffect.gameObject.SetActive(false);
        biteEffect.gameObject.SetActive(true);
        biteEffect.Play();

        // Disattiva dopo un po'
        CancelInvoke(nameof(DisableVFX));
        Invoke(nameof(DisableVFX), autoDisableDelay);
    }

    private void DisableVFX()
    {
        biteEffect.Stop(); // stoppa l'effetto (opzionale)
        biteEffect.gameObject.SetActive(false); // nasconde l'oggetto
    }
}
