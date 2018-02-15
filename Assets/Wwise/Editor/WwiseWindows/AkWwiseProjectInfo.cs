#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using UnityEngine;
using UnityEditor;

public static class AkWwiseProjectInfo
{
	public static AkWwiseProjectData m_Data;

	private const string WwiseEditorProjectDataDirectory = "Wwise/Editor/ProjectData";
	private const string AssetsWwiseProjectDataPath = "Assets/" + WwiseEditorProjectDataDirectory + "/AkWwiseProjectData.asset";

	public static AkWwiseProjectData GetData()
	{
		if (m_Data == null && System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Wwise")))
		{
			try
			{
				m_Data = AssetDatabase.LoadAssetAtPath<AkWwiseProjectData>(AssetsWwiseProjectDataPath);

				if (m_Data == null)
				{
					if (!System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, WwiseEditorProjectDataDirectory)))
						System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, WwiseEditorProjectDataDirectory));

					m_Data = ScriptableObject.CreateInstance<AkWwiseProjectData>();
					string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(AssetsWwiseProjectDataPath);
					AssetDatabase.CreateAsset(m_Data, assetPathAndName);
				}
			}
			catch (Exception e)
			{
				Debug.Log("WwiseUnity: Unable to load Wwise Data: " + e.ToString());
			}
		}

		return m_Data;
	}


	public static bool Populate()
	{
		bool bDirty = false;
		if (AkWwisePicker.WwiseProjectFound)
		{
			bDirty = AkWwiseWWUBuilder.Populate();
			bDirty |= AkWwiseXMLBuilder.Populate();
			if (bDirty)
				EditorUtility.SetDirty(AkWwiseProjectInfo.GetData());
		}
		return bDirty;
	}
}
#endif