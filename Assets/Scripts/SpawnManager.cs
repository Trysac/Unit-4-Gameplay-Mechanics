using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] GameObject enemyPrefb;
    [SerializeField] GameObject powerupPrefab;
    [SerializeField] float spawnRange = 1f;
    [SerializeField] int enemiesCount;
    [SerializeField] int waveNumber = 0;

    float spawnPosX;
    float spawnPosZ;

    void Update()
    {
        enemiesCount = FindObjectsOfType<Enemy>().Length;
        if (enemiesCount == 0) 
        {
            Instantiate(powerupPrefab, GenerateSpawnPosition(), powerupPrefab.transform.rotation);
            waveNumber++;
            SpawnEnemiesWaves(waveNumber);
        }
    }

    private void SpawnEnemiesWaves(int enemiesToSpawn) 
    {
        for (int i = 0; i < enemiesToSpawn; i++) 
        { 
            Instantiate(enemyPrefb, GenerateSpawnPosition(), enemyPrefb.transform.rotation);
        }      
    }

    private Vector3 GenerateSpawnPosition() 
    {
        spawnPosX = Random.Range(-spawnRange, spawnRange);
        spawnPosZ = Random.Range(-spawnRange, spawnRange);
        Vector3 randomPos = new Vector3(spawnPosX, 0, spawnPosZ);
        return randomPos;
    }
}
