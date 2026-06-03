using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Builds the FuzzyBrain about panel as a self-contained UIToolkit VisualElement.
    /// Shared between FuzzyBrainWindow (idle right-pane) and FuzzyBrainAboutWindow (popup).
    /// </summary>
    public static class FuzzyBrainAboutPanel
    {
        // ── Version ───────────────────────────────────────────────────────────────

        public const string Version = "1.0.0";

        // ── URLs ──────────────────────────────────────────────────────────────────

        private const string UrlDocs    = "https://logicandchaos.gitbook.io/fuzzy-brain";
        private const string UrlWeb     = "https://fuzzybrain.page.gd/";
        private const string UrlDiscord = "https://discord.gg/b2axbQ7NYD";

        // ── Logo ──────────────────────────────────────────────────────────────────

        private const string LogoPath = "Assets/FuzzyBrain/Editor/logo/FuzzyBrainLogo200x200.png";

        // ── Colors ────────────────────────────────────────────────────────────────

        private static readonly Color ColorLink    = new Color(0.25f, 0.60f, 1.00f);
        private static readonly Color ColorDivider = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        private static readonly Color ColorSubtle  = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color ColorHeader  = new Color(0.13f, 0.13f, 0.13f, 0.6f);

        // ── Build ─────────────────────────────────────────────────────────────────

        /// <summary>Builds and returns the about panel as a self-contained VisualElement.</summary>
        public static VisualElement Build()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow      = 1f;
            root.style.paddingLeft   = 16f;
            root.style.paddingRight  = 16f;
            root.style.paddingTop    = 16f;
            root.style.paddingBottom = 16f;

            root.Add(BuildHeader());
            root.Add(BuildDescription());
            root.Add(BuildLinksSection());
            root.Add(BuildSpacer(8f));
            root.Add(BuildVersionLabel());

            return root;
        }

        // ── Sections ──────────────────────────────────────────────────────────────

        private static VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection           = FlexDirection.Column;
            header.style.alignItems              = Align.Center;
            header.style.backgroundColor         = ColorHeader;
            header.style.borderTopLeftRadius     = 6f;
            header.style.borderTopRightRadius    = 6f;
            header.style.borderBottomLeftRadius  = 6f;
            header.style.borderBottomRightRadius = 6f;
            header.style.paddingLeft             = 12f;
            header.style.paddingRight            = 12f;
            header.style.paddingTop              = 12f;
            header.style.paddingBottom           = 12f;
            header.style.minHeight               = 180f;
            header.style.marginBottom            = 12f;

            Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
            if (logo != null)
            {
                var logoEl = new Image { image = logo };
                logoEl.style.width         = 100f;
                logoEl.style.height        = 100f;
                logoEl.style.marginBottom  = 10f;
                logoEl.style.flexShrink    = 0f;
                header.Add(logoEl);
            }

            var title = new Label("FuzzyBrain");
            title.style.fontSize                = 22f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign          = TextAnchor.MiddleCenter;
            header.Add(title);

            var tagline = new Label("A rule-based behaviour system for Unity");
            tagline.style.fontSize            = 11f;
            tagline.style.color               = ColorSubtle;
            tagline.style.marginTop           = 4f;
            tagline.style.unityFontStyleAndWeight = FontStyle.Italic;
            tagline.style.unityTextAlign      = TextAnchor.MiddleCenter;
            header.Add(tagline);

            return header;
        }

        private static VisualElement BuildDescription()
        {
            var block = new VisualElement();
            block.style.marginTop    = 8f;
            block.style.marginBottom = 28f;

            var heading = new Label("What is FuzzyBrain?");
            heading.style.fontSize                = 13f;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom            = 4f;
            block.Add(heading);

            var body = new Label(
                "FuzzyBrain replaces hand-written if/else chains and custom state machines with " +
                "a prioritised list of Acts. Each Act is a possible behaviour — a pattern of " +
                "Conditions that describes a situation — and fuzzy pattern matching is used to " +
                "determine which one fires, making your Actors react to changes.");
            body.style.fontSize   = 12f;
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.color      = ColorSubtle;
            block.Add(body);

            return block;
        }

        private static VisualElement BuildLinksSection()
        {
            var section = new VisualElement();
            section.style.flexDirection = FlexDirection.Column;
            section.style.marginTop     = 8f;

            section.Add(BuildLinkButton("📖  Documentation", UrlDocs,    "Full API reference and guides"));
            section.Add(BuildLinkButton("🌐  Website & Contact", UrlWeb, "Project homepage and contact form"));
            section.Add(BuildLinkButton("💬  Discord",       UrlDiscord, "Community and support"));

            return section;
        }

        private static VisualElement BuildVersionLabel()
        {
            var label = new Label($"FuzzyBrain v{Version}");
            label.style.fontSize  = 10f;
            label.style.color     = new Color(0.4f, 0.4f, 0.4f);
            label.style.marginTop = 8f;
            label.style.alignSelf = Align.FlexEnd;
            return label;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static VisualElement BuildLinkButton(string linkText, string url, string description)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.marginBottom  = 10f;

            var btn = new Button(() => Application.OpenURL(url)) { text = linkText };
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.color             = ColorLink;
            btn.style.fontSize          = 12f;
            btn.style.marginRight       = 8f;
            btn.style.paddingLeft       = 0f;
            btn.style.paddingRight      = 0f;
            btn.style.backgroundColor   = Color.clear;
            btn.style.borderTopWidth    = 0f;
            btn.style.borderBottomWidth = 0f;
            btn.style.borderLeftWidth   = 0f;
            btn.style.borderRightWidth  = 0f;
            row.Add(btn);

            var desc = new Label($"— {description}");
            desc.style.fontSize = 11f;
            desc.style.color    = ColorSubtle;
            row.Add(desc);

            return row;
        }

        private static VisualElement BuildDivider()
        {
            var d = new VisualElement();
            d.style.height          = 1f;
            d.style.backgroundColor = ColorDivider;
            d.style.marginTop       = 6f;
            d.style.marginBottom    = 6f;
            return d;
        }

        private static VisualElement BuildSpacer(float height)
        {
            var s = new VisualElement();
            s.style.height = height;
            return s;
        }
    }
}
