using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed = 5f;

    Rigidbody myRigidbody;
    Transform focalPoint;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        focalPoint = FindObjectOfType<RotateCamera>().gameObject.transform;
    }

    void Update()
    {
        float forwardInput = Input.GetAxis("Vertical");
        myRigidbody.AddForce(focalPoint.forward * forwardInput * speed);
    }
}
