#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using UnityEditor;
using UnityEngine;

public class AkWwiseXMLBuilder
{
	public static bool Populate()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
		{
			return false;
		}

		// Try getting the SoundbanksInfo.xml file for Windows or Mac first, then try to find any other available platform.
		string FullSoundbankPath = AkBasePathGetter.GetPlatformBasePath();
		string filename = System.IO.Path.Combine(FullSoundbankPath, "SoundbanksInfo.xml");

		if (!System.IO.File.Exists(filename))
		{
			FullSoundbankPath = System.IO.Path.Combine(Application.streamingAssetsPath, WwiseSetupWizard.Settings.SoundbankPath);
			var foundFiles = System.IO.Directory.GetFiles(FullSoundbankPath, "SoundbanksInfo.xml", System.IO.SearchOption.AllDirectories);
			if (foundFiles.Length > 0)
			{
				// We just want any file, doesn't matter which one.
				filename = foundFiles[0];
			}
		}

		bool bChanged = false;
		if (System.IO.File.Exists(filename))
		{
			var time = System.IO.File.GetLastWriteTime(filename);
			if (time <= s_LastParsed)
				return false;

			var doc = new System.Xml.XmlDocument();
			doc.Load(filename);

			var soundBanks = doc.GetElementsByTagName("SoundBanks");
			for (int i = 0; i < soundBanks.Count; i++)
			{
				var soundBank = soundBanks[i].SelectNodes("SoundBank");
				for (int j = 0; j < soundBank.Count; j++)
					bChanged = SerialiseSoundBank(soundBank[j]) || bChanged;
			}
		}
		return bChanged;
	}

	static bool SerialiseSoundBank(System.Xml.XmlNode node)
	{
		bool bChanged = false;
		var includedEvents = node.SelectNodes("IncludedEvents");
		for (int i = 0; i < includedEvents.Count; i++)
		{
			var events = includedEvents[i].SelectNodes("Event");
			for (int j = 0; j < events.Count; j++)
				bChanged = SerialiseMaxAttenuation(events[j]) || SerialiseEstimatedDuration(events[j]) || bChanged;
		}
		return bChanged;
	}

	static bool SerialiseMaxAttenuation(System.Xml.XmlNode node)
	{
		bool bChanged = false;
		for (int i = 0; i < AkWwiseProjectInfo.GetData().EventWwu.Count; i++)
		{
			for (int j = 0; j < AkWwiseProjectInfo.GetData().EventWwu[i].List.Count; j++)
			{
				if (node.Attributes["MaxAttenuation"] != null && node.Attributes["Name"].InnerText == AkWwiseProjectInfo.GetData().EventWwu[i].List[j].Name)
				{
					float radius = float.Parse(node.Attributes["MaxAttenuation"].InnerText);
					if (AkWwiseProjectInfo.GetData().EventWwu[i].List[j].maxAttenuation != radius)
					{
						AkWwiseProjectInfo.GetData().EventWwu[i].List[j].maxAttenuation = radius;
						bChanged = true;
					}
					break;
				}
			}
		}
		return bChanged;
	}

	static bool SerialiseEstimatedDuration(System.Xml.XmlNode node)
	{
		bool bChanged = false;
		for (int i = 0; i < AkWwiseProjectInfo.GetData().EventWwu.Count; i++)
		{
			for (int j = 0; j < AkWwiseProjectInfo.GetData().EventWwu[i].List.Count; j++)
			{
				if (node.Attributes["Name"].InnerText == AkWwiseProjectInfo.GetData().EventWwu[i].List[j].Name)
				{
					if (node.Attributes["DurationMin"] != null)
					{
						float minDuration = Mathf.Infinity;
						if (string.Compare(node.Attributes["DurationMin"].InnerText, "Infinite") != 0)
							minDuration = float.Parse(node.Attributes["DurationMin"].InnerText);

						if (AkWwiseProjectInfo.GetData().EventWwu[i].List[j].minDuration != minDuration)
						{
							AkWwiseProjectInfo.GetData().EventWwu[i].List[j].minDuration = minDuration;
							bChanged = true;
						}
					}

					if (node.Attributes["DurationMax"] != null)
					{
						float maxDuration = Mathf.Infinity;
						if (string.Compare(node.Attributes["DurationMax"].InnerText, "Infinite") != 0)
							maxDuration = float.Parse(node.Attributes["DurationMax"].InnerText);

						if (AkWwiseProjectInfo.GetData().EventWwu[i].List[j].maxDuration != maxDuration)
						{
							AkWwiseProjectInfo.GetData().EventWwu[i].List[j].maxDuration = maxDuration;
							bChanged = true;
						}
					}
					break;
				}
			}
		}
		return bChanged;
	}

	static System.DateTime s_LastParsed = System.DateTime.MinValue;
}
#endif