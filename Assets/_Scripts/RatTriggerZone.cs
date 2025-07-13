using UnityEngine;

public class RatTriggerZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // o "Rat", dipende dal tag
        {
            FindFirstObjectByType<DialogueManager>().ContinuePrisonerDialogue();
            gameObject.SetActive(false); // se vuoi disattivarlo dopo
        }
    }
}
