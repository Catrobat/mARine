using UnityEngine;

public class AuthUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject landingPanel;
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject forgotPasswordPanel;

    // Landing → Login
    public void ShowLogin()
    {
        landingPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    // Landing → Register
    public void ShowRegister()
    {
        landingPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    // Login/Register → Landing
    public void ShowLanding()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        landingPanel.SetActive(true);
    }

    // Login → Forgot Password
    public void ShowForgotPassword()
    {
        loginPanel.SetActive(false);
        forgotPasswordPanel.SetActive(true);
    }

    // Forgot Password → Login
    public void ShowLoginFromForgot()
    {
        forgotPasswordPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    // Login → Register
    public void ShowRegisterFromLogin()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    // Register → Login
    public void ShowLoginFromRegister()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    // Register → Landing (Back button)
    public void ShowLandingFromRegister()
    {
        registerPanel.SetActive(false);
        landingPanel.SetActive(true);
    }
}