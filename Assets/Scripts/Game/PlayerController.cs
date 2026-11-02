using System.Collections;
using RingOut.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace RingOut.Game
{
    /// <summary>Rolls the player along the camera's forward axis and runs the power-up.</summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float speed = 5f;
        [SerializeField] float repulseForceWithPowerUp = 10f;
        [SerializeField] float powerUpDuration = 5f;
        [SerializeField] bool hasPowerUp;
        [FormerlySerializedAs("porwerUpIndicator")]
        [SerializeField] GameObject powerUpIndicator;

        Rigidbody body;
        Transform focalPoint;

        public bool HasPowerUp => hasPowerUp;

        void Start()
        {
            powerUpIndicator.SetActive(false);
            hasPowerUp = false;
            body = GetComponent<Rigidbody>();
            focalPoint = FindFirstObjectByType<RotateCamera>().transform;
        }

        void Update()
        {
            float forwardInput = Input.GetAxis("Vertical");
            body.AddForce(focalPoint.forward * forwardInput * speed);

            if (powerUpIndicator.activeSelf)
            {
                powerUpIndicator.transform.rotation = Quaternion.Euler(Vector3.zero);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("PowerUp"))
            {
                StartCoroutine(PowerUpCountdown());
                Destroy(other.gameObject);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Enemy") && hasPowerUp)
            {
                Rigidbody enemyBody = collision.gameObject.GetComponent<Rigidbody>();
                Vec3 impulse = Push.Knockback(ToVec(transform.position), ToVec(collision.transform.position), repulseForceWithPowerUp);
                enemyBody.AddForce(new Vector3(impulse.X, impulse.Y, impulse.Z), ForceMode.Impulse);
            }
        }

        IEnumerator PowerUpCountdown()
        {
            hasPowerUp = true;
            powerUpIndicator.SetActive(true);
            yield return new WaitForSeconds(powerUpDuration);
            powerUpIndicator.SetActive(false);
            hasPowerUp = false;
        }

        static Vec3 ToVec(Vector3 v) => new Vec3(v.x, v.y, v.z);
    }
}
