using UnityEngine;

namespace XRMultiplayer
{
    public class PenTip : MonoBehaviour
    {
        [SerializeField] private Renderer penRenderer;

        [SerializeField] private SimplePen simplePen;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("ColorPallet"))
            {
                ColorPallet pallet = other.GetComponent<ColorPallet>();
                if (pallet != null)
                {
                    simplePen.SetColor(pallet.GetColor());
                }
            }
        }
    }
}