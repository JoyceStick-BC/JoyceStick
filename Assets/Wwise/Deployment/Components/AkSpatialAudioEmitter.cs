#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
﻿using UnityEngine;
using System;
using System.Collections;

[AddComponentMenu("Wwise/AkSpatialAudioEmitter")]
[RequireComponent(typeof(AkGameObj))]
///@brief Add this script on the GameObject which represents an emitter that uses the Spatial Audio API.
public class AkSpatialAudioEmitter : AkSpatialAudioBase
{
    [Header("Early Reflections")]

    /// The Auxiliary Bus with a Reflect plug-in Effect applied.
    public AK.Wwise.AuxBus reflectAuxBus;

    [Range(1, 4)]
    /// The maximum number of reflections that will be processed for a sound path before it reaches the listener.
    /// Reflection processing grows exponentially with the order of reflections, so this number should be kept low. Valid range: 1-4.
    public uint reflectionsOrder = 1;

    [Range(0, 1)]
    /// The gain [0, 1] applied to the reflect auxiliary bus.
    public float reflectionsAuxBusGain = 1;

    /// A heuristic to stop the computation of reflections. Should be no longer (and possibly shorter for less CPU usage) than the maximum attenuation of the sound emitter.
    public float reflectionMaxPathLength = 1000;

    [Header("Rooms")]

    [Range(0, 1)]
    /// Send gain (0.f-1.f) that is applied when sending to the aux bus associated with the room that the emitter is in.
    public float roomReverbAuxBusGain = 1;

    private void OnEnable()
    {
        AkEmitterSettings emitterSettings = new AkEmitterSettings();

        emitterSettings.reflectAuxBusID = (uint)reflectAuxBus.ID;
        emitterSettings.reflectionMaxPathLength = reflectionMaxPathLength;
        emitterSettings.reflectionsAuxBusGain = reflectionsAuxBusGain;
        emitterSettings.reflectionsOrder = reflectionsOrder;
        emitterSettings.reflectorFilterMask = unchecked((uint)(-1));
        emitterSettings.roomReverbAuxBusGain = roomReverbAuxBusGain;
        emitterSettings.useImageSources = 0;

        if (AkSoundEngine.RegisterEmitter(gameObject, emitterSettings) == AKRESULT.AK_Success)
            SetGameObjectInRoom();
    }

    private void OnDisable()
    {
        AkSoundEngine.UnregisterEmitter(gameObject);
    }

#if UNITY_EDITOR
    [Header("Debug Draw")]
    /// This allows you to visualize first order reflection sound paths.
    public bool drawFirstOrderReflections = false;
    /// This allows you to visualize second order reflection sound paths.
    public bool drawSecondOrderReflections = false;
    /// This allows you to visualize third or higher order reflection sound paths.
    public bool drawHigherOrderReflections = false;
    public bool drawSoundPropagation = false;

    private const uint kMaxIndirectPaths = 64;
    private const uint kMaxPropagationPaths = 16;
    private AkSoundPathInfoArray indirectPathInfoArray = new AkSoundPathInfoArray((int)kMaxIndirectPaths);
    private AkPropagationPathInfoArray propagationPathInfoArray = new AkPropagationPathInfoArray((int)kMaxPropagationPaths);
    private AkSoundPropagationPathParams indirectPathsParams = new AkSoundPropagationPathParams();
    private AkSoundPropagationPathParams soundPropagationPathsParams = new AkSoundPropagationPathParams();

    private Color32 colorLightBlue = new Color32(157, 235, 243, 255);
    private Color32 colorDarkBlue = new Color32(24, 96, 103, 255);

    private Color32 colorLightYellow = new Color32(252, 219, 162, 255);
    private Color32 colorDarkYellow = new Color32(169, 123, 39, 255);

    private Color32 colorLightRed = new Color32(252, 177, 162, 255);
    private Color32 colorDarkRed = new Color32(169, 62, 39, 255);

    private Color32 colorLightGrey = new Color32(75, 75, 75, 255);
    private Color32 colorDarkGrey = new Color32(35, 35, 35, 255);

    private Color32 colorPurple = new Color32(73, 46, 116, 255);
    private Color32 colorGreen = new Color32(38, 113, 88, 255);
    private Color32 colorRed = new Color32(170, 67, 57, 255);

    private float radiusSphere = 0.25f;
    private float radiusSphereMin = 0.1f;
    private float radiusSphereMax = 0.4f;

    void OnDrawGizmos()
    {
        if(Application.isPlaying && AkSoundEngine.IsInitialized())
        {
            if (drawFirstOrderReflections || drawSecondOrderReflections || drawHigherOrderReflections)
                DebugDrawEarlyReflections();

            if (drawSoundPropagation)
                DebugDrawSoundPropagation();
        }
    }

    Vector3 ConvertVector(AkVector vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    static void DrawLabelInFrontOfCam(Vector3 position, string name, float distance, Color c)
    {
        GUIStyle style = new GUIStyle();
        Vector3 oncam = Camera.current.WorldToScreenPoint(position);

        if ((oncam.x >= 0) && (oncam.x <= Camera.current.pixelWidth) &&
            (oncam.y >= 0) && (oncam.y <= Camera.current.pixelHeight) &&
            (oncam.z > 0) && (oncam.z < distance))
        {
            style.normal.textColor = c;
            UnityEditor.Handles.Label(position, name, style);
        }
    }

    void DebugDrawEarlyReflections()
    {
        if (AkSoundEngine.QueryIndirectPaths(gameObject, indirectPathsParams, indirectPathInfoArray, (uint)indirectPathInfoArray.Count()) == AKRESULT.AK_Success)
        {
            for (int idxPath = (int)indirectPathsParams.numValidPaths - 1; idxPath >= 0; --idxPath)
            {
                var path = indirectPathInfoArray.GetSoundPathInfo(idxPath);
                uint order = path.numReflections;

                if ((drawFirstOrderReflections && order == 1) ||
                    (drawSecondOrderReflections && order == 2) ||
                    (drawHigherOrderReflections && order > 2))
                {
                    Color32 colorLight;
                    Color32 colorDark;

                    switch (order - 1)
                    {
                        case 0:
                            colorLight = colorLightBlue;
                            colorDark = colorDarkBlue;
                            break;
                        case 1:
                            colorLight = colorLightYellow;
                            colorDark = colorDarkYellow;
                            break;
                        case 2:
                        default:
                            colorLight = colorLightRed;
                            colorDark = colorDarkRed;
                            break;
                    }

                    Vector3 emitterPos = ConvertVector(indirectPathsParams.emitterPos);
                    Vector3 listenerPt = ConvertVector(indirectPathsParams.listenerPos);

                    for (int idxSeg = (int)path.numReflections - 1; idxSeg >= 0; --idxSeg)
                    {
                        Vector3 reflectionPt = ConvertVector(path.GetReflectionPoint((uint)idxSeg));

                        Debug.DrawLine(listenerPt, reflectionPt, path.isOccluded ? colorLightGrey : colorLight);

                        Gizmos.color = path.isOccluded ? colorLightGrey : colorLight;
                        Gizmos.DrawWireSphere(reflectionPt, (radiusSphere / 2) / order);

                        if (!path.isOccluded)
                        {
                            var triangle = path.GetTriangle((uint)idxSeg);

                            var triPt0 = ConvertVector(triangle.point0);
                            var triPt1 = ConvertVector(triangle.point1);
                            var triPt2 = ConvertVector(triangle.point2);

                            Debug.DrawLine(triPt0, triPt1, colorDark);
                            Debug.DrawLine(triPt1, triPt2, colorDark);
                            Debug.DrawLine(triPt2, triPt0, colorDark);

                            DrawLabelInFrontOfCam(reflectionPt, path.GetTriangle((uint)idxSeg).strName, 100000, colorDark);
                        }

                        listenerPt = reflectionPt;
                    }

                    if (!path.isOccluded)
                    {
                        // Finally the last path segment towards the emitter.
                        Debug.DrawLine(listenerPt, emitterPos, path.isOccluded ? colorLightGrey : colorLight);
                    }
                    else
                    {
                        Vector3 occlusionPt = ConvertVector(path.occlusionPoint);
                        Gizmos.color = colorDarkGrey;
                        Gizmos.DrawWireSphere(occlusionPt, radiusSphere / order);
                    }
                }
            }
        }
    }

    void DebugDrawSoundPropagation()
    {
        if (AkSoundEngine.QuerySoundPropagationPaths(gameObject, soundPropagationPathsParams, propagationPathInfoArray, (uint)propagationPathInfoArray.Count()) == AKRESULT.AK_Success)
        {
            for (int idxPath = (int)soundPropagationPathsParams.numValidPaths - 1; idxPath >= 0; --idxPath)
            {
                var path = propagationPathInfoArray.GetPropagationPathInfo(idxPath);

                {
                    Vector3 emitterPos = ConvertVector(soundPropagationPathsParams.emitterPos);
                    Vector3 prevPt = ConvertVector(soundPropagationPathsParams.listenerPos);

                    for (int idxSeg = 0; idxSeg < (int)path.numNodes; ++idxSeg)
                    {
                        Vector3 portalPt = ConvertVector(path.GetNodePoint((uint)idxSeg));

                        if (idxSeg != 0)
                        {
                            Debug.DrawLine(prevPt, portalPt, colorPurple);
                        }

                        float radWet = radiusSphereMin + (1.0f - path.wetDiffractionAngle / (float)Math.PI) * (radiusSphereMax - radiusSphereMin);
                        float radDry = radiusSphereMin + (1.0f - path.dryDiffractionAngle / (float)Math.PI) * (radiusSphereMax - radiusSphereMin);

                        Gizmos.color = colorGreen;
                        Gizmos.DrawWireSphere(portalPt, radWet);
                        Gizmos.color = colorRed;
                        Gizmos.DrawWireSphere(portalPt, radDry);

                        prevPt = portalPt;
                    }

                    Debug.DrawLine(prevPt, emitterPos, colorPurple);
                }
            }
        }
    }
#endif
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.