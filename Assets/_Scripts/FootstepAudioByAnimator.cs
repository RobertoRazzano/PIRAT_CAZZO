using UnityEngine;

public class RatAudioControllerByAnimator : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float footstepVolume = 0.7f;

    public AudioClip biteClip;
    [Range(0f, 1f)] public float biteVolume = 0.8f;

    [Header("Animator States")]
    public string walkingStateName = "WalkRatAnimation";
    public string biteStateName1 = "Bite";
    public string biteStateName2 = "BiteWithJumpBack";

    private Animator animator;
    private AudioSource audioSource;

    private bool isWalking = false;
    private bool bitePlayed = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

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
        string currentState = stateInfo.IsName(walkingStateName) ? walkingStateName :
                              stateInfo.IsName(biteStateName1) ? biteStateName1 :
                              stateInfo.IsName(biteStateName2) ? biteStateName2 : "";

        // --- Passi ---
        if (currentState == walkingStateName)
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

        // --- Morso (in uno dei due stati previsti) ---
        if (stateInfo.IsName(biteStateName2)) // Solo BITE con salto all'indietro
        {
            if (!bitePlayed && biteClip != null)
            {
                audioSource.loop = false;
                audioSource.PlayOneShot(biteClip, biteVolume);
                bitePlayed = true;
            }
        }
        else
        {
            bitePlayed = false;
        }
            }
}