using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GenerateTool
{
    //distanceCulls必须是3个元素
    public static VegetationPreDCData BuildDCCeil(Bounds maxBounds, int ceilSize)
    {
        int xNum = Mathf.CeilToInt((maxBounds.extents.x * 2) / ceilSize);
        int zNum = Mathf.CeilToInt((maxBounds.extents.z * 2) / ceilSize);

        VegetationPreDCData preDcCeil = new VegetationPreDCData(xNum, zNum, ceilSize, maxBounds);
        VegetationCeilGather ceilGather = preDcCeil.ceilGather;

        Bounds[][] allBounds = new Bounds[xNum][];
        Bounds[][] realityBounds = new Bounds[xNum][];
        

        for (int index = 0; index < xNum; index++)
        {
            allBounds[index] = new Bounds[zNum];
            realityBounds[index] = new Bounds[zNum];
        }

        float xExtend = maxBounds.extents.x / xNum;
        float zExtend = maxBounds.extents.z / zNum;

        Vector3 startPos = maxBounds.center - maxBounds.extents + new Vector3(xExtend, maxBounds.extents.y, zExtend);
        float orginZ = startPos.z;

        //分块构建bound
        for (int r = 1; r <= xNum; r++)
        {
            startPos.z = orginZ;
            for (int l = 1; l <= zNum; l++)
            {
                allBounds[r - 1][l - 1] = new Bounds(startPos, new Vector3(xExtend * 2, maxBounds.extents.y * 2, zExtend * 2));
                startPos.z += zExtend * 2;

                realityBounds[r - 1][l - 1] = new Bounds();

                ceilGather[r - 1][l - 1] = new VegetationCeil(allBounds[r - 1][l - 1]);
            }
            startPos.x += xExtend * 2;
        }
        return preDcCeil;
    }

    public static List<LODGroup> ConvertTerrainData(Terrain terrain)
    {
        List<LODGroup> lodGroups = new List<LODGroup>();
        if (terrain)
        {
            var terrainData = terrain.terrainData;

            foreach (var treeInstance in terrainData.treeInstances)
            {
                var prop = terrainData.treePrototypes[treeInstance.prototypeIndex];
                var inst = PrefabUtility.InstantiatePrefab(prop.prefab) as GameObject;
                inst.transform.position = new Vector3(treeInstance.position.x * terrainData.size.x, treeInstance.position.y * terrainData.size.y, treeInstance.position.z * terrainData.size.z);
                //inst.transform.rotation = inst.transform.rotation * Quaternion.AngleAxis(Mathf.Rad2Deg * treeInstance.rotation, Vector3.up);
                inst.transform.localScale += new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale);
                LODGroup lodGroup = inst.GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    lodGroups.Add(lodGroup);
                }
            }

            // 清空地表tree
            /*terrainData.treeInstances = Array.Empty<TreeInstance>();
            terrainData.treePrototypes = Array.Empty<TreePrototype>();*/

            var patchCount = Mathf.Ceil((float)terrainData.detailResolution / terrainData.detailResolutionPerPatch);
            var terrainPosOffset = GameObject.FindObjectOfType<Terrain>().transform.position;
            for (int layer = 0; layer < terrainData.detailPrototypes.Length; layer++)
            {
                var layerProp = terrainData.detailPrototypes[layer];
                for (int i = 0; i < patchCount; i++)
                {
                    for (int j = 0; j < patchCount; j++)
                    {
                        var insts = terrainData.ComputeDetailInstanceTransforms(i, j, layer, 1, out Bounds bounds);
                        foreach (var inst in insts)
                        {
                            var prefab = PrefabUtility.InstantiatePrefab(layerProp.prototype) as GameObject;
                            prefab.transform.position = new Vector3(inst.posX + terrainPosOffset.x, inst.posY + terrainPosOffset.y, inst.posZ + terrainPosOffset.z);
                            prefab.transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * inst.rotationY, Vector3.up);
                            prefab.transform.localScale = new Vector3(inst.scaleXZ, inst.scaleY, inst.scaleXZ);
                            LODGroup lodGroup = prefab.GetComponent<LODGroup>();
                            if (lodGroup != null)
                            {
                                lodGroups.Add(lodGroup);
                            }
                        }
                    }
                }

                /*// 清空地表Details
                // Get all of layer zero.
                var map = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, layer);

                // For each pixel in the detail map...
                for (int y = 0; y < terrainData.detailHeight; y++)
                {
                    for (int x = 0; x < terrainData.detailWidth; x++)
                    {
                        map[x, y] = 0;
                    }
                }
                // Assign the modified map back.
                terrainData.SetDetailLayer(0, 0, 0, map);*/
            }

            /*// 清空地表Details
            terrainData.detailPrototypes = Array.Empty<DetailPrototype>();
            terrainData.RefreshPrototypes();*/
        }
        return lodGroups;
    }

}
