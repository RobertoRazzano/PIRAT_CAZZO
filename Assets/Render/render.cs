using UnityEngine;
using System.IO;
using System.Collections;

public class ScreenshotRenderer : MonoBehaviour
{
    public RenderTexture renderTexture;
    public Camera renderCamera;

    void Start()
    {
        StartCoroutine(CaptureAfterFrame());
    }

    IEnumerator CaptureAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        // Forza il render
        if (renderCamera != null)
        {
            renderCamera.targetTexture = renderTexture;
            renderCamera.Render();
        }

        SaveRenderTextureToPNG();
    }

    void SaveRenderTextureToPNG()
    {
        RenderTexture.active = renderTexture;

        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/RenderedImage.png";
        File.WriteAllBytes(path, bytes);

        Debug.Log("✔️ Saved to: " + path);

        RenderTexture.active = null;
        Destroy(tex);
    }
}
