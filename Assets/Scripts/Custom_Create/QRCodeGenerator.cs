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

        string folderPath = Path.Combine(Application.persistentDataPath, "QRCodeExports");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, moduleName + "_QR.png");
        File.WriteAllBytes(filePath, pngData);

        Debug.Log($"QR Code saved at: {filePath}");
        return filePath;
    }
}
