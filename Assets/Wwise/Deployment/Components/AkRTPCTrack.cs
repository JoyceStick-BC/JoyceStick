#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.

#if UNITY_2017_1_OR_NEWER
﻿
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

using UnityEngine.UI;

[TrackColor(0.32f, 0.13f, 0.13f)]
// Specifies the type of Playable Asset this track manages
[TrackClipType(typeof(AkRTPCPlayable))]
// Use if the track requires a binding to a scene object or asset
[TrackBindingType(typeof(GameObject))]
public class AkRTPCTrack : TrackAsset
{
    public AK.Wwise.RTPC Parameter;
    // override the type of mixer playable used by this track
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var playable = ScriptPlayable<AkRTPCPlayableBehaviour>.Create(graph, inputCount);
        setPlayableProperties();
        return playable;
    }

    public void setPlayableProperties()
    {
        IEnumerable<TimelineClip> clips = GetClips();
        foreach (TimelineClip clip in clips)
        {
            AkRTPCPlayable clipPlayable = (AkRTPCPlayable)clip.asset;
            clipPlayable.Parameter = Parameter;
            clipPlayable.OwningClip = clip;
        }
    }

    public void OnValidate()
    {
        IEnumerable<TimelineClip> clips = GetClips();
        foreach (TimelineClip clip in clips)
        {
            AkRTPCPlayable clipPlayable = (AkRTPCPlayable)clip.asset;
            clipPlayable.Parameter = Parameter;
        }
    }
}

#endif //UNITY_2017_1_OR_NEWER

#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.