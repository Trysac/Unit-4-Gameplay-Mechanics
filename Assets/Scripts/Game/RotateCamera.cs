using UnityEngine;

namespace RingOut.Game
{
    /// <summary>Turns the camera's focal point around the island with the horizontal axis.</summary>
    public class RotateCamera : MonoBehaviour
    {
        [SerializeField] float rotationSpeed = 1f;

        void Update()
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            transform.Rotate(Vector3.up, horizontalInput * rotationSpeed * Time.deltaTime);
        }
    }
}
