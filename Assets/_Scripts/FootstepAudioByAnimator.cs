using UnityEngine;

public class RatAudioControllerByAnimator : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float footstepVolume = 0.7f;

    public AudioClip biteClip;
    [Range(0f, 1f)] public float biteVolume = 0.8f;

    public AudioClip biteWithJumpBackClip;
    [Range(0f, 1f)] public float biteWithJumpBackVolume = 0.8f;

    public AudioClip backflipClip;
    [Range(0f, 1f)] public float backflipVolume = 0.8f;

    [Header("Animator States")]
    public string walkingStateName = "WalkRatAnimation";
    public string biteStateName = "Bite";
    public string biteWithJumpBackStateName = "BiteWithJumpBack";
    public string backflipStateName = "Backflip";

    private Animator animator;
    [SerializeField] private AudioSource audioSource;

    private bool isWalking = false;
    private bool actionSoundPlayed = false;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource mancante su " + gameObject.name);
        }
        else
        {
            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // --- Passi ---
        if (stateInfo.IsName(walkingStateName))
        {
            if (!isWalking && footstepClip != null)
            {
                audioSource.clip = footstepClip;
                audioSource.volume = footstepVolume;
                audioSource.loop = true;
                audioSource.Play();
                isWalking = true;
            }
        }
        else
        {
            if (isWalking)
            {
                audioSource.Stop();
                isWalking = false;
            }
        }

        // --- Azioni con suoni singoli ---
        if (stateInfo.IsName(biteWithJumpBackStateName))
        {
            if (!actionSoundPlayed && biteWithJumpBackClip != null)
            {
                audioSource.loop = false;
                audioSource.PlayOneShot(biteWithJumpBackClip, biteWithJumpBackVolume);
                actionSoundPlayed = true;
            }
        }
        else if (stateInfo.IsName(biteStateName))
        {
            if (!actionSoundPlayed && biteClip != null)
            {
                audioSource.loop = false;
                audioSource.PlayOneShot(biteClip, biteVolume);
                actionSoundPlayed = true;
            }
        }
        else if (stateInfo.IsName(backflipStateName))
        {
            if (!actionSoundPlayed && backflipClip != null)
            {
                audioSource.loop = false;
                audioSource.PlayOneShot(backflipClip, backflipVolume);
                actionSoundPlayed = true;
            }
        }
        else
        {
            actionSoundPlayed = false;
        }
    }
}