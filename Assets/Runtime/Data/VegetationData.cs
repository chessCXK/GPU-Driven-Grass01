using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VegetationData : ScriptableObject
{
    [SerializeField]
    public int clusterCount;

    [SerializeField]
    public List<VegetationList> allObj;

    [SerializeField]
    public List<VegetationAsset> assetList;

    [HideInInspector,SerializeField]
    public byte[] clusterData;

    [SerializeField]
    public List<ClusterKindData> clusterKindData;

    [SerializeField]
    public VegetationPreDCData preDCCeils;
}

[Serializable]
public enum VegetationGrade
{
    Low = 0,
    Medium,
    High
}