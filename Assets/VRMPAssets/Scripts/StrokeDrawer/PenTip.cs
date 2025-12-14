using UnityEngine;

namespace XRMultiplayer
{
    public class PenTip : MonoBehaviour
    {
        [SerializeField] private SimplePen simplePen;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("ColorPallet"))
            {
                ColorPallet pallet = other.GetComponent<ColorPallet>();
                if (pallet != null)
                {
                    simplePen.RequestSetColor(pallet.GetColor());
                }
            }
        }
    }
}