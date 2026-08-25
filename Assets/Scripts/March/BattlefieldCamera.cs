using UnityEngine;

namespace WarbornMarch
{
    /// <summary>
    /// Simple orbit camera for viewing the Sundered Ford battlefield.
    /// Right-click drag to orbit, scroll to zoom, WASD to pan.
    /// </summary>
    public class BattlefieldCamera : MonoBehaviour
    {
        [Header("Orbit Settings")]
        public float orbitSpeed = 120f;
        public float panSpeed = 15f;
        public float zoomSpeed = 10f;
        public float minDistance = 5f;
        public float maxDistance = 60f;
        public float minPitch = 5f;
        public float maxPitch = 80f;

        private float yaw = 0f;
        private float pitch = 45f;
        private float distance = 25f;
        private Vector3 target = Vector3.zero;

        void Start()
        {
            transform.position = target + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
            transform.LookAt(target);
        }

        void Update()
        {
            // Orbit with right mouse button
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                pitch -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // Zoom with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            // Pan with WASD
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (h != 0 || v != 0)
            {
                Vector3 move = new Vector3(h, 0, v) * panSpeed * Time.deltaTime;
                target += transform.TransformDirection(move);
            }

            // Update camera position
            transform.position = target + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
            transform.LookAt(target);
        }
    }
}
