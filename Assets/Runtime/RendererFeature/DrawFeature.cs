using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawInstanceDirectFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

    private DrawInstanceDirectPass m_pass;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_pass);
    }

    public override void Create()
    {
        m_pass = new DrawInstanceDirectPass(renderPassEvent);
    }
}
public class DrawInstanceDirectPass : ScriptableRenderPass
{
    private static ProfilingSampler s_profilingSampler = new ProfilingSampler("HZBDrawPass");

    public DrawInstanceDirectPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        RenderInstance(cmd, renderingData.cameraData.camera);
        //÷¥––
        context.ExecuteCommandBuffer(cmd);

        //ªÿ ’
        CommandBufferPool.Release(cmd);
    }
    private static void RenderInstance(CommandBuffer cmd, Camera camera)
    {
        HiZGlobelManager m_gManager = HiZGlobelManager.Instance;
        if (!m_gManager.IsSure)
        {
            return;
        }

        List<uint> args = new List<uint>();
        List<VegetationList> allVegetation = m_gManager.VData.allObj;
        List<VegetationAsset> assetList = m_gManager.VData.assetList;
        List<ClusterKindData> clusterKindData = m_gManager.VData.clusterKindData;

        using (new ProfilingScope(cmd, s_profilingSampler))
        {
            Shader.SetGlobalBuffer(HZBBufferName._ResultBuffer, m_gManager.ResultBuffer);
#if UNITY_EDITOR
            if (m_gManager.enableDebugBuffer || camera.name == "SceneCamera")
            {
                int argsCount = -1;
                foreach (var vegetationList in allVegetation)
                {
                    VegetationAsset asset = assetList[vegetationList.assetId];
                    for (int lodIndex = 0; lodIndex < asset.lodAsset.Count; lodIndex++)
                    {
                        var lod = asset.lodAsset[lodIndex];
                        argsCount++;
#if UNITY_EDITOR
                        lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, asset.instanceBuffer);
                        ClusterKindData cKindData = clusterKindData[vegetationList.clusterData[0].clusterKindIndex];
                        lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, cKindData.kindResultStart + vegetationList.clusterData.Count * lodIndex);

#endif
                        cmd.DrawMeshInstancedIndirect(lod.mesh, 0, lod.materialRun, 0, m_gManager.ArgsBuffer, sizeof(uint) * 5 * argsCount);
                        //break;
                    }
                }
            }
            else
#endif
            {
                VegetationCeilGather ceilGather = m_gManager.VData.preDCCeils.ceilGather;
                VegetationCeil column = ceilGather.GetCeil(camera.transform.position);
                if(column != null)
                {
                    foreach (var drawIndex in column.dcIndexList)
                    {
                        int vegetationIndex = drawIndex.x;
                        int lodIndex = drawIndex.y;

                        var vegetationList = allVegetation[vegetationIndex];
                        VegetationAsset asset = assetList[vegetationList.assetId];

                        var lod = asset.lodAsset[lodIndex];

                        ClusterKindData cKindData = clusterKindData[vegetationList.clusterData[0].clusterKindIndex];
                        int argsCount = cKindData.argsIndex + lodIndex;
#if UNITY_EDITOR
                        lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, asset.instanceBuffer);
                        lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, cKindData.kindResultStart + vegetationList.clusterData.Count * lodIndex);
#endif
                        cmd.DrawMeshInstancedIndirect(lod.mesh, 0, lod.materialRun, 0, m_gManager.ArgsBuffer, sizeof(uint) * 5 * argsCount);
                    }
                }
                
            }
        }
    }

}