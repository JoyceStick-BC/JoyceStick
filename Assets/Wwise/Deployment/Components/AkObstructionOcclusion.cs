#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class AkObstructionOcclusion : MonoBehaviour
{
    [Tooltip("Layers of obstructers/occluders")]
    /// Indicates which layers act as obstructers/occluders.
    public LayerMask LayerMask = -1;

    [Tooltip("The number of seconds between raycasts")]
    /// The number of seconds between obstruction/occlusion checks.
    public float refreshInterval = 1;
    private float refreshTime = 0;

    [Tooltip("Fade time in seconds")]
    /// The number of seconds for fade ins and fade outs.
    public float fadeTime = 0.5f;
    protected float fadeRate = 0.0f;

    [Tooltip("Maximum distance to perform the obstruction/occlusion. Negative values mean infinite")]
    /// The maximum distance at which to perform obstruction/occlusion. A negative value will be interpreted as inifinite distance.
    public float maxDistance = -1.0f;

    protected void InitIntervalsAndFadeRates()
    {
        refreshTime = Random.Range(0.0f, refreshInterval);
        fadeRate = 1 / fadeTime;
    }

    protected class ObstructionOcclusionValue
    {
        public float targetValue = 0.0f;
        public float currentValue = 0.0f;

        public bool Update(float fadeRate)
        {
            if (Mathf.Approximately(targetValue, currentValue))
                return false;

            currentValue += fadeRate * Mathf.Sign(targetValue - currentValue) * Time.deltaTime;
            currentValue = Mathf.Clamp(currentValue, 0.0f, 1.0f);

            return true;
        }
    }

    protected Dictionary<AkAudioListener, ObstructionOcclusionValue> ObstructionOcclusionValues = new Dictionary<AkAudioListener, ObstructionOcclusionValue>();
    private List<AkAudioListener> listenersToRemove = new List<AkAudioListener>();

    protected void UpdateObstructionOcclusionValues(List<AkAudioListener> listenerList)
    {
        // add new listeners
        for (int i = 0; i < listenerList.Count; ++i)
        {
            if (!ObstructionOcclusionValues.ContainsKey(listenerList[i]))
                ObstructionOcclusionValues.Add(listenerList[i], new ObstructionOcclusionValue());
        }

        // remove listeners
        foreach (var ObsOccPair in ObstructionOcclusionValues)
        {
            if (!listenerList.Contains(ObsOccPair.Key))
                listenersToRemove.Add(ObsOccPair.Key);
        }
        for (int i = 0; i < listenersToRemove.Count; ++i)
        {
            ObstructionOcclusionValues.Remove(listenersToRemove[i]);
        }
    }

    protected void UpdateObstructionOcclusionValues(AkAudioListener listener)
    {
        if (!listener)
            return;

        // add new listeners
        if (!ObstructionOcclusionValues.ContainsKey(listener))
            ObstructionOcclusionValues.Add(listener, new ObstructionOcclusionValue());

        // remove listeners
        foreach (var ObsOccPair in ObstructionOcclusionValues)
        {
            if (listener != ObsOccPair.Key)
                listenersToRemove.Add(ObsOccPair.Key);
        }
        for (int i = 0; i < listenersToRemove.Count; ++i)
        {
            ObstructionOcclusionValues.Remove(listenersToRemove[i]);
        }
    }

    protected void CastRays()
    {
        if (refreshTime > refreshInterval)
        {
            refreshTime -= refreshInterval;

            foreach (var ObsOccPair in ObstructionOcclusionValues)
            {
                var listener = ObsOccPair.Key;
                var ObsOccValue = ObsOccPair.Value;

                Vector3 difference = listener.transform.position - transform.position;
                float magnitude = difference.magnitude;

                if (maxDistance > 0 && magnitude > maxDistance)
                {
                    ObsOccValue.targetValue = ObsOccValue.currentValue;
                }
                else
                {
                    // since we already need magnitude, it is faster to make the division ourselves than to use difference.normalized
                    ObsOccValue.targetValue = Physics.Raycast(transform.position, difference / magnitude, magnitude, LayerMask.value) ? 1.0f : 0.0f;
                }
            }
        }

        refreshTime += Time.deltaTime;
    }
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.