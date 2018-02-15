#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

// The inspector for AkInitializer is overriden to trap changes to initialization parameters and persist them across scenes.
[CustomEditor(typeof(AkInitializer))]
public class AkInitializerInspector : Editor
{
	AkInitializer m_AkInit;

	//This data is a copy of the AkInitializer parameters.  
	//We need it to reapply the same values to copies of the object in different scenes
	SerializedProperty m_basePath;
	SerializedProperty m_language;
	SerializedProperty m_defaultPoolSize;
	SerializedProperty m_lowerPoolSize;
    SerializedProperty m_streamingPoolSize;
	SerializedProperty m_preparePoolSize;
	SerializedProperty m_memoryCutoffThreshold;
    SerializedProperty m_monitorPoolSize;
    SerializedProperty m_monitorQueuePoolSize;
	SerializedProperty m_callbackManagerBufferSize;
	SerializedProperty m_engineLogging;
    SerializedProperty m_spatialAudioPoolSize;
    SerializedProperty m_maxSoundPropagationDepth;
    SerializedProperty m_diffractionFlags;

    void OnEnable()
	{
		m_AkInit = target as AkInitializer;

		m_basePath = serializedObject.FindProperty("basePath");
		m_language = serializedObject.FindProperty("language");
		m_defaultPoolSize = serializedObject.FindProperty("defaultPoolSize");
		m_lowerPoolSize = serializedObject.FindProperty("lowerPoolSize");
        m_streamingPoolSize = serializedObject.FindProperty("streamingPoolSize");
		m_preparePoolSize = serializedObject.FindProperty("preparePoolSize");
		m_memoryCutoffThreshold = serializedObject.FindProperty("memoryCutoffThreshold");
        m_monitorPoolSize = serializedObject.FindProperty("monitorPoolSize");
        m_monitorQueuePoolSize = serializedObject.FindProperty("monitorQueuePoolSize");
		m_callbackManagerBufferSize = serializedObject.FindProperty("callbackManagerBufferSize");
		m_engineLogging = serializedObject.FindProperty("engineLogging");
        m_spatialAudioPoolSize = serializedObject.FindProperty("spatialAudioPoolSize");
        m_maxSoundPropagationDepth = serializedObject.FindProperty("maxSoundPropagationDepth");
        m_diffractionFlags = serializedObject.FindProperty("diffractionFlags");
    }

    public override void OnInspectorGUI()
	{
		serializedObject.Update();

		GUILayout.BeginVertical();
		EditorGUILayout.PropertyField(m_basePath, new GUIContent("Base Path"));
		EditorGUILayout.PropertyField(m_language, new GUIContent("Language"));
		EditorGUILayout.PropertyField(m_defaultPoolSize, new GUIContent("Default Pool Size (KB)"));
		EditorGUILayout.PropertyField(m_lowerPoolSize, new GUIContent("Lower Pool Size (KB)"));
        EditorGUILayout.PropertyField(m_streamingPoolSize, new GUIContent("Streaming Pool Size (KB)"));
		EditorGUILayout.PropertyField(m_preparePoolSize, new GUIContent("Prepare Pool Size (KB)"));
		EditorGUILayout.PropertyField(m_memoryCutoffThreshold, new GUIContent("Memory Cutoff Threshold"));
        EditorGUILayout.PropertyField(m_monitorPoolSize, new GUIContent("Monitor Pool Size (KB)"));
        EditorGUILayout.PropertyField(m_monitorQueuePoolSize, new GUIContent("Monitor Queue Pool Size (KB)"));
        EditorGUILayout.PropertyField(m_callbackManagerBufferSize, new GUIContent("CallbackManager Buffer Size (KB)"));
		EditorGUILayout.PropertyField(m_engineLogging, new GUIContent("Enable Wwise engine logging"));
		GUILayout.EndVertical();

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Spatial Audio Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_spatialAudioPoolSize, new GUIContent("Spatial Audio Pool Size (KB)"));
        EditorGUILayout.PropertyField(m_maxSoundPropagationDepth, new GUIContent("Max Sound Propagation Depth"));

        int displayMask = GetDisplayMask(m_diffractionFlags.intValue);
        displayMask = EditorGUILayout.MaskField(new GUIContent("Diffraction Flags"), displayMask, SupportedFlags);
        m_diffractionFlags.intValue = GetWwiseMask(displayMask);
        
        serializedObject.ApplyModifiedProperties();
		if (GUI.changed)
		{
			AkWwiseProjectInfo.GetData().SaveInitSettings(m_AkInit);
		}
	}

    #region Diffraction Flags
    static string[] m_supportedFlags = null;
    static int[] m_supportedValues = null;

    private static void SetupSupportedValuesAndFlags()
    {
        int[] types = (int[])Enum.GetValues(typeof(AkDiffractionFlags));
        int[] unsupportedValues =
        {
            (int)AkDiffractionFlags.DefaultDiffractionFlags
        };

        m_supportedFlags = new string[types.Length - unsupportedValues.Length];
        m_supportedValues = new int[types.Length - unsupportedValues.Length];

        int index = 0;
        for (int i = 0; i < types.Length; i++)
        {
            if (!Contains(unsupportedValues, types[i]))
            {
                m_supportedFlags[index] = Enum.GetName(typeof(AkDiffractionFlags), types[i]).Substring(17);
                m_supportedValues[index] = types[i];
                index++;
            }
        }
    }

    private static bool Contains(int[] in_array, int in_value)
    {
        for (int i = 0; i < in_array.Length; i++)
            if (in_array[i] == in_value)
                return true;

        return false;
    }

    public static string[] SupportedFlags
    {
        get
        {
            if (m_supportedFlags == null)
                SetupSupportedValuesAndFlags();

            return m_supportedFlags;
        }
    }

    public static int GetDisplayMask(int in_wwiseMask)
    {
        if (m_supportedValues == null)
            SetupSupportedValuesAndFlags();

        int displayMask = 0;
        for (int i = 0; i < m_supportedValues.Length; i++)
            if ((m_supportedValues[i] & in_wwiseMask) != 0)
                displayMask |= (1 << i);

        return displayMask;
    }

    public static int GetWwiseMask(int in_displayMask)
    {
        if (m_supportedValues == null)
            SetupSupportedValuesAndFlags();

        int wwiseMask = 0;
        for (int i = 0; i < m_supportedValues.Length; i++)
            if ((in_displayMask & (1 << i)) != 0)
                wwiseMask |= m_supportedValues[i];

        return wwiseMask;
    }
    #endregion Diffraction Flags
}
#endif
