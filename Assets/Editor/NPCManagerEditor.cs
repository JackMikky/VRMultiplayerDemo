using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NPCManager))]
public class NPCManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("--- Editor Testing Tool ---", EditorStyles.boldLabel);

        NPCManager manager = (NPCManager)target;

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("[Test] Directly apprehending the suspect", GUILayout.Height(40)))
        {
            manager.CatchSuspectInEditor();
        }

        GUI.enabled = true;
    }
}