using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // The barren-state prompt (Design Document 6.3).
    //
    // At the barren stage the app offers three options: restore the missing species
    // and watch recovery, rewind to the last healthy day, or start again. Recovery
    // deliberately takes longer than the collapse did — which is the most important
    // single lesson the feature can deliver, so the card says so out loud.
    //
    // Shown once per collapse. If the learner dismisses it and the reef falls apart
    // again later, it returns.
    public class BarrenPromptUI : MonoBehaviour
    {
        LivingReefController _reef;
        GameObject _root;
        TMP_Text _body;
        Button _rewindButton;
        bool _shownForThisCollapse;

        public static BarrenPromptUI Create(Transform parent, LivingReefController reef)
        {
            var host = EcoUIKit.Empty(parent, "BarrenPrompt");
            EcoUIKit.Stretch(EcoUIKit.Rect(host), 0f, 0f);

            var ui = host.AddComponent<BarrenPromptUI>();
            ui._reef = reef;
            ui.Build(host.transform);
            return ui;
        }

        void Build(Transform parent)
        {
            var scrim = EcoUIKit.Panel(parent, "Scrim", new Color(0f, 0f, 0f, 0.72f));
            EcoUIKit.Stretch(EcoUIKit.Rect(scrim), 0f, 0f);

            var card = EcoUIKit.Panel(scrim.transform, "Card", EcoUIKit.PanelBgSoft);
            var cardRect = EcoUIKit.Rect(card);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(860f, 720f);

            var title = EcoUIKit.Text(card.transform, "Your reef is barren", 36f,
                                      new Color(0.90f, 0.46f, 0.42f));
            var titleRect = EcoUIKit.Rect(title.gameObject);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-64f, 52f);
            titleRect.anchoredPosition = new Vector2(0f, -32f);

            _body = EcoUIKit.Text(card.transform, "", 25f, EcoUIKit.TextMain);
            var bodyRect = EcoUIKit.Rect(_body.gameObject);
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(40f, 260f);
            bodyRect.offsetMax = new Vector2(-40f, -100f);

            float y = 190f;
            MakeButton(card.transform, "Put the missing species back", ref y, EcoUIKit.Accent, () =>
            {
                _reef.RestoreAllSpecies();
                Close();
            });

            _rewindButton = MakeButton(card.transform, "Rewind to the last healthy day", ref y,
                                       EcoUIKit.Track, () =>
            {
                _reef.Rewind();
                Close();
            });

            MakeButton(card.transform, "Start this reef again", ref y, EcoUIKit.Track, () =>
            {
                _reef.RestartEcosystem();
                Close();
            });

            var dismiss = EcoUIKit.Button(card.transform, "Leave it as it is", 22f,
                                          new Color(0f, 0f, 0f, 0f), EcoUIKit.TextDim, Close);
            var dismissRect = EcoUIKit.Rect(dismiss.gameObject);
            dismissRect.anchorMin = new Vector2(0.5f, 0f);
            dismissRect.anchorMax = new Vector2(0.5f, 0f);
            dismissRect.pivot = new Vector2(0.5f, 0f);
            dismissRect.sizeDelta = new Vector2(420f, 46f);
            dismissRect.anchoredPosition = new Vector2(0f, 16f);

            _root = scrim;
            _root.SetActive(false);
        }

        Button MakeButton(Transform parent, string label, ref float y, Color colour, System.Action action)
        {
            var button = EcoUIKit.Button(parent, label, 25f, colour,
                                         colour == EcoUIKit.Accent ? Color.white : EcoUIKit.TextMain,
                                         action);
            var rect = EcoUIKit.Rect(button.gameObject);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(760f, 62f);
            rect.anchoredPosition = new Vector2(0f, y);
            y -= 72f;
            return button;
        }

        // Called every frame while the reef is barren; shows itself once.
        public void MaybeShow()
        {
            if (_shownForThisCollapse || _root.activeSelf) return;
            _shownForThisCollapse = true;

            bool canRewind = _reef.CanRewind;
            _rewindButton.gameObject.SetActive(canRewind);
            _rewindButton.interactable = canRewind;

            _body.text =
                "Only detritus and the slowest-growing survivors are left.\n\n" +
                "This is what a collapsed reef looks like. Putting the missing species back " +
                "will start a recovery, but it will take far longer than the collapse did — " +
                "an ecosystem is much quicker to break than to rebuild.";

            _root.SetActive(true);
        }

        // Re-arms once the reef has recovered, so a second collapse prompts again.
        public void NotifyRecovered() => _shownForThisCollapse = false;

        public void Close() => _root.SetActive(false);
    }
}
