using UnityEngine;
using UnityEngine.UI;
using ZXing;
using System;
using UnityEngine.SceneManagement;
using System.IO;

public class QRCodeScanner : MonoBehaviour
{
    public RawImage cameraFeedImage;
    public Text feedbackText; // Optional UI text to show status
    private WebCamTexture webcamTexture;
    private IBarcodeReader reader;

    private bool isScanning = true;
    private bool isAdjusted = false;

    void Start()
    {
        webcamTexture = new WebCamTexture();
        cameraFeedImage.texture = webcamTexture;
        cameraFeedImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();

        reader = new BarcodeReader();
    }

    void Update()
    {
        if (!isScanning || webcamTexture == null || !webcamTexture.isPlaying)
            return;

        // Wait until webcam is ready
        if (webcamTexture.width < 100 || webcamTexture.height < 100)
            return;

        // Adjust only once after webcam is initialized
        if (!isAdjusted)
        {
            AdjustCameraFeedAspect();
            isAdjusted = true;
        }

        try
        {
            Color32[] pixels = webcamTexture.GetPixels32();
            var result = reader.Decode(pixels, webcamTexture.width, webcamTexture.height);

            if (result != null)
            {
                isScanning = false;

                string moduleJson = result.Text;
                Debug.Log("QR Code Scanned: " + moduleJson);

                EnvironmentData importedData = JsonUtility.FromJson<EnvironmentData>(moduleJson);

                if (importedData != null && !string.IsNullOrEmpty(importedData.environmentName))
                {
                    feedbackText.text = $"Scanned: {importedData.environmentName}";

                    string moduleName = importedData.environmentName;
                    string jsonPath = ModuleSaveManager.GetModulePath(moduleName);

                    string json = JsonUtility.ToJson(importedData, true);
                    File.WriteAllText(jsonPath, json);

                    PlayerPrefs.SetString(moduleName, json);
                    PlayerPrefs.SetString("SelectedModulePath", jsonPath);
                    PlayerPrefs.Save();

                    Debug.Log($"Module '{moduleName}' imported and saved at: {jsonPath}");
                    Invoke("ReturnToStart", 2f);
                }
                else
                {
                    feedbackText.text = "Failed to parse module.";
                    isScanning = true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("QR scan error: " + ex.Message);
        }
    }

    void AdjustCameraFeedAspect()
    {
        if (cameraFeedImage == null || webcamTexture == null) return;

        float videoRatio = (float)webcamTexture.width / webcamTexture.height;
        RectTransform rt = cameraFeedImage.GetComponent<RectTransform>();
        RectTransform parentRT = rt.parent.GetComponent<RectTransform>();

        float parentW = parentRT.rect.width;
        float parentH = parentRT.rect.height;

        float parentRatio = parentW / parentH;

        if (videoRatio > parentRatio)
        {
            // Wider: full width, adjust height
            rt.sizeDelta = new Vector2(parentW, parentW / videoRatio);
        }
        else
        {
            // Taller: full height, adjust width
            rt.sizeDelta = new Vector2(parentH * videoRatio, parentH);
        }
    }

    public void ReturnToStart()
    {
        webcamTexture.Stop();
        SceneManager.LoadScene("StartScreenScene");
    }
}
