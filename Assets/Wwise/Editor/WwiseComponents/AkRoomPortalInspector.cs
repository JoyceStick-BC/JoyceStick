#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[CanEditMultipleObjects]
[CustomEditor(typeof(AkRoomPortal))]
public class AkRoomPortalInspector : Editor
{
    AkUnityEventHandlerInspector m_OpenPortalEventHandlerInspector = new AkUnityEventHandlerInspector();
    AkUnityEventHandlerInspector m_ClosePortalEventHandlerInspector = new AkUnityEventHandlerInspector();

    [UnityEditor.MenuItem("GameObject/Wwise/Room Portal", false, 1)]
    public static void CreatePortal()
    {
        GameObject portal = new GameObject("RoomPortal");

        Undo.AddComponent<AkRoomPortal>(portal);
        portal.GetComponent<Collider>().isTrigger = true;

        Selection.objects = new UnityEngine.Object[] { portal };
    }

    AkRoomPortal m_roomPortal;
    AkRoom.PriorityList[] roomList = new AkRoom.PriorityList[] { new AkRoom.PriorityList(), new AkRoom.PriorityList() };

    int[] m_selectedIndex = new int[2];

    void OnEnable()
    {
        m_OpenPortalEventHandlerInspector.Init(serializedObject, "triggerList", "Open On: ", false);
        m_ClosePortalEventHandlerInspector.Init(serializedObject, "closePortalTriggerList", "Close On: ", false);

        m_roomPortal = target as AkRoomPortal;

        m_roomPortal.FindOverlappingRooms(roomList);
        for (int i = 0; i < 2; i++)
        {
            int index = roomList[i].BinarySearch(m_roomPortal.rooms[i]);
            m_selectedIndex[i] = index == -1 ? 0 : index;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        m_OpenPortalEventHandlerInspector.OnGUI();
        m_ClosePortalEventHandlerInspector.OnGUI();

        m_roomPortal.FindOverlappingRooms(roomList);

        GUILayout.BeginVertical("Box");
        {
            string[] labels = new string[2]{ "Back", "Front" };

            for (int i = 0; i < 2; i++)
            {
                int roomListCount = roomList[i].rooms.Count;
                string[] roomLabels = new string[roomListCount];

                for (int j = 0; j < roomListCount; j++)
                    roomLabels[j] = (j + 1) + ". " + roomList[i].rooms[j].name;

                m_selectedIndex[i] = EditorGUILayout.Popup(labels[i] + " Room", Mathf.Clamp(m_selectedIndex[i], 0, roomListCount - 1), roomLabels);

                m_roomPortal.rooms[i] = (m_selectedIndex[i] < 0 || m_selectedIndex[i] >= roomListCount) ? null : roomList[i].rooms[m_selectedIndex[i]];
            }
        }
        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif