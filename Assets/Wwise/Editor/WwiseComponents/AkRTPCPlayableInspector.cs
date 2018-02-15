#if UNITY_EDITOR

#if UNITY_2017_1_OR_NEWER

//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using System;

[CustomEditor(typeof(AkRTPCPlayable))]
public class AkRTPCPlayableInspector : Editor
{
    AkRTPCPlayable playable;

    SerializedProperty setRTPCGlobally;
    SerializedProperty overrideTrackObject;
    SerializedProperty RTPCObject;
    SerializedProperty Behaviour;

    public void OnEnable()
    {
        playable = target as AkRTPCPlayable;

        setRTPCGlobally = serializedObject.FindProperty("setRTPCGlobally");
        overrideTrackObject = serializedObject.FindProperty("overrideTrackObject");
        RTPCObject = serializedObject.FindProperty("RTPCObject");
        Behaviour = serializedObject.FindProperty("template");

        if (playable != null && playable.OwningClip != null)
        {
            string componentName = GetRTPCName(new Guid(playable.Parameter.valueGuid));
            playable.OwningClip.displayName = componentName;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Space(2);

        GUILayout.BeginVertical("Box");
        {
            if (setRTPCGlobally != null)
            {
                EditorGUILayout.PropertyField(setRTPCGlobally, new GUIContent("Set RTPC Globally: "));
                if (!setRTPCGlobally.boolValue)
                {
                    if (overrideTrackObject != null)
                    {
                        EditorGUILayout.PropertyField(overrideTrackObject, new GUIContent("Override Track Object: "));
                        if (overrideTrackObject.boolValue)
                        {
                            if (RTPCObject != null)
                                EditorGUILayout.PropertyField(RTPCObject, new GUIContent("RTPC Object: "));
                        }
                    }
                }
            }
        }

        GUILayout.EndVertical();
        if (Behaviour != null)
            EditorGUILayout.PropertyField(Behaviour, new GUIContent("Animated Value: "), true);
        if (playable != null && playable.OwningClip != null)
        {
            string componentName = GetRTPCName(new Guid(playable.Parameter.valueGuid));
            playable.OwningClip.displayName = componentName;
        }

        serializedObject.ApplyModifiedProperties();
    }

    public string GetRTPCName(Guid in_guid)
    {
        var list = AkWwiseProjectInfo.GetData().RtpcWwu;

        for (int i = 0; i < list.Count; i++)
        {
            var element = list[i].List.Find(x => new Guid(x.Guid).Equals(in_guid));
            if (element != null)
                return element.Name;
        }

        return string.Empty;
    }
}

#endif //UNITY_2017_1_OR_NEWER

#endif // #if UNITY_EDITOR