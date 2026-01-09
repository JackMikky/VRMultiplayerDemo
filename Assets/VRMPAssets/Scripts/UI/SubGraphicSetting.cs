using UnityEngine;

namespace VRMPAssets.Scripts.UI
{
    public class SubGraphicSetting : MonoBehaviour
    {
        [HideInInspector]
        public SceneChangeSubGraphic graphic;

        [Header("Standby Object")]
        public GameObject standbyObject;
    }
}