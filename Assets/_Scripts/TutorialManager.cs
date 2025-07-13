using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private static TutorialManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- Cheese Tutorials
    public static bool HasTutorialBeenShown(CheesePowerUpType type)
    {
        return GameStateManager.Instance.HasSeenTutorial($"Cheese_{type}");
    }

    public static void MarkTutorialAsShown(CheesePowerUpType type)
    {
        GameStateManager.Instance.SetTutorialSeen($"Cheese_{type}", true);
    }

    // --- Trap Tutorials
    public static bool HasTutorialBeenShown(TrapType type)
    {
        return GameStateManager.Instance.HasSeenTutorial($"Trap_{type}");
    }

    public static void MarkTutorialAsShown(TrapType type)
    {
        GameStateManager.Instance.SetTutorialSeen($"Trap_{type}", true);
    }
}
