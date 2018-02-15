#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
﻿//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

public class AkMIDIPostArray
{
	int SIZE_OF = AkSoundEnginePINVOKE.CSharp_AkMIDIPost_GetSizeOf();
	int m_Count = 0;
	IntPtr m_Buffer = IntPtr.Zero;

	public AkMIDIPostArray(int size)
	{
		m_Count = size;
		m_Buffer = Marshal.AllocHGlobal(m_Count * SIZE_OF);
	}

	~AkMIDIPostArray()
	{
		Marshal.FreeHGlobal(m_Buffer);
		m_Buffer = IntPtr.Zero;
	}

	public AkMIDIPost this[int index]
	{
		get
		{
			if (index >= m_Count)
				throw new IndexOutOfRangeException("Out of range access in AkMIDIPostArray");

			return new AkMIDIPost(GetObjectPtr(index), false);
		}

		set
		{
			if (index >= m_Count)
				throw new IndexOutOfRangeException("Out of range access in AkMIDIPostArray");

			AkSoundEnginePINVOKE.CSharp_AkMIDIPost_Clone(GetObjectPtr(index), AkMIDIPost.getCPtr(value));
		}
	}

	public void PostOnEvent(uint in_eventID, UnityEngine.GameObject gameObject)
	{
		var gameObjectID = AkSoundEngine.GetAkGameObjectID(gameObject);
		AkSoundEngine.PreGameObjectAPICall(gameObject, gameObjectID);
		AkSoundEnginePINVOKE.CSharp_AkMIDIPost_PostOnEvent(m_Buffer, in_eventID, gameObjectID, (uint)m_Count);
	}

	public void PostOnEvent(uint in_eventID, UnityEngine.GameObject gameObject, int count)
	{
		if (count >= m_Count)
			throw new IndexOutOfRangeException("Out of range access in AkMIDIPostArray");

		var gameObjectID = AkSoundEngine.GetAkGameObjectID(gameObject);
		AkSoundEngine.PreGameObjectAPICall(gameObject, gameObjectID);
		AkSoundEnginePINVOKE.CSharp_AkMIDIPost_PostOnEvent(m_Buffer, in_eventID, gameObjectID, (uint)count);
	}

	public IntPtr GetBuffer()
	{
		return m_Buffer;
	}

	public int Count()
	{
		return m_Count;
	}

	IntPtr GetObjectPtr(int index)
	{
		return (IntPtr)(m_Buffer.ToInt64() + SIZE_OF * index);
	}
};
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.