using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SceneTransitionZone : MonoBehaviour
{
    [Header("Build Settings")]
    [Tooltip("Indice della scena successiva (0-based, come in Build Settings)")]
    public int nextSceneBuildIndex;

    [Header("Input System")]
    [Tooltip("Il tuo asset .inputactions")]
    public InputActionAsset inputActions;
    [Tooltip("Nome della Action Map (es. \"Player\")")]
    public string actionMapName = "Player";
    [Tooltip("Nome dell'azione da premere per confermare (es. \"ChangeScene\")")]
    public string actionName = "ChangeScene";

    [Header("Prompt UI Manager")]
    [Tooltip("Il componente che gestisce la UI e il timeScale")]
    public PromptUIManager promptUI;

    private InputAction _changeSceneAction;
    private bool _isInZone;

    private void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogError($"[{name}] InputActionAsset non assegnato!");
            return;
        }

        // Trovo l'action specifica dentro l'asset
        _changeSceneAction = inputActions.FindAction($"{actionMapName}/{actionName}");
        if (_changeSceneAction == null)
        {
            Debug.LogError($"[{name}] Action '{actionName}' non trovata in map '{actionMapName}'");
            return;
        }

        _changeSceneAction.performed += OnChangeScene;
        _changeSceneAction.Enable();
    }

    private void OnDisable()
    {
        if (_changeSceneAction != null)
        {
            _changeSceneAction.performed -= OnChangeScene;
            _changeSceneAction.Disable();
            _changeSceneAction = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isInZone = true;

        // Mostra il prompt e congela il tempo
        promptUI.ShowPrompt(
            InputKeyType.ButtonEast,              // scegli l'icona opportuna
            "CHANGE FLOOR",  // messaggio dinamico
            freezeTime: true                      // blocca Time.timeScale
        );
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _isInZone = false;

        // Nasconde prompt e riporta il tempo
        promptUI.HidePrompt();
    }

    private void OnChangeScene(InputAction.CallbackContext ctx)
    {
        if (!_isInZone) return;

        // 1) nascondo subito il prompt (unfreeze + nasconde tutto)
        promptUI.HidePrompt();

        // 2) salvo stato e cambio scena
        GameStateManager.Instance.SaveRatData();
        SceneManager.LoadScene(nextSceneBuildIndex);
    }

}
