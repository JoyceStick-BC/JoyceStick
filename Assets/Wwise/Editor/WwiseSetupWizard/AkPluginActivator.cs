#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AkPluginActivator
{
	static AkPluginActivator()
	{
		ActivatePluginsForEditor();
	}

	public const string ALL_PLATFORMS = "All";
	public const string CONFIG_DEBUG = "Debug";
	public const string CONFIG_PROFILE = "Profile";
	public const string CONFIG_RELEASE = "Release";

	private const string EditorConfiguration = CONFIG_PROFILE;

	private const string MENU_PATH = "Assets/Wwise/Activate Plugins/";
    private const BuildTarget INVALID_BUILD_TARGET = (BuildTarget)(-1);

	// Use reflection because projects that were created in Unity 4 won't have the CurrentPluginConfig field
	public static string GetCurrentConfig()
	{
		FieldInfo CurrentPluginConfigField = typeof(AkWwiseProjectData).GetField("CurrentPluginConfig");
		string CurrentConfig = string.Empty;
		AkWwiseProjectData data = AkWwiseProjectInfo.GetData();

		if (CurrentPluginConfigField != null && data != null)
			CurrentConfig = (string)CurrentPluginConfigField.GetValue(data);

		if (string.IsNullOrEmpty(CurrentConfig))
			CurrentConfig = CONFIG_PROFILE;

		return CurrentConfig;
	}

	static void SetCurrentConfig(string config)
	{
		FieldInfo CurrentPluginConfigField = typeof(AkWwiseProjectData).GetField("CurrentPluginConfig");
		AkWwiseProjectData data = AkWwiseProjectInfo.GetData();
		if (CurrentPluginConfigField != null)
			CurrentPluginConfigField.SetValue(data, config);

		EditorUtility.SetDirty(AkWwiseProjectInfo.GetData());
	}

	private static void ActivateConfig(string config)
	{
		SetCurrentConfig(config);
		CheckMenuItems(config);
	}

	[UnityEditor.MenuItem(MENU_PATH + CONFIG_DEBUG)]
	public static void ActivateDebug()
	{
		ActivateConfig(CONFIG_DEBUG);
	}

	[UnityEditor.MenuItem(MENU_PATH + CONFIG_PROFILE)]
	public static void ActivateProfile()
	{
		ActivateConfig(CONFIG_PROFILE);
	}

	[UnityEditor.MenuItem(MENU_PATH + CONFIG_RELEASE)]
	public static void ActivateRelease()
	{
		ActivateConfig(CONFIG_RELEASE);
	}

	private const string WwisePluginFolder = "Wwise/Deployment/Plugins";

	class StaticPluginRegistration
	{
		private bool Active = false;

		private HashSet<string> FactoriesHeaderFilenames = new HashSet<string>();

		private BuildTarget Target;

		public StaticPluginRegistration(BuildTarget target)
		{
			Target = target;
		}

		public void TryAddLibrary(string AssetPath)
		{
			Active = true;

			if (AssetPath.Contains(".a"))
			{
				//Extract the lib name, generate the registration code.
				int begin = AssetPath.LastIndexOf('/') + 4;
				int end = AssetPath.LastIndexOf('.') - begin;
				string LibName = AssetPath.Substring(begin, end);    //Remove the lib prefix and the .a extension                    

				if (!LibName.Contains("AkSoundEngine"))
					FactoriesHeaderFilenames.Add(LibName + "Factory.h");
			}
			else if (AssetPath.Contains("Factory.h"))
			{
				FactoriesHeaderFilenames.Add(System.IO.Path.GetFileName(AssetPath));
			}
		}

		public void TryWriteToFile()
		{
			if (!Active)
				return;

			string RelativePath;
			string CppText;

			if (Target == BuildTarget.iOS)
			{
				RelativePath = "/iOS/DSP/AkiOSPlugins.cpp";
				CppText = "#define AK_IOS";
			}
			else if (Target == BuildTarget.tvOS)
			{
				RelativePath = "/tvOS/DSP/AktvOSPlugins.cpp";
				CppText = "#define AK_IOS";
			}
			else if (Target == SwitchBuildTarget)
			{
				RelativePath = "/Switch/NX64/DSP/AkSwitchPlugins.cpp";
				CppText = "#define AK_NX";
			}
			else
				return;

			CppText += @"
namespace AK { class PluginRegistration; };
#define AK_STATIC_LINK_PLUGIN(_pluginName_) \
extern AK::PluginRegistration _pluginName_##Registration; \
void *_pluginName_##_fp = (void*)&_pluginName_##Registration;

";

			foreach (var filename in FactoriesHeaderFilenames)
				CppText += "#include \"" + filename + "\"\n";

			try
			{
				string FullPath = System.IO.Path.Combine(Application.dataPath, WwisePluginFolder + RelativePath);
				System.IO.File.WriteAllText(FullPath, CppText);
				FactoriesHeaderFilenames.Clear();
			}
			catch (Exception e)
			{
				Debug.LogError("Wwise: Could not write <" + RelativePath + ">. Exception: " + e.Message);
			}
		}
	}

	private static BuildTarget GetPlatformBuildTarget(string platform)
	{
		var targets = Enum.GetNames(typeof(BuildTarget));
		var values = Enum.GetValues(typeof(BuildTarget));

		for (int ii = 0; ii < targets.Length; ++ii)
			if (platform.Equals(targets[ii]))
				return (BuildTarget)values.GetValue(ii);

		return INVALID_BUILD_TARGET;
	}

	// The following check is required until "BuildTarget.Switch" is available on all versions of Unity that we support.
	private static BuildTarget SwitchBuildTarget = GetPlatformBuildTarget("Switch");

	// returns the name of the folder that contains plugins for a specific target
	private static string GetPluginDeploymentPlatformName(BuildTarget target)
	{
		switch (target)
		{
			case BuildTarget.Android:
				return "Android";

			case BuildTarget.iOS:
				return "iOS";

			case BuildTarget.tvOS:
				return "tvOS";

			case BuildTarget.StandaloneLinux:
			case BuildTarget.StandaloneLinux64:
			case BuildTarget.StandaloneLinuxUniversal:
				return "Linux";

#if UNITY_2017_3_OR_NEWER
            case BuildTarget.StandaloneOSX:
#else
            case BuildTarget.StandaloneOSXIntel:
			case BuildTarget.StandaloneOSXIntel64:
			case BuildTarget.StandaloneOSXUniversal:
#endif
                return "Mac";

			case BuildTarget.PS4:
				return "PS4";

			case BuildTarget.PSP2:
				return "Vita";

			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return "Windows";

			case BuildTarget.WSAPlayer:
				return "WSA";

			case BuildTarget.XboxOne:
				return "XboxOne";

#if UNITY_5_6_OR_NEWER
			case BuildTarget.Switch:
				return "Switch";
#endif
		}

		return target.ToString();
	}

	public static void ActivatePluginsForDeployment(BuildTarget target, bool Activate)
	{
		bool ChangedSomeAssets = false;

		StaticPluginRegistration staticPluginRegistration = (target == BuildTarget.iOS || target == BuildTarget.tvOS || target == SwitchBuildTarget) ? new StaticPluginRegistration(target) : null;

		//PluginImporter[] importers = PluginImporter.GetImporters(target);
		PluginImporter[] importers = PluginImporter.GetAllImporters();

		foreach (PluginImporter pluginImporter in importers)
		{
			if (!pluginImporter.assetPath.Contains(WwisePluginFolder))
				continue;

			string[] splitPath = pluginImporter.assetPath.Split('/');

			// Path is Assets/Wwise/Deployment/Plugins/Platform. We need the platform string
			string pluginPlatform = splitPath[4];
			if (pluginPlatform != GetPluginDeploymentPlatformName(target))
				continue;

			string pluginArch = string.Empty;
			string pluginConfig = string.Empty;

			switch (pluginPlatform)
			{
				case "iOS":
				case "tvOS":
				case "PS4":
				case "XboxOne":
					pluginConfig = splitPath[5];
					break;

				case "Android":
					pluginArch = splitPath[5];
					pluginConfig = splitPath[6];

					if (pluginArch == "armeabi-v7a")
						pluginImporter.SetPlatformData(BuildTarget.Android, "CPU", "ARMv7");
					else if (pluginArch == "x86")
						pluginImporter.SetPlatformData(BuildTarget.Android, "CPU", "x86");
					else
						Debug.Log("WwiseUnity: Architecture not found: " + pluginArch);
					break;

				case "Linux":
					pluginArch = splitPath[5];
					pluginConfig = splitPath[6];

					if (pluginArch == "x86")
					{
						pluginImporter.SetPlatformData(BuildTarget.StandaloneLinux, "CPU", "x86");
						pluginImporter.SetPlatformData(BuildTarget.StandaloneLinux64, "CPU", "None");
						pluginImporter.SetPlatformData(BuildTarget.StandaloneLinuxUniversal, "CPU", "x86");
					}
					else if (pluginArch == "x86_64")
					{
						pluginImporter.SetPlatformData(BuildTarget.StandaloneLinux, "CPU", "None");
						pluginImporter.SetPlatformData(BuildTarget.StandaloneLinux64, "CPU", "x86_64");
						pluginImporter.SetPlatformData(BuildTarget.StandaloneLinuxUniversal, "CPU", "x86_64");
					}
					else
					{
						Debug.Log("WwiseUnity: Architecture not found: " + pluginArch);
						continue;
					}

#if UNITY_2017_3_OR_NEWER
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "None");
#else
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXIntel, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXIntel64, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXUniversal, "CPU", "None");
#endif
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneWindows, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "None");
					break;

				case "Mac":
					pluginConfig = splitPath[5];
					pluginImporter.SetPlatformData(BuildTarget.StandaloneLinux, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneLinux64, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneLinuxUniversal, "CPU", "None");

#if UNITY_2017_3_OR_NEWER
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "AnyCPU");
#else
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXIntel, "CPU", "AnyCPU");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXIntel64, "CPU", "AnyCPU");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXUniversal, "CPU", "AnyCPU");
#endif
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneWindows, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "None");
					break;

				case "WSA":
					pluginArch = splitPath[5];
					pluginConfig = splitPath[6];

					if (pluginArch == "WSA_UWP_Win32")
					{
						pluginImporter.SetPlatformData(BuildTarget.WSAPlayer, "SDK", "AnySDK");
						pluginImporter.SetPlatformData(BuildTarget.WSAPlayer, "CPU", "X86");
					}
					else if (pluginArch == "WSA_UWP_x64")
					{
						pluginImporter.SetPlatformData(BuildTarget.WSAPlayer, "SDK", "AnySDK");
						pluginImporter.SetPlatformData(BuildTarget.WSAPlayer, "CPU", "X64");
					}
					else if (pluginArch == "WSA_UWP_ARM")
					{
						pluginImporter.SetPlatformData(BuildTarget.WSAPlayer, "SDK", "AnySDK");
						pluginImporter.SetPlatformData(BuildTarget.WSAPlayer, "CPU", "ARM");
					}
					break;

				case "Windows":
					pluginArch = splitPath[5];
					pluginConfig = splitPath[6];

					if (pluginArch == "x86")
					{
						pluginImporter.SetPlatformData(BuildTarget.StandaloneWindows, "CPU", "AnyCPU");
						pluginImporter.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "None");
					}
					else if (pluginArch == "x86_64")
					{
						pluginImporter.SetPlatformData(BuildTarget.StandaloneWindows, "CPU", "None");
						pluginImporter.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", "AnyCPU");
					}
					else
					{
						Debug.Log("WwiseUnity: Architecture not found: " + pluginArch);
						continue;
					}

					pluginImporter.SetPlatformData(BuildTarget.StandaloneLinux, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneLinux64, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneLinuxUniversal, "CPU", "None");
#if UNITY_2017_3_OR_NEWER
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "None");
#else
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXIntel, "CPU", "None");
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXIntel64, "CPU", "None");
					pluginImporter.SetPlatformData(BuildTarget.StandaloneOSXUniversal, "CPU", "None");
#endif
                    break;

				case "Switch":
					pluginArch = splitPath[5];
					pluginConfig = splitPath[6];

					if (SwitchBuildTarget == INVALID_BUILD_TARGET)
						continue;

					if (pluginArch != "NX32" && pluginArch != "NX64")
					{
						Debug.Log("WwiseUnity: Architecture not found: " + pluginArch);
						continue;
					}
					break;

				default:
					Debug.Log("WwiseUnity: Unknown platform: " + pluginPlatform);
					continue;
			}

			bool AssetChanged = false;
			if (pluginImporter.GetCompatibleWithAnyPlatform())
			{
				pluginImporter.SetCompatibleWithAnyPlatform(false);
				AssetChanged = true;
			}
	
			bool bActivate = true;
			if (pluginConfig == "DSP")
			{
				if (!IsPluginUsed(pluginPlatform, System.IO.Path.GetFileNameWithoutExtension(pluginImporter.assetPath)))
					bActivate = false;
				else if (staticPluginRegistration != null)
					staticPluginRegistration.TryAddLibrary(pluginImporter.assetPath);
			}
			else if (pluginConfig != GetCurrentConfig())
				bActivate = false;

			if (!bActivate && target == BuildTarget.WSAPlayer)
			{
				// Workaround for TLS ALLOCATOR ALLOC_TEMP_THREAD ERROR: We need to explicitly deactivate the plugins for WSA
				// even if they are already reported as deactivated. If we don't, we get the ALLOC_TEMP_THREAD errors for
				// some reason...
				AssetChanged = true;
			}
			else
				AssetChanged |= (pluginImporter.GetCompatibleWithPlatform(target) == !bActivate || !Activate);

			pluginImporter.SetCompatibleWithPlatform(target, bActivate && Activate);

			if (AssetChanged)
			{
				ChangedSomeAssets = true;
				AssetDatabase.ImportAsset(pluginImporter.assetPath);
			}
		}

		if (ChangedSomeAssets && staticPluginRegistration != null)
			staticPluginRegistration.TryWriteToFile();
	}

	public static void ActivatePluginsForEditor()
	{
		PluginImporter[] importers = PluginImporter.GetAllImporters();
		bool ChangedSomeAssets = false;

		foreach (PluginImporter pluginImporter in importers)
		{
			if (!pluginImporter.assetPath.Contains(WwisePluginFolder))
				continue;

			string[] splitPath = pluginImporter.assetPath.Split('/');

			// Path is Assets/Wwise/Deployment/Plugins/Platform. We need the platform string
			string pluginPlatform = splitPath[4];
			string pluginConfig = string.Empty;
			string editorCPU = string.Empty;
			string editorOS = string.Empty;

			switch (pluginPlatform)
			{
				case "Mac":
					pluginConfig = splitPath[5];
					editorCPU = "AnyCPU";
					editorOS = "OSX";
					break;

				case "Windows":
					editorCPU = splitPath[5];
					pluginConfig = splitPath[6];
					editorOS = "Windows";
					break;

				default:
					break;
			}

			bool AssetChanged = false;
			if (pluginImporter.GetCompatibleWithAnyPlatform())
			{
				pluginImporter.SetCompatibleWithAnyPlatform(false);
				AssetChanged = true;
			}

			bool bActivate = false;
			if (!string.IsNullOrEmpty(editorOS))
			{
				if (pluginConfig == "DSP")
				{
					if (!s_PerPlatformPlugins.ContainsKey(pluginPlatform))
						continue;

					bActivate = IsPluginUsed(pluginPlatform, System.IO.Path.GetFileNameWithoutExtension(pluginImporter.assetPath));
				}
				else
					bActivate = pluginConfig == EditorConfiguration;

				if (bActivate)
				{
					pluginImporter.SetEditorData("CPU", editorCPU);
					pluginImporter.SetEditorData("OS", editorOS);
				}
			}

			AssetChanged |= (pluginImporter.GetCompatibleWithEditor() != bActivate);
			pluginImporter.SetCompatibleWithEditor(bActivate);

			if (AssetChanged)
			{
				ChangedSomeAssets = true;
				AssetDatabase.ImportAsset(pluginImporter.assetPath);
			}
		}

		if (ChangedSomeAssets)
			Debug.Log("WwiseUnity: Plugins successfully activated for " + EditorConfiguration + " in Editor.");
	}

	static void CheckMenuItems(string config)
	{
		/// Set checkmark on menu item
		UnityEditor.Menu.SetChecked(MENU_PATH + CONFIG_DEBUG, config == CONFIG_DEBUG);
		UnityEditor.Menu.SetChecked(MENU_PATH + CONFIG_PROFILE, config == CONFIG_PROFILE);
		UnityEditor.Menu.SetChecked(MENU_PATH + CONFIG_RELEASE, config == CONFIG_RELEASE);
	}

	public static void DeactivateAllPlugins()
	{
		PluginImporter[] importers = PluginImporter.GetAllImporters();

		foreach (PluginImporter pluginImporter in importers)
		{
			if (!pluginImporter.assetPath.Contains(WwisePluginFolder))
				continue;

			pluginImporter.SetCompatibleWithAnyPlatform(false);
			AssetDatabase.ImportAsset(pluginImporter.assetPath);
		}
	}

	static Dictionary<string, DateTime> s_LastParsed = new Dictionary<string, DateTime>();
	static Dictionary<string, HashSet<string>> s_PerPlatformPlugins = new Dictionary<string, HashSet<string>>();

	static public bool IsPluginUsed(string in_UnityPlatform, string in_PluginName)
	{
		// For WSA, we use the plugin info for Windows, since they share banks. Same for tvOS vs iOS.
		string pluginDSPPlatform = in_UnityPlatform;
		switch (pluginDSPPlatform) 
		{
		case "WSA":
			pluginDSPPlatform = "Windows";
			break;
		case "tvOS":
			pluginDSPPlatform = "iOS";
			break;
		}

		if (!s_PerPlatformPlugins.ContainsKey(pluginDSPPlatform))
			return false;   //XML not parsed, don't touch anything.

		if (in_PluginName.Contains("AkSoundEngine"))
			return true;

		string pluginName = in_PluginName;
		if (in_PluginName.StartsWith("lib"))
		{
			//That's a unix-y type of plugin, just remove the prefix to find our name.
			pluginName = in_PluginName.Substring(3);
		}

		HashSet<string> plugins;
		if (s_PerPlatformPlugins.TryGetValue(pluginDSPPlatform, out plugins))
		{
			if (in_UnityPlatform != "iOS" && in_UnityPlatform != "tvOS" && in_UnityPlatform != "Switch")
				return plugins.Contains(pluginName);

			//iOS, tvOS, and Switch deal with the static libs directly, unlike all other platforms.
			//Luckily the DLL name is always a subset of the lib name.
			foreach (string pl in plugins)
			{
				if (!string.IsNullOrEmpty(pl) && pluginName.Contains(pl))
					return true;

				if (String.Compare(pl, "iZotope", false) == 0 && pluginName.StartsWith("iZ"))
					return true;
			}

			//Exceptions

			if (in_UnityPlatform == "iOS" && pluginName.Contains("AkiOSPlugins"))
				return true;

			if (in_UnityPlatform == "tvOS" && pluginName.Contains("AktvOSPlugins"))
				return true;

			if (in_UnityPlatform == "Switch" && pluginName.Contains("AkSwitchPlugins"))
				return true;

			if (plugins.Contains("AkSoundSeedAir") && (pluginName.Contains("SoundSeedWind") || pluginName.Contains("SoundSeedWoosh")))
				return true;
		}

		return false;
	}

	public static void Update(bool forceUpdate = false)
	{
		//Gather all GeneratedSoundBanks folder from the project
		IDictionary<string, string> allPaths = AkUtilities.GetAllBankPaths();
		bool bNeedRefresh = false;
		string projectPath = System.IO.Path.GetDirectoryName(AkUtilities.GetFullPath(Application.dataPath, WwiseSettings.LoadSettings().WwiseProjectPath));

		var pfMap = AkUtilities.GetPlatformMapping();

		//Go through all BasePlatforms 
		foreach (var pairPF in pfMap)
		{
			//Go through all custom platforms related to that base platform and check if any of the bank files were updated.
			bool bParse = forceUpdate;
			List<string> fullPaths = new List<string>();
			foreach (string customPF in pairPF.Value)
			{
				string bankPath;
				if (!allPaths.TryGetValue(customPF, out bankPath))
					continue;

				string pluginFile = "";
				try
				{
					pluginFile = System.IO.Path.Combine(projectPath, System.IO.Path.Combine(bankPath, "PluginInfo.xml"));
					pluginFile = pluginFile.Replace('/', System.IO.Path.DirectorySeparatorChar);
					if (!System.IO.File.Exists(pluginFile))
					{
						//Try in StreamingAssets too.
						pluginFile = System.IO.Path.Combine(System.IO.Path.Combine(AkBasePathGetter.GetFullSoundBankPath(), customPF), "PluginInfo.xml");
						if (!System.IO.File.Exists(pluginFile))
							continue;
					}
					fullPaths.Add(pluginFile);

					var t = System.IO.File.GetLastWriteTime(pluginFile);
					var lastTime = DateTime.MinValue;
					s_LastParsed.TryGetValue(customPF, out lastTime);
					if (lastTime < t)
					{
						bParse = true;
						s_LastParsed[customPF] = t;
					}
				}
				catch (Exception ex)
				{
					Debug.LogError("Wwise: " + pluginFile + " could not be parsed. " + ex.Message);
				}
			}

			if (bParse)
			{
				string platform = pairPF.Key;

				HashSet<string> newDlls = ParsePluginsXML(platform, fullPaths);
				HashSet<string> oldDlls = null;

				//Remap base Wwise platforms to Unity platform folders names
				if (platform.Contains("Vita"))
					platform = "Vita";
				//else other platforms already have the right name

				s_PerPlatformPlugins.TryGetValue(platform, out oldDlls);
				s_PerPlatformPlugins[platform] = newDlls;

				//Check if there was any change.
				if (!bNeedRefresh && oldDlls != null)
				{
					if (oldDlls.Count == newDlls.Count)
						oldDlls.IntersectWith(newDlls);

					bNeedRefresh |= oldDlls.Count != newDlls.Count;
				}
				else
					bNeedRefresh |= newDlls.Count > 0;
			}
		}

		if (bNeedRefresh)
			ActivatePluginsForEditor();

		string currentConfig = GetCurrentConfig();
		CheckMenuItems(currentConfig);
	}

	enum PluginID
	{
		SineGenerator = 0x00640002, //Sine
		WwiseSilence = 0x00650002, //Wwise Silence
		ToneGenerator = 0x00660002, //Tone Generator
		WwiseParametricEQ = 0x00690003, //Wwise Parametric EQ
		Delay = 0x006A0003, //Delay
		WwiseCompressor = 0x006C0003, //Wwise Compressor
		WwiseExpander = 0x006D0003, //Wwise Expander
		WwisePeakLimiter = 0x006E0003, //Wwise Peak Limiter
		MatrixReverb = 0x00730003, //Matrix Reverb
		WwiseRoomVerb = 0x00760003, //Wwise RoomVerb
		WwiseMeter = 0x00810003, //Wwise Meter
		Gain = 0x008B0003, //Gain
		VitaReverb = 0x008C0003, //Vita Reverb
		VitaCompressor = 0x008D0003, //Vita Compressor
		VitaDelay = 0x008E0003, //Vita Delay
		VitaDistortion = 0x008F0003, //Vita Distortion
		VitaEQ = 0x00900003 //Vita EQ
	}

	static HashSet<PluginID> builtInPluginIDs = new HashSet<PluginID>
	{
		PluginID.SineGenerator,
		PluginID.WwiseSilence,
		PluginID.ToneGenerator,
		PluginID.WwiseParametricEQ,
		PluginID.Delay,
		PluginID.WwiseCompressor,
		PluginID.WwiseExpander,
		PluginID.WwisePeakLimiter,
		PluginID.MatrixReverb,
		PluginID.WwiseRoomVerb,
		PluginID.WwiseMeter,
		PluginID.Gain,
		PluginID.VitaReverb,
		PluginID.VitaCompressor,
		PluginID.VitaDelay,
		PluginID.VitaDistortion,
		PluginID.VitaEQ,
	};

	private static HashSet<string> ParsePluginsXML(string platform, List<string> in_pluginFiles)
	{
		var newDlls = new HashSet<string>();

		foreach (string pluginFile in in_pluginFiles)
		{
			if (!System.IO.File.Exists(pluginFile))
				continue;

			try
			{
				var doc = new XmlDocument();
				doc.Load(pluginFile);
				var Navigator = doc.CreateNavigator();
				var pluginInfoNode = Navigator.SelectSingleNode("//PluginInfo");
				string boolMotion = pluginInfoNode.GetAttribute("Motion", "");

				var it = Navigator.Select("//Plugin");

				if (boolMotion == "true")
					newDlls.Add("AkMotion");

				foreach (XPathNavigator node in it)
				{
					uint pid = UInt32.Parse(node.GetAttribute("ID", ""));
					if (pid == 0)
						continue;

					string dll = string.Empty;

					if (platform == "Switch")
					{
						if ((PluginID)pid == PluginID.WwiseMeter)
							dll = "AkMeter";
					}
					else if (builtInPluginIDs.Contains((PluginID)pid))
						continue;

					if (string.IsNullOrEmpty(dll))
						dll = node.GetAttribute("DLL", "");

					newDlls.Add(dll);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Wwise: " + pluginFile + " could not be parsed. " + ex.Message);
			}
		}

		return newDlls;
	}
}
#endif