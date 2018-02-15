#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

static class AkWwiseIDConverter
{
	private static string s_bankDir = Application.dataPath;
	private static string s_converterScript = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(Application.dataPath, "Wwise"), "Tools"), "WwiseIDConverter.py");
	private static string s_progTitle = "WwiseUnity: Converting SoundBank IDs";

	[UnityEditor.MenuItem("Assets/Wwise/Convert Wwise SoundBank IDs", false, (int)AkWwiseMenuOrder.ConvertIDs)]
	public static void ConvertWwiseSoundBankIDs()
	{
		string bankIdHeaderPath = EditorUtility.OpenFilePanel("Choose Wwise SoundBank ID C++ header", s_bankDir, "h");
		if (string.IsNullOrEmpty(bankIdHeaderPath))
		{
			UnityEngine.Debug.Log("WwiseUnity: User canceled the action.");
			return;
		}

		var start = new System.Diagnostics.ProcessStartInfo();
		start.FileName = "python";
		start.Arguments = string.Format("\"{0}\" \"{1}\"", s_converterScript, bankIdHeaderPath);
		start.UseShellExecute = false;
		start.RedirectStandardOutput = true;

		string progMsg = "WwiseUnity: Converting C++ SoundBank IDs into C# ...";
		EditorUtility.DisplayProgressBar(s_progTitle, progMsg, 0.5f);

		using (var process = System.Diagnostics.Process.Start(start))
		{
			process.WaitForExit();
			try
			{
				//ExitCode throws InvalidOperationException if the process is hanging
				if (process.ExitCode == 0)
				{
					EditorUtility.DisplayProgressBar(s_progTitle, progMsg, 1.0f);
					UnityEngine.Debug.Log(string.Format("WwiseUnity: SoundBank ID conversion succeeded. Find generated Unity script under {0}.", s_bankDir));
				}
				else
				{
					UnityEngine.Debug.LogError("WwiseUnity: Conversion failed.");
				}

				AssetDatabase.Refresh();
			}
			catch (System.Exception ex)
			{
				AssetDatabase.Refresh();

				EditorUtility.ClearProgressBar();
				UnityEngine.Debug.LogError(string.Format("WwiseUnity: SoundBank ID conversion process failed with exception: {}. Check detailed logs under the folder: Assets/Wwise/Logs.", ex));
			}

			EditorUtility.ClearProgressBar();
		}
	}
}
#endif // #if UNITY_EDITOR
