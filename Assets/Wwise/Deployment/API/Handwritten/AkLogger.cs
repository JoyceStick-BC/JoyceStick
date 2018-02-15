#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////


public class AkLogger
{
	// @todo sjl: Have SWIG specify the delegate's signature (possibly in AkSoundEngine) so that we can automatically determine the appropriate string marshaling.
	[System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
	public delegate void ErrorLoggerInteropDelegate([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string message);
	ErrorLoggerInteropDelegate errorLoggerDelegate = new ErrorLoggerInteropDelegate(WwiseInternalLogError);

	static AkLogger ms_Instance = new AkLogger();

	private AkLogger()
	{
		if (ms_Instance == null)
		{
			ms_Instance = this;
			AkSoundEngine.SetErrorLogger(errorLoggerDelegate);
		}
	}

	~AkLogger()
	{
		if (ms_Instance == this)
		{
			ms_Instance = null;
			errorLoggerDelegate = null;
			AkSoundEngine.SetErrorLogger();
		}
	}

	public static AkLogger Instance { get { return ms_Instance; } } // used to force instantiation of this singleton

	public void Init()
	{
		// used to force instantiation of this singleton
	}

	[AOT.MonoPInvokeCallback(typeof(ErrorLoggerInteropDelegate))]
	public static void WwiseInternalLogError(string message)
	{
		UnityEngine.Debug.LogError("Wwise: " + message);
	}

	public static void Message(string message)
	{
		UnityEngine.Debug.Log("WwiseUnity: " + message);
	}

	public static void Warning(string message)
	{
		UnityEngine.Debug.LogWarning("WwiseUnity: " + message);
	}

	public static void Error(string message)
	{
		UnityEngine.Debug.LogError("WwiseUnity: " + message);
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.