using System.Text;
using CreateEnv.Ecosystem.Memory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // "While you were away" (Design Document 8.2).
    //
    // At most four lines, ranked so births, deaths and new generations always beat a
    // percentage change in biomass — a card that opens with "the fan alga is down
    // four percent" when a whole generation hatched has buried the story.
    //
    // Skipped entirely for a short absence. Coming back after ten minutes to be told
    // that nothing much happened is worse than not being told anything.
    public class WelcomeBackUI : MonoBehaviour
    {
        LivingReefController _reef;
        GameObject _root;
        TMP_Text _title, _body;

        public System.Action onWhyRequested;

        public static WelcomeBackUI Create(Transform parent, LivingReefController reef)
        {
            var host = EcoUIKit.Empty(parent, "WelcomeBack");
            EcoUIKit.Stretch(EcoUIKit.Rect(host), 0f, 0f);

            var ui = host.AddComponent<WelcomeBackUI>();
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
            cardRect.sizeDelta = new Vector2(880f, 720f);

            _title = EcoUIKit.Text(card.transform, "While you were away", 34f, EcoUIKit.TextMain);
            var titleRect = EcoUIKit.Rect(_title.gameObject);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-72f, 48f);
            titleRect.anchoredPosition = new Vector2(0f, -34f);

            _body = EcoUIKit.Text(card.transform, "", 25f, EcoUIKit.TextMain);
            var bodyRect = EcoUIKit.Rect(_body.gameObject);
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(44f, 118f);
            bodyRect.offsetMax = new Vector2(-44f, -96f);

            var why = EcoUIKit.Button(card.transform, "Why?", 24f, EcoUIKit.Track,
                                      EcoUIKit.TextMain,
                                      () => { Close(); onWhyRequested?.Invoke(); });
            var whyRect = EcoUIKit.Rect(why.gameObject);
            whyRect.anchorMin = new Vector2(0f, 0f);
            whyRect.anchorMax = new Vector2(0.42f, 0f);
            whyRect.pivot = new Vector2(0f, 0f);
            whyRect.sizeDelta = new Vector2(-44f, 58f);
            whyRect.anchoredPosition = new Vector2(44f, 34f);

            var dive = EcoUIKit.Button(card.transform, "Dive back in", 24f, EcoUIKit.Accent,
                                       Color.white, Close);
            var diveRect = EcoUIKit.Rect(dive.gameObject);
            diveRect.anchorMin = new Vector2(0.42f, 0f);
            diveRect.anchorMax = new Vector2(1f, 0f);
            diveRect.pivot = new Vector2(1f, 0f);
            diveRect.sizeDelta = new Vector2(-44f, 58f);
            diveRect.anchoredPosition = new Vector2(-44f, 34f);

            _root = scrim;
            _root.SetActive(false);
        }

        public void Close()
        {
            _root.SetActive(false);
            _reef.ClearReport();
        }

        public bool ShowIfDue()
        {
            if (!_reef.HasSomethingToReport) return false;

            var away = _reef.Resumed;
            var sb = new StringBuilder();

            int days = away.daysToRun;
            sb.Append("<size=115%>")
              .Append(days == 1 ? "A day passed in your ocean." : $"{days} days passed in your ocean.")
              .Append("</size>\n");

            if (away.wasCapped)
            {
                // Saying the cap out loud, rather than quietly applying it. A learner
                // returning after a month should not have to wonder why their reef is
                // only a fortnight older.
                sb.Append("<color=#9FB2C4>You were away about ")
                  .Append(Mathf.RoundToInt((float)(away.hoursAway / 24.0)))
                  .Append(" days. An hour away is a day in your ocean, up to two weeks — ")
                  .Append("so it advanced by the full fortnight and waited for you.</color>\n");
            }
            sb.Append('\n');

            var notable = _reef.Chronicle.journal.Notable(_reef.DayOnArrival, 4);
            if (notable.Count == 0)
            {
                sb.Append("Nothing much changed. The reef held steady.");
            }
            else
            {
                foreach (var e in notable)
                {
                    string line = ReefJournal.Describe(e, _reef.Octopuses);
                    if (!string.IsNullOrEmpty(line))
                        sb.Append("•  ").Append(line).Append('\n');
                }
            }

            _body.text = sb.ToString();
            _root.SetActive(true);
            return true;
        }
    }
}
