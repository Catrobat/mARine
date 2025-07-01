using System.IO;
using UnityEngine;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;

public static class QRCodeGenerator
{
    public static string GenerateAndSaveQRCode(string dataToEncode, string moduleName)
    {
        var qrWriter = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 512,
                Width = 512,
                Margin = 1
            }
        };

        var pixelData = qrWriter.Write(dataToEncode);

        // Convert to Texture2D
        Texture2D qrTexture = new Texture2D(pixelData.Width, pixelData.Height, TextureFormat.RGBA32, false);
        qrTexture.LoadRawTextureData(pixelData.Pixels);
        qrTexture.Apply();

        byte[] pngData = qrTexture.EncodeToPNG();
        if (pngData == null)
        {
            Debug.LogError("Failed to encode QR to PNG.");
            return null;
        }

        // Save in persistent data path (internal storage, not user accessible)
        string internalFolder = Path.Combine(Application.persistentDataPath, "QRCodeExports");
        if (!Directory.Exists(internalFolder))
            Directory.CreateDirectory(internalFolder);

        string internalFilePath = Path.Combine(internalFolder, moduleName + "_QR.png");
        File.WriteAllBytes(internalFilePath, pngData);

        Debug.Log($"Internal QR saved at: {internalFilePath}");

#if UNITY_ANDROID && !UNITY_EDITOR
        // Also save in Downloads folder for user access
        string externalFolder = Path.Combine("/storage/emulated/0/Download", "QRCodeExports");
        if (!Directory.Exists(externalFolder))
            Directory.CreateDirectory(externalFolder);

        string externalFilePath = Path.Combine(externalFolder, moduleName + "_QR.png");
        File.WriteAllBytes(externalFilePath, pngData);
        Debug.Log($"External QR saved at: {externalFilePath}");
#endif

        return internalFilePath; // Return internal path for app logic
    }
}
