using UnityEngine;

public class DialoguePromptEvents : MonoBehaviour
{
    public PromptUIManager promptUI;

    public void ShowMovePrompt()
    {
        promptUI.ShowPrompt(InputKeyType.LeftStick, "Muoviti con lo stick sinistro");
    }

    public void ShowBitePrompt()
    {
        promptUI.ShowPrompt(InputKeyType.ButtonSouth, "Premi A per mordere la guardia");
    }

    public void HidePrompt()
    {
        promptUI.HidePrompt();
    }
}

