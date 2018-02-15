#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
﻿using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class represents an example audio input manager and is responsible for managing the audio sample and format callbacks provided to the Wwise Audio Input plug-in.
/// </summary>
public static class AkAudioInputManager
{
	/// <summary>
	/// Audio sample delegate that is sent to C++.
	/// </summary>
	[System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
	public delegate bool AudioSamplesInteropDelegate(uint playingID, [System.Runtime.InteropServices.In,
		System.Runtime.InteropServices.Out,
		System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPArray, SizeParamIndex = 3)] float[] samples, uint channelIndex, uint frames);

	/// <summary>
	/// Audio format delegate that is sent to C++.
	/// </summary>
	[System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
	public delegate void AudioFormatInteropDelegate(uint playingID, IntPtr format);

	/// <summary>
	/// Sanitized audio sample delegate to be used by classes that implement audio input plug-ins. For every event posted, this delegate is called once per audio frame for each channel until the delegates for all the channels associated with this event return false.
	/// </summary>
	/// <param name="playingID">The playingID of a sound that uses the audio input plug-in.</param>
	/// <param name="channelIndex">The number of the channel associated with this specific invocation of the delegate.</param>
	/// <param name="samples">The sample array that MUST be filled even when returning false.</param>
	/// <returns>Return true when more sample frames are require and false when complete.</returns>
	public delegate bool AudioSamplesDelegate(uint playingID, uint channelIndex, float[] samples);

	/// <summary>
	/// Sanitized audio format delegate to be used by classes that implement audio input plug-ins. The samples are ALWAYS set to be non-interleaved 32-bit float.
	/// </summary>
	/// <param name="playingID">The playingID of a sound that uses the audio input plug-in.</param>
	/// <param name="format">The C# analog of the C++ AkAudioFormat class.</param>
	public delegate void AudioFormatDelegate(uint playingID, AkAudioFormat format);

	/// <summary>
	/// This method is used to post events that use the Wwise Audio Input plug-in.
	/// </summary>
	/// <param name="akEvent">The event to post.</param>
	/// <param name="gameObject">The GameObject that the event will be posted on.</param>
	/// <param name="sampleDelegate">The C# audio sample delegate.</param>
	/// <param name="formatDelegate">The C# audio format delegate. If not specified, defaults to a mono source running at the sample rate of the sound engine.</param>
	/// <returns>The playingID of the newly instantiated sound associated with the posted event.</returns>
	static public uint PostAudioInputEvent(AK.Wwise.Event akEvent, GameObject gameObject, AudioSamplesDelegate sampleDelegate, AudioFormatDelegate formatDelegate = null)
	{
		TryInitialize();
		uint playingID = akEvent.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, EventCallback);
		AddPlayingID(playingID, sampleDelegate, formatDelegate);
		return playingID;
	}

	/// <summary>
	/// This method is used to post events that use the Wwise Audio Input plug-in.
	/// </summary>
	/// <param name="akEventID">The ID of the event to post.</param>
	/// <param name="gameObject">The GameObject that the event will be posted on.</param>
	/// <param name="sampleDelegate">The C# audio sample delegate.</param>
	/// <param name="formatDelegate">The C# audio format delegate. If not specified, defaults to a mono source running at the sample rate of the sound engine.</param>
	/// <returns>The playingID of the newly instantiated sound associated with the posted event.</returns>
	static public uint PostAudioInputEvent(uint akEventID, GameObject gameObject, AudioSamplesDelegate sampleDelegate, AudioFormatDelegate formatDelegate = null)
	{
		TryInitialize();
		uint playingID = AkSoundEngine.PostEvent(akEventID, gameObject, (uint)AkCallbackType.AK_EndOfEvent, EventCallback, null);
		AddPlayingID(playingID, sampleDelegate, formatDelegate);
		return playingID;
	}

	/// <summary>
	/// This method is used to post events that use the Wwise Audio Input plug-in.
	/// </summary>
	/// <param name="akEventName">The name of the event to post.</param>
	/// <param name="gameObject">The GameObject that the event will be posted on.</param>
	/// <param name="sampleDelegate">The C# audio sample delegate.</param>
	/// <param name="formatDelegate">The C# audio format delegate. If not specified, defaults to a mono source running at the sample rate of the sound engine.</param>
	/// <returns>The playingID of the newly instantiated sound associated with the posted event.</returns>
	static public uint PostAudioInputEvent(string akEventName, GameObject gameObject, AudioSamplesDelegate sampleDelegate, AudioFormatDelegate formatDelegate = null)
	{
		TryInitialize();
		uint playingID = AkSoundEngine.PostEvent(akEventName, gameObject, (uint)AkCallbackType.AK_EndOfEvent, EventCallback, null);
		AddPlayingID(playingID, sampleDelegate, formatDelegate);
		return playingID;
	}

	static bool initialized = false;
	static Dictionary<uint, AudioSamplesDelegate> audioSamplesDelegates = new Dictionary<uint, AudioSamplesDelegate>();
	static Dictionary<uint, AudioFormatDelegate> audioFormatDelegates = new Dictionary<uint, AudioFormatDelegate>();
	static AkAudioFormat audioFormat = new AkAudioFormat();
	static AudioSamplesInteropDelegate audioSamplesDelegate = new AudioSamplesInteropDelegate(InternalAudioSamplesDelegate);
	static AudioFormatInteropDelegate audioFormatDelegate = new AudioFormatInteropDelegate(InternalAudioFormatDelegate);

	[AOT.MonoPInvokeCallback(typeof(AudioSamplesInteropDelegate))]
	static bool InternalAudioSamplesDelegate(uint playingID, float[] samples, uint channelIndex, uint frames)
	{
		return audioSamplesDelegates.ContainsKey(playingID) && audioSamplesDelegates[playingID](playingID, channelIndex, samples);
	}

	[AOT.MonoPInvokeCallback(typeof(AudioFormatInteropDelegate))]
	static void InternalAudioFormatDelegate(uint playingID, IntPtr format)
	{
		if (audioFormatDelegates.ContainsKey(playingID))
		{
			audioFormat.setCPtr(format);
			audioFormatDelegates[playingID](playingID, audioFormat);
		}
	}

	static void TryInitialize()
	{
		if (!initialized)
		{
			initialized = true;
			AkSoundEngine.SetAudioInputCallbacks(audioSamplesDelegate, audioFormatDelegate);
		}
	}

	static void AddPlayingID(uint playingID, AudioSamplesDelegate sampleDelegate, AudioFormatDelegate formatDelegate)
	{
		if (playingID == AkSoundEngine.AK_INVALID_PLAYING_ID || sampleDelegate == null)
			return;

		audioSamplesDelegates.Add(playingID, sampleDelegate);
		if (formatDelegate != null)
			audioFormatDelegates.Add(playingID, formatDelegate);
	}

	static void EventCallback(object cookie, AkCallbackType type, AkCallbackInfo callbackInfo)
	{
		if (type == AkCallbackType.AK_EndOfEvent)
		{
			var info = callbackInfo as AkEventCallbackInfo;
			if (info != null)
			{
				audioSamplesDelegates.Remove(info.playingID);
				audioFormatDelegates.Remove(info.playingID);
			}
		}
	}
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.