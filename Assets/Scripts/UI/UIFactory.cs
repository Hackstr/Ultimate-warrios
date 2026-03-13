using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace TacticalDuelist.UI
{
    public static class UIFactory
    {
        #region Colors

        public static readonly Color BgDark = new(0.06f, 0.06f, 0.1f, 1f);
        public static readonly Color PanelBg = new(0.1f, 0.1f, 0.16f, 1f);
        public static readonly Color AccentPrimary = new(1f, 0.42f, 0.21f, 1f);   // #FF6B35
        public static readonly Color AccentBlue = new(0.25f, 0.45f, 0.95f, 1f);
        public static readonly Color AccentGold = new(1f, 0.85f, 0.2f, 1f);
        public static readonly Color AccentRed = new(0.85f, 0.2f, 0.2f, 1f);
        public static readonly Color AccentGreen = new(0.13f, 0.77f, 0.37f, 1f);  // #22C55E
        public static readonly Color TextWhite = new(0.92f, 0.92f, 0.96f, 1f);
        public static readonly Color TextGray = new(0.42f, 0.45f, 0.50f, 1f);     // #6B7280
        public static readonly Color TextLight = new(0.61f, 0.64f, 0.69f, 1f);    // #9CA3AF
        public static readonly Color ButtonBg = new(0.16f, 0.2f, 0.32f, 1f);
        public static readonly Color ButtonBgSecondary = new(0.22f, 0.22f, 0.3f, 1f);
        public static readonly Color BarBg = new(0.12f, 0.12f, 0.18f, 1f);
        public static readonly Color CardBg = new(0.12f, 0.12f, 0.18f, 1f);
        public static readonly Color BorderSubtle = new(0.22f, 0.22f, 0.3f, 1f);
        public static readonly Color DotFilled = new(1f, 0.85f, 0.2f, 1f);
        public static readonly Color DotEmpty = new(0.25f, 0.25f, 0.3f, 1f);

        #endregion

        #region Core

        public static Canvas CreateCanvas(string name = "Canvas")
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1.0f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static EventSystem CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            var es = go.AddComponent<EventSystem>();
            var input = go.AddComponent<InputSystemUIInputModule>();
            input.AssignDefaultActions();
            return es;
        }

        #endregion

        #region Panels

        public static RectTransform CreatePanel(Transform parent, string name, Color? bg = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);

            var img = go.AddComponent<Image>();
            img.color = bg ?? BgDark;
            img.raycastTarget = true;

            return rt;
        }

        public static RectTransform CreateContainer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        #endregion

        #region Text

        public static TextMeshProUGUI CreateText(
            Transform parent, string name, string content,
            float fontSize = 24f,
            TextAlignmentOptions align = TextAlignmentOptions.Center,
            Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.color = color ?? TextWhite;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        #endregion

        #region Button

        public static (Button btn, TextMeshProUGUI label) CreateButton(
            Transform parent, string name, string text,
            Color? bg = null, Color? textColor = null,
            float width = 300f, float height = 60f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var img = go.AddComponent<Image>();
            img.color = bg ?? ButtonBg;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            btn.colors = colors;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            Stretch(textRt);
            textRt.offsetMin = new Vector2(8, 4);
            textRt.offsetMax = new Vector2(-8, -4);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = textColor ?? TextWhite;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return (btn, tmp);
        }

        #endregion

        #region Image & Slider

        public static Image CreateImage(Transform parent, string name,
            Color? color = null, float w = 64f, float h = 64f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);

            var img = go.AddComponent<Image>();
            img.color = color ?? Color.white;
            return img;
        }

        /// <summary>
        /// Creates a properly structured Slider used as a stat bar.
        /// Background + fill area + fill image, non-interactable.
        /// </summary>
        public static Slider CreateStatBar(Transform parent, string name,
            Color? fill = null, float w = 280f, float h = 20f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);

            var bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            Stretch(bgRt);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = BarBg;
            bgImg.raycastTarget = false;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            Stretch(faRt);
            faRt.offsetMin = new Vector2(3, 3);
            faRt.offsetMax = new Vector2(-3, -3);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fRt = fillGo.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fill ?? AccentBlue;
            fillImg.raycastTarget = false;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fRt;
            slider.interactable = false;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.transition = Selectable.Transition.None;

            return slider;
        }

        #endregion

        #region Difficulty Dots

        /// <summary>
        /// Creates a row of filled/empty circle images for difficulty rating.
        /// Returns the parent container so it can be laid out.
        /// </summary>
        public static RectTransform CreateDifficultyDots(
            Transform parent, string name, int filled, int total = 5,
            float dotSize = 24f, float spacing = 8f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            for (int i = 0; i < total; i++)
            {
                var dot = CreateImage(go.transform, $"Dot{i}",
                    i < filled ? DotFilled : DotEmpty,
                    dotSize, dotSize);
                dot.raycastTarget = false;
            }

            return rt;
        }

        #endregion

        #region ScrollView

        /// <summary>
        /// Creates a ScrollRect with viewport and content container.
        /// Returns (scrollRect, contentTransform) for populating.
        /// </summary>
        public static (ScrollRect scroll, RectTransform content) CreateScrollView(
            Transform parent, string name,
            bool horizontal = false, bool vertical = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();

            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = horizontal;
            scroll.vertical = vertical;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.scrollSensitivity = 30f;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            Stretch(vpRt);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.GetComponent<RectTransform>();

            if (horizontal && !vertical)
            {
                cRt.anchorMin = new Vector2(0, 0);
                cRt.anchorMax = new Vector2(0, 1);
                cRt.pivot = new Vector2(0, 0.5f);
                cRt.sizeDelta = new Vector2(0, 0);
            }
            else
            {
                cRt.anchorMin = new Vector2(0, 1);
                cRt.anchorMax = new Vector2(1, 1);
                cRt.pivot = new Vector2(0.5f, 1f);
                cRt.sizeDelta = new Vector2(0, 0);
            }

            scroll.viewport = vpRt;
            scroll.content = cRt;

            return (scroll, cRt);
        }

        #endregion

        #region Layout

        public static VerticalLayoutGroup AddVertical(GameObject go,
            float spacing = 8f, RectOffset pad = null,
            bool controlHeight = true)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = pad ?? new RectOffset(20, 20, 20, 20);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childControlWidth = true;
            v.childControlHeight = controlHeight;
            return v;
        }

        public static HorizontalLayoutGroup AddHorizontal(GameObject go,
            float spacing = 8f, RectOffset pad = null)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = pad ?? new RectOffset(4, 4, 4, 4);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childControlWidth = false;
            h.childControlHeight = false;
            return h;
        }

        public static GridLayoutGroup AddGrid(GameObject go,
            Vector2 cellSize, Vector2 spacing, int pad = 8)
        {
            var g = go.AddComponent<GridLayoutGroup>();
            g.cellSize = cellSize;
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = TextAnchor.MiddleCenter;
            return g;
        }

        public static LayoutElement AddLayoutElement(GameObject go,
            float prefW = -1f, float prefH = -1f,
            float minW = -1f, float minH = -1f,
            bool flexW = false, bool flexH = false)
        {
            var le = go.AddComponent<LayoutElement>();
            if (prefW >= 0) le.preferredWidth = prefW;
            if (prefH >= 0) le.preferredHeight = prefH;
            if (minW >= 0) le.minWidth = minW;
            if (minH >= 0) le.minHeight = minH;
            if (flexW) le.flexibleWidth = 1;
            if (flexH) le.flexibleHeight = 1;
            return le;
        }

        public static ContentSizeFitter AddFitter(GameObject go,
            ContentSizeFitter.FitMode hFit = ContentSizeFitter.FitMode.Unconstrained,
            ContentSizeFitter.FitMode vFit = ContentSizeFitter.FitMode.PreferredSize)
        {
            var f = go.AddComponent<ContentSizeFitter>();
            f.horizontalFit = hFit;
            f.verticalFit = vFit;
            return f;
        }

        #endregion

        #region RectTransform Helpers

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void SetAnchored(RectTransform rt,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static RectTransform CreateSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            AddLayoutElement(go, prefH: height);
            return go.GetComponent<RectTransform>();
        }

        #endregion
    }
}
