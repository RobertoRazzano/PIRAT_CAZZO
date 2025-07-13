using UnityEngine;
using UnityEditor;

public class GenerateRatOutlineMesh : MonoBehaviour
{
    [MenuItem("Tools/Generate Outline Mesh for Rat")]
    private static void GenerateOutlineMesh()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("Nessun oggetto selezionato. Seleziona il GameObject con SkinnedMeshRenderer.");
            return;
        }

        SkinnedMeshRenderer smr = selected.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError("Il GameObject selezionato non ha uno SkinnedMeshRenderer.");
            return;
        }

        string outlineName = selected.name + "_Outline";
        Transform parent = selected.transform.parent;
        Transform existingOutline = parent.Find(outlineName);
        if (existingOutline != null)
        {
            Debug.LogWarning("Esiste già una mesh di outline. Rimuovila prima di crearne una nuova.");
            return;
        }

        // Crea GameObject
        GameObject outlineGO = new GameObject(outlineName);
        outlineGO.transform.SetParent(parent);
        outlineGO.transform.localPosition = selected.transform.localPosition;
        outlineGO.transform.localRotation = selected.transform.localRotation;
        outlineGO.transform.localScale = selected.transform.localScale;

        // Copia la SkinnedMeshRenderer
        SkinnedMeshRenderer outlineSMR = outlineGO.AddComponent<SkinnedMeshRenderer>();
        outlineSMR.sharedMesh = smr.sharedMesh;
        outlineSMR.rootBone = smr.rootBone;
        outlineSMR.bones = smr.bones;

        // Imposta materiali
        string path = EditorUtility.OpenFilePanel("Seleziona il materiale Outline", "Assets", "mat");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Operazione annullata. Nessun materiale selezionato.");
            DestroyImmediate(outlineGO);
            return;
        }

        // Converte il path in asset path relativo
        if (!path.StartsWith(Application.dataPath))
        {
            Debug.LogError("Il materiale deve essere nella cartella Assets.");
            DestroyImmediate(outlineGO);
            return;
        }
        string assetPath = "Assets" + path.Substring(Application.dataPath.Length);
        Material outlineMat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        if (outlineMat == null)
        {
            Debug.LogError("Materiale non valido.");
            DestroyImmediate(outlineGO);
            return;
        }

        int subMeshCount = smr.sharedMesh.subMeshCount;
        Material[] materials = new Material[subMeshCount];
        for (int i = 0; i < subMeshCount; i++)
        {
            materials[i] = outlineMat;
        }
        outlineSMR.sharedMaterials = materials;

        // Ottimizzazioni opzionali
        outlineSMR.updateWhenOffscreen = true;
        outlineSMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineSMR.receiveShadows = false;
        outlineSMR.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        Debug.Log("✅ Outline mesh creata con successo per: " + selected.name);
    }
}
