#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
using UnityEngine;
using System.Threading;

#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

public class AkSoundEngineController
{
	#region Public Data Members
	public readonly static string s_DefaultBasePath = System.IO.Path.Combine("Audio", "GeneratedSoundBanks");
	public static string s_Language = "English(US)";
	public static int s_DefaultPoolSize = 4096;
	public static int s_LowerPoolSize = 2048;
	public static int s_StreamingPoolSize = 1024;
	public static int s_PreparePoolSize = 0;
	public static float s_MemoryCutoffThreshold = 0.95f;
	public static int s_MonitorPoolSize = 128;
	public static int s_MonitorQueuePoolSize = 64;
	public static int s_CallbackManagerBufferSize = 4;
	public static bool s_EngineLogging = true;
	public static int s_SpatialAudioPoolSize = 4096;

	public string basePath = s_DefaultBasePath;
	public string language = s_Language;
	public bool engineLogging = s_EngineLogging;
	#endregion

	static AkSoundEngineController ms_Instance = null;

	public static AkSoundEngineController Instance
	{
		get
		{
			if (ms_Instance == null)
				ms_Instance = new AkSoundEngineController();
			return ms_Instance;
		}
	}

	~AkSoundEngineController()
	{
		if (ms_Instance == this)
		{
#if UNITY_EDITOR
#if UNITY_2017_2_OR_NEWER
			EditorApplication.pauseStateChanged -= OnPauseStateChanged;
#else
			EditorApplication.playmodeStateChanged -= OnEditorPlaymodeStateChanged;
#endif
			EditorApplication.update -= LateUpdate;
#endif
			ms_Instance = null;
		}
		// Do nothing. AkTerminator handles sound engine termination.
	}

	public static string GetDecodedBankFolder()
	{
		return "DecodedBanks";
	}

	public static string GetDecodedBankFullPath()
	{
#if (UNITY_ANDROID || UNITY_IOS || UNITY_SWITCH) && !UNITY_EDITOR
		// This is for platforms that only have a specific file location for persistent data.
		return System.IO.Path.Combine(Application.persistentDataPath, GetDecodedBankFolder());
#else
		return System.IO.Path.Combine(AkBasePathGetter.GetPlatformBasePath(), GetDecodedBankFolder());
#endif
	}

	public void LateUpdate()
	{
		//Execute callbacks that occurred in last frame (not the current update)     
		AkCallbackManager.PostCallbacks();
		AkBankManager.DoUnloadBanks();

		if (Application.isPlaying)
			AkAudioListener.DefaultListeners.Refresh();

		AkSoundEngine.RenderAudio();
	}

	public void Init(AkInitializer akInitializer)
	{
#if UNITY_EDITOR
		string[] arguments = Environment.GetCommandLineArgs();
		if (Array.IndexOf(arguments, "-nographics") >= 0 && Array.IndexOf(arguments, "-wwiseEnableWithNoGraphics") < 0)
			return;
#endif

		engineLogging = akInitializer.engineLogging;

		AkLogger.Instance.Init();

		AKRESULT result;
		uint BankID;
		if (AkSoundEngine.IsInitialized())
		{
#if UNITY_EDITOR
			if (Application.isPlaying || BuildPipeline.isBuildingPlayer)
			{
				AkSoundEngine.ClearBanks();
				AkBankManager.Reset();

				result = AkSoundEngine.LoadBank("Init.bnk", AkSoundEngine.AK_DEFAULT_POOL_ID, out BankID);
				if (result != AKRESULT.AK_Success)
					Debug.LogError("WwiseUnity: Failed load Init.bnk with result: " + result.ToString());
			}

			result = AkCallbackManager.Init(akInitializer.callbackManagerBufferSize * 1024);
			if (result != AKRESULT.AK_Success)
			{
				Debug.LogError("WwiseUnity: Failed to initialize Callback Manager. Terminate sound engine.");
				AkSoundEngine.Term();
				return;
			}

			EditorApplication.update += LateUpdate;
#endif
			return;
		}

#if UNITY_EDITOR
		if (BuildPipeline.isBuildingPlayer)
			return;
#endif

		Debug.Log("WwiseUnity: Initialize sound engine ...");
		basePath = akInitializer.basePath;
		language = akInitializer.language;

		//Use default properties for most SoundEngine subsystem.  
		//The game programmer should modify these when needed.  See the Wwise SDK documentation for the initialization.
		//These settings may very well change for each target platform.
		AkMemSettings memSettings = new AkMemSettings();
		memSettings.uMaxNumPools = 20;

		AkDeviceSettings deviceSettings = new AkDeviceSettings();
		AkSoundEngine.GetDefaultDeviceSettings(deviceSettings);

		AkStreamMgrSettings streamingSettings = new AkStreamMgrSettings();
		streamingSettings.uMemorySize = (uint)akInitializer.streamingPoolSize * 1024;

		AkInitSettings initSettings = new AkInitSettings();
		AkSoundEngine.GetDefaultInitSettings(initSettings);
		initSettings.uDefaultPoolSize = (uint)akInitializer.defaultPoolSize * 1024;
		initSettings.uMonitorPoolSize = (uint)akInitializer.monitorPoolSize * 1024;
		initSettings.uMonitorQueuePoolSize = (uint)akInitializer.monitorQueuePoolSize * 1024;
#if (!UNITY_ANDROID && !UNITY_WSA) || UNITY_EDITOR // Exclude WSA. It only needs the name of the DLL, and no path.
		initSettings.szPluginDLLPath = System.IO.Path.Combine(Application.dataPath, "Plugins" + System.IO.Path.DirectorySeparatorChar);
#endif

		AkPlatformInitSettings platformSettings = new AkPlatformInitSettings();
		AkSoundEngine.GetDefaultPlatformInitSettings(platformSettings);
		platformSettings.uLEngineDefaultPoolSize = (uint)akInitializer.lowerPoolSize * 1024;
		platformSettings.fLEngineDefaultPoolRatioThreshold = akInitializer.memoryCutoffThreshold;

		AkMusicSettings musicSettings = new AkMusicSettings();
		AkSoundEngine.GetDefaultMusicSettings(musicSettings);

		AkSpatialAudioInitSettings spatialAudioSettings = new AkSpatialAudioInitSettings();
		spatialAudioSettings.uPoolSize = (uint)akInitializer.spatialAudioPoolSize * 1024;
		spatialAudioSettings.uMaxSoundPropagationDepth = akInitializer.maxSoundPropagationDepth;
		spatialAudioSettings.uDiffractionFlags = (uint)akInitializer.diffractionFlags;

#if UNITY_EDITOR
		AkSoundEngine.SetGameName(Application.productName + " (Editor)");
#else
		AkSoundEngine.SetGameName(Application.productName);
#endif

		result = AkSoundEngine.Init(memSettings, streamingSettings, deviceSettings, initSettings, platformSettings, musicSettings, spatialAudioSettings, (uint)akInitializer.preparePoolSize * 1024);

		if (result != AKRESULT.AK_Success)
		{
			Debug.LogError("WwiseUnity: Failed to initialize the sound engine. Abort.");
			AkSoundEngine.Term();
			return; //AkSoundEngine.Init should have logged more details.
		}

		string basePathToSet = AkBasePathGetter.GetSoundbankBasePath();
		if (string.IsNullOrEmpty(basePathToSet))
		{
			Debug.LogError("WwiseUnity: Couldn't find soundbanks base path. Terminate sound engine.");
			AkSoundEngine.Term();
			return;
		}

		result = AkSoundEngine.SetBasePath(basePathToSet);
		if (result != AKRESULT.AK_Success)
		{
			Debug.LogError("WwiseUnity: Failed to set soundbanks base path. Terminate sound engine.");
			AkSoundEngine.Term();
			return;
		}

#if !UNITY_SWITCH
		// Calling Application.persistentDataPath crashes Switch
		string decodedBankFullPath = GetDecodedBankFullPath();
		// AkSoundEngine.SetDecodedBankPath creates the folders for writing to (if they don't exist)
		AkSoundEngine.SetDecodedBankPath(decodedBankFullPath);
#endif

		AkSoundEngine.SetCurrentLanguage(language);

#if !UNITY_SWITCH
		// Calling Application.persistentDataPath crashes Switch
		// AkSoundEngine.AddBasePath is currently only implemented for iOS and Android; No-op for all other platforms.
		AkSoundEngine.AddBasePath(Application.persistentDataPath + System.IO.Path.DirectorySeparatorChar);
		// Adding decoded bank path last to ensure that it is the first one used when writing decoded banks.
		AkSoundEngine.AddBasePath(decodedBankFullPath);
#endif

		result = AkCallbackManager.Init(akInitializer.callbackManagerBufferSize * 1024);
		if (result != AKRESULT.AK_Success)
		{
			Debug.LogError("WwiseUnity: Failed to initialize Callback Manager. Terminate sound engine.");
			AkSoundEngine.Term();
			return;
		}

		AkBankManager.Reset();

		Debug.Log("WwiseUnity: Sound engine initialized.");

		//Load the init bank right away.  Errors will be logged automatically.
		result = AkSoundEngine.LoadBank("Init.bnk", AkSoundEngine.AK_DEFAULT_POOL_ID, out BankID);
		if (result != AKRESULT.AK_Success)
		{
			Debug.LogError("WwiseUnity: Failed load Init.bnk with result: " + result.ToString());
		}

#if UNITY_EDITOR
#if UNITY_2017_2_OR_NEWER
        EditorApplication.pauseStateChanged += OnPauseStateChanged;
#else
		EditorApplication.playmodeStateChanged += OnEditorPlaymodeStateChanged;
#endif
		EditorApplication.update += LateUpdate;
#endif
	}

	public void Terminate()
	{
		if (!AkSoundEngine.IsInitialized())
			return; //Don't term twice

		// Stop everything, and make sure the callback buffer is empty. We try emptying as much as possible, and wait 10 ms before retrying.
		// Callbacks can take a long time to be posted after the call to RenderAudio().
		AkSoundEngine.StopAll();
		AkSoundEngine.ClearBanks();
		AkSoundEngine.RenderAudio();
		int retry = 5;
		do
		{
			int numCB = 0;
			do
			{
				numCB = AkCallbackManager.PostCallbacks();

				// This is a WSA-friendly sleep
				using (EventWaitHandle tmpEvent = new ManualResetEvent(false))
				{
					tmpEvent.WaitOne(System.TimeSpan.FromMilliseconds(1));
				}
			}
			while (numCB > 0);

			// This is a WSA-friendly sleep
			using (EventWaitHandle tmpEvent = new ManualResetEvent(false))
			{
				tmpEvent.WaitOne(System.TimeSpan.FromMilliseconds(10));
			}
			retry--;
		}
		while (retry > 0);

		AkSoundEngine.Term();

		// Make sure we have no callbacks left after Term. Some might be posted during termination.
		AkCallbackManager.PostCallbacks();

		AkCallbackManager.Term();
		AkBankManager.Reset();
	}

#if UNITY_EDITOR

	// Enable/Disable the audio when pressing play/pause in the editor.
#if UNITY_2017_2_OR_NEWER
    private static void OnPauseStateChanged(PauseState pauseState)
    {
        ActivateAudio(pauseState != PauseState.Paused);
    }
#else
	private static void OnEditorPlaymodeStateChanged()
	{
		ActivateAudio(!EditorApplication.isPaused);
	}
#endif

#elif !UNITY_IOS
    //Keep out of UNITY_EDITOR because the sound needs to keep playing when switching windows (remote debugging in Wwise, for example).
	//On iOS, application interruptions are handled in the sound engine already.
	void OnApplicationPause(bool pauseStatus) 
	{
		ActivateAudio(!pauseStatus);
	}

	void OnApplicationFocus(bool focus)
	{
		ActivateAudio(focus);
	}
#endif

	private static void ActivateAudio(bool activate)
	{
		if (AkSoundEngine.IsInitialized())
		{
			if (activate)
				AkSoundEngine.WakeupFromSuspend();
			else
				AkSoundEngine.Suspend();

			AkSoundEngine.RenderAudio();
		}
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.