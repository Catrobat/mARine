using UnityEngine;

public class SketchfabViewer : MonoBehaviour
{
    private WebViewObject webViewObject;
    private bool isVisible = false;

    const string sketchfabURL = "https://sketchfab.com/models/ab347bb7a8b84ebb8b52bf9c580c54eb/embed" +
                                "?autostart=1" +
                                "&annotations_visible=1" +
                                "&ui_stop=0" +
                                "&ui_inspector=0" +
                                "&ui_watermark_link=0" +
                                "&ui_help=0" +
                                "&ui_settings=0" +
                                "&fullscreen=0" +
                                "&ui_vr=0";

    public void ShowSketchfab()
    {
        if (webViewObject == null)
        {
            webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
            webViewObject.Init(
                enableWKWebView: true,
                transparent: false,
                zoom: true,
                ua: "Unity"
            );

            webViewObject.SetMargins(0, 0, 0, 0);
            webViewObject.LoadURL(sketchfabURL);
        }

        webViewObject.SetVisibility(true);
        isVisible = true;
    }

    public void HideSketchfab()
    {
        if (webViewObject != null)
        {
            webViewObject.SetVisibility(false);
        }

        isVisible = false;
    }

    void Update()
    {
        if (isVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            HideSketchfab(); // Back button closes viewer
        }
    }
}
