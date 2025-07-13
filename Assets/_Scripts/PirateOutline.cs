using UnityEngine;

public class PirateOutline : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;
    private Renderer _renderer;
    private Material _defaultMaterial;
    private bool _outlineActive;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _defaultMaterial = _renderer.material;
    }

    public void SetOutline(bool enable)
    {
        if (_renderer == null || outlineMaterial == null) return;
        if (_outlineActive == enable) return;
        _outlineActive = enable;

        if (enable)
        {
            // aggiunge l’outline come secondo materiale
            Material[] mats = new Material[2] {
                _renderer.materials[0],
                outlineMaterial
            };
            _renderer.materials = mats;
        }
        else
        {
            // torna al solo materiale di default
            Material[] mats = new Material[1] { _defaultMaterial };
            _renderer.materials = mats;
        }
    }
}
