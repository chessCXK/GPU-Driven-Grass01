using System;
using UnityEngine;

[Serializable]
public struct InstanceBuffer
{
    public Matrix4x4 worldMatrix;
    public Matrix4x4 worldInverseMatrix;
    public InstanceBuffer(Matrix4x4 worldMatrix)
    {
        this.worldMatrix = worldMatrix;
        this.worldInverseMatrix = worldMatrix.inverse;
    }
}

[Serializable]
public struct ClusterData
{
    public Vector3 center;
    public Vector3 extends;
    //当前类型cluster中的位置
    public int clusterIndex;

    public int clusterKindIndex;
    public ClusterData(Bounds bound)
    {
        center = bound.center;
        extends = bound.extents;
        clusterIndex = clusterKindIndex = -1;
    }

    public ClusterData(ClusterData other, int clusterIndex, int clusterKindIndex)
    {
        center = other.center;
        extends = other.extends;
        this.clusterIndex = clusterIndex;
        this.clusterKindIndex = clusterKindIndex;
    }
}

[Serializable]
public struct ClusterKindData
{

    public int argsIndex;

    //该种类型的result起始位置
    public int kindResultStart;

    //有多少LOD
    public int lodNum;

    //该类型的Cluster有多少个
    public int elementNum;

    //只运行4级LOD
    public Vector4 lodRelative;


    public ClusterKindData(int argsIndex, int kindResultStart, int elementNum, int lodNum, Vector4 lodRelative, int shadowLODLevel)
    {
        this.argsIndex = argsIndex;
        this.kindResultStart = kindResultStart;
        this.elementNum = elementNum;
        this.lodNum = lodNum;
        this.lodRelative = lodRelative;
    }
}


