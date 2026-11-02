using RingOut.Core;
using UnityEngine;

namespace RingOut.Game
{
    /// <summary>The generated materials, physics materials and synthesized audio, wired up by ProjectBuilder.</summary>
    public sealed class GameArt : ScriptableObject
    {
        [Header("Materials")]
        public Material Floor;
        public Material Rock;
        public Material Trim;
        public Material Player;
        public Material Grunt;
        public Material Fast;
        public Material Heavy;
        public Material Splitter;
        public Material Mini;
        public Material Boss;
        public Material Pickup;
        public Material Ring;
        public Material Rocket;
        public Material Glow;
        public Material Sky;

        [Header("Physics")]
        public PhysicsMaterial BallPhysics;
        public PhysicsMaterial FloorPhysics;

        [Header("Music")]
        public AudioClip TitleMusic;
        public AudioClip BattleMusic;

        [Header("Arena")]
        public AudioClip Bump;
        public AudioClip Hit;
        public AudioClip Knock;
        public AudioClip RingOut;
        public AudioClip Fall;
        public AudioClip Dash;
        public AudioClip Smash;
        public AudioClip RocketLaunch;
        public AudioClip RocketHit;
        public AudioClip Split;
        public AudioClip BossCharge;
        public AudioClip PowerUp;
        public AudioClip PowerEnd;
        public AudioClip Chain;

        [Header("Run")]
        public AudioClip WaveStart;
        public AudioClip BossWave;
        public AudioClip Fight;
        public AudioClip WaveClear;
        public AudioClip NewRecord;
        public AudioClip GameOver;

        [Header("Menus")]
        public AudioClip Move;
        public AudioClip Select;
        public AudioClip Pause;

        public Material EnemyMaterial(EnemyKind kind) => kind switch
        {
            EnemyKind.Grunt => Grunt,
            EnemyKind.Fast => Fast,
            EnemyKind.Heavy => Heavy,
            EnemyKind.Splitter => Splitter,
            EnemyKind.Mini => Mini,
            _ => Boss,
        };
    }
}
