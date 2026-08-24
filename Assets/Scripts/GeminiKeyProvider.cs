using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;

public static class GeminiKeyProvider
{
    private const string k_Collection = "configs";
    private const string k_Document = "apiKey";
    private const string k_Field = "GEMINI_API_KEY";

    private static string s_CachedKey;

    public static async Task<string> FetchFromFirestoreAsync()
    {
        if (!string.IsNullOrWhiteSpace(s_CachedKey))
            return s_CachedKey;

        try
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogWarning($"[GeminiKeyProvider] Firebase dependencies unavailable: {status}");
                return "";
            }

            DocumentSnapshot snap = await FirebaseFirestore.DefaultInstance
                .Collection(k_Collection).Document(k_Document).GetSnapshotAsync();

            if (!snap.Exists || !snap.TryGetValue(k_Field, out string key) || string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning($"[GeminiKeyProvider] {k_Collection}/{k_Document} missing or empty {k_Field}.");
                return "";
            }

            s_CachedKey = key;
            return key;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GeminiKeyProvider] Failed to fetch Gemini key from Firestore: {ex.Message}");
            return "";
        }
    }
}
