using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TodoListClean
{
    /// <summary>
    /// Small uGUI builder. Text is TextMeshPro using the same font as the game's
    /// Diagnostics panel; icons are tiny pixel images drawn at runtime so they
    /// never depend on glyphs being present in the game font.
    /// </summary>
    internal static class UIKit
    {
        public static readonly Color HeaderBg = new Color32(0x3B, 0x40, 0x54, 0xF0);
        public static readonly Color RowBgBase = new Color32(0x24, 0x27, 0x33, 0xFF);
        public static readonly Color ButtonBg = new Color32(0x4A, 0x50, 0x68, 0xFF);
        public static readonly Color Accent = new Color32(0x9B, 0x5C, 0x9E, 0xFF);
        public static readonly Color Danger = new Color32(0x8E, 0x3A, 0x3F, 0xFF);
        public static readonly Color Clear = new Color(1f, 1f, 1f, 0f);
        public static readonly Color TextColor = new Color32(0xF0, 0xF0, 0xF4, 0xFF);
        public static readonly Color TextDim = new Color32(0xA8, 0xAC, 0xBE, 0xFF);

        // ------------------------------------------------------------------ font

        private static TMP_FontAsset font;
        private static Material fontMaterial;
        private static FontStyles fontStyle = FontStyles.Normal;
        private static bool fontResolved;

        /// <summary>
        /// Copy the font of the live Diagnostics panel (preferring its Graystroke text),
        /// falling back to Graystroke by name, then the game's default font.
        /// </summary>
        private static void ResolveFont()
        {
            if (fontResolved) return;
            fontResolved = true;

            LocText pick = null;
            try
            {
                LocText anyDiag = null, anyGraystroke = null;
                foreach (var lt in Resources.FindObjectsOfTypeAll<LocText>())
                {
                    if (lt == null || lt.font == null) continue;
                    bool gray = lt.font.name.ToUpperInvariant().Contains("GRAYSTROKE");
                    bool inScene = lt.gameObject.scene.IsValid();
                    if (inScene && HasAncestor(lt.transform, "diagnostic"))
                    {
                        if (gray) { pick = lt; break; }
                        if (anyDiag == null) anyDiag = lt;
                    }
                    if (gray && anyGraystroke == null) anyGraystroke = lt;
                }
                if (pick == null) pick = anyGraystroke ?? anyDiag;
            }
            catch { pick = null; }

            if (pick != null)
            {
                font = pick.font;
                fontMaterial = pick.fontSharedMaterial;
                fontStyle = pick.fontStyle;
            }
            if (font == null)
            {
                try { font = Localization.GetFont("GRAYSTROKE REGULAR SDF"); } catch { font = null; }
            }
            if (font == null)
            {
                try { font = Localization.GetFont(Localization.GetDefaultFontName()); } catch { font = null; }
            }
            if (fontMaterial != null && font != null && fontMaterial.mainTexture != null
                && font.material != null && fontMaterial.mainTexture != font.material.mainTexture)
                fontMaterial = null; // material from another atlas would render garbage
        }

        private static bool HasAncestor(Transform t, string fragment)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name.ToLowerInvariant().Contains(fragment)) return true;
            return false;
        }

        // ------------------------------------------------------------------ icons (12x12, '#' = pixel)

        private static readonly Dictionary<string, string[]> IconArt = new Dictionary<string, string[]>
        {
            ["up"] = new[]
            {
                "............",
                "............",
                "............",
                ".....##.....",
                "....####....",
                "...######...",
                "..########..",
                ".##########.",
                "............",
                "............",
                "............",
                "............",
            },
            ["top"] = new[]
            {
                "............",
                ".##########.",
                ".##########.",
                "............",
                ".....##.....",
                "....####....",
                "...######...",
                "..########..",
                ".##########.",
                "............",
                "............",
                "............",
            },
            ["bottom"] = new[]
            {
                "............",
                "............",
                "............",
                ".##########.",
                "..########..",
                "...######...",
                "....####....",
                ".....##.....",
                "............",
                ".##########.",
                ".##########.",
                "............",
            },
            ["down"] = new[]
            {
                "............",
                "............",
                "............",
                "............",
                ".##########.",
                "..########..",
                "...######...",
                "....####....",
                ".....##.....",
                "............",
                "............",
                "............",
            },
            ["right"] = new[]
            {
                "............",
                "............",
                "...#........",
                "...##.......",
                "...###......",
                "...####.....",
                "...####.....",
                "...###......",
                "...##.......",
                "...#........",
                "............",
                "............",
            },
            ["pencil"] = new[]
            {
                "............",
                "........##..",
                ".......####.",
                "......#..###",
                ".....#..###.",
                "....#..###..",
                "...#..###...",
                "..#..###....",
                ".#..###.....",
                ".#####......",
                ".###........",
                "............",
            },
            ["camera"] = new[]
            {
                "............",
                "............",
                "....####....",
                ".##########.",
                ".####..####.",
                ".###....###.",
                ".###....###.",
                ".####..####.",
                ".##########.",
                ".##########.",
                "............",
                "............",
            },
            ["cross"] = new[]
            {
                "............",
                "............",
                "..##....##..",
                "..###..###..",
                "...######...",
                "....####....",
                "....####....",
                "...######...",
                "..###..###..",
                "..##....##..",
                "............",
                "............",
            },
            ["plus"] = new[]
            {
                "............",
                "............",
                ".....##.....",
                ".....##.....",
                ".....##.....",
                "..########..",
                "..########..",
                ".....##.....",
                ".....##.....",
                ".....##.....",
                "............",
                "............",
            },
            ["minus"] = new[]
            {
                "............",
                "............",
                "............",
                "............",
                "............",
                "..########..",
                "..########..",
                "............",
                "............",
                "............",
                "............",
                "............",
            },
            ["gear"] = new[]
            {
                "............",
                ".....##.....",
                "..##.##.##..",
                "..########..",
                "...##..##...",
                ".###....###.",
                ".###....###.",
                "...##..##...",
                "..########..",
                "..##.##.##..",
                ".....##.....",
                "............",
            },
        };

        private static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

        public static Sprite Icon(string name)
        {
            Sprite s;
            if (iconCache.TryGetValue(name, out s) && s != null) return s;
            if (name == "arrow")
            {
                s = FindGameArrow() ?? SmoothDownTriangle();
                iconCache[name] = s;
                return s;
            }
            string[] art;
            if (!IconArt.TryGetValue(name, out art)) return null;

            int h = art.Length, w = art[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                string line = art[h - 1 - y]; // texture rows start at the bottom
                for (int x = 0; x < w; x++)
                    px[y * w + x] = x < line.Length && line[x] == '#'
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            s.name = "TodoListClean_" + name;
            iconCache[name] = s;
            return s;
        }

        /// <summary>Rotation (degrees) that makes the arrow sprite point down; collapsed adds 90 (points right).</summary>
        public static float ArrowBaseRotation;

        /// <summary>
        /// Reuse the collapse arrow from the game's Resources panel so ours matches exactly.
        /// Only accepts a small arrow-like image under a "resource" object; otherwise null.
        /// </summary>
        private static Sprite FindGameArrow()
        {
            try
            {
                Image best = null;
                int bestScore = 0;
                foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
                {
                    if (img == null || img.sprite == null || !img.gameObject.scene.IsValid()) continue;
                    string n = img.sprite.name.ToLowerInvariant();
                    if (!(n.Contains("arrow") || n.Contains("expand") || n.Contains("collapse") || n.Contains("icondown"))) continue;

                    int score = 1;
                    if (HasAncestor(img.transform, "resource")) score += 4;
                    if (n.Contains("down")) score += 2;
                    if (n.Contains("left") || n.EndsWith("up")) score -= 3;
                    var r = img.rectTransform.rect;
                    if (r.width > 24f || r.height > 24f) score -= 3; // header arrows are small
                    if (score > bestScore) { bestScore = score; best = img; }
                }
                if (best == null || bestScore < 5) return null;
                // The Resources panel is expanded by default, so its current rotation = "pointing down".
                ArrowBaseRotation = best.rectTransform.localEulerAngles.z;
                return best.sprite;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Anti-aliased solid triangle pointing down, styled like the in-game panel arrows.</summary>
        private static Sprite SmoothDownTriangle()
        {
            const int size = 32, ss = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            // y grows upward in texture space: flat edge on top, apex at the bottom
            Vector2 a = new Vector2(5f, 24f), b = new Vector2(27f, 24f), c = new Vector2(16f, 9f);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            var p = new Vector2(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss);
                            if (InTriangle(p, a, b, c)) hits++;
                        }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            ArrowBaseRotation = 0f;
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s.name = "TodoListClean_arrow";
            return s;
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(neg && pos);
        }

        private static float Cross(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }

        // ------------------------------------------------------------------ building blocks

        public static RectTransform Make(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float padX = 0f, float padY = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        public static LayoutElement Size(GameObject go, float width = -1f, float height = -1f, float flexWidth = -1f)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (width >= 0f) { le.minWidth = width; le.preferredWidth = width; }
            if (height >= 0f) { le.minHeight = height; le.preferredHeight = height; }
            if (flexWidth >= 0f) le.flexibleWidth = flexWidth;
            return le;
        }

        public static TextMeshProUGUI AddText(GameObject go, string value, float size, Color color, TextAlignmentOptions align)
        {
            ResolveFont();
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
            t.fontStyle = fontStyle;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.richText = false;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.text = value;
            return t;
        }

        public static HorizontalLayoutGroup HRow(GameObject go, float spacing, RectOffset padding, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static VerticalLayoutGroup VColumn(GameObject go, float spacing, RectOffset padding)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static void SetColors(Selectable s, Color bg)
        {
            var cb = s.colors;
            cb.normalColor = bg;
            cb.selectedColor = bg;
            cb.highlightedColor = bg.a <= 0.01f ? new Color(1f, 1f, 1f, 0.10f) : Shift(bg, 0.12f);
            cb.pressedColor = bg.a <= 0.01f ? new Color(1f, 1f, 1f, 0.18f) : Shift(bg, -0.08f);
            cb.disabledColor = new Color(bg.r, bg.g, bg.b, bg.a * 0.4f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            s.colors = cb;
        }

        private static Color Shift(Color c, float d)
        {
            return new Color(Mathf.Clamp01(c.r + d), Mathf.Clamp01(c.g + d), Mathf.Clamp01(c.b + d), c.a);
        }

        private static Button BaseButton(RectTransform rt, Color bg, UnityAction onClick)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            SetColors(b, bg);
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        public static Image AddIcon(Transform parent, Sprite sprite, float size, Color color)
        {
            var rt = Make("Icon", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Size(rt.gameObject, size, size);
            return img;
        }

        /// <summary>Fixed-size button with a centred pixel icon.</summary>
        public static Button IconButton(Transform parent, string icon, Color bg, Color fg, float size, UnityAction onClick)
        {
            var rt = Make("Btn_" + icon, parent);
            var b = BaseButton(rt, bg, onClick);
            Size(rt.gameObject, size, size);
            var irt = Make("Icon", rt);
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(12f, 12f);
            var img = irt.gameObject.AddComponent<Image>();
            img.sprite = Icon(icon);
            img.color = fg;
            img.raycastTarget = false;
            return b;
        }

        /// <summary>Flexible-width button whose height follows its wrapping text; optional leading icon.</summary>
        public static Button LabelButton(Transform parent, string label, float fontSize, Color bg, Color fg, float minHeight,
            UnityAction onClick, out TextMeshProUGUI text, string leadingIcon = null, float iconSize = 10f)
        {
            var rt = Make("Label", parent);
            var b = BaseButton(rt, bg, onClick);
            UIKit.HRow(rt.gameObject, 4f, new RectOffset(4, 4, 2, 2));
            var le = Size(rt.gameObject, -1f, -1f, 1f);
            le.minWidth = 40f;
            le.minHeight = minHeight;
            if (leadingIcon != null) AddIcon(rt, Icon(leadingIcon), iconSize, fg);
            var trt = Make("Text", rt);
            text = AddText(trt.gameObject, label, fontSize, fg, TextAlignmentOptions.MidlineLeft);
            Size(trt.gameObject, -1f, -1f, 1f);
            return b;
        }

        public static TMP_InputField Input(Transform parent, string initial, float fontSize, UnityAction<string> onEndEdit)
        {
            var rt = Make("Input", parent);
            rt.gameObject.SetActive(false); // configure before TMP_InputField.OnEnable runs
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.45f);
            var le = Size(rt.gameObject, -1f, 24f, 1f);
            le.minWidth = 40f;

            var area = Make("TextArea", rt);
            Stretch(area, 5f, 2f);
            area.gameObject.AddComponent<RectMask2D>();

            var trt = Make("Text", area);
            Stretch(trt);
            var t = AddText(trt.gameObject, "", fontSize, TextColor, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;

            var f = rt.gameObject.AddComponent<TMP_InputField>();
            f.textViewport = area;
            f.textComponent = t;
            if (t.font != null) f.fontAsset = t.font;
            f.pointSize = fontSize;
            f.targetGraphic = img;
            f.navigation = new Navigation { mode = Navigation.Mode.None };
            f.lineType = TMP_InputField.LineType.SingleLine;
            f.characterLimit = 120;
            f.customCaretColor = true;
            f.caretColor = TextColor;
            f.selectionColor = new Color(0.6f, 0.36f, 0.62f, 0.6f);
            f.text = initial ?? "";
            f.onEndEdit.AddListener(onEndEdit);
            rt.gameObject.SetActive(true);
            return f;
        }
    }

    /// <summary>Tracks whether the pointer is anywhere over the panel.</summary>
    internal sealed class PanelHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsOver;
        public void OnPointerEnter(PointerEventData eventData) { IsOver = true; }
        public void OnPointerExit(PointerEventData eventData) { IsOver = false; }
        private void OnDisable() { IsOver = false; }
    }
}
