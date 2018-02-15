#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
﻿using UnityEngine;
using System;
using System.Collections.Generic;

[AddComponentMenu("Wwise/AkRoomPortal")]
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
/// @brief An AkRoomPortal can connect two AkRoom components together.
/// @details 
public class AkRoomPortal : AkUnityEventHandler
{
    /// AkRoomPortals can only connect a maximum of 2 rooms.
    public const int MAX_ROOMS_PER_PORTAL = 2;

    /// The front and back rooms connected by the portal.
    /// The first room is on the negative side of the portal(opposite to the direction of the local Z axis)
    /// The second room is on the positive side of the portal.
    public AkRoom[] rooms = new AkRoom[MAX_ROOMS_PER_PORTAL];
    private ulong frontRoomID = AkRoom.INVALID_ROOM_ID;
    private ulong backRoomID = AkRoom.INVALID_ROOM_ID;

    private AkTransform portalTransform = new AkTransform();
    private AkVector extent = new AkVector();

    /// Access the portal's ID
    public ulong GetID() { return (ulong)GetInstanceID(); }
    
    public List<int> closePortalTriggerList = new List<int>();

    protected override void Awake()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;

        portalTransform.Set(collider.bounds.center.x, collider.bounds.center.y, collider.bounds.center.z,
            transform.forward.x, transform.forward.y, transform.forward.z,
            transform.up.x, transform.up.y, transform.up.z);

        extent.X = collider.size.x * transform.localScale.x / 2;
        extent.Y = collider.size.y * transform.localScale.y / 2;
        extent.Z = collider.size.z * transform.localScale.z / 2;

        frontRoomID = (rooms[1] == null) ? AkRoom.INVALID_ROOM_ID : rooms[1].GetID();
        backRoomID = (rooms[0] == null) ? AkRoom.INVALID_ROOM_ID : rooms[0].GetID();

        RegisterTriggers(closePortalTriggerList, ClosePortal);

        base.Awake();

        //Call the ClosePortal function if registered to the Awake Trigger
        if ((closePortalTriggerList.Contains(AWAKE_TRIGGER_ID)))
            ClosePortal(null);
    }

    protected override void Start()
    {
        base.Start();

        //Call the ClosePortal function if registered to the Start Trigger
        if ((closePortalTriggerList.Contains(START_TRIGGER_ID)))
            ClosePortal(null);
    }

    /// Opens the portal on trigger event
    public override void HandleEvent(GameObject in_gameObject)
    {
        Open();
    }

    /// Closes the portal on trigger event
    public void ClosePortal(GameObject in_gameObject)
    {
        Close();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        UnregisterTriggers(closePortalTriggerList, ClosePortal);

        if (closePortalTriggerList.Contains(DESTROY_TRIGGER_ID))
            ClosePortal(null);
    }

    public void Open()
    {
        ActivatePortal(true);
    }

    public void Close()
    {
        ActivatePortal(false);
    }

    private void ActivatePortal(bool active)
    {
        if (!enabled)
            return;

        if (frontRoomID != backRoomID)
            AkSoundEngine.SetRoomPortal(GetID(), portalTransform, extent, active, frontRoomID, backRoomID);
        else
            Debug.LogError(name + " is not placed/oriented correctly");
    }

    public void FindOverlappingRooms(AkRoom.PriorityList[] roomList)
    {
        BoxCollider portalCollider = gameObject.GetComponent<BoxCollider>();
        if (portalCollider == null)
            return;

        // compute halfExtents and divide the local z extent by 2
        Vector3 halfExtents = new Vector3(
            portalCollider.size.x * transform.localScale.x / 2,
            portalCollider.size.y * transform.localScale.y / 2,
            portalCollider.size.z * transform.localScale.z / 4);

        // move the center backward
        FillRoomList(Vector3.forward * -0.25f, halfExtents, roomList[0]);

        // move the center forward
        FillRoomList(Vector3.forward * 0.25f, halfExtents, roomList[1]);
    }

    void FillRoomList(Vector3 center, Vector3 halfExtents, AkRoom.PriorityList list)
    {
        list.rooms.Clear();

        center = transform.TransformPoint(center);

        Collider[] colliders = Physics.OverlapBox(center, halfExtents, transform.rotation);

        foreach (var collider in colliders)
        {
            var room = collider.gameObject.GetComponent<AkRoom>();
            if (room != null && !list.Contains(room))
                list.Add(room);
        }
    }

    public void SetFrontRoom(AkRoom room)
    {
        rooms[1] = room;
        frontRoomID = (rooms[1] == null) ? AkRoom.INVALID_ROOM_ID : rooms[1].GetID();
    }

    public void SetBackRoom(AkRoom room)
    {
        rooms[0] = room;
        backRoomID = (rooms[0] == null) ? AkRoom.INVALID_ROOM_ID : rooms[0].GetID();
    }

    public void UpdateOverlappingRooms()
    {
        AkRoom.PriorityList[] roomList = new AkRoom.PriorityList[] { new AkRoom.PriorityList(), new AkRoom.PriorityList() };

        FindOverlappingRooms(roomList);
        for (int i = 0; i < 2; i++)
            if (!roomList[i].Contains(rooms[i]))
                rooms[i] = roomList[i].GetHighestPriorityRoom();

        frontRoomID = (rooms[1] == null) ? AkRoom.INVALID_ROOM_ID : rooms[1].GetID();
        backRoomID = (rooms[0] == null) ? AkRoom.INVALID_ROOM_ID : rooms[0].GetID();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!enabled)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 centreOffset = Vector3.zero;
        Vector3 sizeMultiplier = Vector3.one;
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider)
        {
            centreOffset = collider.center;
            sizeMultiplier = collider.size;
        }

        // color faces
        Vector3[] faceCenterPos = new Vector3[4];
        faceCenterPos[0] = Vector3.Scale(new Vector3(0.5f, 0.0f, 0.0f), sizeMultiplier);
        faceCenterPos[1] = Vector3.Scale(new Vector3(0.0f, 0.5f, 0.0f), sizeMultiplier);
        faceCenterPos[2] = Vector3.Scale(new Vector3(-0.5f, 0.0f, 0.0f), sizeMultiplier);
        faceCenterPos[3] = Vector3.Scale(new Vector3(0.0f, -0.5f, 0.0f), sizeMultiplier);

        Vector3[] faceSize = new Vector3[4];
        faceSize[0] = new Vector3(0, 1, 1);
        faceSize[1] = new Vector3(1, 0, 1);
        faceSize[2] = faceSize[0];
        faceSize[3] = faceSize[1];

        Gizmos.color = new Color32(255, 204, 0, 100);
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawCube(faceCenterPos[i] + centreOffset, Vector3.Scale(faceSize[i], sizeMultiplier));
        }

        // draw line in the center of the portal
        Vector3[] CornerCenterPos = faceCenterPos;
        CornerCenterPos[0].y += 0.5f * sizeMultiplier.y;
        CornerCenterPos[1].x -= 0.5f * sizeMultiplier.x;
        CornerCenterPos[2].y -= 0.5f * sizeMultiplier.y;
        CornerCenterPos[3].x += 0.5f * sizeMultiplier.x;

        Gizmos.color = Color.red;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(CornerCenterPos[i] + centreOffset, CornerCenterPos[(i+1)%4] + centreOffset);
        }
    }
#endif
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.