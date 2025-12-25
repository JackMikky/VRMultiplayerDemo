using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PXR.Construction.Runtime
{
    public class TwinObjectPrimary : TwinObjectBase
    {
        /// <summary>
        /// XRGrabInteractableコンポーネント
        /// </summary>
        public XRGrabInteractable XRGrabInteractable;

        protected override void Update()
        {
            base.Update();

            MirroringRequest?.Invoke(this);

            if (!XRGrabInteractable)
            {
                return;
            }

            XRGrabInteractable.deactivated.AddListener((interactor) =>
            {
                // TODO : グラブされていなくてかつ静止しているときにスナップ処理？
            });
        }
    }
}
