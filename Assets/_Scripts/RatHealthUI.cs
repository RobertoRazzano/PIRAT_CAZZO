using UnityEngine;
using UnityEngine.UI;

public class RatHealthUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image healthFill;
    

    public void UpdateHealthBar(int current, int max)
    {
        // Debug.Log($"UpdateHealthBar called: {current}/{max}");
        if (healthFill != null)
        {
            float percent = (float)current / max;
            healthFill.fillAmount = percent;
        }
    }
}
