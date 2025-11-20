using UnityEngine;
using UnityEditor;

public class StencilBatchSetter : EditorWindow
{
    public Material[] materials;
    public int stencilRef = 1;
    public int stencilComp = (int)UnityEngine.Rendering.CompareFunction.Equal;
    public int stencilPass = (int)UnityEngine.Rendering.StencilOp.Keep;
    public int stencilFail = (int)UnityEngine.Rendering.StencilOp.Keep;
    public int stencilZFail = (int)UnityEngine.Rendering.StencilOp.Keep;

    [MenuItem("Tools/Stencil Batch Setter")]
    public static void ShowWindow()
    {
        GetWindow<StencilBatchSetter>("Stencil Batch Setter");
    }

    void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);
        EditorGUILayout.PropertyField(so.FindProperty("materials"), true);
        stencilRef = EditorGUILayout.IntField("Stencil Reference", stencilRef);
        stencilComp = EditorGUILayout.IntPopup("Compare Function", stencilComp, System.Enum.GetNames(typeof(UnityEngine.Rendering.CompareFunction)), System.Enum.GetValues(typeof(UnityEngine.Rendering.CompareFunction)) as int[]);
        stencilPass = EditorGUILayout.IntPopup("Pass Operation", stencilPass, System.Enum.GetNames(typeof(UnityEngine.Rendering.StencilOp)), System.Enum.GetValues(typeof(UnityEngine.Rendering.StencilOp)) as int[]);
        stencilFail = EditorGUILayout.IntPopup("Fail Operation", stencilFail, System.Enum.GetNames(typeof(UnityEngine.Rendering.StencilOp)), System.Enum.GetValues(typeof(UnityEngine.Rendering.StencilOp)) as int[]);
        stencilZFail = EditorGUILayout.IntPopup("ZFail Operation", stencilZFail, System.Enum.GetNames(typeof(UnityEngine.Rendering.StencilOp)), System.Enum.GetValues(typeof(UnityEngine.Rendering.StencilOp)) as int[]);

        if (GUILayout.Button("Apply to Materials"))
        {
            foreach (var mat in materials)
            {
                if (mat == null) continue;
                mat.SetInt("_StencilRef", stencilRef);
                mat.SetInt("_StencilComp", stencilComp);
                mat.SetInt("_Stencil", stencilRef); // Some shaders use _Stencil
                mat.SetInt("_StencilOp", stencilPass);
                mat.SetInt("_StencilFail", stencilFail);
                mat.SetInt("_StencilZFail", stencilZFail);
                EditorUtility.SetDirty(mat);
            }
        }

        so.ApplyModifiedProperties();
    }
}
