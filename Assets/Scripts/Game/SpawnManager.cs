using RingOut.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace RingOut.Game
{
    /// <summary>Sends the next wave, and a power-up if none is waiting, once every enemy has fallen off.</summary>
    public class SpawnManager : MonoBehaviour
    {
        [FormerlySerializedAs("enemyPrefb")]
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] GameObject powerupPrefab;
        [SerializeField] float spawnRange = 1f;
        [SerializeField] int enemiesCount;
        [SerializeField] int waveNumber;

        public int Wave => waveNumber;

        void Update()
        {
            enemiesCount = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
            if (enemiesCount == 0)
            {
                // One power-up waits on the island at a time; unclaimed ones do not pile up.
                if (GameObject.FindGameObjectsWithTag("PowerUp").Length == 0)
                {
                    Instantiate(powerupPrefab, RandomSpawnPosition(), powerupPrefab.transform.rotation);
                }
                waveNumber++;
                SpawnWave(waveNumber);
            }
        }

        void SpawnWave(int wave)
        {
            int count = Waves.EnemiesIn(wave);
            for (int i = 0; i < count; i++)
            {
                Instantiate(enemyPrefab, RandomSpawnPosition(), enemyPrefab.transform.rotation);
            }
        }

        Vector3 RandomSpawnPosition() =>
            new Vector3(Random.Range(-spawnRange, spawnRange), 0f, Random.Range(-spawnRange, spawnRange));
    }
}
