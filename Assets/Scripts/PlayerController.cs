using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    [SerializeField] float repulseForceWithPowerUp = 10f;
    [SerializeField] float powerUpDuration = 5f;
    [SerializeField] bool hasPowerUp;
    [SerializeField] GameObject porwerUpIndicator;

    Rigidbody myRigidbody;
    Transform focalPoint;

    void Start()
    {
        porwerUpIndicator.SetActive(false);
        hasPowerUp = false;
        myRigidbody = GetComponent<Rigidbody>();
        focalPoint = FindObjectOfType<RotateCamera>().gameObject.transform;
    }

    void Update()
    {
        float forwardInput = Input.GetAxis("Vertical");
        myRigidbody.AddForce(focalPoint.forward * forwardInput * speed);

        if (porwerUpIndicator.activeSelf) 
        {
            porwerUpIndicator.transform.rotation = Quaternion.Euler(Vector3.zero);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.Equals("PowerUp")) 
        {
            StartCoroutine(PowerupCountdownRoutine());
            Destroy(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Enemy") && hasPowerUp)
        {
            Rigidbody enemyRigidbody = collision.gameObject.GetComponent<Rigidbody>();
            Vector3 awayFromPlayer = collision.transform.position - this.transform.position;
            enemyRigidbody.AddForce(awayFromPlayer * repulseForceWithPowerUp, ForceMode.Impulse);
        }
    }

    private IEnumerator PowerupCountdownRoutine() 
    {
        hasPowerUp = true;
        porwerUpIndicator.SetActive(true);
        yield return new WaitForSeconds(powerUpDuration);
        porwerUpIndicator.SetActive(false);
        hasPowerUp = false;
    }
}
