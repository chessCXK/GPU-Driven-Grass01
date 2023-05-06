using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BuildHZBFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    private BuildHZBPass m_pass;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_pass);
    }

    public override void Create()
    {
        m_pass = new BuildHZBPass(renderPassEvent);
    }
}
public class BuildHZBPass : ScriptableRenderPass
{
    private Material m_buildHZBMat;
    private ComputeShader m_buildHZBCs;

    private int m_csBuildHZB;
    private int m_csScrTexture = Shader.PropertyToID("scrTexture");
    private int m_csDestTexture = Shader.PropertyToID("destTexture");
    private int m_csDepthRTSize = Shader.PropertyToID("_depthRTSize");

    private ProfilingSampler m_profilingSampler = new ProfilingSampler("BuildHZBPass");

    public BuildHZBPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
    }

    private (int, int) GetDepthRTWidthFromScreen(int screenWidth)
    {
        if (screenWidth >= 2048)
        {
            return (1024, 10);
        }
        else if (screenWidth >= 1024)
        {
            return (512, 9);
        }
        else
        {
            return (256, 8);
        }
    }
    //降低一级，理由是远处几乎不需要被裁剪
    private Vector2Int GetHizSizeFromScreen(int screenWidth, int screenHeight, out int mips)
    {
        int NumMipsX = Mathf.Max(Mathf.CeilToInt(Mathf.Log(screenWidth, 2)) - 1, 1);
        int NumMipsY = Mathf.Max(Mathf.CeilToInt(Mathf.Log(screenHeight, 2)) - 1, 1);
        Vector2Int hzbSize = new Vector2Int(1 << NumMipsX, 1 << NumMipsY);
        mips = Mathf.Max(NumMipsX, NumMipsY);
        return hzbSize;
    }
    private void CheckEnvironment(ref RenderingData renderingData)
    {
        HiZGlobelManager m_gManager = HiZGlobelManager.Instance;
        var proj = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, true);
        Matrix4x4 lastVp = proj * renderingData.cameraData.camera.worldToCameraMatrix;
        m_gManager.LastVp = lastVp;

        if (m_buildHZBMat == null)
        {
            m_buildHZBMat = new Material(Shader.Find("Hidden/BuildHIZ"));
        }
        if (m_buildHZBCs == null)
        {
            m_buildHZBCs = Resources.Load<ComputeShader>("Shader/BuildHIZCs");
            m_csBuildHZB = m_buildHZBCs.FindKernel("BuildHZB");
        }

        var hizTexutreRT = m_gManager.HizTexutreRT;
        Vector2Int hzbSize = GetHizSizeFromScreen(renderingData.cameraData.camera.pixelWidth, renderingData.cameraData.camera.pixelHeight, out m_gManager.NumMips);
        if (hzbSize == m_gManager.HzbSize && hizTexutreRT != null)
        {
            return;
        }
        m_gManager.HzbSize = hzbSize;

        if (hizTexutreRT != null)
        {
            hizTexutreRT.Release();
        }

        hizTexutreRT = new RenderTexture(hzbSize.x, hzbSize.y, 0, RenderTextureFormat.RHalf, m_gManager.NumMips);
        hizTexutreRT.name = "HizDepthRT";
        hizTexutreRT.useMipMap = true;
        hizTexutreRT.autoGenerateMips = false;
        hizTexutreRT.enableRandomWrite = true;
        hizTexutreRT.wrapMode = TextureWrapMode.Clamp;
        hizTexutreRT.filterMode = FilterMode.Point;
        hizTexutreRT.Create();

        m_gManager.HizTexutreRT = hizTexutreRT;
    }
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        if (!camera.CompareTag("MainCamera"))
        {
            return;
        }
#if UNITY_EDITOR

        if (renderingData.cameraData.camera.name == "SceneCamera" ||
                renderingData.cameraData.camera.name == "Preview Camera")
            return;
#endif

        CheckEnvironment(ref renderingData);

        HiZGlobelManager m_gManager = HiZGlobelManager.Instance;
        if (!m_gManager.IsSure)
        {
            return;
        }

        bool bUseCompute = false;
        int maxMipBatchSize = bUseCompute ? 1 : 1;

        var hizTexutreRT = m_gManager.HizTexutreRT;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_profilingSampler))
        {
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetRenderTarget(hizTexutreRT, 0);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_buildHZBMat, 0, 0);

            RenderTexture tempRT = null;
            if (!bUseCompute)
            {
                // 创建一个临时的RenderTexture对象
                tempRT = RenderTexture.GetTemporary(hizTexutreRT.descriptor);
            }

            for (int startMip = maxMipBatchSize; startMip < m_gManager.NumMips; startMip += maxMipBatchSize)
            {
                float destMip = 1 << startMip;
                Vector4 srcSize = new Vector4(Mathf.Round(m_gManager.HzbSize.x / destMip), Mathf.Round(m_gManager.HzbSize.y / destMip), 0, 0);
                if (bUseCompute)
                {
                    cmd.SetComputeVectorParam(m_buildHZBCs, m_csDepthRTSize, srcSize);
                    cmd.SetComputeTextureParam(m_buildHZBCs, m_csBuildHZB, m_csScrTexture, hizTexutreRT, startMip - 1);
                    cmd.SetComputeTextureParam(m_buildHZBCs, m_csBuildHZB, m_csDestTexture, hizTexutreRT, startMip);
                    cmd.DispatchCompute(m_buildHZBCs, m_csBuildHZB, (int)srcSize.x, (int)srcSize.y, 1);
                }
                else
                {
                    cmd.SetGlobalFloat("_MipLevel", startMip - 1);
                    cmd.SetGlobalTexture("_HizDepthRT", hizTexutreRT);
                    cmd.SetRenderTarget(tempRT, startMip - 1);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_buildHZBMat, 0, 2);

                    srcSize = new Vector4(1 / srcSize.x, 1 / srcSize.y, 0, 0);
                    cmd.SetGlobalVector("_SrcSize", srcSize);
                    cmd.SetGlobalTexture("_HizDepthRT", tempRT);
                    cmd.SetRenderTarget(hizTexutreRT, startMip);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_buildHZBMat, 0, 1);
                }
            }

            if (tempRT != null)
            {
                RenderTexture.ReleaseTemporary(tempRT);
            }
        }
        //执行
        context.ExecuteCommandBuffer(cmd);

        //回收
        CommandBufferPool.Release(cmd);
    }


}