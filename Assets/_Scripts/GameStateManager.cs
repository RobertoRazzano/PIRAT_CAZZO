using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public Dictionary<string, bool> tutorialsSeen = new Dictionary<string, bool>();
    public string lastSceneName = "";

    private CameraControlManager cameraControlManager;

    [Header("Riferimenti al topo")]
    public BonusMalus bonusMalus;                   // componente che gestisce la vita
    public RatInputHandler ratInputHandler;         // tuo handler dei movimenti e boost
    public RatInteractionManager ratInteraction;    // gestisce damage boost, poison leak, ecc.

    [Header("PowerUp Configuration")]
    public PowerUpConfig powerUpConfig;

    [SerializeField] private GameObject tutorial;
    [SerializeField] private GameObject dialogueManager;

    [Header("Tag del punto di spawn in ogni scena")]
    public string spawnPointTag = "SpawnPoint";

    [System.Serializable]
    private class RatData
    {
        public int health;

        public bool speedActive;
        public float speedMultiplier;
        public float speedTimeLeft;

        public bool damageActive;
        public int damageAmount;

        public bool poisonReady;
    }
    private RatData ratData = new RatData();

    // serve per non applicare il LoadRatData() sulla prima scena all'avvio
    private bool skipInitialLoad = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Chiamalo **prima** di cambiare scena
    /// </summary>
    public void SaveRatData()
    {
        // Aggiungi controlli nulli per assicurarti che i riferimenti siano validi prima di usarli.
        if (bonusMalus == null || ratInputHandler == null || ratInteraction == null)
        {
            Debug.LogWarning("GameStateManager: Tentativo di salvare dati del ratto, ma alcuni riferimenti (BonusMalus, RatInputHandler, RatInteractionManager) non sono validi. Assicurati che il ratto sia presente e configurato nella scena attuale.");
            return; // Interrompi la funzione se i riferimenti sono nulli per evitare l'errore.
        }

        ratData.health = bonusMalus.currentHealth;

        ratData.speedActive = ratInputHandler.speedBoostActive;
        ratData.speedMultiplier = ratInputHandler.currentSpeedBoostMultiplier;
        ratData.speedTimeLeft = ratInputHandler.speedBoostRemainingTime;

        ratData.damageActive = ratInteraction.IsDamageBoostActive;
        ratData.damageAmount = ratInteraction.GetCurrentDamageBoostAmount();

        ratData.poisonReady = ratInteraction.CanPee;
    }


    // In GameStateManager.cs

    void Update()
    {
        // Controlla i tasti numerici da 1 a 4
        for (int i = 0; i <= 3; i++) // Per scene 0, 1, 2, 3
        {
            if (Keyboard.current != null && Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
            {
                // Verifica che l'indice della scena sia valido
                if (i < SceneManager.sceneCountInBuildSettings)
                {
                    Debug.Log($"Caricamento scena di debug: {i}");
                    SaveRatData(); // Salva i dati del topo prima di cambiare scena
                    SceneManager.LoadScene(i); // Carica la scena corrispondente all'indice
                }
                else
                {
                    Debug.LogWarning($"La scena con indice {i} non esiste nelle Build Settings.");
                }
                break; // Esci dal loop una volta trovato il tasto premuto
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (skipInitialLoad)
        {
            skipInitialLoad = false;
            lastSceneName = scene.name;
            return;
        }

        cameraControlManager = FindFirstObjectByType<CameraControlManager>();

        // --- LOGICA AGGIORNATA ---
        var ratGO = GameObject.FindGameObjectWithTag("Player");
        if (ratGO != null)
        {
            bonusMalus = ratGO.GetComponent<BonusMalus>();
            ratInputHandler = ratGO.GetComponent<RatInputHandler>();

            // Assicurati che anche ratInteraction venga assegnato qui.
            ratInteraction = ratGO.GetComponent<RatInteractionManager>();

            // 1. AGGIORNA IL TARGET DELLA CAMERA SUBITO!
            if (cameraControlManager != null)
            {
                cameraControlManager.UpdateTarget(ratGO.transform);
            }
        }
        else
        {
            Debug.LogWarning("GameStateManager: non ho trovato il Player in scena.");
            return;
        }

        var spawn = GameObject.FindWithTag(spawnPointTag);
        if (spawn != null && bonusMalus != null)
        {
            var rt = bonusMalus.transform;
            rt.position = spawn.transform.position;
            rt.rotation = spawn.transform.rotation;
        }

        bool isRespawning = (scene.name == lastSceneName);

        if (isRespawning)
        {
            Debug.Log("Respawning in the same scene. Resetting health and camera.");
            bonusMalus.currentHealth = bonusMalus.maxHealth;

            // ✅ AGGIUNGI QUESTO: Ferma esplicitamente la coroutine dello zoom di morte
            if (cameraControlManager != null)
            {
                if (cameraControlManager.deathZoomCoroutine != null)
                {
                    cameraControlManager.StopCoroutine(cameraControlManager.deathZoomCoroutine);
                    cameraControlManager.deathZoomCoroutine = null;
                }
            }
        }
        else
        {
            Debug.Log("Loading new scene. Loading rat data.");
           LoadRatData(); // La tua funzione per caricare i dati
        }

        // 2. RESETTA LA TELECAMERA (ora nell'ordine corretto)
        if (cameraControlManager != null)
        {
            // Prima resetta il yaw, POI resetta la camera
            cameraControlManager.ResetYawOnRespawn();
            cameraControlManager.ResetCameraAfterRespawn();
        }

        // Aggiorna la UI della salute
        var healthUI = FindObjectOfType<RatHealthUI>();
        if (healthUI != null && bonusMalus != null)
            healthUI.UpdateHealthBar(bonusMalus.currentHealth, bonusMalus.maxHealth);

        lastSceneName = scene.name;
    }



    public void SetTutorialSeen(string tutorialName, bool seen = true)
    {
        tutorialsSeen[tutorialName] = seen;
        
        
    }

    public bool HasSeenTutorial(string tutorialName)
    {
        return tutorialsSeen.ContainsKey(tutorialName) && tutorialsSeen[tutorialName];
    }

    private void LoadRatData()
    {
        // 1) Ripristina la vita
        bonusMalus.currentHealth = ratData.health;
        bonusMalus.onHealthChanged?.Invoke(ratData.health, bonusMalus.maxHealth);

        // 2) Ripristino power-up via flags + config
        ratInteraction.ApplyPowerUps(
            ratData.speedActive,      // flag speed
            ratData.speedMultiplier,  // moltiplicatore
            ratData.speedTimeLeft,    // tempo rimanente

            ratData.damageActive,     // flag damage
            ratData.damageAmount,     // quantità danno

            ratData.poisonReady       // flag pipì
        );
    }

}
