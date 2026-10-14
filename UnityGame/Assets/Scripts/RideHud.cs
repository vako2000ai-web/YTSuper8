using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

namespace RollerCoasterHeroes
{
    public sealed class RideHud
    {
        readonly Font font;
        readonly Text time, speed, coins, score, state, toast, controls, result, resultDetail;
        readonly Image progress;
        readonly GameObject results;
        float toastSeconds;
        static readonly CultureInfo English = CultureInfo.InvariantCulture;
        readonly Color ink = new Color(.045f, .08f, .15f, .94f);
        public Canvas Canvas { get; }
        public RideHud(Transform root)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("First Ride 2.0 HUD", typeof(RectTransform)); go.transform.SetParent(root, false);
            Canvas = go.AddComponent<Canvas>(); Canvas.renderMode = RenderMode.ScreenSpaceOverlay; Canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var top = Panel("Broadcast header", go.transform, new Vector2(48, -44), new Vector2(1824, 114), ink);
            Label(top, "ROLLER COASTER HEROES", new Vector2(24, -15), new Vector2(700, 46), 34, Color.white);
            Label(top, "FIRST RIDE 2.0  /  GREEN HILLS", new Vector2(26, -65), new Vector2(650, 28), 20, RideArt.Teal);
            state = Label(top, "RACING", new Vector2(1510, -35), new Vector2(280, 45), 27, RideArt.Teal, TextAnchor.MiddleRight);
            var stats = Panel("Race telemetry", go.transform, new Vector2(48, -180), new Vector2(310, 388), ink);
            Label(stats, "RACER  /  FOX", new Vector2(24, -20), new Vector2(265, 36), 25, RideArt.Gold);
            time = Stat(stats, "TIME", 76); speed = Stat(stats, "SPEED  /  KM/H", 150);
            coins = Stat(stats, "COINS", 224); score = Stat(stats, "SCORE", 298);
            var bottom = Panel("Route progress", go.transform, new Vector2(48, -940), new Vector2(1824, 96), ink);
            Label(bottom, "START", new Vector2(24, -12), new Vector2(130, 30), 18, Color.white);
            Label(bottom, "FINISH", new Vector2(1660, -12), new Vector2(140, 30), 18, RideArt.Gold, TextAnchor.MiddleRight);
            var bar = Panel("Progress track", bottom, new Vector2(24, -52), new Vector2(1776, 12), new Color(.2f, .28f, .36f));
            var fill = Panel("Progress fill", bar, Vector2.zero, new Vector2(1776, 12), RideArt.Teal);
            progress = fill.GetComponent<Image>();
            // Use width rather than a filled sprite, so no texture assets are required.
            controls = Label(go.transform, "", new Vector2(48, -885), new Vector2(1824, 34), 21, Color.white, TextAnchor.MiddleRight);
            toast = Label(go.transform, "", new Vector2(530, -200), new Vector2(860, 70), 42, RideArt.Gold, TextAnchor.MiddleCenter);
            var shadow = toast.gameObject.AddComponent<Shadow>(); shadow.effectColor = ink; shadow.effectDistance = new Vector2(2, -3);
            var card = Panel("Finish results", go.transform, new Vector2(1060, -330), new Vector2(790, 430), ink);
            results = card.gameObject;
            Label(card, "RIDE COMPLETE!", new Vector2(40, -35), new Vector2(710, 65), 45, RideArt.Gold, TextAnchor.MiddleCenter);
            result = Label(card, "", new Vector2(40, -120), new Vector2(710, 90), 62, Color.white, TextAnchor.MiddleCenter);
            resultDetail = Label(card, "", new Vector2(40, -230), new Vector2(710, 65), 26, RideArt.Teal, TextAnchor.MiddleCenter);
            Label(card, "PRESS R TO RIDE AGAIN", new Vector2(40, -335), new Vector2(710, 46), 25, Color.white, TextAnchor.MiddleCenter);
            Reset();
        }
        RectTransform Panel(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; Place(rect, pos, size); go.GetComponent<Image>().color = color; return rect;
        }
        static void Place(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = pos; rect.sizeDelta = size;
        }
        Text Label(Transform parent, string value, Vector2 pos, Vector2 size, int pointSize, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(value.Length > 0 ? value : "Value", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, pos, size); var text = go.GetComponent<Text>();
            text.font = font; text.fontSize = pointSize; text.fontStyle = FontStyle.Bold; text.color = color; text.text = value;
            text.alignment = align; text.raycastTarget = false; return text;
        }
        Text Stat(Transform parent, string title, float y)
        {
            Label(parent, title, new Vector2(24, -y), new Vector2(262, 24), 17, new Color(.6f, .72f, .79f));
            return Label(parent, "0", new Vector2(24, -y - 24), new Vector2(262, 44), 32, Color.white);
        }
        public void Reset() { results.SetActive(false); toastSeconds = 0; toast.text = ""; }
        public void Announce(string value, Color color) { toast.text = value; toast.color = color; toastSeconds = 1.5f; }
        public void Tick(RideModel model, RideAudio audio, bool wide, bool paused, float dt)
        {
            time.text = model.Seconds.ToString("F1", English) + " s";
            speed.text = (model.Finished ? 0 : model.Speed * 3.6f).ToString("F0", English);
            coins.text = model.Coins.ToString("D2", English); score.text = model.Score.ToString("N0", English);
            state.text = paused ? "PAUSED" : model.Finished ? "FINISHED" : model.EffectSeconds > 0 ?
                (model.Effect == PickupKind.Boost ? "BOOST  " : "BRAKE  ") + model.EffectSeconds.ToString("F1", English) + "s" : "RACING";
            progress.rectTransform.sizeDelta = new Vector2(1776 * model.Distance / model.Track.Length, 12);
            controls.text = "R  RESTART     SPACE  PAUSE     C  " + (wide ? "WIDE" : "CHASE") + " CAMERA     M  MUSIC " + (audio.Muted ? "OFF" : "ON");
            toastSeconds -= dt; if (toastSeconds <= 0) toast.text = "";
            results.SetActive(model.Finished);
            if (model.Finished)
            {
                result.text = model.Score.ToString("N0", English) + " PTS";
                resultDetail.text = model.Coins + " COINS   /   " + model.Seconds.ToString("F2", English) + " SECONDS";
            }
        }
    }
}
