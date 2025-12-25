using PXR.Construction.Runtime;
using UnityEditor;
using UnityEngine;

namespace PXR.Construction.Editor
{
    [CustomEditor(typeof(TwinObjectPrimary))]
    public class TwinObjectPrimaryGUI : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            var targetComponent = target as TwinObjectPrimary;

            // TODO : ちゃんと個数引っ張ってくる
            for (int i = 0; i < 5; i++)
            {
                if (GUILayout.Button($"SetView Type{i}"))
                {
                    targetComponent.SetView(i);
                }
            }
        }
    }
}
