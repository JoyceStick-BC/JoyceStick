#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(AkEvent))]
public class AkEventInspector : AkBaseInspector
{
	public class AkEditorEventPlayer
	{
		private static AkEditorEventPlayer ms_Instance = null;

		public static AkEditorEventPlayer Instance
		{
			get
			{
				if (ms_Instance == null)
					ms_Instance = new AkEditorEventPlayer();
				return ms_Instance;
			}
		}

		private List<AkEvent> akEvents = new List<AkEvent>();

		private void CallbackHandler(object in_cookie, AkCallbackType in_type, object in_info)
		{
			if (in_type == AkCallbackType.AK_EndOfEvent)
				RemoveAkEvent(in_cookie as AkEvent);
		}

		public void PlayEvent(AkEvent akEvent)
		{
			if (IsEventPlaying(akEvent))
				return;

			uint playingID = AkSoundEngine.PostEvent((uint)akEvent.eventID, akEvent.gameObject, (uint)AkCallbackType.AK_EndOfEvent, CallbackHandler, akEvent);
			if (playingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
				AddAkEvent(akEvent);
		}

		public void StopEvent(AkEvent akEvent)
		{
			if (!IsEventPlaying(akEvent))
				return;

			AKRESULT result = AkSoundEngine.ExecuteActionOnEvent((uint)akEvent.eventID, AkActionOnEventType.AkActionOnEventType_Stop, akEvent.gameObject, 0);
			if (result == AKRESULT.AK_Success)
				RemoveAkEvent(akEvent);
			else
				Debug.LogWarning("WwiseUnity: AkEditorEventPlayer: Failed to stop event: " + akEvent.name + "(id: " + akEvent.eventID + ")!");
		}

		void AddAkEvent(AkEvent akEvent)
		{
			akEvents.Add(akEvent);

			// In the case where objects are being placed in edit mode and then previewed, their positions won't yet be updated so we ensure they're updated here.
			AkSoundEngine.SetObjectPosition(akEvent.gameObject, akEvent.transform);
		}

		void RemoveAkEvent(AkEvent akEvent)
		{
			if (akEvent != null)
				akEvents.Remove(akEvent);
		}

		public bool IsEventPlaying(AkEvent akEvent)
		{
			return akEvents.Contains(akEvent);
		}

		public void StopAll()
		{
			AkSoundEngine.StopAll();
		}
	}

	SerializedProperty eventID;
	SerializedProperty enableActionOnEvent;
	SerializedProperty actionOnEventType;
	SerializedProperty curveInterpolation;
	SerializedProperty transitionDuration;
	SerializedProperty callbackData;

	AkUnityEventHandlerInspector m_UnityEventHandlerInspector = new AkUnityEventHandlerInspector();

    private GameObject emitterObject;

	public void OnEnable()
	{
		m_UnityEventHandlerInspector.Init(serializedObject);
		
		eventID				= serializedObject.FindProperty("eventID");
		enableActionOnEvent	= serializedObject.FindProperty("enableActionOnEvent");
		actionOnEventType	= serializedObject.FindProperty("actionOnEventType");
		curveInterpolation	= serializedObject.FindProperty("curveInterpolation");
		transitionDuration	= serializedObject.FindProperty("transitionDuration");

		callbackData = serializedObject.FindProperty("m_callbackData");

		m_guidProperty = new SerializedProperty[1];
		m_guidProperty[0]	= serializedObject.FindProperty("valueGuid.Array");
		
		//Needed by the base class to know which type of component its working with
		m_typeName		= "Event";
		m_objectType	= AkWwiseProjectData.WwiseObjectType.EVENT;
    }
 
    public override void OnChildInspectorGUI()
	{	
		serializedObject.Update();

		m_UnityEventHandlerInspector.OnGUI();

		GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

		GUILayout.BeginVertical("Box");
		{
			EditorGUILayout.PropertyField(enableActionOnEvent, new GUIContent("Action On Event: "));

			if(enableActionOnEvent.boolValue)
			{
				EditorGUILayout.PropertyField(actionOnEventType, new GUIContent("Action On EventType: "));
				EditorGUILayout.PropertyField(curveInterpolation, new GUIContent("Curve Interpolation: "));
				EditorGUILayout.Slider(transitionDuration, 0.0f, 60.0f, new GUIContent("Fade Time (secs): "));
			}
		}
		GUILayout.EndVertical();

		GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

		GUILayout.BeginVertical("Box");
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(callbackData);
			if (EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
		}
		GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();

        GUILayout.BeginVertical("Box");
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            float inspectorWidth = Screen.width - GUI.skin.box.margin.left - GUI.skin.box.margin.right;

            if (targets.Length == 1)
            {
                AkEvent akEvent = (AkEvent)target;
                bool eventPlaying = AkEditorEventPlayer.Instance.IsEventPlaying(akEvent);
                if (eventPlaying)
                {
                    if (GUILayout.Button("Stop", style, GUILayout.MaxWidth(inspectorWidth)))
                    {
                        GUIUtility.hotControl = 0;
                        AkEditorEventPlayer.Instance.StopEvent(akEvent);
                    }
                }
                else
                {
                    if (GUILayout.Button("Play", style, GUILayout.MaxWidth(inspectorWidth)))
                    {
                        GUIUtility.hotControl = 0;
                        AkEditorEventPlayer.Instance.PlayEvent(akEvent);

                    }
                }
            }
            else
            {
                bool playingEventsSelected = false;
                bool stoppedEventsSelected = false;
                for (int i = 0; i < targets.Length; ++i)
                {
                    AkEvent akEventTarget = targets[i] as AkEvent;
                    if (akEventTarget != null)
                    {
                        if (AkEditorEventPlayer.Instance.IsEventPlaying(akEventTarget))
                            playingEventsSelected = true;
                        else
                            stoppedEventsSelected = true;
                        if (playingEventsSelected && stoppedEventsSelected)
                            break;
                    }

                }

                if (stoppedEventsSelected && GUILayout.Button("Play Multiple", style, GUILayout.MaxWidth(inspectorWidth)))
                {
                    for (int i = 0; i < targets.Length; ++i)
                    {
                        AkEvent akEventTarget = targets[i] as AkEvent;
                        if (akEventTarget != null)
                        {
                            AkEditorEventPlayer.Instance.PlayEvent(akEventTarget);
                        }
                    }
                }
                if (playingEventsSelected && GUILayout.Button("Stop Multiple", style, GUILayout.MaxWidth(inspectorWidth)))
                {
                    for (int i = 0; i < targets.Length; ++i)
                    {
                        AkEvent akEventTarget = targets[i] as AkEvent;
                        if (akEventTarget != null)
                        {
                            AkEditorEventPlayer.Instance.StopEvent(akEventTarget);
                        }
                    }
                }
            }

            if (GUILayout.Button("Stop All", style, GUILayout.MaxWidth(inspectorWidth)))
            {
                GUIUtility.hotControl = 0;
                AkEditorEventPlayer.Instance.StopAll();
            }
        }

        GUILayout.EndVertical();
    }

	public override string UpdateIds(Guid[] in_guid)
	{
		for(int i = 0; i < AkWwiseProjectInfo.GetData().EventWwu.Count; i++)
		{
			AkWwiseProjectData.Event e = AkWwiseProjectInfo.GetData().EventWwu[i].List.Find(x => new Guid(x.Guid).Equals(in_guid[0]));
			
			if(e != null)
			{
				eventID.intValue = e.ID;
				serializedObject.ApplyModifiedProperties();

				return e.Name;
			}
		}

		return string.Empty;
	}
}
#endif
