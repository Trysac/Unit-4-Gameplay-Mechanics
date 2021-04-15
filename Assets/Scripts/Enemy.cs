using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float speed = 1f;
    
    Rigidbody enemyRb; 
    Transform player;

    void Start()
    {
        enemyRb = GetComponent<Rigidbody>();
        player = FindObjectOfType<PlayerController>().gameObject.transform;
    }


    void Update()
    {
        if (transform.position.y < -5) 
        { 
            Destroy(gameObject); 
        }

        Vector3 lookDirection = (player.position - this.transform.position).normalized;
        enemyRb.AddForce(lookDirection * speed);
    }
}
