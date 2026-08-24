using UnityEngine;
using Firebase;
using Firebase.Firestore;
using System.Threading.Tasks;

public class FirebaseTest : MonoBehaviour
{
    async void Start()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status != DependencyStatus.Available)
        {
            Debug.LogError("Firebase dependency issue: " + status);
            return;
        }

        Debug.Log("Firebase Ready");

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        DocumentSnapshot snap =
            await db.Collection("packages")
                    .Document("essential")
                    .GetSnapshotAsync();

        if (snap.Exists)
        {
            Debug.Log("Package Found!");

            Debug.Log("Package Name: " + snap.GetValue<string>("packageName"));
            Debug.Log("Entry Scene: " + snap.GetValue<string>("entryScene"));
            Debug.Log("Version: " + snap.GetValue<long>("version"));
            Debug.Log("Settings URL: " + snap.GetValue<string>("settingsUrl"));
            Debug.Log("Catalog URL: " + snap.GetValue<string>("catalogBinUrl"));
            Debug.Log("Bundle URL: " + snap.GetValue<string>("bundleUrl"));
        }
        else
        {
            Debug.LogError("Document not found");
        }
    }
}