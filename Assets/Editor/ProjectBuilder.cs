using System;
using System.IO;
using RingOut.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace RingOut.EditorTools
{
    /// <summary>
    /// Generates the textures, materials, physics materials, sky, font atlases, art asset, icon,
    /// scene and player settings from code, and builds the players, so every binary asset can be
    /// recreated from text. Batch mode:
    /// <code>
    /// Unity -batchmode -quit -projectPath . -executeMethod RingOut.EditorTools.ProjectBuilder.SetUpProject
    /// Unity -batchmode -quit -projectPath . -executeMethod RingOut.EditorTools.ProjectBuilder.BuildWindows -buildOutput &lt;folder&gt;
    /// Unity -batchmode -quit -projectPath . -executeMethod RingOut.EditorTools.ProjectBuilder.BuildWebGL -buildOutput &lt;folder&gt;
    /// </code>
    /// </summary>
    public static class ProjectBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string ArtFolder = "Assets/Art";
        const string ArtPath = "Assets/Art/GameArt.asset";
        const string IconPath = "Assets/Art/AppIcon.png";
        const string AudioFolder = "Assets/Audio";
        const string DisplayFontSource = "Assets/Fonts/Exo2-Black.ttf";
        const string DisplayFontPath = "Assets/UI/ExoBlack.asset";
        const string TextFontSource = "Assets/Fonts/Exo2-SemiBold.ttf";
        const string TextFontPath = "Assets/UI/ExoSemiBold.asset";
        const string LayoutPath = "Assets/UI/Hud.uxml";
        const string StylePath = "Assets/UI/Hud.uss";
        const string ThemePath = "Assets/UI/HudTheme.tss";
        const string PanelSettingsPath = "Assets/UI/HudPanelSettings.asset";
        const string Ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        [UnityEditor.MenuItem("Ring Out/Regenerate Art, Scene and Player Settings")]
        public static void SetUpProject()
        {
            CreateFont(DisplayFontSource, DisplayFontPath, "ExoBlack");
            CreateFont(TextFontSource, TextFontPath, "ExoSemiBold");
            // The style sheet refers to the fonts by path; re-import it so it picks up the new assets.
            AssetDatabase.ImportAsset(StylePath, ImportAssetOptions.ForceUpdate);
            GameArt art = CreateArt();
            PanelSettings panel = CreatePanelSettings();
            CreateScene(art, panel);
            ConfigurePlayer(CreateIcon());
            AssetDatabase.SaveAssets();
            Debug.Log("Ring Out project set up.");
        }

        /// <summary>
        /// Enters play mode and lets ScreenshotScenarios capture the README screenshots, then exits.
        /// Run in batch mode without -quit: <c>-executeMethod ...CaptureScreenshots -screenshot &lt;folder&gt;</c>.
        /// </summary>
        public static void CaptureScreenshots()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "RingOut.exe");

        public static void BuildWebGL() => Build(BuildTarget.WebGL, string.Empty);

        // ---- Font ----

        /// <summary>
        /// A static font atlas of the printable ASCII range from an Exo 2 TrueType font (used
        /// unmodified under the SIL Open Font License; the atlas only rasterises it).
        /// </summary>
        static FontAsset CreateFont(string sourcePath, string assetPath, string name)
        {
            if (AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            FontAsset font = FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false);
            font.name = name;
            if (!font.TryAddCharacters(Ascii, out string missing))
            {
                Debug.LogWarning($"{name} is missing: {missing}");
            }
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, assetPath);
            foreach (Texture2D atlas in font.atlasTextures)
            {
                atlas.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            font.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return font;
        }

        // ---- Art ----

        static GameArt CreateArt()
        {
            Directory.CreateDirectory(ArtFolder);
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(ArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<GameArt>();
                AssetDatabase.CreateAsset(art, ArtPath);
            }

            Shader standard = Shader.Find("Standard");
            Shader additive = Shader.Find("Legacy Shaders/Particles/Additive");

            Texture2D tiles = SaveTexture("FloorTiles", 256, FloorTilePixel, TextureWrapMode.Repeat);
            Texture2D rock = SaveTexture("Rock", 256, RockPixel, TextureWrapMode.Repeat);
            Texture2D enemySkin = SaveTexture("EnemyBall", 256, (u, v) => BallPixel(u, v, false), TextureWrapMode.Repeat);
            Texture2D playerSkin = SaveTexture("PlayerBall", 256, (u, v) => BallPixel(u, v, true), TextureWrapMode.Repeat);
            Texture2D dot = SaveTexture("SoftDot", 64, SoftDotPixel, TextureWrapMode.Clamp);

            art.Floor = Lit("Floor", standard, Palette.Floor, tiles, 0.25f);
            art.Rock = Lit("Rock", standard, Palette.Rock, rock, 0.05f);
            art.Trim = Lit("Trim", standard, Palette.Trim, null, 0.4f, Palette.Trim * 1.4f);
            art.Player = Lit("Player", standard, Color.white, playerSkin, 0.7f, Palette.PlayerStripe * 0.08f);
            art.Grunt = Lit("Grunt", standard, Palette.Of(RingOut.Core.EnemyKind.Grunt), enemySkin, 0.55f);
            art.Fast = Lit("Fast", standard, Palette.Of(RingOut.Core.EnemyKind.Fast), enemySkin, 0.6f);
            art.Heavy = Lit("Heavy", standard, Palette.Of(RingOut.Core.EnemyKind.Heavy), enemySkin, 0.75f, null, 0.6f);
            art.Splitter = Lit("Splitter", standard, Palette.Of(RingOut.Core.EnemyKind.Splitter), enemySkin, 0.55f);
            art.Mini = Lit("Mini", standard, Palette.Of(RingOut.Core.EnemyKind.Mini), enemySkin, 0.55f);
            art.Boss = Lit("Boss", standard, Palette.Of(RingOut.Core.EnemyKind.Boss), enemySkin, 0.7f, Palette.Of(RingOut.Core.EnemyKind.Boss) * 0.6f);
            art.Pickup = Lit("Pickup", standard, Color.white, null, 0.8f, Color.white);
            art.Rocket = Lit("Rocket", standard, Palette.Rockets, null, 0.6f, Palette.Rockets * 1.5f);
            art.Glow = MaterialAt("Glow", additive);
            art.Glow.mainTexture = dot;
            art.Glow.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorUtility.SetDirty(art.Glow);
            art.Ring = MaterialAt("Ring", additive);
            art.Ring.mainTexture = null;
            art.Ring.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorUtility.SetDirty(art.Ring);
            art.Sky = CreateSky();

            art.BallPhysics = PhysicsAt("Ball", 0.3f, 0.55f, PhysicsMaterialCombine.Maximum);
            art.FloorPhysics = PhysicsAt("Floor", 0.6f, 0f, PhysicsMaterialCombine.Average);

            art.TitleMusic = Clip("title_theme", music: true);
            art.BattleMusic = Clip("battle_theme", music: true);
            art.Bump = Clip("bump");
            art.Hit = Clip("hit");
            art.Knock = Clip("knock");
            art.RingOut = Clip("ring_out");
            art.Fall = Clip("fall");
            art.Dash = Clip("dash");
            art.Smash = Clip("smash");
            art.RocketLaunch = Clip("rocket");
            art.RocketHit = Clip("rocket_hit");
            art.Split = Clip("split");
            art.BossCharge = Clip("boss_charge");
            art.PowerUp = Clip("powerup");
            art.PowerEnd = Clip("power_end");
            art.Chain = Clip("chain");
            art.WaveStart = Clip("wave_start");
            art.BossWave = Clip("boss_wave");
            art.Fight = Clip("fight");
            art.WaveClear = Clip("wave_clear");
            art.NewRecord = Clip("new_record");
            art.GameOver = Clip("game_over");
            art.Move = Clip("move");
            art.Select = Clip("select");
            art.Pause = Clip("pause");
            EditorUtility.SetDirty(art);
            return art;
        }

        static Material MaterialAt(string name, Shader shader)
        {
            string path = $"{ArtFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            return material;
        }

        /// <summary>A Standard material; an emission colour turns on its glow.</summary>
        static Material Lit(string name, Shader shader, Color color, Texture2D texture, float smoothness,
            Color? emission = null, float metallic = 0f)
        {
            Material material = MaterialAt(name, shader);
            material.color = color;
            material.mainTexture = texture;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        static PhysicsMaterial PhysicsAt(string name, float friction, float bounciness, PhysicsMaterialCombine bounceCombine)
        {
            string path = $"{ArtFolder}/{name}.physicMaterial";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(material, path);
            }
            material.dynamicFriction = friction;
            material.staticFriction = friction;
            material.bounciness = bounciness;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            material.bounceCombine = bounceCombine;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>A dusk sky: Unity's procedural sky shader with a violet tint and a low sun.</summary>
        static Material CreateSky()
        {
            Material sky = MaterialAt("Sky", Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunDisk", 2f);
            sky.SetFloat("_SunSize", 0.05f);
            sky.SetFloat("_SunSizeConvergence", 4f);
            sky.SetFloat("_AtmosphereThickness", 1.15f);
            sky.SetColor("_SkyTint", Palette.Hex("#a48cf0"));
            // The camera looks down, so most of the backdrop is the haze below the horizon.
            sky.SetColor("_GroundColor", Palette.Hex("#6b3f86"));
            sky.SetFloat("_Exposure", 1.3f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        static Texture2D SaveTexture(string name, int size, Func<float, float, Color> pixel, TextureWrapMode wrap)
        {
            string path = $"{ArtFolder}/{name}.png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = pixel((x + 0.5f) / size, (y + 0.5f) / size);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = name == "SoftDot";
            importer.wrapMode = wrap;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Square floor tiles with bevelled grout and a little per-tile shade.</summary>
        static Color FloorTilePixel(float u, float v)
        {
            float cells = 2f;
            float cu = u * cells, cv = v * cells;
            float fu = cu - Mathf.Floor(cu), fv = cv - Mathf.Floor(cv);
            float edge = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
            float grout = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge - 0.02f) / 0.05f));
            int tile = (int)Mathf.Floor(cu) * 7 + (int)Mathf.Floor(cv) * 13;
            float shade = 0.9f + 0.1f * ((tile * 37) % 5) / 4f;
            float grain = 0.96f + 0.04f * Mathf.PerlinNoise(u * 40f, v * 40f);
            float bevel = edge < 0.1f ? 1.08f : 1f;
            float value = Mathf.Lerp(0.55f, shade * bevel, grout) * grain;
            return new Color(value, value, value * 1.03f, 1f);
        }

        /// <summary>Layered rock strata.</summary>
        static Color RockPixel(float u, float v)
        {
            float strata = Mathf.PerlinNoise(u * 4f, v * 24f + Mathf.PerlinNoise(u * 8f, v * 3f) * 2f);
            float grit = Mathf.PerlinNoise(u * 60f, v * 60f);
            float value = 0.7f + 0.3f * strata - 0.1f * grit;
            return new Color(value, value * 0.95f, value * 1.05f, 1f);
        }

        /// <summary>
        /// A ball skin on the sphere's UVs (u around the equator, v pole to pole): a band round
        /// the middle and a stripe from pole to pole, so a rolling ball reads as rolling.
        /// </summary>
        static Color BallPixel(float u, float v, bool player)
        {
            float band = Mathf.Abs(v - 0.5f);
            float stripe = Mathf.Min(Mathf.Abs(u - 0.25f), Mathf.Abs(u - 0.75f));
            Color baseColor = Color.white;
            Color mark = player ? Palette.PlayerStripe : new Color(0.32f, 0.3f, 0.36f);
            float amount = Mathf.Max(Coverage(0.06f - band, 0.01f), Coverage(0.035f - stripe, 0.008f));
            Color color = Color.Lerp(baseColor, mark, amount);
            if (!player)
            {
                // A soft darkening towards the poles keeps the colours rich.
                color *= 0.85f + 0.15f * (1f - Mathf.Abs(v - 0.5f) * 2f);
                color.a = 1f;
            }
            return color;
        }

        static Color SoftDotPixel(float u, float v)
        {
            float dx = u * 2f - 1f, dy = v * 2f - 1f;
            float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            return new Color(1f, 1f, 1f, alpha * alpha * (3f - 2f * alpha));
        }

        static float Coverage(float inside, float width) => Mathf.Clamp01(inside / width + 0.5f);

        static AudioClip Clip(string name, bool music = false)
        {
            string path = $"{AudioFolder}/{name}.ogg";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing {path}: run python tools/audio/build.py first.");
                return null;
            }
            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            sample.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = sample;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---- UI ----

        static PanelSettings CreatePanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        // ---- Scene ----

        static void CreateScene(GameArt art, PanelSettings panel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = art.Sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex("#8f7fd6");
            RenderSettings.ambientEquatorColor = Palette.Hex("#a0607a");
            RenderSettings.ambientGroundColor = Palette.Hex("#2a2045");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.Fog;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 150f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            var sun = new GameObject("Sun");
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Palette.Hex("#ffd9b0");
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.7f;
            RenderSettings.sun = light;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 300f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.AddComponent<AudioListener>();
            CameraRig rig = cameraObject.AddComponent<CameraRig>();

            var ui = new GameObject("HUD");
            var document = ui.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            Hud hud = ui.AddComponent<Hud>();

            var game = new GameObject("Game");
            var director = game.AddComponent<GameDirector>();
            var serialized = new SerializedObject(director);
            serialized.FindProperty("art").objectReferenceValue = art;
            serialized.FindProperty("hud").objectReferenceValue = hud;
            serialized.FindProperty("cameraRig").objectReferenceValue = rig;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer(Texture2D icon)
        {
            PlayerSettings.companyName = "Trysac";
            PlayerSettings.productName = "Ring Out";
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
            var standaloneIcons = new Texture2D[sizes.Length];
            Array.Fill(standaloneIcons, icon);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, standaloneIcons, IconKind.Any);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            // Static hosts such as GitHub Pages do not send Content-Encoding for .br/.gz files.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
        }

        // ---- Icon: the player's ball knocking an enemy off a round island ----

        static Texture2D CreateIcon()
        {
            const int size = 1024;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = IconPixel(x + 0.5f - size / 2f, y + 0.5f - size / 2f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(IconPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        }

        static Color IconPixel(float x, float y)
        {
            const float aa = 1.5f;
            float tile = RoundedBox(x, y, 470f, 470f, 170f);
            Color color = Color.Lerp(Palette.SkyTop, Palette.Hex("#6a3f8f"), Mathf.Clamp01((y + 470f) / 940f));
            color = Color.Lerp(color, Palette.SkyHorizon, Mathf.Clamp01(1f - Mathf.Abs(y + 40f) / 260f) * 0.45f);
            color.a = Coverage(-tile, aa);

            // The island: a flattened disc with its glowing trim and a rocky underside.
            float ix = x / 400f, iy = (y + 150f) / 120f;
            float island = Mathf.Sqrt(ix * ix + iy * iy) - 1f;
            float under = Mathf.Max(Mathf.Abs(x) / 400f - (y + 150f + 420f) / 420f, y + 150f);
            if (under < 0f && y < -150f)
            {
                color = Over(color, Palette.Rock, Coverage(-under * 400f, aa));
            }
            color = Over(color, Palette.Floor, Coverage(-island * 120f, aa));
            float trim = Mathf.Abs(island * 120f + 8f) - 7f;
            color = Over(color, Palette.Trim, Coverage(-trim, aa));

            // The enemy, flying off the right edge.
            float enemy = Length(x - 300f, y - 70f) - 105f;
            color = Shade(color, x - 300f, y - 70f, 105f, Palette.Of(RingOut.Core.EnemyKind.Grunt), enemy, aa);
            // Motion streaks behind it.
            for (int i = 0; i < 3; i++)
            {
                float streak = RoundedBox(x - 150f + i * 10f, y - 40f - i * 45f, 70f - i * 12f, 7f, 7f);
                color = Over(color, Color.white, Coverage(-streak, aa) * 0.7f);
            }

            // The player's ball with its cyan band, centre-left on the island.
            float player = Length(x + 120f, y + 20f) - 165f;
            color = Shade(color, x + 120f, y + 20f, 165f, Color.white, player, aa);
            float band = Mathf.Abs((y + 20f) - (x + 120f) * 0.25f) - 18f;
            if (player < 0f)
            {
                color = Over(color, Palette.PlayerStripe, Coverage(-band, aa) * Coverage(-player, aa));
            }

            // An impact star between them.
            float star = Mathf.Min(RoundedBox(x - 120f, y - 30f, 60f, 10f, 10f), RoundedBox(x - 120f, y - 30f, 10f, 60f, 10f));
            color = Over(color, Palette.Gold, Coverage(-star, aa) * color.a);
            return color;
        }

        /// <summary>A lit sphere disc: lighter towards the top-left, with a highlight.</summary>
        static Color Shade(Color below, float dx, float dy, float radius, Color body, float distance, float aa)
        {
            if (distance > aa)
            {
                return below;
            }
            float light = Mathf.Clamp01(0.55f + 0.45f * (-dx * 0.6f + dy * 0.8f) / radius);
            Color shaded = Color.Lerp(body * 0.55f, Color.Lerp(body, Color.white, 0.15f), light);
            shaded.a = 1f;
            float shine = Length((dx + radius * 0.35f) / 1.5f, dy - radius * 0.45f) - radius * 0.18f;
            shaded = Color.Lerp(shaded, Color.white, Coverage(-shine, 6f) * 0.8f);
            return Over(below, shaded, Coverage(-distance, aa));
        }

        static float Length(float x, float y) => Mathf.Sqrt(x * x + y * y);

        static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(x) - (halfWidth - radius);
            float qy = Mathf.Abs(y) - (halfHeight - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static Color Over(Color below, Color above, float amount)
        {
            Color mixed = Color.Lerp(below, above, amount);
            mixed.a = below.a;
            return mixed;
        }

        // ---- Builds ----

        static void Build(BuildTarget target, string fileName)
        {
            string folder = Argument("-buildOutput");
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentException("Pass -buildOutput <folder> outside the repository.");
            }
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = string.IsNullOrEmpty(fileName) ? folder : Path.Combine(folder, fileName),
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Build {target}: {summary.result}, {summary.totalSize / (1024 * 1024f):0.0} MB, {summary.totalErrors} errors.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
