using UnityEngine;
using UnityEngine.UI;

public class QuickTimeUIManager : MonoBehaviour
{
    public RectTransform shrinkingImage;
    public RectTransform targetImage;
    public float duration = 1.5f;
    public float CurrentScale => shrinkingImage.localScale.x;
    public float StartingScale => 2.2f; // oppure [SerializeField]

    private float timer = 0f;
    private bool active = false;

    public bool IsQuickTimeActive => active;
    public float Precision => Mathf.Clamp01(timer / duration);
    public bool tutorialMode = false;


    public void StartQuickTime()
    {
        timer = 0f;
        active = true;
        gameObject.SetActive(true);

        duration = tutorialMode ? 3.5f : 1.5f;

        Time.timeScale = 0f;

        if (shrinkingImage != null)
            shrinkingImage.localScale = Vector3.one * 2.2f;
    }

    public void StopQuickTime()
    {
        active = false;
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!active) return;


        timer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(timer / duration);

        if (shrinkingImage != null)
        {
            float start = 3f;
            float end = 0.2f;
            float current = Mathf.Lerp(start, end, t);
            shrinkingImage.localScale = new Vector3(current, current, current);
        }

        if (t >= 1f)
        {
            StopQuickTime();
        }
    }
}