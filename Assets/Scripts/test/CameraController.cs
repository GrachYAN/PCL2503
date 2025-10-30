using UnityEngine;

namespace ChessMiniDemo
{
    public class CameraController : MonoBehaviour
    {
        public Transform boardTransform;
        public Camera cam;

        public Vector3 whiteCamPosition = new Vector3(4, 6, -4);
        public Vector3 blackCamPosition = new Vector3(4, 6, 12);
        public Vector3 lookAtPoint = new Vector3(4, 0, 4);

        void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        public void WhitePerspective()
        {
            if (cam == null) return;
            cam.transform.position = whiteCamPosition;
            cam.transform.LookAt(lookAtPoint);
        }

        public void BlackPerspective()
        {
            if (cam == null) return;
            cam.transform.position = blackCamPosition;
            cam.transform.LookAt(lookAtPoint);
        }
    }
}
