#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.

#if UNITY_2017_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

//--------------------------------------------------------------------------------------------
// The representation of the Timeline Clip
//--------------------------------------------------------------------------------------------

[System.Serializable]
public class AkRTPCPlayable : PlayableAsset, ITimelineClipAsset
{
    public AkRTPCPlayableBehaviour template = new AkRTPCPlayableBehaviour();

    public bool setRTPCGlobally = false;
    public bool overrideTrackObject = false;
    public ExposedReference<GameObject> RTPCObject;
    private AK.Wwise.RTPC RTPC;
    public AK.Wwise.RTPC Parameter
    {
        get { return RTPC; }
        set { RTPC = value; }
    }
    private TimelineClip owningClip;
    public TimelineClip OwningClip
    {
        get { return owningClip; }
        set { owningClip = value; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var playable = ScriptPlayable<AkRTPCPlayableBehaviour>.Create(graph, template);
        AkRTPCPlayableBehaviour b = playable.GetBehaviour();
        InitializeBehavior(graph, ref b, go);
        return playable;
    }

    public void InitializeBehavior(PlayableGraph graph, ref AkRTPCPlayableBehaviour b, GameObject owner)
    {
        b.overrideTrackObject = overrideTrackObject;
        b.setRTPCGlobally = setRTPCGlobally;

        if (overrideTrackObject)
            b.rtpcObject = RTPCObject.Resolve(graph.GetResolver());
        else
            b.rtpcObject = owner;

        b.parameter = RTPC;
    }

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Looping & ClipCaps.Extrapolation & ClipCaps.ClipIn & ClipCaps.SpeedMultiplier; }
    }
}


//--------------------------------------------------------------------------------------------
// The behaviour template.
//--------------------------------------------------------------------------------------------

[System.Serializable]
public class AkRTPCPlayableBehaviour : PlayableBehaviour
{
    private bool m_SetRTPCGlobally = false;
    public bool setRTPCGlobally
    {
        set { m_SetRTPCGlobally = value; }
    }
    private bool m_OverrideTrackObject = false;
    public bool overrideTrackObject
    {
        set { m_OverrideTrackObject = value; }
    }
    private GameObject m_RTPCObject;
    public GameObject rtpcObject
    {
        set { m_RTPCObject = value; }
        get { return m_RTPCObject; }
    }

    private AK.Wwise.RTPC m_Parameter;
    public AK.Wwise.RTPC parameter
    {
        set { m_Parameter = value; }
    }

    public float RTPCValue = 0.0f;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!m_OverrideTrackObject)
        {
            // At this point, m_RTPCObject will have been set to the timeline owner object in AkRTPCPlayable::CreatePlayable().
            // If the track object is null, we keep using the timeline owner object. Otherwise, we swap it for the track object.
            GameObject obj = playerData as GameObject;
            if (obj != null)
            {
                m_RTPCObject = obj;
            }
        }//If we are overriding the track object, the m_RTPCObject will have been resolved in AkRTPCPlayable::CreatePlayable().

        if (m_Parameter != null)
        {
            if (m_SetRTPCGlobally || m_RTPCObject == null)
                m_Parameter.SetGlobalValue(RTPCValue);
            else
                m_Parameter.SetValue(m_RTPCObject, RTPCValue);
        }
    }
}

#endif //UNITY_2017_1_OR_NEWER

#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.