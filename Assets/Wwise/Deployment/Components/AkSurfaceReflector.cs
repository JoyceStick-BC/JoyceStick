#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Wwise/AkSurfaceReflector")]
[DisallowMultipleComponent]
///@brief This component will convert the triangles of the GameObject's geometry into sound reflective surfaces.
///@details This component requires a Mesh Filter component. The triangles of the mesh will be sent to the Spatial Audio wrapper by calling SpatialAudio::AddGeometrySet(). The triangles will reflect the sound emitted from AkSpatialAudioEmitter components.
[RequireComponent(typeof(MeshFilter))]
public class AkSurfaceReflector : MonoBehaviour
{
    /// All triangles of the component's mesh will be applied with this texture. The texture will change the filter parameters of the sound reflected from this component.
    public AK.Wwise.AcousticTexture AcousticTexture;

    private MeshFilter MeshFilter;

    /// <summary>
    /// Sends the mesh filter's triangles and their acoustic texture to Spatial Audio
    /// </summary>
    /// <param name="acousticTexture"></param>
    /// <param name="meshFilter"></param>
    static public void AddGeometrySet(AK.Wwise.AcousticTexture acousticTexture, MeshFilter meshFilter)
    {
        if (meshFilter == null)
        {
            Debug.Log(meshFilter.name + ": No mesh found!");
        }
        else
        {
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            int count = mesh.triangles.Length / 3;
            using (AkTriangleArray triangleArray = new AkTriangleArray(count))
            {
                for (int i = 0; i < count; ++i)
                {
                    using (var triangle = triangleArray.GetTriangle(i))
                    {
                        Vector3 point0 = meshFilter.transform.TransformPoint(vertices[triangles[3 * i + 0]]);
                        Vector3 point1 = meshFilter.transform.TransformPoint(vertices[triangles[3 * i + 1]]);
                        Vector3 point2 = meshFilter.transform.TransformPoint(vertices[triangles[3 * i + 2]]);

                        triangle.point0.X = point0.x;
                        triangle.point0.Y = point0.y;
                        triangle.point0.Z = point0.z;

                        triangle.point1.X = point1.x;
                        triangle.point1.Y = point1.y;
                        triangle.point1.Z = point1.z;

                        triangle.point2.X = point2.x;
                        triangle.point2.Y = point2.y;
                        triangle.point2.Z = point2.z;

                        triangle.textureID = (uint)acousticTexture.ID;
                        triangle.reflectorChannelMask = unchecked((uint)(-1));

                        triangle.strName = meshFilter.gameObject.name + "_" + i;
                    }
                }

                AkSoundEngine.SetGeometry((ulong)meshFilter.GetInstanceID(), triangleArray, (uint)count);
            }
        }
    }

    /// <summary>
    /// Remove the corresponding mesh filter's geometry from Spatial Audio.
    /// </summary>
    /// <param name="meshFilter"></param>
    static public void RemoveGeometrySet(MeshFilter meshFilter)
    {
        if (meshFilter != null)
        {
            AkSoundEngine.RemoveGeometry((ulong)meshFilter.GetInstanceID());
        }
    }

    private void Awake()
    {
        MeshFilter = GetComponent<MeshFilter>();
    }

    private void OnEnable()
    {
        AddGeometrySet(AcousticTexture, MeshFilter);
    }

    private void OnDisable()
    {
        RemoveGeometrySet(MeshFilter);
    }
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.