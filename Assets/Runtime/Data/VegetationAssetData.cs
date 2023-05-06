using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class VegetationList
{
    [SerializeField]
    public int assetId;

    [HideInInspector, SerializeField]
    public List<InstanceBuffer> InstanceData = new List<InstanceBuffer>();

    [SerializeField]
    public List<ClusterData> clusterData = new List<ClusterData>();

    public void AddData(Matrix4x4 worldMatrix, ClusterData clusterData)
    {
        InstanceBuffer instance = new InstanceBuffer(worldMatrix);
        this.InstanceData.Add(instance);
        this.clusterData.Add(clusterData);
    }
}

[Serializable]
public class VegetationLOD
{
    [SerializeField] [FormerlySerializedAs("mesh")] 
    public Mesh mesh;

    //todo
    [SerializeField] [FormerlySerializedAs("materialPath")] 
    public Material materialData;

    [HideInInspector,NonSerialized]
    public Material materialRun;
}

/// <summary>
/// 植被资源
/// </summary>
[Serializable]
public class VegetationAsset
{
    public int id;
    public List<VegetationLOD> lodAsset = new List<VegetationLOD>();

    [HideInInspector, NonSerialized]
    public int lodLevel;
    [HideInInspector, NonSerialized]
    public int lodLevelLow;

    //阴影到LOD第几级, -1表示没阴影;
    public int shadowLODLevel;

    [NonSerialized]
    public ComputeBuffer instanceBuffer;

#if UNITY_EDITOR
    public Vector4 lodRelative;

    
    public VegetationAsset(int id, LODGroup lodGroup, int shadowLODLevel)
    {
        this.id = id;
        this.shadowLODLevel = shadowLODLevel;
        LOD[] lods = lodGroup.GetLODs();
        if (lods.Length == 0)
        {
            return;
        }
        int i = 0;
        foreach(var lod in lods)
        {
            Renderer[] rds = lod.renderers;
            if (rds.Length == 0)
            {
                return;
            }

            switch(i)
            {
                case 0:
                    lodRelative.x = lod.screenRelativeTransitionHeight;
                    break;
                case 1:
                    lodRelative.y = lod.screenRelativeTransitionHeight;
                    break;
                case 2:
                    lodRelative.z = lod.screenRelativeTransitionHeight;
                    break;
                case 3:
                    lodRelative.w = lod.screenRelativeTransitionHeight;
                    break;
            }
            i++;
            Renderer rd = rds[0];
            MeshFilter meshfilter = rd.GetComponent<MeshFilter>();

            VegetationLOD vLOD = new VegetationLOD();
            vLOD.mesh = meshfilter.sharedMesh;
            vLOD.materialData = rd.sharedMaterial;
            lodAsset.Add(vLOD);
        }
    }
#endif
}
