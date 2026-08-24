using Firebase.Auth;
using Firebase.Extensions;
using Google;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AccountManager : MonoBehaviour
{
    public void SignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();

#if UNITY_ANDROID && !UNITY_EDITOR
    GoogleSignIn.DefaultInstance.SignOut();

    // Reset Google config singleton
    GoogleSignIn.Configuration = null;
#endif

        Debug.Log("User signed out.");

        SceneManager.LoadScene("AuthScene");
    }

    public void SendResetPasswordEmail()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;

        if (user == null)
        {
            Debug.LogError("No user signed in.");
            return;
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            Debug.LogError("No email linked to this account.");
            return;
        }

        FirebaseAuth.DefaultInstance
            .SendPasswordResetEmailAsync(user.Email)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("Password reset email sent.");
                }
                else
                {
                    Debug.LogError("Failed to send reset email: " + task.Exception);
                }
            });
    }
}