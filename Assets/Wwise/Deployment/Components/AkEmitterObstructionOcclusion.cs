#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Wwise/AkEmitterObstructionOcclusion")]
[RequireComponent(typeof(AkGameObj))]
/// @brief Obstructs/Occludes the emitter of the current game object from its listeners if at least one object is between them.
/// @details The current implementation does not support occlusion.
public class AkEmitterObstructionOcclusion : AkObstructionOcclusion
{
    private AkGameObj m_gameObj;

    private void Awake()
    {
        InitIntervalsAndFadeRates();
        m_gameObj = GetComponent<AkGameObj>();
    }

    private void Update()
    {
        bool useSpatialAudio = AkRoom.IsSpatialAudioEnabled;

        // Update Listeners
        if (useSpatialAudio)
            UpdateObstructionOcclusionValues(AkSpatialAudioListener.TheSpatialAudioListener);
        else
            UpdateObstructionOcclusionValues(m_gameObj.ListenerList);

        CastRays();

        // Set Obstruction/Occlusion
        foreach (var ObsOccPair in ObstructionOcclusionValues)
        {
            var ObsOccValue = ObsOccPair.Value;

            if (ObsOccValue.Update(fadeRate))
            {
                if (useSpatialAudio)
                    AkSoundEngine.SetEmitterObstruction(gameObject, ObsOccValue.currentValue);
                else
                    AkSoundEngine.SetObjectObstructionAndOcclusion(gameObject, ObsOccPair.Key.gameObject, 0.0f, ObsOccValue.currentValue);
            }
        }
    }
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.