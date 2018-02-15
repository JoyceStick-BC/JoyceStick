#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class AkTriangleArray : IDisposable
{
    int SIZE_OF_AKTRIANGLE = AkSoundEnginePINVOKE.CSharp_AkTriangleProxy_GetSizeOf();

    public AkTriangleArray(int count)
    {
        m_Count = count;
        m_Buffer = Marshal.AllocHGlobal(count * SIZE_OF_AKTRIANGLE);

        if (m_Buffer != IntPtr.Zero)
            for (int i = 0; i < count; ++i)
                AkSoundEnginePINVOKE.CSharp_AkTriangleProxy_Clear(GetObjectPtr(i));
    }

    ~AkTriangleArray()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (m_Buffer != IntPtr.Zero)
        {
            for (int i = 0; i < m_Count; ++i)
                AkSoundEnginePINVOKE.CSharp_AkTriangleProxy_DeleteName(GetObjectPtr(i));

            Marshal.FreeHGlobal(m_Buffer);
            m_Buffer = IntPtr.Zero;
            m_Count = 0;
        }
    }

	public void Reset()
	{
		m_Count = 0;
	}

    public AkTriangle GetTriangle(int index)
    {
        if (index >= m_Count)
            return null;

        return new AkTriangle(GetObjectPtr(index), false);
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
        return (IntPtr)(m_Buffer.ToInt64() + SIZE_OF_AKTRIANGLE * index);
    }

    private IntPtr m_Buffer;
    private int m_Count;
};
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.