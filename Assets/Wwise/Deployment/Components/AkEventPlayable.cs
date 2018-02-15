#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2017 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

#if UNITY_2017_1_OR_NEWER

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Xml;

#if UNITY_EDITOR
public class MinMaxEventDuration
{
    private Vector2 MinMaxDuration = Vector2.zero;
    public float MinDuration
    {
        get { return MinMaxDuration.x; }
        set { MinMaxDuration.Set(value, MinMaxDuration.y); }
    }
    public float MaxDuration
    {
        get { return MinMaxDuration.y; }
        set { MinMaxDuration.Set(MinMaxDuration.x, value); }
    }

    static public MinMaxEventDuration GetMinMaxDuration(AK.Wwise.Event akEvent)
    {
        MinMaxEventDuration result = new MinMaxEventDuration();
        string FullSoundbankPath = AkBasePathGetter.GetPlatformBasePath();
        string filename = System.IO.Path.Combine(FullSoundbankPath, "SoundbanksInfo.xml");
        float MaxDuration = 1000000.0f;
        if (System.IO.File.Exists(filename))
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            XmlNodeList soundBanks = doc.GetElementsByTagName("SoundBanks");
            for (int i = 0; i < soundBanks.Count; i++)
            {
                XmlNodeList soundBank = soundBanks[i].SelectNodes("SoundBank");
                for (int j = 0; j < soundBank.Count; j++)
                {
                    XmlNodeList includedEvents = soundBank[j].SelectNodes("IncludedEvents");
                    for (int ie = 0; ie < includedEvents.Count; ie++)
                    {
                        XmlNodeList events = includedEvents[i].SelectNodes("Event");
                        for (int e = 0; e < events.Count; e++)
                        {
                            if (events[e].Attributes["Id"] != null && uint.Parse(events[e].Attributes["Id"].InnerText) == (uint)akEvent.ID)
                            {
                                if (events[e].Attributes["DurationType"] != null && events[e].Attributes["DurationType"].InnerText == "Infinite")
                                {
                                    // Set both min and max to MaxDuration for infinite events
                                    result.MinDuration = MaxDuration;
                                    result.MaxDuration = MaxDuration;
                                }
                                if (events[e].Attributes["DurationMin"] != null)
                                {
                                    result.MinDuration = float.Parse(events[e].Attributes["DurationMin"].InnerText);
                                }
                                if (events[e].Attributes["DurationMax"] != null)
                                {
                                    result.MaxDuration = float.Parse(events[e].Attributes["DurationMax"].InnerText);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
}
#endif //UNITY_EDITOR

public class WwiseEventTracker
{
    public bool  eventIsPlaying            = false;
    public bool  fadeoutTriggered          = false;
    public float currentDuration           = -1.0f;
    public float currentDurationProportion = 1.0f;
    public float previousEventStartTime    = 0.0f;
    public uint playingID = 0;
    public void CallbackHandler(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            eventIsPlaying = false;
            fadeoutTriggered = false;
        }
        else if (in_type == AkCallbackType.AK_Duration)
        {
            float estimatedDuration = ((AkDurationCallbackInfo)in_info).fEstimatedDuration;
            currentDuration = (estimatedDuration * currentDurationProportion) / 1000.0f;
        }
    }
}

/// @brief A playable asset containing a Wwise event that can be placed within a \ref AkEventTrack in a timeline.
/// @details Use this class to play Wwise events from a timeline and synchronise them to the animation. Events will be emitted from the GameObject that is bound to the AkEventTrack. Use the overrideTrackEmitterObject option to choose a different GameObject from which to emit the Wwise event. 
/// \sa
/// - \ref AkEventTrack
/// - \ref AkEventPlayableBehavior
[System.Serializable]
public class AkEventPlayable : PlayableAsset, ITimelineClipAsset
{

    public bool overrideTrackEmitterObject = false;
    public ExposedReference<GameObject> emitterObjectRef;

    private float easeInDuration = 0.0f;
    private float easeOutDuration = 0.0f;
    private float blendInDuration = 0.0f;
    private float blendOutDuration = 0.0f;

    public void setEaseInDuration(float d) { easeInDuration = d; }
    public void setEaseOutDuration(float d) { easeOutDuration = d; }
    public void setBlendInDuration(float d) { blendInDuration = d; }
    public void setBlendOutDuration(float d) { blendOutDuration = d; }

    [SerializeField]
    float eventDurationMin = -1.0f;
    [SerializeField]
    float eventDurationMax = -1.0f;

    public AK.Wwise.Event akEvent;

    public bool retriggerEvent = false;

    private WwiseEventTracker eventTracker = new WwiseEventTracker();
	
#if UNITY_EDITOR
    //Used to track when the event has been changed in OnValidate so that the duration can be updated at the correct time.
    private int previousEventID = 0;
#endif

    private TimelineClip owningClip;
    public TimelineClip OwningClip
    {
        get { return owningClip; }
        set { owningClip = value; }
    }

    public override double duration
    {
        get
        {
            if (akEvent == null)
                return base.duration;

            return (double)eventDurationMax;
        }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<AkEventPlayableBehavior> playable = ScriptPlayable<AkEventPlayableBehavior>.Create(graph);
        AkEventPlayableBehavior b = playable.GetBehaviour();
        initializeBehaviour(graph, b, owner);
        b.akEventMinDuration = eventDurationMin;
        b.akEventMaxDuration = eventDurationMax;
        return playable;
    }

    public ClipCaps clipCaps
    {
        get
        {
            if (!retriggerEvent)
                return ClipCaps.All;
            return ClipCaps.Looping & ClipCaps.Extrapolation & ClipCaps.ClipIn & ClipCaps.SpeedMultiplier;
        }
    }

    public void initializeBehaviour(PlayableGraph graph, AkEventPlayableBehavior b, GameObject owner)
    {
        b.akEvent = akEvent;
        b.eventTracker = eventTracker;
        b.easeInDuration = easeInDuration;
        b.easeOutDuration = easeOutDuration;
        b.blendInDuration = blendInDuration;
        b.blendOutDuration = blendOutDuration;
        b.eventShouldRetrigger = retriggerEvent;
        b.overrideTrackEmittorObject = overrideTrackEmitterObject;

        if (overrideTrackEmitterObject)
            b.eventObject = emitterObjectRef.Resolve(graph.GetResolver());
        else
            b.eventObject = owner;
    }

#if UNITY_EDITOR
    private void updateWwiseEventDurations()
    {
        if (akEvent != null)
        {
            MinMaxEventDuration MinMaxDuration = MinMaxEventDuration.GetMinMaxDuration(akEvent);
            eventDurationMin = MinMaxDuration.MinDuration;
            eventDurationMax = MinMaxDuration.MaxDuration;
        }
    }

    public void OnValidate()
    {
        if (previousEventID != akEvent.ID)
        {
            previousEventID = akEvent.ID;
            updateWwiseEventDurations();
            if (owningClip != null)
            {
                owningClip.duration = eventDurationMax;
            }
        }
    }
#endif
}

/// @brief Defines the behavior of a \ref AkEventPlayable within a \ref AkEventTrack.
/// \sa
/// - \ref AkEventTrack
/// - \ref AkEventPlayable
public class AkEventPlayableBehavior : PlayableBehaviour
{
    static public int scrubPlaybackLengthMs = 100;

    public enum AkPlayableAction
    {
        None          = 0,
        Playback      = 1,
        Retrigger     = 2,
        Stop          = 4,
        DelayedStop   = 8,
        Seek          = 16,
        FadeIn        = 32,
        FadeOut       = 64,
    };

    public AK.Wwise.Event akEvent = null;

    public float easeInDuration   = 0.0f;
    public float easeOutDuration  = 0.0f;
    public float blendInDuration  = 0.0f;
    public float blendOutDuration = 0.0f;

    public float akEventMinDuration = -1.0f;
    public float akEventMaxDuration = -1.0f;

    public float lastEffectiveWeight = 1.0f;

    public uint requiredActions = 0;

    public bool eventShouldRetrigger = false;

    public bool overrideTrackEmittorObject = false;
    public GameObject eventObject;

    public WwiseEventTracker eventTracker;

    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (eventTracker != null)
        {
            // We disable scrubbing in edit mode, due to an issue with how FrameData.EvaluationType is handled in edit mode.
            // This is a known issue and Unity are aware of it: https://fogbugz.unity3d.com/default.asp?953109_kitf7pso0vmjm0m0
            bool scrubbing = info.evaluationType == FrameData.EvaluationType.Evaluate && Application.isPlaying;
            if (scrubbing && ShouldPlay(playable))
            {
                if (!eventTracker.eventIsPlaying)
                {
                    requiredActions |= (uint)AkPlayableAction.Playback;
                    requiredActions |= (uint)AkPlayableAction.DelayedStop;
                    checkForFadeIn((float)playable.GetTime());
                    checkForFadeOut(playable);
                }
                requiredActions |= (uint)AkPlayableAction.Seek;
            }
            else // The clip is playing but the event hasn't been triggered. We need to start the event and jump to the correct time.
            {
                if (!eventTracker.eventIsPlaying && (requiredActions & (uint)AkPlayableAction.Playback) == 0)
                {
                    requiredActions |= (uint)AkPlayableAction.Retrigger;
                    checkForFadeIn((float)playable.GetTime());
                }
                checkForFadeOut(playable);
            }
        }
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (akEvent != null)
        {
            if (ShouldPlay(playable))
            {
                requiredActions |= (uint)AkPlayableAction.Playback;
                // If we've explicitly set the playhead, only play a small snippet.
                // We disable scrubbing in edit mode, due to an issue with how FrameData.EvaluationType is handled in edit mode.
                // This is a known issue and Unity are aware of it: https://fogbugz.unity3d.com/default.asp?953109_kitf7pso0vmjm0m0
                if (info.evaluationType == FrameData.EvaluationType.Evaluate && Application.isPlaying)
                {
                    requiredActions |= (uint)AkPlayableAction.DelayedStop;
                    checkForFadeIn((float)playable.GetTime());
                    checkForFadeOut(playable);
                }
                else
                {
                    float proportionalTime = getProportionalTime(playable);
                    float alph = 0.05f;
                    if (proportionalTime > alph) // we need to jump to the correct position in the case where the event is played from some non-start position.
                    {
                        requiredActions |= (uint)AkPlayableAction.Seek;
                    }
                    checkForFadeIn((float)playable.GetTime());
                    checkForFadeOut(playable);
                }
            }
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (eventObject != null)
            stopEvent();
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!overrideTrackEmittorObject)
        {
            GameObject obj = playerData as GameObject;
            if (obj != null)
            {
                eventObject = obj;
            }
        }
        if (eventObject != null)
        {
            float clipTime = (float)playable.GetTime();
            if (actionIsRequired(AkPlayableAction.Playback))
                playEvent();
            if (eventShouldRetrigger && actionIsRequired(AkPlayableAction.Retrigger))
                retriggerEvent(playable);
            if (actionIsRequired(AkPlayableAction.Stop))
                akEvent.Stop(eventObject);
            if (actionIsRequired(AkPlayableAction.DelayedStop))
                stopEvent(scrubPlaybackLengthMs);
            if (actionIsRequired(AkPlayableAction.Seek))
                seekToTime(playable);
            if (actionIsRequired(AkPlayableAction.FadeIn))
                triggerFadeIn(clipTime);
            if (actionIsRequired(AkPlayableAction.FadeOut))
            {
                float timeLeft = (float)(playable.GetDuration() - playable.GetTime());
                triggerFadeOut(timeLeft);
            }
        }
        requiredActions = (uint)AkPlayableAction.None;
    }

    private bool actionIsRequired(AkPlayableAction actionType)
    {
        return (requiredActions & (uint)actionType) != 0;
    }

    /** Check the playable time against the Wwise event duration to see if playback should occur.
     */
    private bool ShouldPlay(Playable playable)
    {
        if (eventTracker != null)
        {
            // If max and min duration values from metadata are equal, we can assume a deterministic event.
            if (akEventMaxDuration == akEventMinDuration && akEventMinDuration != -1.0f)
            {
                return (float)playable.GetTime() < akEventMaxDuration || eventShouldRetrigger;
            }
            else // Otherwise we need to use the estimated duration for the current event.
            {
                float currentTime = (float)playable.GetTime() - eventTracker.previousEventStartTime;
                float currentDuration = eventTracker.currentDuration;
                float maxDuration = currentDuration == -1.0f ? (float)playable.GetDuration() : currentDuration;
                return currentTime < maxDuration || eventShouldRetrigger;
            }
        }
        return false;
    }

    private bool fadeInRequired(float currentClipTime)
    {
        // Check whether we are currently within a fade in or blend in segment.
        float remainingBlendInDuration = blendInDuration - currentClipTime;
        float remainingEaseInDuration = easeInDuration - currentClipTime;
        return remainingBlendInDuration > 0.0f || remainingEaseInDuration > 0.0f;
    }
    private void checkForFadeIn(float currentClipTime)
    {
        if (fadeInRequired(currentClipTime))
            requiredActions |= (uint)AkPlayableAction.FadeIn;
    }
    private void checkForFadeInImmediate(float currentClipTime)
    {
        if (fadeInRequired(currentClipTime))
            triggerFadeIn(currentClipTime);
    }
    
    private bool fadeOutRequired(Playable playable)
    {
        // Check whether we are currently within a fade out or blend out segment.
        float timeLeft = (float)(playable.GetDuration() - playable.GetTime());
        float remainingBlendOutDuration = blendOutDuration - timeLeft;
        float remainingEaseOutDuration = easeOutDuration - timeLeft;
        return remainingBlendOutDuration >= 0.0f || remainingEaseOutDuration >= 0.0f;
    }
    private void checkForFadeOutImmediate(Playable playable)
    {
        if (eventTracker != null && !eventTracker.fadeoutTriggered)
        {
            if (fadeOutRequired(playable))
            {
                float timeLeft = (float)(playable.GetDuration() - playable.GetTime());
                triggerFadeOut(timeLeft);
            }
        }
    }
    private void checkForFadeOut(Playable playable)
    {
        if (eventTracker != null && !eventTracker.fadeoutTriggered)
        {
            if (fadeOutRequired(playable))
                requiredActions |= (uint)AkPlayableAction.FadeOut;
        }
    }

    protected void triggerFadeIn(float currentClipTime)
    {
        if (eventObject != null && akEvent != null)
        {
            float fadeDuration = Mathf.Max(easeInDuration - currentClipTime, blendInDuration - currentClipTime);
            if (fadeDuration > 0.0f)
            {
                akEvent.ExecuteAction(eventObject, AkActionOnEventType.AkActionOnEventType_Pause, 0, AkCurveInterpolation.AkCurveInterpolation_Linear);
                akEvent.ExecuteAction(eventObject, AkActionOnEventType.AkActionOnEventType_Resume,
                                      (int)(fadeDuration * 1000.0f), AkCurveInterpolation.AkCurveInterpolation_Linear);
            }
        }
    }

    protected void triggerFadeOut(float fadeDuration)
    {
        if (eventObject != null && akEvent != null)
        {
            if (eventTracker != null)
                eventTracker.fadeoutTriggered = true;
            akEvent.ExecuteAction(eventObject, AkActionOnEventType.AkActionOnEventType_Stop,
                                  (int)(fadeDuration * 1000.0f), AkCurveInterpolation.AkCurveInterpolation_Linear);
        }
    }

    protected void stopEvent(int transition = 0)
    {
        if (eventObject != null && akEvent != null && eventTracker.eventIsPlaying)
        {
            akEvent.Stop(eventObject, transition);
            if (eventTracker != null)
                eventTracker.eventIsPlaying = false;
        }
    }

    protected void playEvent()
    {
        if (eventObject != null && akEvent != null && eventTracker != null)
        {
            eventTracker.playingID = akEvent.Post(eventObject, (uint)AkCallbackType.AK_EndOfEvent | (uint)AkCallbackType.AK_Duration, eventTracker.CallbackHandler, null);
            if (eventTracker.playingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                eventTracker.eventIsPlaying = true;
                eventTracker.currentDurationProportion = 1.0f;
                eventTracker.previousEventStartTime = 0.0f;
            }
        }
    }

    protected void retriggerEvent(Playable playable)
    {
        if (eventObject != null && akEvent != null && eventTracker != null)
        {
            eventTracker.playingID = akEvent.Post(eventObject, (uint)AkCallbackType.AK_EndOfEvent | (uint)AkCallbackType.AK_Duration, eventTracker.CallbackHandler, null);
            if (eventTracker.playingID != AkSoundEngine.AK_INVALID_PLAYING_ID)
            {
                eventTracker.eventIsPlaying = true;
                float proportionOfDurationLeft = seekToTime(playable);
                eventTracker.currentDurationProportion = proportionOfDurationLeft;
                eventTracker.previousEventStartTime = (float)playable.GetTime();
            }
        }
    }

    protected float getProportionalTime (Playable playable)
    {
        if (eventTracker != null)
        {
            // If max and min duration values from metadata are equal, we can assume a deterministic event.
            if (akEventMaxDuration == akEventMinDuration && akEventMinDuration != -1.0f)
            {
                // If the timeline clip has length greater than the event duration, we want to loop.
                return (((float)playable.GetTime() % akEventMaxDuration) / akEventMaxDuration);
            }
            else // Otherwise we need to use the estimated duration for the current event.
            {
                float currentTime = (float)playable.GetTime() - eventTracker.previousEventStartTime;
                float currentDuration = eventTracker.currentDuration;
                float maxDuration = currentDuration == -1.0f ? (float)playable.GetDuration() : currentDuration;
                // If the timeline clip has length greater than the event duration, we want to loop.
                return ((currentTime % maxDuration) / maxDuration);
            }
        }
        return 0.0f;
    }

    // Seek to the current time, taking looping into account.
    // Return the proportion of the current event estimated duration that is left, after the seek.
    protected float seekToTime(Playable playable)
    {
        if (eventObject != null && akEvent != null)
        {
            float proportionalTime = getProportionalTime(playable);
            if (proportionalTime < 1.0f) // Avoids Wwise "seeking beyond end of event: audio will stop" error.
            {
                AkSoundEngine.SeekOnEvent((uint)akEvent.ID, eventObject, proportionalTime);
                return 1.0f - proportionalTime;
            }
        }
        return 1.0f;
    }
}
#endif //UNITY_2017_1_OR_NEWER
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.