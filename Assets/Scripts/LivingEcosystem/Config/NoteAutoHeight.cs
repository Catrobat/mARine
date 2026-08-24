using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem
{
    // Sizes a wrapping note row to its text.
    //
    // The environment builder's form lays its rows out with a VerticalLayoutGroup and
    // each row prefab declares a fixed height through a LayoutElement. A warning line
    // that wraps to three lines therefore overflows its row and prints on top of the
    // next one. This measures the text at whatever width the row actually ends up
    // with and writes that back to the LayoutElement.
    //
    // It only recomputes when the width changes, so it costs nothing once settled.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(LayoutElement))]
    public class NoteAutoHeight : MonoBehaviour
    {
        TMP_Text _text;
        LayoutElement _element;
        RectTransform _rect;
        float _lastWidth = -1f;
        string _lastText;

        void Awake()
        {
            _rect = (RectTransform)transform;
            _text = GetComponent<TMP_Text>();
            _element = GetComponent<LayoutElement>();
        }

        void OnEnable() => _lastWidth = -1f;

        void LateUpdate()
        {
            if (_text == null || _element == null) return;

            float width = _rect.rect.width;
            // Width is zero until the surrounding layout has run at least once.
            if (width <= 1f) return;

            if (Mathf.Abs(width - _lastWidth) < 0.5f && _text.text == _lastText) return;
            _lastWidth = width;
            _lastText = _text.text;

            float height = _text.GetPreferredValues(_text.text, width, 0f).y;
            height = Mathf.Max(24f, height + 10f);

            _element.preferredHeight = height;
            _element.minHeight = height;

            if (_rect.parent is RectTransform parent)
                LayoutRebuilder.MarkLayoutForRebuild(parent);
        }
    }
}
