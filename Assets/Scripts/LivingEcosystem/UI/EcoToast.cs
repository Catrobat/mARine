using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // A line of text that appears near the bottom of the screen, waits, and goes.
    //
    // For the one thing a learner needs told and does not need to act on: where their
    // report was saved. A dialog would demand a tap to dismiss for information that is
    // over as soon as it is read, and on Android the share sheet has already covered
    // the screen by then — so this sits low, above the thumb, and leaves on its own.
    //
    // Deliberately not Android's native Toast. This one shows in the Editor too, which
    // is where the report is tested.
    public class EcoToast : MonoBehaviour
    {
        const float FadeSeconds = 0.22f;

        CanvasGroup _group;
        TMP_Text _label;
        RectTransform _card;
        float _hold;

        public static EcoToast Create(Transform parent)
        {
            var host = EcoUIKit.Empty(parent, "Toast");
            EcoUIKit.Stretch(EcoUIKit.Rect(host), 0f, 0f);

            var toast = host.AddComponent<EcoToast>();
            toast.Build(host.transform);
            return toast;
        }

        void Build(Transform parent)
        {
            var card = EcoUIKit.Panel(parent, "Card", new Color(0.06f, 0.10f, 0.14f, 0.96f));
            _card = EcoUIKit.Rect(card);
            _card.anchorMin = new Vector2(0.5f, 0f);
            _card.anchorMax = new Vector2(0.5f, 0f);
            _card.pivot = new Vector2(0.5f, 0f);
            _card.sizeDelta = new Vector2(880f, 96f);
            _card.anchoredPosition = new Vector2(0f, 150f);

            _label = EcoUIKit.Text(card.transform, "", 24f, EcoUIKit.TextMain,
                                   TextAlignmentOptions.Center);
            _label.textWrappingMode = TextWrappingModes.Normal;
            EcoUIKit.Stretch(EcoUIKit.Rect(_label.gameObject), 30f, 16f);

            _group = card.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            // Nothing here is tappable, and a full-width strip that swallowed taps
            // would block the reef underneath it for as long as it was on screen.
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        public void Show(string message, float seconds = 4.5f)
        {
            if (string.IsNullOrEmpty(message)) return;

            _label.text = message;

            // Grown to fit, so a long file name is not clipped and a short one is not
            // marooned in a wide empty bar.
            float height = Mathf.Clamp(_label.GetPreferredValues(message, 820f, 0f).y + 32f,
                                       78f, 220f);
            _card.sizeDelta = new Vector2(880f, height);

            _hold = seconds;
            enabled = true;
        }

        void Update()
        {
            if (_hold > 0f)
            {
                _hold -= Time.unscaledDeltaTime;
                _group.alpha = Mathf.MoveTowards(_group.alpha, 1f,
                                                 Time.unscaledDeltaTime / FadeSeconds);
                return;
            }

            _group.alpha = Mathf.MoveTowards(_group.alpha, 0f,
                                             Time.unscaledDeltaTime / FadeSeconds);

            // Nothing left to fade: stop taking a frame every frame.
            if (_group.alpha <= 0f) enabled = false;
        }
    }
}
