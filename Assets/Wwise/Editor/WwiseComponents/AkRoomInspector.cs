#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(AkRoom))]
public class AkRoomInspector : Editor
{
    AkRoom m_AkRoom;

    SerializedProperty reverbAuxBus;
    SerializedProperty reverbLevel;
    SerializedProperty wallOcclusion;
    SerializedProperty priority;

    void OnEnable()
    {
        m_AkRoom = target as AkRoom;

        reverbAuxBus = serializedObject.FindProperty("reverbAuxBus");
        reverbLevel = serializedObject.FindProperty("reverbLevel");
        wallOcclusion = serializedObject.FindProperty("wallOcclusion");
        priority = serializedObject.FindProperty("priority");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.BeginVertical("Box");
        {
            EditorGUILayout.PropertyField(reverbAuxBus);
            EditorGUILayout.PropertyField(reverbLevel);
            EditorGUILayout.PropertyField(wallOcclusion);
            EditorGUILayout.PropertyField(priority);
        }
        GUILayout.EndVertical();

        AkGameObjectInspector.RigidbodyCheck(m_AkRoom.gameObject);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif