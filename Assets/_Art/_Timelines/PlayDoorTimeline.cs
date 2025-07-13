using UnityEngine;
using UnityEngine.Playables;

public class PlayDoorTimeline : MonoBehaviour
{
    [SerializeField]
    private PlayableDirector director;

    [SerializeField]
    private float delay = 0.5f;

    void Start()
    {
        if (director != null)
        {
            Invoke(nameof(PlayTimeline), delay);
        }
        else
        {
            Debug.LogWarning("PlayableDirector non assegnato su " + gameObject.name);
        }
    }

    private void PlayTimeline()
    {
        director.Play();
    }
}