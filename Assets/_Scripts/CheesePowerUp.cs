using System.Collections;
using UnityEngine;

public enum CheesePowerUpType { Heal, SpeedBoost, DamageBoost, PoisonLeak }

public class CheesePowerUp : MonoBehaviour
{
    [Header("Power-up Settings")]
    public CheesePowerUpType powerUpType;
    public int healAmount = 20;
    public float speedMultiplier = 1.5f;
    public float speedDuration = 5f;
    public int extraDamage = 10;

    [HideInInspector] public bool wasNear = false;

    [Header("Prefabs & VFX")]
    public GameObject poisonPuddlePrefab;
    
    [SerializeField] private GameObject VFXPrefab;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip healClip;
    [SerializeField] private AudioClip speedBoostClip;
    [SerializeField] private AudioClip damageBoostClip;
    [SerializeField] private AudioClip poisonLeakClip;

    [Header("Tutorial Dialogue")]
    [SerializeField] private DialogueSequence tutorialDialogueSequence;

    [Header("Outline & Trigger")]
    private Material _defaultMaterial;
    private bool outlineActive = false;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private SphereCollider triggerCollider;
    [SerializeField] private string playerTag = "Player";

    private Renderer _renderer;
    private AudioSource _audioSource;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _defaultMaterial = _renderer.material;
        }
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
    }

    public void EnableOutline(bool enable)
    {
        if (_renderer == null || outlineMaterial == null || triggerCollider == null) return;

        if (outlineActive == enable) return;
        outlineActive = enable;

        if (enable)
        {
            Material[] newMats = new Material[2];
            newMats[0] = _renderer.materials[0];
            newMats[1] = outlineMaterial;
            _renderer.materials = newMats;

            triggerCollider.enabled = true; // ✅ attiva il collider
        }
        else
        {
            Material[] newMats = new Material[1];
            newMats[0] = _renderer.materials[0];
            _renderer.materials = newMats;

            triggerCollider.enabled = false; // ✅ spegne anche il collider
            wasNear = false; // ← serve per bloccare riattivazioni immediate

        }
    }

    public void ActivatePowerUp(RatInteractionManager rat)
    {


        if (!TutorialManager.HasTutorialBeenShown(powerUpType))
        {
            // Mostra il tutorial usando il tuo sistema (es. DialogueManager)
            // Esempio (modifica in base alla tua implementazione specifica):
            DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
            if (dialogueManager != null && tutorialDialogueSequence != null)
            {
                dialogueManager.StartDialogue(tutorialDialogueSequence);
            }

            TutorialManager.MarkTutorialAsShown(powerUpType);
            return; // Esci, powerup verrà preso solo dopo aver mostrato il tutorial
        }

        // Procedi con l'attivazione normale del power-up
        Debug.Log($"ActivatePowerUp: {powerUpType}");
        var bonusMalus = rat.GetComponent<BonusMalus>();
        bool consumed = false;

        switch (powerUpType)
        {
            case CheesePowerUpType.Heal:
                if (bonusMalus != null && bonusMalus.currentHealth < bonusMalus.maxHealth)
                {
                    PlaySound(healClip, 1f);
                    bonusMalus.Heal(healAmount);
                    consumed = true;
                    StartCoroutine(EnableHealVFXAfterDestroy(rat.transform, 1.7f));
                }
                break;

            case CheesePowerUpType.SpeedBoost:
                var ratInput = rat.GetComponent<RatInputHandler>();
                if (ratInput != null)
                {
                    PlaySound(speedBoostClip, 1f);
                    ratInput.StartCoroutine(ratInput.SpeedBoostRoutine(speedMultiplier, speedDuration));
                    consumed = true;
                    StartCoroutine(EnableSpeedVFXAfterDestroy(ratInput, 1.7f));
                }
                break;

            case CheesePowerUpType.DamageBoost:
                PlaySound(damageBoostClip, 1f);
                consumed = true;
                StartCoroutine(EnableDamageVFXAfterDestroy(rat, 1.7f));
                break;

            case CheesePowerUpType.PoisonLeak:
                PlaySound(poisonLeakClip, 1f);
                consumed = true;
                StartCoroutine(EnablePeeVFXAfterDestroy(rat, 1.7f));
                break;
        }

        if (consumed)
            StartCoroutine(DestroyAfterDelay(1.7f));
    }

    private IEnumerator DestroyAfterDelay(float totalDelay)
    {
        yield return new WaitForSeconds(1f);

        if (_renderer != null)
            _renderer.enabled = false;

        // NON disabilitare qui il trigger
        // if (triggerCollider != null)
        //     triggerCollider.enabled = false;

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
            mainCollider.enabled = false;

        float remainingDelay = Mathf.Max(0f, totalDelay - 1f);
        yield return new WaitForSeconds(remainingDelay);

        //if (triggerCollider != null)
          //  triggerCollider.enabled = false; // spegne il trigger definitivamente per sicurezza

        // forza sempre spegnimento dell'outline
        EnableOutline(false);

        Destroy(gameObject);
    }

    private IEnumerator EnableSpeedVFXAfterDestroy(RatInputHandler ratInput, float delay)
    {
        yield return new WaitForSeconds(delay);
        ratInput.SetSpeedVFX(VFXPrefab);
        PlaySound(speedBoostClip);
    }

    private IEnumerator EnableDamageVFXAfterDestroy(RatInteractionManager rat, float delay)
{
    yield return new WaitForSeconds(delay);
    rat.ActivateDamageBoost(extraDamage, VFXPrefab);
    PlaySound(damageBoostClip);
}

    private IEnumerator EnablePeeVFXAfterDestroy(RatInteractionManager rat, float delay)
{
    yield return new WaitForSeconds(delay);

    rat.PreparePoisonLeak(poisonPuddlePrefab, VFXPrefab);

    var trapConfig = GetComponent<TrapConfig>();
    if (trapConfig != null && trapConfig.enableTrapFromPuddle)
        rat.ConfigurePuddleTrap(trapConfig.trapPrefabs);

    PlaySound(poisonLeakClip);
}


private void PlaySound(AudioClip clip, float delay = 0f)
{
    if (_audioSource != null && clip != null)
    {
        StartCoroutine(PlaySoundDelayed(clip, delay));
    }
}

private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
{
    yield return new WaitForSeconds(delay);
    _audioSource.PlayOneShot(clip);
}


    private IEnumerator EnableHealVFXAfterDestroy(Transform ratTransform, float delay)
{
    yield return new WaitForSeconds(delay);
    if (VFXPrefab != null)
    {
        var vfx = Instantiate(VFXPrefab, ratTransform.position, Quaternion.identity, ratTransform);
        vfx.transform.localPosition = Vector3.zero;
        Destroy(vfx, 2f);
    }
    PlaySound(healClip);
}
    
     private void OnTriggerExit(Collider other)
    {
        if (outlineActive && other.CompareTag(playerTag))
        {
            EnableOutline(false); 
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            powerUpType.ToString()
        );
    }
#endif
}