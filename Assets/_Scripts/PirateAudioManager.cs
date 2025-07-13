using UnityEngine;
using State = PirateController.State;

public class PirateAudioManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public State currentState { get; private set; }
    private PirateController pirate;
    private AudioSource audioSource;
    private AudioClip lastClipPlayed = null; // Per evitare di riprodurre lo stesso clip consecutivamente


    [Header("Audio Clips per Stato")]
    public AudioClip idleClip;
    //public AudioClip walikingClip;
    public AudioClip attackClip;
    public AudioClip beingHealedClip;
    public AudioClip deadClip;

    [Header("Audio Clips per WALKING")]
    public AudioClip[] walkingClips; // Array di 50 suoni di passi
    //Private AudioClip lastClipPlayed;
    [SerializeField] private float walkingClipCooldown = 0.5f; // Tempo fra un passo e l'altro
    [SerializeField] private float nextWalkingClipTime = 0f; // Quando riprodurre il prossimo suono di passi



    void Awake()
    {
        pirate = GetComponent<PirateController>();
        audioSource = GetComponent<AudioSource>();
    }
    void Start()
    {

        //Inizializzo lo stato corrente
        currentState = pirate.GetCurrentState();

    }

    // Update is called once per frame
    void Update()
    {
        currentState = pirate.GetCurrentState();

        if (pirate.IsDead)
        {
            if (deadClip != null && lastClipPlayed != deadClip)
            {
                PlayClip(deadClip, false); // suono di morte
            }
            return; // Non fare nulla se il pirata è morto
        };


        switch (currentState)
        {
            case State.Patrol:
                PlayWalkingSound();
                break;
            case State.Suspicious:
                PlayClip(idleClip, true); // suono sospetto
                break;
            case State.Chasing:
                // suono inseguimento
                PlayWalkingSound();
                break;
            case State.Attacking:
                //PlayClip(attackClip, false); 
                break;
            case State.BeingHealed:
                PlayClip(beingHealedClip, false); // suono di guarigione
                break;
        }


    }

    private void PlayClip(AudioClip clip, bool loop = true)
    {
        if (clip == null || clip == lastClipPlayed)
            return;

        audioSource.Stop(); //Ferma immediatamente qualsiasi suono che l'AudioSource sta attualmente riproducendo.
        audioSource.clip = clip; // Imposta il nuovo AudioClip da riprodurre.
        audioSource.loop = loop; // Specifica se il suono deve essere ripetuto in loop continuo (true) o suonato una volta sola (false).
        audioSource.Play(); //riproduce l'audio 

        lastClipPlayed = clip;
    }

    private void PlayWalkingSound()

    {
        if (Time.time < nextWalkingClipTime)
            return; // Non riprodurre il suono finché non è passato abbastanza tempo

        // Scegli un clip casuale da walkingClips
        AudioClip randomClip = walkingClips[Random.Range(0, walkingClips.Length)];

        // Se il clip è lo stesso dell'ultimo, ne scegliamo un altro
        if (randomClip == lastClipPlayed)
            randomClip = walkingClips[Random.Range(0, walkingClips.Length)];

        // Riproduci il suono
        audioSource.Stop();
        audioSource.clip = randomClip;
        audioSource.loop = false;
        audioSource.Play();

        lastClipPlayed = randomClip;

        // Imposta il prossimo momento in cui un altro suono può essere riprodotto
        nextWalkingClipTime = Time.time + walkingClipCooldown;
    }

    private void PlayIdleSound()
    {
        if (idleClip == null || idleClip == lastClipPlayed) return;

        audioSource.Stop();
        audioSource.clip = idleClip;
        audioSource.loop = true; // Loop per l'idle
        audioSource.Play();

        lastClipPlayed = idleClip;
    }
    
    public void PlayAttackSound()
    {
        if (attackClip == null) return;

        audioSource.Stop();
        audioSource.clip = attackClip;
        audioSource.loop = false;
        audioSource.Play();

        lastClipPlayed = attackClip;
    }

}
