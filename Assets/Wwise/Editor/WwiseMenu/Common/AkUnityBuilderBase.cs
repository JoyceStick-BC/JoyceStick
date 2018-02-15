#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System;

public class AkUnityIntegrationBuilderBase 
{
	protected string m_platform = "Undefined";
	protected string m_assetsDir = "Undefined";
	protected string m_assetsPluginsDir = "Undefined";
	protected string m_buildScriptDir = "Undefined";
	protected string m_buildScriptFile = "Undefined";
	protected string m_wwiseSdkDir = "";
	protected string m_shell = "python";
	private string m_progTitle = "WwiseUnity: Rebuilding Unity Integration Progress";

	public AkUnityIntegrationBuilderBase()
	{		
		var unityProjectRoot = System.IO.Directory.GetCurrentDirectory();
		m_assetsDir = System.IO.Path.Combine(unityProjectRoot, "Assets");
		m_assetsPluginsDir = System.IO.Path.Combine(m_assetsDir, "Plugins");
		m_buildScriptDir = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(m_assetsDir, "Wwise"), "AkSoundEngine"), "Common");
		m_buildScriptFile = "BuildWwiseUnityIntegration.py";
	}

	public void BuildByConfig(string config, string arch)
	{
		if (EditorApplication.isPlaying)
		{
			UnityEngine.Debug.LogWarning("WwiseUnity: Editor is in play mode. Stop playing any scenes and retry. Aborted.");
		    return;
		}

		// Try to parse config to get Wwise location.
		string configPath = System.IO.Path.Combine(m_buildScriptDir, "BuildWwiseUnityIntegration.json");
		var fi = new System.IO.FileInfo(configPath);
		if ( fi.Exists )
		{
			string msg = string.Format("WwiseUnity: Found preference file: {0}. Use build variables defined in it.", configPath);
			UnityEngine.Debug.Log(msg);
		}
		else 
		{
			string msg = string.Format("WwiseUnity: Preference file: {0} is unavailable. Need user input.", configPath);
			UnityEngine.Debug.Log(msg);

			m_wwiseSdkDir = EditorUtility.OpenFolderPanel("Choose Wwise SDK folder", ".", "");

			bool isUserCancelledBuild = m_wwiseSdkDir == "";
			if (isUserCancelledBuild)
			{
				UnityEngine.Debug.Log("WwiseUnity: User cancelled the build.");
				return;
			}	
		}

		if ( ! PreBuild() )
		{
			return;
		}
		
		
		// On Windows, separate shell console window will open. When building is done, close the Window yourself if it stays active. Usually at the end you will see the last line says "Build succeeded" or "Build failed".
		string progMsg = string.Format("WwiseUnity: Rebuilding Wwise Unity Integration for {0} ({1}) ...", m_platform, config);
		UnityEngine.Debug.Log(progMsg);

		ProcessStartInfo start = new ProcessStartInfo();
		start.FileName = m_shell;
		
		start.Arguments = GetProcessArgs(config, arch);
		if (start.Arguments == "")
		{
			return;
		}
		start.UseShellExecute = false;
		start.RedirectStandardOutput = true;

		EditorUtility.DisplayProgressBar(m_progTitle, progMsg, 0.5f);

		using(var process = Process.Start(start))
		{
			using(var reader = process.StandardOutput)
			{
		     	process.WaitForExit();

				try
				{
					//ExitCode throws InvalidOperationException if the process is hanging
				
					bool isBuildSucceeded = ( process.ExitCode == 0 );
					if ( isBuildSucceeded )
					{
						EditorUtility.DisplayProgressBar(m_progTitle, progMsg, 1.0f);
						UnityEngine.Debug.Log("WwiseUnity: Build succeeded. Check detailed logs under the Logs folder.");
					}
					else
					{
						UnityEngine.Debug.LogError("WwiseUnity: Build failed. Check detailed logs under the Logs folder.");
					}

					AssetDatabase.Refresh();

					EditorUtility.ClearProgressBar();
				}
				catch (Exception ex)
				{
					AssetDatabase.Refresh();

					UnityEngine.Debug.LogError(string.Format ("WwiseUnity: Build process failed with exception: {}. Check detailed logs under the Logs folder.", ex));
					EditorUtility.ClearProgressBar();
				}
			}
		}
	}

	protected virtual string GetProcessArgs(string config, string arch)
	{
		string scriptPath = System.IO.Path.Combine(m_buildScriptDir, m_buildScriptFile);
		string args = string.Format("\"{0}\" -p {1} -c {2}", scriptPath, m_platform, config);
		if (arch != null)
		{
			args += string.Format(" -a {0}", arch);
		}

		if (m_wwiseSdkDir != "")
		{
			// User user-specified WWISESDK, and update preference.
			args += string.Format(" -w \"{0}\" -u", m_wwiseSdkDir);
		}

		return args;
	}

	protected virtual bool PreBuild()
	{
		return true;
	}
	
}

#endif // #if UNITY_EDITOR