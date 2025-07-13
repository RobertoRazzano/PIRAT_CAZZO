using UnityEngine;

public class TrapTrigger : MonoBehaviour
{
    public PromptUIManager promptUIManager;
    private bool hasTriggered = false;
    public TrapType trapType;


    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;
        if (TutorialManager.HasTutorialBeenShown(trapType)) return;

        hasTriggered = true;
        TutorialManager.MarkTutorialAsShown(trapType);

        switch (trapType)
        {
            case TrapType.Spring:
                promptUIManager.ShowText("Attention! This trap deals damage to you", true);
                break;

            case TrapType.Glue:
                promptUIManager.ShowText("Attention! This trap holds you, move to break here", true);
                break;

            case TrapType.Slide:
                promptUIManager.ShowText("Attention! This trap makes you slip", true);
                break;
        }
    }
}
