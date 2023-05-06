using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public class GenerateHizDataEditor
{
    static DebugDrawScene s_debugScrpit = null;
    static string runtimeDataDir = "Assets";

    static int s_ceilSize = 16;

    [MenuItem("Chess/Build")]

    public static void Test()
    {
        var runtimeDataPath = $"{runtimeDataDir}/VegetationData.asset";
        VegetationData runtimeData = AssetDatabase.LoadAssetAtPath<VegetationData>(runtimeDataPath);
        if (runtimeData == null)
        {
            runtimeData = ScriptableObject.CreateInstance<VegetationData>();
            if (!Directory.Exists(runtimeDataDir))
            {
                Directory.CreateDirectory(runtimeDataDir);
            }
            AssetDatabase.CreateAsset(runtimeData, runtimeDataPath);
        }
        UpdateVegetationAreaDataAsset(runtimeData);
        HiZDataLoader loader = GameObject.FindObjectOfType<HiZDataLoader>();
        if(loader)
        {
            loader.data = runtimeData;
            loader.OnEnable();
        }
        EditorUtility.SetDirty(runtimeData);
    }
    public static bool UpdateVegetationAreaDataAsset(VegetationData assetData)
    {
        assetData.allObj = new List<VegetationList>();
        assetData.assetList = new List<VegetationAsset>();
        Dictionary<Mesh, int> editorAssetsIndex = new Dictionary<Mesh, int>();
        var terrain = GameObject.FindObjectOfType<Terrain>();
        List<LODGroup> lodgs = GenerateTool.ConvertTerrainData(terrain);
        //return false;
        if (lodgs.Count == 0)
        {
            return false;
        }
        Bounds maxBounds = new Bounds();
        int i = 0;
        foreach (var lodg in lodgs)
        {
            LOD[] lods = lodg.GetLODs();
            if (lods.Length == 0)
            {
                continue;
            }
            Renderer[] rds = lods[0].renderers;
            if (rds.Length == 0)
            {
                continue;
            }
            Renderer rd = rds[0];
            MeshFilter meshfilter = rd.GetComponent<MeshFilter>();
            Mesh mesh = meshfilter.sharedMesh;
            int index = 0;
            if (!editorAssetsIndex.TryGetValue(mesh, out index))
            {
                index = editorAssetsIndex.Count;

                int lodLevel = -1;
                for(int j = 0; j < lods.Length; j++)
                {
                    var lod = lods[j];
                    if(lods[0].renderers.Length == 0 || lods[0].renderers[0] == null)
                    {
                        continue;
                    }
                    if(lods[0].renderers[0].shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                    {
                        lodLevel = j;
                        break;
                    }
                }
                
                VegetationAsset vAsset = new VegetationAsset(index, lodg, lodLevel);

                assetData.assetList.Add(vAsset);

                editorAssetsIndex.Add(mesh, index);
            }

            if (i == 0)
            {
                maxBounds = rd.bounds;
            }
            else
            {
                maxBounds.Encapsulate(rd.bounds);
            }
            i++;
        }

        int count = 0;
        foreach (var lodg in lodgs)
        {
            if(AddVegetation(assetData.allObj, editorAssetsIndex, lodg))
            {
                count++;
            }
        }
        assetData.clusterCount = count;

        GeneateRenderData(assetData);

        assetData.preDCCeils = GenerateTool.BuildDCCeil(terrain.terrainData.bounds, s_ceilSize);
        DebugDrawScene  debugDraw = FindDebugDrawScenea();
        debugDraw.preDCCeils = assetData.preDCCeils;
        debugDraw.b = maxBounds;

        RunBakedEditor.RunBakeDCCeil(assetData);

        foreach (var item in lodgs)
        {
            GameObject.DestroyImmediate(item.gameObject);
        }
        return true;
    }

    public static DebugDrawScene FindDebugDrawScenea()
    {
        if (s_debugScrpit != null)
        {
            return s_debugScrpit;
        }
        s_debugScrpit = GameObject.FindObjectOfType<DebugDrawScene>();
        if (s_debugScrpit == null)
        {
            var obj = new GameObject();
            obj.name = "!DebugScrpitObject";
            s_debugScrpit = obj.AddComponent<DebugDrawScene>();
        }
        return s_debugScrpit;
    }
    private static bool AddVegetation(List<VegetationList> allObjMatrix, Dictionary<Mesh, int> editorAssetsIndex, LODGroup lodGroup)
    {
        LOD[] lods = lodGroup.GetLODs();

        if (lods.Length == 0)
        {
            return false;
        }
        Renderer[] rds = lods[0].renderers;
        if (rds.Length == 0)
        {
            return false;
        }

        MeshFilter meshfilter = rds[0].GetComponent<MeshFilter>();
        Mesh mesh = meshfilter.sharedMesh;
        if (!editorAssetsIndex.TryGetValue(mesh, out var index))
        {
            return false;
        }
        VegetationList vegetation = allObjMatrix.Find(t => t.assetId == index);
        if(vegetation == null)
        {
            vegetation = new VegetationList();
            vegetation.assetId = index;
            allObjMatrix.Add(vegetation);
        }

        Matrix4x4 matrix = Matrix4x4.TRS(lodGroup.transform.position, lodGroup.transform.rotation, lodGroup.transform.localScale);
        ClusterData cData = new ClusterData(rds[0].bounds);

        vegetation.AddData(matrix, cData);
        return true;
    }

    private static void GeneateRenderData(VegetationData assetData)
    {
        List<VegetationList> allVegetation = assetData.allObj;
        List<VegetationAsset> assetList = assetData.assetList;

        var clusters = new NativeArray<ClusterData>(assetData.clusterCount, Allocator.Temp);
        var clusterKindData = new List<ClusterKindData>();
        int clusterOffset = 0;
        int kindResultStart = 0;
        int argsIndex = 0;
        for (int i = 0; i < allVegetation.Count; i++)
        {
            var vegetationList = allVegetation[i];
            VegetationAsset asset = assetList[vegetationList.assetId];

            int clusterCount = vegetationList.clusterData.Count;
            for (int j = 0; j < clusterCount; j++)
            {
                vegetationList.clusterData[j] = new ClusterData(vegetationList.clusterData[j], j, i);
                
            }
            clusterKindData.Add(new ClusterKindData(argsIndex, kindResultStart, clusterCount, asset.lodAsset.Count, asset.lodRelative, asset.shadowLODLevel));
            NativeArray<ClusterData>.Copy(vegetationList.clusterData.ToArray(), 0, clusters, clusterOffset, clusterCount);

            argsIndex += asset.lodAsset.Count;
            clusterOffset += clusterCount;
            kindResultStart += (asset.lodAsset.Count * clusterCount);
        }
        assetData.clusterData = clusters.ToRawBytes();
        assetData.clusterKindData = clusterKindData;
        

    }
}
