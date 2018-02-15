#if UNITY_EDITOR && UNITY_5_6_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

public partial class AkBuildPreprocessor : IPreprocessBuild, IPostprocessBuild
{
	/// <summary>
	/// User hook called to retrieve the custom platform name used to determine the base path. Do not modify platformName to use default platform names.
	/// </summary>
	/// <param name="platformName">The custom platform name.</param>
	static partial void GetCustomPlatformName(ref string platformName, BuildTarget target);

	private static string GetPlatformName(BuildTarget target)
	{
		string platformSubDir = string.Empty;
		GetCustomPlatformName(ref platformSubDir, target);
		if (!string.IsNullOrEmpty(platformSubDir))
			return platformSubDir;

		switch (target)
		{
			case BuildTarget.Android:
				return "Android";

			case BuildTarget.iOS:
			case BuildTarget.tvOS:
				return "iOS";

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
			case BuildTarget.WSAPlayer:
				return "Windows";

			case BuildTarget.XboxOne:
				return "XboxOne";

			case BuildTarget.Switch:
				return "Switch";
		}

		return target.ToString();
	}

	public int callbackOrder { get { return 0; } }
	string destinationSoundBankFolder = string.Empty;

	private static bool SetDestinationPath(string platformName, ref string destinationFolder)
	{
		destinationFolder = System.IO.Path.Combine(AkBasePathGetter.GetFullSoundBankPath(), platformName);
		return !string.IsNullOrEmpty(destinationFolder);
	}

	public static bool CopySoundbanks(bool generate, string platformName, ref string destinationFolder)
	{
		if (string.IsNullOrEmpty(platformName))
		{
			Debug.LogError("WwiseUnity: Could not determine platform name for <" + platformName + "> platform");
		}
		else
		{
			if (generate)
			{
				List<string> platforms = new List<string> { platformName };
				AkUtilities.GenerateSoundbanks(platforms);
			}

			string sourceFolder = AkBasePathGetter.GetPlatformBasePathEditor(platformName);
			if (string.IsNullOrEmpty(sourceFolder))
			{
				Debug.LogError("WwiseUnity: Could not find source folder for <" + platformName + "> platform. Did you remember to generate your banks?");
			}
			else if (!SetDestinationPath(platformName, ref destinationFolder))
			{
				Debug.LogError("WwiseUnity: Could not find destination folder for <" + platformName + "> platform");
			}
			else if (!AkUtilities.DirectoryCopy(sourceFolder, destinationFolder, true))
			{
				destinationFolder = null;
				Debug.LogError("WwiseUnity: Could not copy soundbank folder for <" + platformName + "> platform");
			}
			else
			{
				Debug.Log("WwiseUnity: Copied soundbank folder to streaming assets folder <" + destinationFolder + "> for <" + platformName + "> platform build");
				return true;
			}
		}

		return false;
	}

	public static void DeleteSoundbanks(string destinationFolder)
	{
		if (!string.IsNullOrEmpty(destinationFolder))
		{
			System.IO.Directory.Delete(destinationFolder, true);
			Debug.Log("WwiseUnity: Deleting streaming assets folder <" + destinationFolder + ">");
		}
	}


#if UNITY_2018_1_OR_NEWER
    public void OnPreprocessBuild(BuildReport report)
    {
        BuildTarget target = report.summary.platform;
        string path = report.summary.outputPath;
#else
    public void OnPreprocessBuild(BuildTarget target, string path)
	{
#endif
		if (WwiseSetupWizard.Settings.CopySoundBanksAsPreBuildStep)
		{
			string platformName = GetPlatformName(target);
			if (!CopySoundbanks(WwiseSetupWizard.Settings.GenerateSoundBanksAsPreBuildStep, platformName, ref destinationSoundBankFolder))
			{
				Debug.LogError("WwiseUnity: Soundbank folder has not been copied for <" + target + "> target at <" + path + ">. This will likely result in a build without sound!!!");
			}
		}

		// @todo sjl - only update for target platform
		AkPluginActivator.Update(true);
		AkPluginActivator.ActivatePluginsForDeployment(target, true);
	}

#if UNITY_2018_1_OR_NEWER
    public void OnPostprocessBuild(BuildReport report)
    {
        BuildTarget target = report.summary.platform;
#else
    public void OnPostprocessBuild(BuildTarget target, string path)
	{
#endif
		AkPluginActivator.ActivatePluginsForDeployment(target, false);
		DeleteSoundbanks(destinationSoundBankFolder);
		destinationSoundBankFolder = string.Empty;
	}

}
#endif // #if UNITY_EDITOR && UNITY_5_6_OR_NEWER
