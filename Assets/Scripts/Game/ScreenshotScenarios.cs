using System;
using System.Collections;
using System.IO;
using RingOut.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace RingOut.Game
{
    /// <summary>
    /// Opt-in capture of README screenshots: <c>RingOut.exe -screenshot &lt;folder&gt; -scenario title,play,...</c>.
    /// Without the -screenshot argument nothing is created, so normal runs and WebGL builds
    /// (which have no command line) are unaffected. The same hook runs inside the editor
    /// (ProjectBuilder.CaptureScreenshots), which renders the camera and the HUD off screen and
    /// blends them, for machines that cannot run the player.
    /// With <c>-frames N -fps F</c> each scenario is recorded instead as N numbered frames
    /// (<c>&lt;folder&gt;/&lt;scenario&gt;/frame_0000.png</c>) at a fixed timestep of 1/F seconds;
    /// the <c>gameplay</c> scenario plays a wave with a scripted player for the README GIF.
    /// </summary>
    public sealed class ScreenshotScenarios : MonoBehaviour
    {
        const string DefaultScenarios = "title,play,powers,smash,boss,ring,islands,crumbling,pause,results,howto,records";
        const int Width = 1280;
        const int Height = 720;

        string folder;
        string[] scenarios;
        RenderTexture sceneTarget;
        RenderTexture uiTarget;
        GameDirector director;
        int frames;
        int fps = 15;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            string target = Argument("-screenshot");
            if (string.IsNullOrEmpty(target))
            {
                return;
            }
            var host = new GameObject(nameof(ScreenshotScenarios)).AddComponent<ScreenshotScenarios>();
            host.folder = target;
            host.scenarios = (Argument("-scenario") ?? DefaultScenarios).Split(',');
            int.TryParse(Argument("-frames"), out host.frames);
            if (int.TryParse(Argument("-fps"), out int rate) && rate > 0)
            {
                host.fps = rate;
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            director = FindAnyObjectByType<GameDirector>();
            director.UseMemoryStore();
            if (Application.isEditor)
            {
                RenderOffscreen();
            }
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (string raw in scenarios)
            {
                string scenario = raw.Trim();
                director.Seed(StableSeed(scenario));
                director.ScriptedInput = false;
                director.ScriptedMove = Vector3.zero;
                yield return Stage(scenario);
                if (frames > 0)
                {
                    yield return Record(scenario);
                    Time.timeScale = 1f;
                    director.ScriptedInput = false;
                    continue;
                }
                Texture2D shot;
                if (Application.isEditor)
                {
                    yield return null;
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(folder, scenario + ".png"), shot.EncodeToPNG());
                Destroy(shot);
                Debug.Log($"Saved screenshot {scenario}.png");
                Time.timeScale = 1f;
                director.ScriptedInput = false;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        IEnumerator Stage(string scenario)
        {
            switch (scenario)
            {
                case "title":
                    director.ToTitle();
                    yield return new WaitForSecondsRealtime(3.5f);
                    break;
                case "howto":
                    director.ToTitle();
                    director.Activate("howto");
                    yield return new WaitForSecondsRealtime(1.5f);
                    break;
                case "records":
                    director.Records.Submit(4850, 7, 4, 21);
                    director.Records.Submit(12300, 12, 5, 48);
                    director.Records.Submit(2100, 4, 2, 9);
                    director.ToTitle();
                    director.Activate("records");
                    yield return new WaitForSecondsRealtime(1.5f);
                    break;
                case "play":
                    yield return Run(3, 2.5f);
                    yield return RingOut(1);
                    yield return new WaitForSecondsRealtime(0.3f);
                    break;
                case "powers":
                    yield return Run(4, 2.2f);
                    // Only the granted power-ups: the wave's own pickup stays out of the way.
                    Destroy(director.PowerUp.gameObject);
                    director.Match.PowerUpCollected(PowerUpKind.Knockback);
                    director.Match.PowerUpCollected(PowerUpKind.Rockets);
                    yield return new WaitForSecondsRealtime(1.4f);
                    break;
                case "smash":
                    yield return Run(3, 2.2f);
                    director.Match.PowerUpCollected(PowerUpKind.Smash);
                    yield return new WaitForSecondsRealtime(1.1f);
                    director.Act();
                    float waited = 0f;
                    while (director.Player.Smashing && waited < 2f)
                    {
                        waited += Time.deltaTime;
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(0.3f);
                    break;
                case "boss":
                    yield return Run(5, 2.2f);
                    float wind = 0f;
                    while (wind < 5f && !BossWindingUp())
                    {
                        wind += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(0.3f);
                    break;
                case "ring":
                    yield return Run(6, 2.6f);
                    break;
                case "islands":
                    yield return Run(11, 2.6f);
                    break;
                case "crumbling":
                    // Wait out the countdown first: only the fight phase counts toward the crumble.
                    yield return Run(16, 1.9f);
                    // Fast-forward the fight so the edge has crumbled in.
                    director.Match.Step(25f);
                    director.ScriptedMove = new Vector3(0.3f, 0f, -1f).normalized;
                    yield return new WaitForSecondsRealtime(0.5f);
                    break;
                case "pause":
                    yield return Run(2, 2.4f);
                    director.TogglePause();
                    yield return new WaitForSecondsRealtime(0.6f);
                    break;
                case "results":
                    yield return Run(3, 2.4f);
                    yield return RingOut(2);
                    yield return new WaitForSecondsRealtime(0.6f);
                    director.Player.Place(new Vector3(0f, -4f, 0f));
                    float wait = 0f;
                    while (director.Screen != GameScreen.Results && wait < 8f)
                    {
                        wait += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(0.7f);
                    break;
                case "gameplay":
                    yield return Run(3, 2.2f);
                    director.Match.PowerUpCollected(PowerUpKind.Smash);
                    yield return new WaitForSecondsRealtime(0.2f);
                    break;
                default:
                    Debug.LogWarning($"Unknown screenshot scenario '{scenario}'.");
                    break;
            }
        }

        /// <summary>Writes <see cref="frames"/> frames of the scenario at a fixed timestep.</summary>
        IEnumerator Record(string scenario)
        {
            string target = Path.Combine(folder, scenario);
            Directory.CreateDirectory(target);
            Time.captureFramerate = fps;
            for (int i = 0; i < frames; i++)
            {
                if (scenario == "gameplay")
                {
                    PlayStep(i);
                }
                yield return null;
                Texture2D shot;
                if (Application.isEditor)
                {
                    shot = CaptureOffscreen();
                }
                else
                {
                    yield return new WaitForEndOfFrame();
                    shot = ScreenCapture.CaptureScreenshotAsTexture();
                }
                File.WriteAllBytes(Path.Combine(target, $"frame_{i:0000}.png"), shot.EncodeToPNG());
                Destroy(shot);
            }
            Time.captureFramerate = 0;
            Debug.Log($"Saved {frames} frames of {scenario}");
        }

        /// <summary>
        /// One frame of the scripted player: get between the arena centre and the nearest enemy,
        /// then charge it outward, smash once early on and dash later, and back off the edge.
        /// </summary>
        void PlayStep(int frame)
        {
            PlayerBall player = director.Player;
            if (player == null || director.Match == null || director.Match.IsOver)
            {
                return;
            }
            Vector3 at = player.transform.position;
            at.y = 0f;
            EnemyBall nearest = null;
            float best = float.MaxValue;
            foreach (EnemyBall enemy in director.Enemies)
            {
                if (enemy == null || enemy.Gone)
                {
                    continue;
                }
                float distance = Vector3.Distance(at, enemy.transform.position);
                if (distance < best)
                {
                    best = distance;
                    nearest = enemy;
                }
            }
            Vector3 move = -at;
            if (nearest != null && at.magnitude < 7.5f)
            {
                Vector3 foe = nearest.transform.position;
                foe.y = 0f;
                Vector3 outward = foe.sqrMagnitude > 0.01f ? foe.normalized : Vector3.forward;
                bool inside = Vector3.Dot(at - foe, outward) < -0.6f;
                move = (inside ? foe + outward * 2f : foe - outward * 2.2f) - at;
            }
            director.ScriptedMove = move.sqrMagnitude > 0.01f ? move.normalized : Vector3.zero;
            if (frame == Mathf.RoundToInt(fps * 1.4f) || (frame > fps * 3f && frame % Mathf.RoundToInt(fps * 1.5f) == 0 && best < 3.5f))
            {
                director.Act();
            }
        }

        /// <summary>Starts a run on a wave and waits for its enemies to land.</summary>
        IEnumerator Run(int wave, float settle)
        {
            director.StartRun(wave);
            director.ScriptedInput = true;
            director.HitStopEnabled = true;
            director.CameraRig.Yaw = 20f;
            yield return new WaitForSecondsRealtime(settle);
        }

        bool BossWindingUp()
        {
            foreach (EnemyBall enemy in director.Enemies)
            {
                if (enemy != null && enemy.Kind == EnemyKind.Boss && enemy.WindingUp)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Drops enemies over the edge one after another to build a chain.</summary>
        IEnumerator RingOut(int count)
        {
            for (int i = 0; i < count; i++)
            {
                foreach (EnemyBall enemy in director.Enemies)
                {
                    if (enemy != null && !enemy.Gone && enemy.Kind != EnemyKind.Boss)
                    {
                        enemy.Body.position = enemy.transform.position + new Vector3(0f, -4f, 0f);
                        enemy.transform.position = enemy.Body.position;
                        break;
                    }
                }
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        /// <summary>Points the camera and the HUD panel at render textures of a fixed size.</summary>
        void RenderOffscreen()
        {
            sceneTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            uiTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Camera.main.targetTexture = sceneTarget;
            var document = director.Hud.GetComponent<UIDocument>();
            PanelSettings panel = Instantiate(document.panelSettings);
            panel.targetTexture = uiTarget;
            panel.clearColor = true;
            panel.colorClearValue = Color.clear;
            document.panelSettings = panel;
        }

        /// <summary>Blends the HUD over the arena, like the screen would show them.</summary>
        Texture2D CaptureOffscreen()
        {
            Color[] scene = Read(sceneTarget);
            Color[] ui = Read(uiTarget);
            for (int i = 0; i < scene.Length; i++)
            {
                Color over = ui[i];
                Color result = Color.Lerp(scene[i], new Color(over.r, over.g, over.b), over.a);
                result.a = 1f;
                scene[i] = result;
            }
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.SetPixels(scene);
            shot.Apply();
            return shot;
        }

        static Color[] Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            Color[] pixels = texture.GetPixels();
            Destroy(texture);
            return pixels;
        }

        static int StableSeed(string text)
        {
            int seed = 17;
            foreach (char c in text)
            {
                seed = seed * 31 + c;
            }
            return seed & 0xffff;
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
