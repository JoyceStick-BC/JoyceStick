#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.

#if UNITY_2017_1_OR_NEWER

//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.855f, 0.8623f, 0.870f)]
[TrackClipType(typeof(AkEventPlayable))]
[TrackBindingType(typeof(GameObject))]
/// @brief A track within timeline that holds \ref AkEventPlayable clips. 
/// @details AkEventTracks are bound to a specific GameObject, which is the default emitter for all of the \ref AkEventPlayable clips. There is an option to override this in /ref AkEventPlayable.
/// \sa
/// - \ref AkEventPlayable
/// - \ref AkEventPlayableBehavior
public class AkEventTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
#if UNITY_EDITOR
        WwiseSettings Settings = WwiseSettings.LoadSettings();
        string WprojPath = AkUtilities.GetFullPath(Application.dataPath, Settings.WwiseProjectPath);
        AkUtilities.EnableBoolSoundbankSettingInWproj("SoundBankGenerateEstimatedDuration", WprojPath);
#endif
        var playable = ScriptPlayable<AkEventPlayableBehavior>.Create(graph);
        playable.SetInputCount(inputCount);
        setFadeTimes();
        setOwnerClips();
        return playable;
    }

    public void setFadeTimes()
    {
        IEnumerable<TimelineClip> clips = GetClips();
        foreach (TimelineClip clip in clips)
        {
            AkEventPlayable clipPlayable = (AkEventPlayable)clip.asset;
            clipPlayable.setBlendInDuration((float) getBlendInTime(clipPlayable));
            clipPlayable.setBlendOutDuration((float) getBlendOutTime(clipPlayable));
            clipPlayable.setEaseInDuration((float) getEaseInTime(clipPlayable));
            clipPlayable.setEaseOutDuration((float) getEaseOutTime(clipPlayable));
        }
    }

    public void setOwnerClips()
    {
        IEnumerable<TimelineClip> clips = GetClips();
        foreach (TimelineClip clip in clips)
        {
            AkEventPlayable clipPlayable = (AkEventPlayable)clip.asset;
            clipPlayable.OwningClip = clip;
        }
    }

    public double getBlendInTime(AkEventPlayable playableClip)
    {
        IEnumerable<TimelineClip> clips = GetClips();
        foreach (TimelineClip clip in clips)
        {
            if (playableClip == (AkEventPlayable)clip.asset)
                return clip.blendInDuration;
        }
        return 0.0;
    }

    public double getBlendOutTime(AkEventPlayable playableClip)
    {
        IEnumerable<TimelineClip> clips = GetClips();
        foreach (TimelineClip clip in clips)
        {
            if (playableClip == (AkEventPlayable)clip.asset)
                return clip.blendOutDuration;
        }
        return 0.0;
    }

    public double getEaseInTime(AkEventPlayable playableClip)
    {
        IEnumerable<TimelineClip> clips = GetClips();
        foreach (TimelineClip clip in clips)
        {
            if (playableClip == (AkEventPlayable)clip.asset)
                return clip.easeInDuration;
        }
        return 0.0;
    }

    public double getEaseOutTime(AkEventPlayable playableClip)
    {
        IEnumerable<TimelineClip> clips = GetClips();
        foreach (TimelineClip clip in clips)
        {
            if (playableClip == (AkEventPlayable)clip.asset)
                return clip.easeOutDuration;
        }
        return 0.0;
    }
}

#endif //UNITY_2017_1_OR_NEWER
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.