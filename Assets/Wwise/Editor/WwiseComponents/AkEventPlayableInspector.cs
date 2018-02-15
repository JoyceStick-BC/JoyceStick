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

[CustomEditor(typeof(AkEventPlayable))]
public class AkEventPlayableInspector : Editor
{
    SerializedProperty[] m_guidProperty;
    SerializedProperty[] m_IDProperty;
    AkEventPlayable m_AkEventPlayable;

    SerializedProperty akEvent;
    SerializedProperty overrideTrackEmitterObject;
    SerializedProperty emitterObjectRef;
    SerializedProperty retriggerEvent;

    private Rect m_pickerPos = new Rect();

    public void OnEnable()
    {
        m_AkEventPlayable          = target as AkEventPlayable;
        akEvent                    = serializedObject.FindProperty("akEvent");
        overrideTrackEmitterObject = serializedObject.FindProperty("overrideTrackEmitterObject");
        emitterObjectRef           = serializedObject.FindProperty("emitterObjectRef");
        retriggerEvent             = serializedObject.FindProperty("retriggerEvent");

        m_IDProperty = new SerializedProperty[1];
        m_IDProperty[0] = akEvent.FindPropertyRelative("ID");
        m_guidProperty = new SerializedProperty[1];
        m_guidProperty[0] = akEvent.FindPropertyRelative("valueGuid.Array");

        if (m_IDProperty[0].intValue == AkSoundEngine.AK_INVALID_UNIQUE_ID)
            EditorApplication.delayCall += DelayCreateCall;
    }

    public override void OnInspectorGUI()
    {
        if (m_AkEventPlayable != null && m_AkEventPlayable.OwningClip != null)
            m_AkEventPlayable.OwningClip.displayName = name;
        serializedObject.Update();

        GUILayout.Space(2);

        GUILayout.BeginVertical("Box");
        {
            EditorGUILayout.PropertyField(overrideTrackEmitterObject, new GUIContent("Override Track Object: "));
            if (overrideTrackEmitterObject.boolValue)
            {
                EditorGUILayout.PropertyField(emitterObjectRef, new GUIContent("Emitter Object Ref: "));
            }
            EditorGUILayout.PropertyField(retriggerEvent, new GUIContent("Retrigger Event: "));
            EditorGUILayout.PropertyField(akEvent, new GUIContent("Event: "));
        }

        if (m_AkEventPlayable != null && m_AkEventPlayable.OwningClip != null)
        {
            string componentName = GetEventName(new Guid(m_AkEventPlayable.akEvent.valueGuid));
            m_AkEventPlayable.OwningClip.displayName = componentName;
        }

        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();

        var currentEvent = UnityEngine.Event.current;
        if (currentEvent.type == EventType.Repaint)
        {
            m_pickerPos = AkUtilities.GetLastRectAbsolute(false);
    }
    }

    public string GetEventName(Guid in_guid)
    {
        var list = AkWwiseProjectInfo.GetData().EventWwu;

        for (int i = 0; i < list.Count; i++)
        {
            var element = list[i].List.Find(x => new Guid(x.Guid).Equals(in_guid));
            if (element != null)
                return element.Name;
        }

        return string.Empty;
    }

    protected void DelayCreateCall()
    {
        AkWwiseComponentPicker.Create(AkWwiseProjectData.WwiseObjectType.EVENT, m_guidProperty, m_IDProperty, akEvent.serializedObject, m_pickerPos);
    }
}

#endif //UNITY_2017_1_OR_NEWER

#endif //UNITY_EDITOR

