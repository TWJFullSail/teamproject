using UnityEngine;

namespace BulletHeavenWeapons
{
    public class WeaponRotator : MonoBehaviour
    {
        public float rotationSpeed = 90f;
        public Vector3 rotationAxis = Vector3.up;

        void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }
}
