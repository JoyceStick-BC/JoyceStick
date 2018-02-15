#if UNITY_2017_1_OR_NEWER

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AkRTPCTrack))]
public class AkRTPCTrackInspector : Editor
{
    SerializedProperty Parameter;

    public void OnEnable()
    {
        Parameter = serializedObject.FindProperty("Parameter");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Space(2);

        GUILayout.BeginVertical("Box");
        {
            EditorGUILayout.PropertyField(Parameter, new GUIContent("Parameter: "));
        }

        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}

#endif //UNITY_2017_1_OR_NEWER