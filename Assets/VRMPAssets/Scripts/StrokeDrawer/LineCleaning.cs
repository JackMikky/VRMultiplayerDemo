using UnityEngine;
namespace VRMPAssets
{
    public class LineCleaning : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("PenTrail"))
            {
                Destroy(other.gameObject);
            }
        }
    }
}
