#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(AkSpatialAudioListener))]
public class AkSpatialAudioListenerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("The current version of Spatial Audio only supports one listener. Make sure to only have one AkSpatialAudioListener active at a time.", MessageType.Info);
    }
}
#endif
