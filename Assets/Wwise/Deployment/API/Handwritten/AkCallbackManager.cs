#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// This class manages the callback queue.  All callbacks from the native Wwise SDK go through this queue.  
/// The queue needs to be driven by regular calls to PostCallbacks().  This is currently done in AkInitializer.cs, in LateUpdate().
/// </summary>
static public class AkCallbackManager
{
	public delegate void EventCallback(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info);
	public delegate void MonitoringCallback(AkMonitorErrorCode in_errorCode, AkMonitorErrorLevel in_errorLevel, uint in_playingID, ulong in_gameObjID, string in_msg);
	public delegate void BankCallback(uint in_bankID, IntPtr in_InMemoryBankPtr, AKRESULT in_eLoadResult, uint in_memPoolId, object in_Cookie);

	static AkEventCallbackInfo AkEventCallbackInfo = new AkEventCallbackInfo(IntPtr.Zero, false);
	static AkDynamicSequenceItemCallbackInfo AkDynamicSequenceItemCallbackInfo = new AkDynamicSequenceItemCallbackInfo(IntPtr.Zero, false);
	static AkMIDIEventCallbackInfo AkMIDIEventCallbackInfo = new AkMIDIEventCallbackInfo(IntPtr.Zero, false);
	static AkMarkerCallbackInfo AkMarkerCallbackInfo = new AkMarkerCallbackInfo(IntPtr.Zero, false);
	static AkDurationCallbackInfo AkDurationCallbackInfo = new AkDurationCallbackInfo(IntPtr.Zero, false);
	static AkMusicSyncCallbackInfo AkMusicSyncCallbackInfo = new AkMusicSyncCallbackInfo(IntPtr.Zero, false);
	static AkMusicPlaylistCallbackInfo AkMusicPlaylistCallbackInfo = new AkMusicPlaylistCallbackInfo(IntPtr.Zero, false);

#if UNITY_IOS && !UNITY_EDITOR
	static AkAudioInterruptionCallbackInfo AkAudioInterruptionCallbackInfo = new AkAudioInterruptionCallbackInfo(IntPtr.Zero, false);
#endif // #if UNITY_IOS && ! UNITY_EDITOR
	static AkAudioSourceChangeCallbackInfo AkAudioSourceChangeCallbackInfo = new AkAudioSourceChangeCallbackInfo(IntPtr.Zero, false);
	static AkMonitoringCallbackInfo AkMonitoringCallbackInfo = new AkMonitoringCallbackInfo(IntPtr.Zero, false);
	static AkBankCallbackInfo AkBankCallbackInfo = new AkBankCallbackInfo(IntPtr.Zero, false);

	public class EventCallbackPackage
	{
		public static EventCallbackPackage Create(EventCallback in_cb, object in_cookie, ref uint io_Flags)
		{
			if (io_Flags == 0 || in_cb == null)
			{
				io_Flags = 0;
				return null;
			}

			EventCallbackPackage evt = new EventCallbackPackage();

			evt.m_Callback = in_cb;
			evt.m_Cookie = in_cookie;
			evt.m_bNotifyEndOfEvent = (io_Flags & (uint)AkCallbackType.AK_EndOfEvent) != 0;
			io_Flags = io_Flags | (uint)AkCallbackType.AK_EndOfEvent;

			m_mapEventCallbacks[evt.GetHashCode()] = evt;
			m_LastAddedEventPackage = evt;

			return evt;
		}

        ~EventCallbackPackage()
        {
            if (m_Cookie != null)
            {
                AkCallbackManager.RemoveEventCallbackCookie(m_Cookie);
            }
        }

		public object m_Cookie;
		public EventCallback m_Callback;
		public bool m_bNotifyEndOfEvent;
		public uint m_playingID = 0;
	};

	public class BankCallbackPackage
	{
		public BankCallbackPackage(BankCallback in_cb, object in_cookie)
		{
			m_Callback = in_cb;
			m_Cookie = in_cookie;

			m_mapBankCallbacks[GetHashCode()] = this;
		}
		public object m_Cookie;
		public BankCallback m_Callback;
	};

	static Dictionary<int, EventCallbackPackage> m_mapEventCallbacks = new Dictionary<int, EventCallbackPackage>();
	static Dictionary<int, BankCallbackPackage> m_mapBankCallbacks = new Dictionary<int, BankCallbackPackage>();

	static EventCallbackPackage m_LastAddedEventPackage = null;

	static public void RemoveEventCallback(uint in_playingID)
	{
		List<int> cookiesToRemove = new List<int>();
		foreach (KeyValuePair<int, EventCallbackPackage> pair in m_mapEventCallbacks)
		{
			if (pair.Value.m_playingID == in_playingID)
			{
				cookiesToRemove.Add(pair.Key);
				break;
			}
		}

		int Count = cookiesToRemove.Count;
		for (int ii = 0; ii < Count; ++ii)
		{
			m_mapEventCallbacks.Remove(cookiesToRemove[ii]);
		}

		AkSoundEnginePINVOKE.CSharp_CancelEventCallback(in_playingID);
	}

	static public void RemoveEventCallbackCookie(object in_cookie)
	{
		List<int> cookiesToRemove = new List<int>();
		foreach (KeyValuePair<int, EventCallbackPackage> pair in m_mapEventCallbacks)
		{
			if (pair.Value.m_Cookie == in_cookie)
			{
				cookiesToRemove.Add(pair.Key);
			}
		}

		int Count = cookiesToRemove.Count;
		for (int ii = 0; ii < Count; ++ii)
		{
			int toRemove = cookiesToRemove[ii];
			m_mapEventCallbacks.Remove(toRemove);
			AkSoundEnginePINVOKE.CSharp_CancelEventCallbackCookie((IntPtr)toRemove);
		}
	}

	static public void RemoveBankCallback(object in_cookie)
	{
		List<int> cookiesToRemove = new List<int>();
		foreach (KeyValuePair<int, BankCallbackPackage> pair in m_mapBankCallbacks)
		{
			if (pair.Value.m_Cookie == in_cookie)
			{
				cookiesToRemove.Add(pair.Key);
			}
		}

		int Count = cookiesToRemove.Count;
		for (int ii = 0; ii < Count; ++ii)
		{
			int toRemove = cookiesToRemove[ii];
			m_mapBankCallbacks.Remove(toRemove);
			AkSoundEnginePINVOKE.CSharp_CancelBankCallbackCookie((IntPtr)toRemove);
		}
	}

	static public void SetLastAddedPlayingID(uint in_playingID)
	{
		if (m_LastAddedEventPackage != null)
		{
			if (m_LastAddedEventPackage.m_playingID == 0)
			{
				m_LastAddedEventPackage.m_playingID = in_playingID;
			}
		}
	}

	static IntPtr m_pNotifMem = IntPtr.Zero;
	static private MonitoringCallback m_MonitoringCB;

#if UNITY_IOS && !UNITY_EDITOR
	public delegate AKRESULT AudioInterruptionCallback(bool in_bEnterInterruption, object in_Cookie);
	// App implements its own callback.
	private static AudioInterruptionCallbackPackage ms_interruptCallbackPkg = null;

	public class AudioInterruptionCallbackPackage
	{
		public AudioInterruptionCallbackPackage(AudioInterruptionCallback in_cb, object in_cookie)
		{
			m_Callback = in_cb;
			m_Cookie = in_cookie;
		}
		public object m_Cookie;
		public AudioInterruptionCallback m_Callback;
	};
#endif // #if UNITY_IOS && ! UNITY_EDITOR

	public delegate AKRESULT BGMCallback(bool in_bOtherAudioPlaying, object in_Cookie);
	// App implements its own callback.
	private static BGMCallbackPackage ms_sourceChangeCallbackPkg = null;

	public class BGMCallbackPackage
	{
		public BGMCallbackPackage(BGMCallback in_cb, object in_cookie)
		{
			m_Callback = in_cb;
			m_Cookie = in_cookie;
		}
		public object m_Cookie;
		public BGMCallback m_Callback;
	};

	static public AKRESULT Init(int BufferSize)
	{
		m_pNotifMem = (BufferSize > 0) ? Marshal.AllocHGlobal(BufferSize) : IntPtr.Zero;

#if UNITY_EDITOR
		AkCallbackSerializer.SetLocalOutput((uint)AkMonitorErrorLevel.ErrorLevel_All);
#endif

		return AkCallbackSerializer.Init(m_pNotifMem, (uint)BufferSize);
	}

	static public void Term()
	{
		if (m_pNotifMem != IntPtr.Zero)
		{
			AkCallbackSerializer.Term();
			Marshal.FreeHGlobal(m_pNotifMem);
			m_pNotifMem = IntPtr.Zero;
		}
	}

	/// Call this to set a function to call whenever Wwise prints a message (warnings or errors).
	static public void SetMonitoringCallback(AkMonitorErrorLevel in_Level, MonitoringCallback in_CB)
	{
		AkCallbackSerializer.SetLocalOutput(in_CB != null ? (uint)in_Level : 0);
		m_MonitoringCB = in_CB;
	}

#if UNITY_IOS && !UNITY_EDITOR
    /// Call this to set a iOS callback interruption function.
    /// By default this callback is not defined.
    static public void SetInterruptionCallback(AudioInterruptionCallback in_CB, object in_cookie)
    {
        ms_interruptCallbackPkg = new AudioInterruptionCallbackPackage(in_CB, in_cookie);
    }
#endif // #if UNITY_IOS && ! UNITY_EDITOR

	/// Call this to set a iOS callback interruption function.
	/// By default this callback is not defined.
	static public void SetBGMCallback(BGMCallback in_CB, object in_cookie)
	{
		ms_sourceChangeCallbackPkg = new BGMCallbackPackage(in_CB, in_cookie);
	}

	/// This function dispatches all the accumulated callbacks from the native sound engine. 
	/// It must be called regularly.  By default this is called in AkInitializer.cs.
	static public int PostCallbacks()
	{
		if (m_pNotifMem == IntPtr.Zero)
			return 0;

		try
		{
			int numCallbacks = 0;

			for (IntPtr pNext = AkCallbackSerializer.Lock(); pNext != IntPtr.Zero; pNext = AkSoundEnginePINVOKE.CSharp_AkSerializedCallbackHeader_pNext_get(pNext), ++numCallbacks)
			{
				IntPtr pPackage = AkSoundEnginePINVOKE.CSharp_AkSerializedCallbackHeader_pPackage_get(pNext);
				AkCallbackType eType = (AkCallbackType)AkSoundEnginePINVOKE.CSharp_AkSerializedCallbackHeader_eType_get(pNext);
				IntPtr pData = AkSoundEnginePINVOKE.CSharp_AkSerializedCallbackHeader_GetData(pNext);

				switch (eType)
				{
					case AkCallbackType.AK_AudioInterruption:
#if UNITY_IOS && !UNITY_EDITOR
						if (ms_interruptCallbackPkg != null && ms_interruptCallbackPkg.m_Callback != null)
						{
							AkAudioInterruptionCallbackInfo.setCPtr(pData);
							ms_interruptCallbackPkg.m_Callback(AkAudioInterruptionCallbackInfo.bEnterInterruption, ms_interruptCallbackPkg.m_Cookie);
						}
#endif // #if UNITY_IOS && ! UNITY_EDITOR
						break;

					case AkCallbackType.AK_AudioSourceChange:
						if (ms_sourceChangeCallbackPkg != null && ms_sourceChangeCallbackPkg.m_Callback != null)
						{
							AkAudioSourceChangeCallbackInfo.setCPtr(pData);
							ms_sourceChangeCallbackPkg.m_Callback(AkAudioSourceChangeCallbackInfo.bOtherAudioPlaying, ms_sourceChangeCallbackPkg.m_Cookie);
						}
						break;

					case AkCallbackType.AK_Monitoring:
						if (m_MonitoringCB != null)
						{
							AkMonitoringCallbackInfo.setCPtr(pData);
							m_MonitoringCB(AkMonitoringCallbackInfo.errorCode, AkMonitoringCallbackInfo.errorLevel, AkMonitoringCallbackInfo.playingID,
								AkMonitoringCallbackInfo.gameObjID, AkMonitoringCallbackInfo.message);
						}
#if UNITY_EDITOR
						else if (AkSoundEngineController.Instance.engineLogging)
						{
							AkMonitoringCallbackInfo.setCPtr(pData);

							string msg = "Wwise: " + AkMonitoringCallbackInfo.message;
							if (AkMonitoringCallbackInfo.gameObjID != AkSoundEngine.AK_INVALID_GAME_OBJECT)
							{
								var obj = EditorUtility.InstanceIDToObject((int)AkMonitoringCallbackInfo.gameObjID) as GameObject;
								if (obj != null)
									msg += " (GameObject: " + obj.ToString() + ")";

								msg += " (Instance ID: " + AkMonitoringCallbackInfo.gameObjID.ToString() + ")";
							}

							if (AkMonitoringCallbackInfo.errorLevel == AkMonitorErrorLevel.ErrorLevel_Error)
								Debug.LogError(msg);
							else
								Debug.Log(msg);
						}
#endif
						break;

					case AkCallbackType.AK_Bank:
						BankCallbackPackage bankPkg = null;
						if (!m_mapBankCallbacks.TryGetValue((int)pPackage, out bankPkg))
						{
							Debug.LogError("WwiseUnity: BankCallbackPackage not found for <" + pPackage + ">.");
							return numCallbacks;
						}
						else
						{
							m_mapBankCallbacks.Remove((int)pPackage);

							if (bankPkg != null && bankPkg.m_Callback != null)
							{
								AkBankCallbackInfo.setCPtr(pData);
								bankPkg.m_Callback(AkBankCallbackInfo.bankID, AkBankCallbackInfo.inMemoryBankPtr, AkBankCallbackInfo.loadResult, 
									(uint)AkBankCallbackInfo.memPoolId, bankPkg.m_Cookie);
							}
						}
						break;

					default:
						EventCallbackPackage eventPkg = null;
						if (!m_mapEventCallbacks.TryGetValue((int)pPackage, out eventPkg))
						{
							Debug.LogError("WwiseUnity: EventCallbackPackage not found for <" + pPackage + ">.");
							return numCallbacks;
						}
						else
						{
							AkCallbackInfo info = null;

							switch (eType)
							{
								case AkCallbackType.AK_EndOfEvent:
									m_mapEventCallbacks.Remove(eventPkg.GetHashCode());
									if (eventPkg.m_bNotifyEndOfEvent)
									{
										AkEventCallbackInfo.setCPtr(pData);
										info = AkEventCallbackInfo;
									}
									break;

								case AkCallbackType.AK_MusicPlayStarted:
									AkEventCallbackInfo.setCPtr(pData);
									info = AkEventCallbackInfo;
									break;

								case AkCallbackType.AK_EndOfDynamicSequenceItem:
									AkDynamicSequenceItemCallbackInfo.setCPtr(pData);
									info = AkDynamicSequenceItemCallbackInfo;
									break;

								case AkCallbackType.AK_MIDIEvent:
									AkMIDIEventCallbackInfo.setCPtr(pData);
									info = AkMIDIEventCallbackInfo;
									break;

								case AkCallbackType.AK_Marker:
									AkMarkerCallbackInfo.setCPtr(pData);
									info = AkMarkerCallbackInfo;
									break;

								case AkCallbackType.AK_Duration:
									AkDurationCallbackInfo.setCPtr(pData);
									info = AkDurationCallbackInfo;
									break;

								case AkCallbackType.AK_MusicSyncUserCue:
								case AkCallbackType.AK_MusicSyncBar:
								case AkCallbackType.AK_MusicSyncBeat:
								case AkCallbackType.AK_MusicSyncEntry:
								case AkCallbackType.AK_MusicSyncExit:
								case AkCallbackType.AK_MusicSyncGrid:
								case AkCallbackType.AK_MusicSyncPoint:
									AkMusicSyncCallbackInfo.setCPtr(pData);
									info = AkMusicSyncCallbackInfo;
									break;

								case AkCallbackType.AK_MusicPlaylistSelect:
									AkMusicPlaylistCallbackInfo.setCPtr(pData);
									info = AkMusicPlaylistCallbackInfo;
									break;

								default:
									Debug.LogError("WwiseUnity: PostCallbacks aborted due to error: Undefined callback type <" + eType + "> found. Callback object possibly corrupted.");
									return numCallbacks;
							}

							if (info != null)
							{
								eventPkg.m_Callback(eventPkg.m_Cookie, eType, info);
							}
						}
						break;
				}
			}

			return numCallbacks;
		}
		finally
		{
			AkCallbackSerializer.Unlock();
		}
	}
};
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.