#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections.Generic;


public abstract class AkSpatialAudioBase : MonoBehaviour
{
    private AkRoom.PriorityList roomPriorityList = new AkRoom.PriorityList();

    protected void SetGameObjectInHighestPriorityRoom()
    {
        ulong highestPriorityRoomID = roomPriorityList.GetHighestPriorityRoomID();
        AkSoundEngine.SetGameObjectInRoom(gameObject, highestPriorityRoomID);
    }

    public void EnteredRoom(AkRoom room)
    {
        roomPriorityList.Add(room);
        SetGameObjectInHighestPriorityRoom();
    }

    public void ExitedRoom(AkRoom room)
    {
        roomPriorityList.Remove(room);
        SetGameObjectInHighestPriorityRoom();
    }

    public void SetGameObjectInRoom()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.0f);
        foreach (var collider in colliders)
        {
            var room = collider.gameObject.GetComponent<AkRoom>();
            if (room != null)
                roomPriorityList.Add(room);
        }
        SetGameObjectInHighestPriorityRoom();
    }
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.