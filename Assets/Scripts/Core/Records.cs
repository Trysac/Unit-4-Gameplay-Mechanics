using System.Collections.Generic;

namespace RingOut.Core
{
    /// <summary>Where records and settings are kept (PlayerPrefs in the game, memory in tests).</summary>
    public interface IRecordStore
    {
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class MemoryRecordStore : IRecordStore
    {
        readonly Dictionary<string, int> values = new Dictionary<string, int>();

        public int GetInt(string key, int fallback) => values.TryGetValue(key, out int value) ? value : fallback;
        public void SetInt(string key, int value) => values[key] = value;
        public void Save() => SaveCount++;

        /// <summary>How many times the store was flushed.</summary>
        public int SaveCount { get; private set; }
    }

    /// <summary>What a finished run changed in the records.</summary>
    public readonly struct RecordUpdate
    {
        public readonly bool NewBestScore;
        public readonly bool NewBestWave;
        public readonly int PreviousBestScore;

        public RecordUpdate(bool newBestScore, bool newBestWave, int previousBestScore)
        {
            NewBestScore = newBestScore;
            NewBestWave = newBestWave;
            PreviousBestScore = previousBestScore;
        }
    }

    /// <summary>The best score, the furthest wave, the longest chain and the runs played, on the device.</summary>
    public sealed class Records
    {
        const string Prefix = "ringout.records.";

        readonly IRecordStore store;

        public Records(IRecordStore store) => this.store = store;

        public int BestScore => store.GetInt(Prefix + "score", 0);
        public int BestWave => store.GetInt(Prefix + "wave", 0);
        public int BestChain => store.GetInt(Prefix + "chain", 0);
        public int Runs => store.GetInt(Prefix + "runs", 0);
        public int RingOuts => store.GetInt(Prefix + "ringouts", 0);

        /// <summary>Records a finished run (the wave it reached) and saves the store once.</summary>
        public RecordUpdate Submit(int score, int wave, int chain, int ringOuts)
        {
            int previous = BestScore;
            bool newScore = score > previous;
            if (newScore)
            {
                store.SetInt(Prefix + "score", score);
            }
            bool newWave = wave > BestWave;
            if (newWave)
            {
                store.SetInt(Prefix + "wave", wave);
            }
            if (chain > BestChain)
            {
                store.SetInt(Prefix + "chain", chain);
            }
            store.SetInt(Prefix + "runs", Runs + 1);
            store.SetInt(Prefix + "ringouts", RingOuts + ringOuts);
            store.Save();
            return new RecordUpdate(newScore, newWave, previous);
        }
    }

    /// <summary>The settings menu, kept on the device between sessions.</summary>
    public sealed class GameSettings
    {
        const string Prefix = "ringout.settings.";
        public const int MaxVolume = 10;
        public const int MaxCameraSpeed = 5;

        /// <summary>Master volume in tenths, 0..10.</summary>
        public int Volume = 8;
        public bool Muted;
        public bool ScreenShake = true;
        /// <summary>How fast the camera turns, 1..5.</summary>
        public int CameraSpeed = 3;
        /// <summary>The on-screen stick and button for touch screens.</summary>
        public bool TouchControls = true;

        public void StepVolume(int step) => Volume = Clamp(Volume + step, 0, MaxVolume);

        public void StepCameraSpeed(int step) => CameraSpeed = Clamp(CameraSpeed + step, 1, MaxCameraSpeed);

        static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        /// <summary>Reads the settings, falling back to the defaults for missing or broken values.</summary>
        public static GameSettings Load(IRecordStore store)
        {
            var defaults = new GameSettings();
            return new GameSettings
            {
                Volume = Clamp(store.GetInt(Prefix + "volume", defaults.Volume), 0, MaxVolume),
                Muted = store.GetInt(Prefix + "muted", 0) == 1,
                ScreenShake = store.GetInt(Prefix + "shake", 1) == 1,
                CameraSpeed = Clamp(store.GetInt(Prefix + "camera", defaults.CameraSpeed), 1, MaxCameraSpeed),
                TouchControls = store.GetInt(Prefix + "touch", 1) == 1,
            };
        }

        public void Save(IRecordStore store)
        {
            store.SetInt(Prefix + "volume", Volume);
            store.SetInt(Prefix + "muted", Muted ? 1 : 0);
            store.SetInt(Prefix + "shake", ScreenShake ? 1 : 0);
            store.SetInt(Prefix + "camera", CameraSpeed);
            store.SetInt(Prefix + "touch", TouchControls ? 1 : 0);
            store.Save();
        }
    }
}
