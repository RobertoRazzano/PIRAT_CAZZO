using UnityEngine;
using UnityEngine.UI;  // Required for Slider

public class InfectionSkillCheckUI : MonoBehaviour
{
    [SerializeField] private Slider _bar;  // Changed from GameObject to Slider
    [SerializeField] private float oscillationSpeed = 2f;
    //private CameraManager _cameraManager;
    private bool isActive = false;
    private float time = 0f;

    [SerializeField] private PirateController _enemyController;  // Added reference to EnemyController

    void Start()
    {
        if (_bar != null)  // Fixed syntax error
        {
            _bar.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Bar not found!");
        }
        //_cameraManager = FindObjectOfType<CameraManager>();
    }

    void Update()
    {
        if (isActive)
        {
            time += Time.deltaTime * oscillationSpeed;
            float value = (Mathf.Sin(time) + 1f) / 2f;
            _bar.value = value;

            if (Input.GetKeyDown(KeyCode.Space))  // Fixed Input capitalization
            {
                CheckResult(value);
                isActive = false;
                _bar.gameObject.SetActive(false);
            }
        }
    }

    public void StartSkillCheck()  // Changed to PascalCase
    { 
        isActive = true;
        time = 0f;
        _bar.gameObject.SetActive(true);
        Debug.Log("Skill check started!");
    }

    private void CheckResult(float value)
    {
        Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        Vector3 jumpDirection = -playerTransform.forward; // Backward direction
        
        if (value < 0.6f)
        {
            Debug.Log("Success!");
            // infezione + salto lontano 
            playerTransform.position += jumpDirection * 8f;
        }
        else if (value < 0.9f)
        {
            Debug.Log("Chase!");
            //salto medio
            playerTransform.position += jumpDirection * 2f;
        }
        else
        {
            // il pirata si gira verso il topo e comincia ad inseguirlo
            
            Debug.Log("Failure!");
            
        }

        // Trova un pirata vicino (oppure usa già la reference se ce l'hai)
        GameObject pirate = GameObject.FindGameObjectWithTag("Pirate");

        /* if (_cameraManager != null && pirate != null)
        {
            _cameraManager.SwitchToPirata(pirate.transform);
        } */
    }
}
