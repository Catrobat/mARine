using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(RectTransform))]
public class FaceScreenSpaceMaskController : MonoBehaviour
{
    [SerializeField] private float widthFromEyeDistance = 1.9f;
    [SerializeField] private Vector2 pixelOffset = new(0f, 0f);
    [SerializeField] private float smoothing = 18f;

    private const int LeftEyeOuter = 33;
    private const int RightEyeOuter = 263;

    private ARFace _face;
    private Camera _camera;
    private RectTransform _canvasRect;
    private RectTransform _rect;
    private Image _image;
    private float _aspect = 0.5f;

    public void Initialize(ARFace face, Camera arCamera, Canvas canvas, Sprite sprite)
    {
        _face = face;
        _camera = arCamera;
        _canvasRect = canvas.transform as RectTransform;
        _rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();

        if (_image != null)
        {
            _image.sprite = sprite;
            _image.raycastTarget = false;
            _image.preserveAspect = true;
        }

        if (sprite != null && sprite.rect.width > 0f)
            _aspect = sprite.rect.height / sprite.rect.width;
    }

    private void LateUpdate()
    {
        if (_face == null || _camera == null || _canvasRect == null)
            return;

        if (!_face.vertices.IsCreated || _face.vertices.Length <= RightEyeOuter)
        {
            SetVisible(false);
            return;
        }

        Vector3 leftWorld = _face.transform.TransformPoint(_face.vertices[LeftEyeOuter]);
        Vector3 rightWorld = _face.transform.TransformPoint(_face.vertices[RightEyeOuter]);
        Vector3 leftScreen = _camera.WorldToScreenPoint(leftWorld);
        Vector3 rightScreen = _camera.WorldToScreenPoint(rightWorld);

        if (leftScreen.z <= 0f || rightScreen.z <= 0f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        Vector2 center = ((Vector2)leftScreen + (Vector2)rightScreen) * 0.5f + pixelOffset;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, center, null, out Vector2 localPoint))
        {
            return;
        }

        float eyePixels = Vector2.Distance(leftScreen, rightScreen);
        float width = Mathf.Max(eyePixels * widthFromEyeDistance, 32f);
        float angle = Mathf.Atan2(leftScreen.y - rightScreen.y, leftScreen.x - rightScreen.x) * Mathf.Rad2Deg;

        _rect.anchoredPosition = Vector2.Lerp(_rect.anchoredPosition, localPoint, Time.deltaTime * smoothing);
        _rect.sizeDelta = Vector2.Lerp(_rect.sizeDelta, new Vector2(width, width * _aspect), Time.deltaTime * smoothing);
        _rect.localRotation = Quaternion.Lerp(_rect.localRotation, Quaternion.Euler(0f, 0f, angle), Time.deltaTime * smoothing);
    }

    private void SetVisible(bool visible)
    {
        if (_image != null && _image.enabled != visible)
            _image.enabled = visible;
    }
}
