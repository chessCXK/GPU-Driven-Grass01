using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class HiZGlobelManager
{
    private bool m_isSure = false;

#if UNITY_EDITOR
    public bool DontRunHZBTest = false;

    public bool enableDebugBuffer = false;

    //激活完自动设置为false
    public bool ClearargsDebugBuffer = false;

    private ComputeShader m_testHZBCs;
    private ComputeBuffer m_visibleArgsBuffer;
    private int m_argsDebugCs = -1;
    private int m_csMaxSize = Shader.PropertyToID("_MaxSize");

    private uint[] m_ClearData;
#endif
    #region 多个Pass公用的数据
    private ComputeBuffer m_clusterBuffer;
    private ComputeBuffer m_clusterKindBuffer;

    private ComputeBuffer m_argsBuffer;

    private ComputeBuffer m_resultBuffer;

    private VegetationData m_vData;

    public int argsCount;

    public int NumMips = -1;
    public RenderTexture HizTexutreRT;
    public Matrix4x4 LastVp;
    public Vector2Int HzbSize = Vector2Int.zero;

    public void CreateComputeBuffer(VegetationData vData)
    {
        if(vData == null || vData.clusterData == null)
        {
            return;
        }
        DisposeComputeBuffer();

        this.m_vData = vData;
        m_clusterBuffer?.Release();
        m_clusterBuffer = new ComputeBuffer(vData.clusterCount, Marshal.SizeOf(typeof(ClusterData)));
        m_clusterBuffer.SetData(vData.clusterData);

        m_resultBuffer?.Release();
        m_resultBuffer = GeneateResultBuffer(vData);


        RunTimeCreateBuffer(vData);
        
        m_isSure = true;
    }
    public void DisposeComputeBuffer()
    {
        if(HizTexutreRT != null)
        {
            HizTexutreRT.Release();
            HizTexutreRT = null;
        }

        if (m_clusterBuffer != null)
        {
            m_clusterBuffer.Dispose();
            m_clusterBuffer = null;
        }

        if(m_argsBuffer != null)
        {
            m_argsBuffer.Dispose();
            m_argsBuffer = null;
        }

        if(m_resultBuffer != null)
        {
            m_resultBuffer.Dispose();
            m_resultBuffer = null;
        }

        m_isSure = false;
    }

    private ComputeBuffer GeneateResultBuffer(VegetationData assetData)
    {
        List<VegetationList> allVegetation = assetData.allObj;
        List<VegetationAsset> assetList = assetData.assetList;

        int resultNum = 0;
        for (int i = 0; i < allVegetation.Count; i++)
        {
            var vegetationList = allVegetation[i];
            VegetationAsset asset = assetList[vegetationList.assetId];

            int clusterCount = vegetationList.clusterData.Count;

            resultNum += (asset.lodAsset.Count * clusterCount);
        }

        return new ComputeBuffer(resultNum, sizeof(uint));
    }
    private void RunTimeCreateBuffer(VegetationData assetData)
    {
        List<VegetationList> allVegetation = assetData.allObj;
        List<ClusterKindData> clusterKindData = assetData.clusterKindData;
        List<VegetationAsset> assetList = assetData.assetList;

        //正常渲染args buffer
        List<uint> args = new List<uint>();
        foreach (var vegetationList in allVegetation)
        {
            VegetationAsset asset = assetList[vegetationList.assetId];
            asset.instanceBuffer?.Release();
            asset.instanceBuffer = new ComputeBuffer(vegetationList.clusterData.Count, Marshal.SizeOf(typeof(InstanceBuffer)));

            asset.instanceBuffer.SetData(vegetationList.InstanceData);
            for (int i = 0; i < asset.lodAsset.Count; i++)
            {
                var lod = asset.lodAsset[i];

                lod.materialRun = GameObject.Instantiate<Material>(lod.materialData);
                lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, asset.instanceBuffer);

                ClusterKindData cKindData = clusterKindData[vegetationList.clusterData[0].clusterKindIndex];
                lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, cKindData.kindResultStart + vegetationList.clusterData.Count * i);

                var mesh = lod.mesh;
                args.Add(mesh.GetIndexCount(0));
                args.Add(0);
                args.Add(mesh.GetIndexStart(0));
                args.Add(mesh.GetBaseVertex(0));
                args.Add(0);
            }
        }
        argsCount = args.Count / 5;
        m_argsBuffer?.Release();
        m_argsBuffer = new ComputeBuffer(argsCount, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        m_argsBuffer.SetData(args);

        //种类buffer
        m_clusterKindBuffer?.Release();
        m_clusterKindBuffer = new ComputeBuffer(assetData.allObj.Count, Marshal.SizeOf(typeof(ClusterKindData)));
        m_clusterKindBuffer.SetData(assetData.clusterKindData);

    }

    private uint[] GetArgs(VegetationLOD lod)
    {
        var mesh = lod.mesh;
        uint[] args = new uint[5];

        args[0] = mesh.GetIndexCount(0);
        args[1] = 0;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;
        return args;
    }

    #endregion

#if UNITY_EDITOR
    public void DispatchComputeDebug(UnityEngine.Rendering.CommandBuffer cmd, int hzbTestCs, ComputeShader testHZBCs)
    {
        if (m_testHZBCs == null)
        {
            m_testHZBCs = testHZBCs;
            
            m_argsDebugCs = testHZBCs.FindKernel("BakedClearArgs");
        }

        if(ClearargsDebugBuffer)
        {
            ClearargsDebugBuffer = false;

            //清除args
           /* testHZBCs.EnableKeyword("_BAKEDCODE");
            m_argsDebugCs = testHZBCs.FindKernel("BakedClearArgs");
            cmd.SetComputeIntParam(m_testHZBCs, m_csMaxSize, m_visibleArgsBuffer.count);
            cmd.SetComputeBufferParam(m_testHZBCs, m_argsDebugCs, HZBBufferName._VisibleArgsDebugBuffer, m_visibleArgsBuffer);
            cmd.DispatchCompute(m_testHZBCs, m_argsDebugCs, Mathf.CeilToInt(m_visibleArgsBuffer.count / 64.0f), 1, 1);*/
            
            if(m_ClearData != null)
            {
                m_visibleArgsBuffer.SetData(m_ClearData);
            }
        }

        if (DontRunHZBTest)
        {
            testHZBCs.DisableKeyword("_BAKEDCODE");
        }
        else
        {
            testHZBCs.EnableKeyword("_BAKEDCODE");
        }

        cmd.SetComputeBufferParam(m_testHZBCs, hzbTestCs, HZBBufferName._VisibleArgsDebugBuffer, m_visibleArgsBuffer);
    }
    public ComputeBuffer EnableDebugBuffer()
    {
        List<VegetationList> allVegetation = m_vData.allObj;
        List<VegetationAsset> assetList = m_vData.assetList;
        int argsCount = 0;

        foreach (var vegetationList in allVegetation)
        {
            VegetationAsset asset = assetList[vegetationList.assetId];
            argsCount += asset.lodAsset.Count;
        }
        m_ClearData = new uint[argsCount];
        m_visibleArgsBuffer = new ComputeBuffer(argsCount, sizeof(uint), ComputeBufferType.IndirectArguments);
        m_visibleArgsBuffer.SetData(m_ClearData);
        enableDebugBuffer = true;
        ClearargsDebugBuffer = true;
        return m_visibleArgsBuffer;
    }
    public void UnEnableDebugBuffer()
    {
        if(m_testHZBCs != null)
        {
            m_testHZBCs.DisableKeyword("_BAKEDCODE");
        }
        
        enableDebugBuffer = false;
        ClearargsDebugBuffer = false;
        m_argsDebugCs = -1;
        m_visibleArgsBuffer.Dispose();
        m_visibleArgsBuffer = null;
        m_testHZBCs = null;
        m_ClearData = null;
    }
#endif

    private static HiZGlobelManager _Instance = null;
    static public HiZGlobelManager Instance { 
        get 
        { 
            if(_Instance == null)
            {
                _Instance = new HiZGlobelManager();
            }
            return _Instance;
        }
    }
    public ComputeBuffer ClusterBuffer { get => m_clusterBuffer; }
    public ComputeBuffer ArgsBuffer { get => m_argsBuffer; }
    public ComputeBuffer ResultBuffer { get => m_resultBuffer; }
    public bool IsSure { get { return m_isSure && m_vData != null && HizTexutreRT != null; } }
    public VegetationData VData { get => m_vData; }
    public ComputeBuffer ClusterKindBuffer { get => m_clusterKindBuffer;}
}
