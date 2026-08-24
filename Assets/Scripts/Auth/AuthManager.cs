using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class AuthManager : MonoBehaviour
{
    private FirebaseAuth auth;
    private GoogleSignInConfiguration googleConfig;

    [Header("Login")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register")]
    public TMP_InputField registerNameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;

    [Header("Forgot Password")]
    public TMP_InputField forgotPasswordEmailInput;

    [Header("Status")]
    public TextMeshProUGUI statusText;

    [Header("Popup UI")]
    public GameObject popupPanel;

    private void Start()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;

                googleConfig = new GoogleSignInConfiguration
                {
                    WebClientId = "26017803680-2fpo3om07o30jog06jmliufnpvvb9icu.apps.googleusercontent.com",
                    RequestIdToken = true,
                    RequestEmail = true
                };

                // Auto-login persistence
                if (auth.CurrentUser != null)
                {
                    SceneManager.LoadScene("StartScene");
                }
            }
            else
            {
                statusText.text = "Firebase initialization failed.";
                Debug.LogError(task.Result);
            }
        });
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && googleConfig != null)
        {
            GoogleSignIn.Configuration = null;
            GoogleSignIn.Configuration = googleConfig;
        }
    }

    private void ShowPopup()
    {
        if (popupPanel == null) return;

        popupPanel.SetActive(true);

        CancelInvoke(nameof(HidePopup));
        Invoke(nameof(HidePopup), 3f);
    }

    private void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public void GuestLogin()
    {
        if (auth == null) return;

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
                SceneManager.LoadScene("StartScene");
            else
            {
                statusText.text = "Guest login failed.";
                Debug.LogError(task.Exception);
            }
        });
    }

    public void EmailLogin()
    {
        if (auth == null) return;

        auth.SignInWithEmailAndPasswordAsync(
            loginEmailInput.text.Trim(),
            loginPasswordInput.text
        ).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                SceneManager.LoadScene("StartScene");
            }
            else
            {
                statusText.text = "Login failed. Check credentials.";
                Debug.LogError(task.Exception);
            }
        });
    }

    public void Register()
    {
        if (auth == null) return;

        if (registerPasswordInput.text != registerConfirmPasswordInput.text)
        {
            statusText.text = "Passwords do not match.";
            return;
        }

        string email = registerEmailInput.text.Trim();

        auth.FetchProvidersForEmailAsync(email)
            .ContinueWithOnMainThread(providerTask =>
            {
                if (providerTask.IsFaulted)
                {
                    statusText.text = "Could not verify email.";
                    Debug.LogError(providerTask.Exception);
                    return;
                }

                var providers = providerTask.Result;

                if (providers != null && providers.Count() > 0)
                {
                    ShowPopup();
                    return;
                }

                auth.CreateUserWithEmailAndPasswordAsync(
                    email,
                    registerPasswordInput.text
                ).ContinueWithOnMainThread(registerTask =>
                {
                    if (registerTask.IsCompletedSuccessfully)
                    {
                        statusText.text = "Account created.";
                        SceneManager.LoadScene("StartScene");
                    }
                    else
                    {
                        if (registerTask.Exception != null &&
                            registerTask.Exception.ToString().ToLower().Contains("already"))
                        {
                            ShowPopup();
                            return;
                        }

                        statusText.text = "Registration failed.";
                        Debug.LogError(registerTask.Exception);
                    }
                });
            });
    }

    public void ResetPassword()
    {
        if (auth == null) return;

        string email = forgotPasswordEmailInput.text.Trim();

        auth.SendPasswordResetEmailAsync(email)
            .ContinueWithOnMainThread(resetTask =>
            {
                if (resetTask.IsCompletedSuccessfully)
                {
                    statusText.text = "Reset email sent.";
                }
                else
                {
                    statusText.text = "Password reset failed.";
                    Debug.LogError(resetTask.Exception);
                }
            });
    }

    public void GoogleSignInUser()
    {
        if (auth == null || googleConfig == null)
        {
            statusText.text = "Auth not initialized yet.";
            return;
        }

        GoogleSignIn.Configuration = null;
        GoogleSignIn.Configuration = googleConfig;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                statusText.text = "Google Sign-In cancelled.";
                return;
            }

            if (task.IsFaulted)
            {
                statusText.text = "Google Sign-In failed.";
                Debug.LogError(task.Exception);
                return;
            }

            GoogleSignInUser googleUser = task.Result;

            string idToken = googleUser.IdToken;

            if (string.IsNullOrEmpty(idToken))
            {
                statusText.text = "Google ID token missing.";
                return;
            }

            Credential credential =
                GoogleAuthProvider.GetCredential(idToken, null);

            auth.SignInWithCredentialAsync(credential)
                .ContinueWithOnMainThread(firebaseTask =>
                {
                    if (firebaseTask.IsCompletedSuccessfully)
                    {
                        FirebaseUser firebaseUser = auth.CurrentUser;

                        if (firebaseUser != null &&
                            string.IsNullOrEmpty(firebaseUser.DisplayName) &&
                            !string.IsNullOrEmpty(googleUser.DisplayName))
                        {
                            UserProfile profile = new UserProfile
                            {
                                DisplayName = googleUser.DisplayName
                            };

                            firebaseUser.UpdateUserProfileAsync(profile);
                        }

                        statusText.text = "Google Sign-In successful.";
                        SceneManager.LoadScene("StartScene");
                    }
                    else
                    {
                        statusText.text = "Firebase Google auth failed.";
                        Debug.LogError(firebaseTask.Exception);
                    }
                });
        });
    }
}