using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RunBakedEditor
{
    static Vector3Int[] s_axisMuti =
    {
        new Vector3Int(1, 1, 1),
        new Vector3Int(1, 1, -1),
        new Vector3Int(1, -1, 1),
        new Vector3Int(1, -1, -1),
        new Vector3Int(-1, 1, 1),
        new Vector3Int(-1, 1, -1),
        new Vector3Int(-1, -1, 1),
        new Vector3Int(-1, -1, -1)
    };
    public static void RunBakeDCCeil(VegetationData runtimeData)
    {
        HiZDataLoader loader = GameObject.FindObjectOfType<HiZDataLoader>();
        if (loader)
        {
            loader.data = runtimeData;
            loader.OnEnable();
        }

        VegetationCeilGather ceilGather = runtimeData.preDCCeils.ceilGather;
        ComputeBuffer compueBuffer = HiZGlobelManager.Instance.EnableDebugBuffer();
        Camera mainCamera = Camera.main;
        var lastPos = mainCamera.transform.position;

        for (int i = 0; i < ceilGather.vegetationCeilRow.Length; i++)
        {
            var row = ceilGather.vegetationCeilRow[i];
            for(int j = 0; j < row.ceilColumn.Length; j++)
            {
                float slider = (i * row.ceilColumn.Length + j) / (float)(ceilGather.vegetationCeilRow.Length * row.ceilColumn.Length);
                EditorUtility.DisplayProgressBar("BakedCeil", "Bakeing Ceil", slider);

                var column = row.ceilColumn[j];

                HiZGlobelManager.Instance.ClearargsDebugBuffer = true;
                Bounds bound = column.bound;
                for (uint z = 0; z < 8; z++)
                {
                    Vector3 pos = bound.center + new Vector3(bound.extents.x * s_axisMuti[z].x, bound.extents.y * s_axisMuti[z].y, bound.extents.z * s_axisMuti[z].z);
                    //ceil的8个点
                    RenderOnceFrame(mainCamera, pos, compueBuffer);
                }
                
                uint[] data = new uint[compueBuffer.count];
                compueBuffer.GetData(data);
                foreach (var argsIndex in data)
                {
                    if(argsIndex == 0)
                    {
                        continue;
                    }

                    int vegetationIndex = (int)argsIndex / 100;
                    int lodIndex = (int)argsIndex % 100 - 1;
                    column.dcIndexList.Add(new Vector2Int(vegetationIndex, lodIndex));
                }
            }
        }
        EditorUtility.ClearProgressBar();
        mainCamera.transform.position = lastPos;
        HiZGlobelManager.Instance.UnEnableDebugBuffer();
    }
    public static void RenderOnceFrame(Camera camera, Vector3 position, ComputeBuffer compueBuffer)
    {
        float lasetFOV = camera.fieldOfView;
        Quaternion lastRotation = camera.transform.rotation;
        camera.transform.position = position;

        for(int i = 0; i < 4;i++)
        {
            camera.transform.rotation = Quaternion.Euler(0, 90 * i, 0);
            HiZGlobelManager.Instance.DontRunHZBTest = true;
            camera.Render();//先渲染一帧，确保生成了深度图

            uint[] data = new uint[compueBuffer.count];
            compueBuffer.GetData(data);

            HiZGlobelManager.Instance.DontRunHZBTest = false;
            camera.Render();
        }
        
        /*camera.transform.rotation = Quaternion.Euler(90, 0, 0);
        HiZGlobelManager.Instance.DontRunHZBTest = true;
        camera.Render();
        HiZGlobelManager.Instance.DontRunHZBTest = false;
        camera.Render();

        camera.transform.rotation = Quaternion.Euler(-90, 0, 0);
        HiZGlobelManager.Instance.DontRunHZBTest = true;
        camera.Render();
        HiZGlobelManager.Instance.DontRunHZBTest = false;
        camera.Render();*/

        camera.transform.rotation = lastRotation;
    }
}
