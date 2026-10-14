using UnityEngine;

namespace Pachacuti
{
    /// <summary>Keeps a quad facing the camera (flames, crowd cards).</summary>
    public sealed class Billboard : MonoBehaviour
    {
        public bool YawOnly;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 to = transform.position - cam.transform.position;
            if (YawOnly) to.y = 0f;
            if (to.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }
    }
}
