using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HZBTestFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingShadows - 1;

    private HZBTestPass m_pass;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_pass);
    }

    public override void Create()
    {
        m_pass = new HZBTestPass(renderPassEvent);
    }
}
public class HZBTestPass : ScriptableRenderPass
{
    private ComputeShader m_testHZBCs;

    private int m_hzbTestCs;
    private int m_clearArgsCs;

    private int m_csMaxSize= Shader.PropertyToID("_MaxSize");
    private int m_frustumPlanes = Shader.PropertyToID("_FrustumPlanes");
    private int m_cameraPos = Shader.PropertyToID("_CameraPos");
    private int m_cameraData = Shader.PropertyToID("_CameraData");
    private int m_lastVp = Shader.PropertyToID("_LastVp");
    private int m_hzbData = Shader.PropertyToID("_HzbData");

    private ProfilingSampler m_profilingSampler = new ProfilingSampler("HZBTestPass");

    private Plane[] m_cameraPlanes = new Plane[6];//原生获得的裁剪面

    private bool isCompile = false;

    private bool enableShadow = true;
    public HZBTestPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
        isCompile = true;
    }

    private void CheckEnvironment(ref RenderingData renderingData)
    {
        if (m_testHZBCs == null)
        {
            m_testHZBCs = Resources.Load<ComputeShader>("Shader/HZBTestCs");
            m_hzbTestCs = m_testHZBCs.FindKernel("HZBTest");
            m_clearArgsCs = m_testHZBCs.FindKernel("ClearArgs");
        }
    }

    private Vector3[] GetFrustumCorners(Camera camera, float distance)
    {
        Vector3[] frustumCorners = new Vector3[4];

        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), distance, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

        for (int i = 0; i < 4; i++)
        {
            frustumCorners[i] = camera.transform.TransformPoint(frustumCorners[i]);
        }
        return frustumCorners;
    }


    public ComputeBuffer testBuffer;
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        HiZGlobelManager m_gManager = HiZGlobelManager.Instance;
        if (!m_gManager.IsSure)
        {
            return;
        }

        if (!renderingData.cameraData.camera.CompareTag("MainCamera"))
        {
            return;
        }
#if UNITY_EDITOR

        if (renderingData.cameraData.camera.name == "SceneCamera" ||
                renderingData.cameraData.camera.name == "Preview Camera")
            return;
#endif
        Camera camera = renderingData.cameraData.camera;
        CheckEnvironment(ref renderingData);

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_profilingSampler))
        {
            //清除args
            cmd.SetComputeIntParam(m_testHZBCs, m_csMaxSize, m_gManager.argsCount);
            cmd.SetComputeBufferParam(m_testHZBCs, m_clearArgsCs, HZBBufferName._ArgsBuffer, m_gManager.ArgsBuffer);
            cmd.DispatchCompute(m_testHZBCs, m_clearArgsCs, Mathf.CeilToInt(m_gManager.argsCount / 64.0f), 1, 1);

            GeometryUtility.CalculateFrustumPlanes(camera, m_cameraPlanes);

            var frustumPlanes = new Vector4[6];
            for (int i = 0; i < 6; ++i)
            {
                Plane p = m_cameraPlanes[i];
                frustumPlanes[i] = new float4(p.normal, p.distance);
            }
#if UNITY_EDITOR
            
            if (m_gManager.enableDebugBuffer)
            {
                m_gManager.DispatchComputeDebug(cmd, m_hzbTestCs, m_testHZBCs);
            }
#endif

            //测试
            cmd.SetComputeVectorArrayParam(m_testHZBCs, m_frustumPlanes, frustumPlanes);
            cmd.SetComputeVectorParam(m_testHZBCs, m_cameraPos, camera.transform.position);
            cmd.SetComputeMatrixParam(m_testHZBCs, m_lastVp, m_gManager.LastVp);
            cmd.SetComputeVectorParam(m_testHZBCs, m_cameraData, new Vector4(camera.fieldOfView, QualitySettings.lodBias, QualitySettings.maximumLODLevel, 0));
            cmd.SetComputeVectorParam(m_testHZBCs, m_hzbData, new Vector4(m_gManager.HizTexutreRT.width, m_gManager.HizTexutreRT.height, m_gManager.NumMips - 1, 0));
            cmd.SetComputeIntParam(m_testHZBCs, m_csMaxSize, m_gManager.VData.clusterCount);
            cmd.SetComputeTextureParam(m_testHZBCs, m_hzbTestCs, HZBBufferName._HizTexutreRT, m_gManager.HizTexutreRT);
            cmd.SetComputeBufferParam(m_testHZBCs, m_hzbTestCs, HZBBufferName._ClusterBuffer, m_gManager.ClusterBuffer);
            cmd.SetComputeBufferParam(m_testHZBCs, m_hzbTestCs, HZBBufferName._ClusterKindBuffer, m_gManager.ClusterKindBuffer);
            cmd.SetComputeBufferParam(m_testHZBCs, m_hzbTestCs, HZBBufferName._ArgsBuffer, m_gManager.ArgsBuffer);
            cmd.SetComputeBufferParam(m_testHZBCs, m_hzbTestCs, HZBBufferName._ResultBuffer, m_gManager.ResultBuffer);
            cmd.DispatchCompute(m_testHZBCs, m_hzbTestCs, Mathf.CeilToInt(m_gManager.VData.clusterCount / 64.0f), 1, 1);
            
        }
        //执行
        context.ExecuteCommandBuffer(cmd);

        //回收
        CommandBufferPool.Release(cmd);
    }


}