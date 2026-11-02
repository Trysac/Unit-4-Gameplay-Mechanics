using UnityEngine;

namespace RingOut.Game
{
    /// <summary>Rolls towards the player and falls off the island.</summary>
    public class Enemy : MonoBehaviour
    {
        [SerializeField] float speed = 1f;

        Rigidbody body;
        Transform player;

        void Start()
        {
            body = GetComponent<Rigidbody>();
            player = FindFirstObjectByType<PlayerController>().transform;
        }

        void Update()
        {
            if (transform.position.y < -5f)
            {
                Destroy(gameObject);
            }
        }

        // Forces go in the physics step: added every rendered frame, they scaled with the frame rate.
        void FixedUpdate()
        {
            Vector3 lookDirection = (player.position - transform.position).normalized;
            body.AddForce(lookDirection * speed);
        }
    }
}
