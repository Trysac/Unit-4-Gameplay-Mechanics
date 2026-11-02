using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RingOut.Game
{
    /// <summary>One row of a menu: an action, or a setting with a value that left/right changes.</summary>
    public sealed class MenuItem
    {
        public string Id;
        public string Label;
        public string Value;
    }

    /// <summary>Everything a menu screen shows.</summary>
    public sealed class MenuModel
    {
        public bool Logo;
        public string Title;
        public bool TitleDanger;
        public string Subtitle;
        public readonly List<(string Label, string Value, bool Highlight)> Body = new List<(string, string, bool)>();
        public readonly List<MenuItem> Items = new List<MenuItem>();
        public int Selected;
        public string Hint;
        /// <summary>A light dim lets the arena show through (pause, results), a heavy one hides it.</summary>
        public bool LightDim;

        public MenuItem Add(string id, string label, string value = null)
        {
            var item = new MenuItem { Id = id, Label = label, Value = value };
            Items.Add(item);
            return item;
        }

        public int IndexOf(string id) => Items.FindIndex(item => item.Id == id);
    }

    /// <summary>
    /// Binds the UI Toolkit HUD (Hud.uxml): score and chain, wave and enemies left, best score,
    /// power-up chips, the action meter, banners, floating points and the touch controls, plus a
    /// generic menu renderer for the title, settings, records, pause and results screens. Menu
    /// rows never take keyboard focus; the director reads the keys, and the mouse or a finger
    /// clicks rows.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class Hud : MonoBehaviour
    {
        const float FloaterLifetime = 1f;

        sealed class Floater
        {
            public Label Label;
            public Vector3 World;
            public float Age;
        }

        VisualElement root;
        VisualElement playHud;
        Label score;
        Label chain;
        VisualElement chainFill;
        Label wave;
        Label enemies;
        Label arenaName;
        Label best;
        VisualElement pauseButton;
        VisualElement powers;
        Label action;
        VisualElement actionFill;
        Label prompt;
        Label banner;
        Label bannerSub;
        VisualElement floaters;
        VisualElement stick;
        VisualElement stickKnob;
        VisualElement touchButton;
        Label touchButtonLabel;
        VisualElement menu;
        VisualElement menuDim;
        VisualElement logo;
        Label menuTitle;
        Label menuSubtitle;
        VisualElement menuBody;
        VisualElement menuItems;
        Label menuHint;
        Label sound;
        float bannerHideAt = float.MaxValue;
        readonly List<Floater> active = new List<Floater>();
        readonly List<VisualElement> rows = new List<VisualElement>();
        int shownScore = -1;

        /// <summary>A menu row was clicked or tapped (its index).</summary>
        public event Action<int> ItemClicked;

        /// <summary>The on-screen pause button was clicked or tapped.</summary>
        public event Action PauseClicked;

        /// <summary>The menu currently shown, or null.</summary>
        public MenuModel Menu { get; private set; }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            playHud = root.Q("play-hud");
            score = root.Q<Label>("score");
            chain = root.Q<Label>("chain");
            chainFill = root.Q("chain-fill");
            wave = root.Q<Label>("wave");
            enemies = root.Q<Label>("enemies");
            arenaName = root.Q<Label>("arena");
            best = root.Q<Label>("best");
            pauseButton = root.Q("pause-button");
            powers = root.Q("powers");
            action = root.Q<Label>("action");
            actionFill = root.Q("action-fill");
            prompt = root.Q<Label>("prompt");
            banner = root.Q<Label>("banner");
            bannerSub = root.Q<Label>("banner-sub");
            floaters = root.Q("floaters");
            stick = root.Q("stick");
            stickKnob = root.Q("stick-knob");
            touchButton = root.Q("touch-button");
            touchButtonLabel = root.Q<Label>("touch-button-label");
            menu = root.Q("menu");
            menuDim = root.Q("menu-dim");
            logo = root.Q("logo");
            menuTitle = root.Q<Label>("menu-title");
            menuSubtitle = root.Q<Label>("menu-subtitle");
            menuBody = root.Q("menu-body");
            menuItems = root.Q("menu-items");
            menuHint = root.Q<Label>("menu-hint");
            sound = root.Q<Label>("sound");
            pauseButton.AddManipulator(new Clickable(() => PauseClicked?.Invoke()));
        }

        // ---- Round HUD ----

        public void ShowPlayHud(bool visible) => SetVisible(playHud, visible);

        public void ResetRun(int bestScore)
        {
            best.text = bestScore.ToString();
            shownScore = -1;
            SetScore(0);
            SetChain(0, 0f);
            ClearFloaters();
        }

        public void SetScore(int value)
        {
            if (shownScore == value)
            {
                return;
            }
            bool pop = shownScore >= 0 && value > shownScore;
            shownScore = value;
            score.text = value.ToString();
            if (pop)
            {
                score.AddToClassList("score--pop");
                score.schedule.Execute(() => score.RemoveFromClassList("score--pop")).ExecuteLater(120);
            }
        }

        public void SetBest(int value) => best.text = value.ToString();

        /// <summary>The ring-out chain and how much of its window is left (0..1).</summary>
        public void SetChain(int count, float left)
        {
            chain.text = count >= 2 ? $"x{Math.Min(count, 5)} CHAIN" : string.Empty;
            chainFill.style.width = Length.Percent(count >= 2 ? Mathf.Clamp01(left) * 100f : 0f);
        }

        public void SetWave(int number, int left, string arena, bool boss)
        {
            wave.text = boss ? $"WAVE {number}  BOSS" : $"WAVE {number}";
            wave.EnableInClassList("wave--boss", boss);
            enemies.text = left == 1 ? "1 ENEMY LEFT" : $"{left} ENEMIES LEFT";
            arenaName.text = arena;
        }

        public string ScoreText => score.text;
        public string WaveText => wave.text;
        public string EnemiesText => enemies.text;

        /// <summary>Chips for the running power-ups with the seconds or charges left.</summary>
        public void SetPowers(float knockbackLeft, float rocketsLeft, int smashes)
        {
            int count = 0;
            if (knockbackLeft > 0f)
            {
                Chip(count++, $"KNOCKBACK  {Mathf.CeilToInt(knockbackLeft)}", Palette.Knockback);
            }
            if (smashes > 0)
            {
                Chip(count++, $"SMASH  x{smashes}", Palette.Smash);
            }
            if (rocketsLeft > 0f)
            {
                Chip(count++, $"ROCKETS  {Mathf.CeilToInt(rocketsLeft)}", Palette.Rockets);
            }
            while (powers.childCount > count)
            {
                powers.RemoveAt(powers.childCount - 1);
            }
        }

        void Chip(int index, string text, Color color)
        {
            Label chip;
            if (index < powers.childCount)
            {
                chip = (Label)powers[index];
            }
            else
            {
                chip = new Label { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("chip");
                powers.Add(chip);
            }
            chip.text = text;
            chip.style.color = color;
            chip.style.borderTopColor = chip.style.borderBottomColor = chip.style.borderLeftColor = chip.style.borderRightColor = color;
        }

        public int PowerChips => powers.childCount;

        public IEnumerable<string> PowerTexts
        {
            get
            {
                for (int i = 0; i < powers.childCount; i++)
                {
                    yield return ((Label)powers[i]).text;
                }
            }
        }

        /// <summary>The action button's name (DASH or SMASH) and how recharged it is (0..1).</summary>
        public void SetAction(string name, float ready)
        {
            action.text = ready >= 1f ? name : name + "  ...";
            action.EnableInClassList("action--ready", ready >= 1f);
            actionFill.style.width = Length.Percent(Mathf.Clamp01(ready) * 100f);
            touchButtonLabel.text = name;
            touchButton.EnableInClassList("touch-button--ready", ready >= 1f);
        }

        /// <summary>Hides the pause button without moving the best score next to it.</summary>
        public void SetPauseButton(bool visible) => pauseButton.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;

        public void SetPrompt(string text)
        {
            prompt.text = text ?? string.Empty;
            SetVisible(prompt, !string.IsNullOrEmpty(text));
        }

        /// <summary>The touch controls: shown on touch screens; the stick follows the finger.</summary>
        public void SetTouch(bool visible, Vector2? origin, Vector2 knob)
        {
            SetVisible(touchButton, visible);
            SetVisible(stick, visible);
            if (!visible || root?.panel == null)
            {
                return;
            }
            stick.EnableInClassList("stick--held", origin.HasValue);
            if (origin.HasValue)
            {
                Vector2 center = ScreenToPanel(origin.Value);
                Vector2 knobPanel = ScreenToPanel(knob);
                stick.style.left = center.x;
                stick.style.top = center.y;
                stick.style.bottom = StyleKeyword.Auto;
                stickKnob.style.translate = new Translate(knobPanel.x - center.x, knobPanel.y - center.y);
            }
            else
            {
                stick.style.left = StyleKeyword.Null;
                stick.style.top = StyleKeyword.Null;
                stick.style.bottom = StyleKeyword.Null;
                stickKnob.style.translate = new Translate(0f, 0f);
            }
        }

        Vector2 ScreenToPanel(Vector2 screen) =>
            RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y));

        /// <summary>Shows a big centred message for a moment; style is null, "danger", "boss" or "good".</summary>
        public void Banner(string text, string subtitle = null, string style = null, int milliseconds = 1100)
        {
            banner.text = text;
            bannerSub.text = subtitle ?? string.Empty;
            foreach (string name in new[] { "danger", "boss", "good" })
            {
                banner.EnableInClassList("banner--" + name, style == name);
            }
            banner.AddToClassList("banner--show");
            bannerSub.AddToClassList("banner--show");
            // Real time, so hit-stop and pause do not hold a banner on screen.
            bannerHideAt = Time.unscaledTime + milliseconds / 1000f;
        }

        public void HideBanner()
        {
            bannerHideAt = float.MaxValue;
            banner.RemoveFromClassList("banner--show");
            bannerSub.RemoveFromClassList("banner--show");
        }

        public string BannerText => banner.ClassListContains("banner--show") ? banner.text : null;

        /// <summary>Text that rises from a point in the arena and fades (floating points).</summary>
        public void Float(Vector3 world, string text, Color color, float size = 26f)
        {
            if (root?.panel == null || Camera.main == null || active.Count > 24)
            {
                return;
            }
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("display");
            label.AddToClassList("floater");
            label.style.color = color;
            label.style.fontSize = size;
            floaters.Add(label);
            var floater = new Floater { Label = label, World = world };
            active.Add(floater);
            Place(floater);
        }

        public int FloaterCount => active.Count;

        public IEnumerable<string> FloaterTexts
        {
            get
            {
                foreach (Floater floater in active)
                {
                    yield return floater.Label.text;
                }
            }
        }

        public void ClearFloaters()
        {
            foreach (Floater floater in active)
            {
                floater.Label.RemoveFromHierarchy();
            }
            active.Clear();
        }

        public void SetSound(string text) => sound.text = text;

        /// <summary>True when a screen point (pixels, origin bottom-left) is over the pause button.</summary>
        public bool IsOverPauseButton(Vector2 screen)
        {
            if (root?.panel == null || playHud.ClassListContains("hidden"))
            {
                return false;
            }
            Rect bounds = pauseButton.worldBound;
            bounds.xMin -= 8f;
            bounds.yMin -= 8f;
            bounds.xMax += 8f;
            bounds.yMax += 8f;
            return bounds.Contains(ScreenToPanel(screen));
        }

        // ---- Menus ----

        public void ShowMenu(MenuModel model)
        {
            Menu = model;
            SetVisible(menu, true);
            // The action meter and the touch controls would sit under the menu's hint.
            playHud.AddToClassList("play-hud--menu");
            menuDim.EnableInClassList("menu__dim--light", model.LightDim);
            SetVisible(logo, model.Logo);
            menuTitle.text = model.Title ?? string.Empty;
            SetVisible(menuTitle, !string.IsNullOrEmpty(model.Title));
            menuTitle.EnableInClassList("menu__title--danger", model.TitleDanger);
            menuSubtitle.text = model.Subtitle ?? string.Empty;
            SetVisible(menuSubtitle, !string.IsNullOrEmpty(model.Subtitle));

            menuBody.Clear();
            foreach ((string label, string value, bool highlight) in model.Body)
            {
                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.AddToClassList("body-line");
                var left = new Label(label) { pickingMode = PickingMode.Ignore };
                left.AddToClassList("body-line__label");
                var right = new Label(value) { pickingMode = PickingMode.Ignore };
                right.AddToClassList("body-line__value");
                right.EnableInClassList("body-line__value--new", highlight);
                line.Add(left);
                line.Add(right);
                menuBody.Add(line);
            }
            SetVisible(menuBody, model.Body.Count > 0);

            menuItems.Clear();
            rows.Clear();
            for (int i = 0; i < model.Items.Count; i++)
            {
                int index = i;
                var row = new VisualElement();
                row.AddToClassList("item");
                var label = new Label { name = "label", pickingMode = PickingMode.Ignore };
                label.AddToClassList("item__label");
                var value = new Label { name = "value", pickingMode = PickingMode.Ignore };
                value.AddToClassList("item__value");
                row.Add(label);
                row.Add(value);
                row.AddManipulator(new Clickable(() => ItemClicked?.Invoke(index)));
                menuItems.Add(row);
                rows.Add(row);
            }
            menuHint.text = model.Hint ?? string.Empty;
            RefreshMenu();
        }

        /// <summary>Redraws the rows' text and the highlight after a value or the selection changed.</summary>
        public void RefreshMenu()
        {
            if (Menu == null)
            {
                return;
            }
            for (int i = 0; i < rows.Count && i < Menu.Items.Count; i++)
            {
                MenuItem item = Menu.Items[i];
                rows[i].Q<Label>("label").text = item.Label;
                rows[i].Q<Label>("value").text = item.Value != null ? $"<  {item.Value}  >" : string.Empty;
                rows[i].EnableInClassList("item--selected", i == Menu.Selected);
            }
        }

        public void HideMenu()
        {
            Menu = null;
            SetVisible(menu, false);
            playHud.RemoveFromClassList("play-hud--menu");
        }

        // ---- Each frame ----

        void LateUpdate()
        {
            if (Time.unscaledTime >= bannerHideAt)
            {
                HideBanner();
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Floater floater = active[i];
                floater.Age += Time.unscaledDeltaTime;
                if (floater.Age >= FloaterLifetime)
                {
                    floater.Label.RemoveFromHierarchy();
                    active.RemoveAt(i);
                    continue;
                }
                Place(floater);
            }
        }

        void Place(Floater floater)
        {
            Camera view = Camera.main;
            if (view == null)
            {
                return;
            }
            float t = floater.Age / FloaterLifetime;
            // Through the viewport, so it also lines up when the camera renders to a texture.
            Vector3 viewport = view.WorldToViewportPoint(floater.World + Vector3.up * (1.2f * Mathf.Sqrt(t)));
            floater.Label.style.left = viewport.x * root.layout.width;
            floater.Label.style.top = (1f - viewport.y) * root.layout.height;
            floater.Label.style.opacity = viewport.z < 0f ? 0f : t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            float pop = t < 0.12f ? 1f + (0.12f - t) * 4f : 1f;
            floater.Label.style.scale = new Scale(new Vector3(pop, pop, 1f));
        }

        static void SetVisible(VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);
    }
}
