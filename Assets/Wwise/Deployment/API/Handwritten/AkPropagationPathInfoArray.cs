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

public class AkPropagationPathInfoArray : IDisposable
{
    int SIZE_OF_STRUCTURE = AkSoundEnginePINVOKE.CSharp_AkPropagationPathInfoProxy_GetSizeOf();

    public AkPropagationPathInfoArray(int count)
    {
        m_Count = count;
        m_Buffer = Marshal.AllocHGlobal(count * SIZE_OF_STRUCTURE);
    }

    ~AkPropagationPathInfoArray()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (m_Buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(m_Buffer);
            m_Buffer = IntPtr.Zero;
            m_Count = 0;
        }
    }

    public void Reset()
    {
        m_Count = 0;
    }

    public AkPropagationPathInfoProxy GetPropagationPathInfo(int index)
    {
        if (index >= m_Count)
            return null;

        return new AkPropagationPathInfoProxy(GetObjectPtr(index), false);
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
        return (IntPtr)(m_Buffer.ToInt64() + SIZE_OF_STRUCTURE * index);
    }

    private IntPtr m_Buffer;
    private int m_Count;
};
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.