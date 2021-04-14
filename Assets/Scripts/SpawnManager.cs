using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] GameObject enemyPrefb;
    [SerializeField] float spawnRange = 1f;

    float spawnPosX;
    float spawnPosZ;

    void Start()
    {
        
        Instantiate(enemyPrefb, GenerateSpawnPosition(), enemyPrefb.transform.rotation); ;
    }

    void Update()
    {
        
    }

    private Vector3 GenerateSpawnPosition() 
    {
        spawnPosX = Random.Range(-spawnRange, spawnRange);
        spawnPosZ = Random.Range(-spawnRange, spawnRange);
        Vector3 randomPos = new Vector3(spawnPosX, 0, spawnPosZ);
        return randomPos;
    }
}
