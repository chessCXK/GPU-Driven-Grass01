using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Serialization;

public static class HZBBufferName
{
    public static int _InstanceBuffer = Shader.PropertyToID("_InstanceBuffer");

    public static int _ClusterBuffer = Shader.PropertyToID("_ClusterBuffer");

    public static int _ClusterKindBuffer = Shader.PropertyToID("_ClusterKindBuffer");

    public static int _ArgsBuffer = Shader.PropertyToID("_ArgsBuffer");

    public static int _ResultBuffer = Shader.PropertyToID("_ResultBuffer");

    public static int _HizTexutreRT = Shader.PropertyToID("_HizTexutreRT");

    public static int _VisibleArgsDebugBuffer = Shader.PropertyToID("_VisibleArgsIndexBuffer");

}

public static class HZBMatParameterName
{

    public static int _ResultOffset = Shader.PropertyToID("_ResultOffset");

}
