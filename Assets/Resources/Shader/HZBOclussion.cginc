#ifndef HZBOCLUSSION
#define HZBOCLUSSION

Texture2D<half> _HizTexutreRT;
SamplerState sampler_HizTexutreRT;

half4x4 _LastVp;

//x:hzbWidth, y hzbHeight, z: numMips
half4 _HzbData;

half HizCull_4x4(half3 center, half3 extents)
{
#ifdef UNITY_REVERSED_Z
    half minZ = 1;
#else
    half minZ = -1;
#endif
    half2 minPos = 1, maxPos = -1;

    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        half3 pos = center + extents * axisMuti[i];
        half4 clipPos = mul(_LastVp, half4(pos, 1));
        clipPos /= clipPos.w;
        minPos = min(clipPos.xy, minPos);
        maxPos = max(clipPos.xy, maxPos);

#ifdef UNITY_REVERSED_Z
        minZ = min(minZ, clipPos.z);
#else
        minZ = max(minZ, clipPos.z);
#endif

    }

    [branch]
    if (minZ >= 1)
    {
        return 0;
    }

    half4 boxUVs = half4(minPos, maxPos);
    boxUVs = saturate(boxUVs * 0.5 + 0.5);

    half4 rectPixels = boxUVs * _HzbData.xyxy;
    half2 rectPixelSize = (rectPixels.zw - rectPixels.xy) * 0.5;	// 0.5 for 4x4

    half maxAxis = max(rectPixelSize.x, rectPixelSize.y);
    half mipLevel = ceil(log2(maxAxis));
    mipLevel = min(mipLevel, _HzbData.z);
    half lowerMipLevel = max(mipLevel - 1, 0);

    half scale = exp2(-lowerMipLevel) * _HzbData.xy;

    half2 left = floor(boxUVs.xy * scale);
    half2 right = ceil(boxUVs.zw * scale);
    half2 lowerRect = right - left;
    if (lowerRect.x <= 4 && lowerRect.y <= 4)
    {
        mipLevel = lowerMipLevel;
    }

#if !UNITY_UV_STARTS_AT_TOP
    boxUVs = half4(boxUVs.x, 1 - boxUVs.y, boxUVs.z, 1 - boxUVs.w);
#endif

    // 4x4 samples
    half2 Scale = 0.5 * (boxUVs.zw - boxUVs.xy) / 3;

    half2 Bias = boxUVs.xy;
    half4 minDepth = 1;
    {
        [unroll]
        for (int i = 0; i < 4; i++)
        {
            half4 depth;
            depth.x = _HizTexutreRT.SampleLevel(sampler_HizTexutreRT, half2(i, 0) * Scale + Bias, mipLevel).r;
            depth.y = _HizTexutreRT.SampleLevel(sampler_HizTexutreRT, half2(i, 1) * Scale + Bias, mipLevel).r;
            depth.z = _HizTexutreRT.SampleLevel(sampler_HizTexutreRT, half2(i, 2) * Scale + Bias, mipLevel).r;
            depth.w = _HizTexutreRT.SampleLevel(sampler_HizTexutreRT, half2(i, 3) * Scale + Bias, mipLevel).r;
            minDepth = min(minDepth, depth);
        }
    }

#ifdef UNITY_REVERSED_Z
    minDepth.xy = max(minDepth.xy, minDepth.zw);
    minDepth.x = max(minDepth.x, minDepth.y);
    return minZ > minDepth.x;
#else

    minDepth.xy = min(minDepth.xy, minDepth.zw);
    minDepth.x = min(minDepth.x, minDepth.y);

    return minZ < minDepth.x;
#endif
}

half HizCull_2x2(half3 center, half3 extents)
{
#ifdef UNITY_REVERSED_Z
    half minZ = 1;
#else
    half minZ = -1;
#endif
    half2 minPos = 1, maxPos = -1;

    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        half3 pos = center + extents * axisMuti[i];
        half4 clipPos = mul(_LastVp, half4(pos, 1));
        clipPos /= clipPos.w;
        minPos = min(clipPos.xy, minPos);
        maxPos = max(clipPos.xy, maxPos);

#ifdef UNITY_REVERSED_Z
        minZ = min(minZ, clipPos.z);
#else
        minZ = max(minZ, clipPos.z);
#endif

    }

    half4 boxUVs = half4(minPos, maxPos);
    boxUVs = saturate(boxUVs * 0.5 + 0.5);

    half2 rectPixelSize = (boxUVs.zw - boxUVs.xy) * _HzbData.xy;
    half maxAxis = max(rectPixelSize.x, rectPixelSize.y);
    half mipLevel = ceil(log2(maxAxis));
    mipLevel = min(mipLevel, _HzbData.z);
    half lowerMipLevel = max(mipLevel - 1, 0);

    half scale = exp2(-lowerMipLevel) * _HzbData.xy;

    half2 left = floor(boxUVs.xy * scale);
    half2 right = ceil(boxUVs.zw * scale);
    half2 lowerRect = right - left;
    if (lowerRect.x <= 2 && lowerRect.y <= 2)
    {
        mipLevel = lowerMipLevel;
    }

#if UNITY_UV_STARTS_AT_TOP
    boxUVs = half4(boxUVs.x, 1 - boxUVs.y, boxUVs.z, 1 - boxUVs.w);
#endif

    half4 depth = half4(_HizTexutreRT.SampleLevel(sampler_HizTexutreRT, boxUVs.xy, mipLevel),
        _HizTexutreRT.SampleLevel(sampler_HizTexutreRT, boxUVs.xw, mipLevel),
        _HizTexutreRT.SampleLevel(sampler_HizTexutreRT, boxUVs.zy, mipLevel),
        _HizTexutreRT.SampleLevel(sampler_HizTexutreRT, boxUVs.zw, mipLevel));

#ifdef UNITY_REVERSED_Z
    depth.xy = max(depth.xy, depth.zw);
    depth.x = max(depth.x, depth.y);
    return minZ > depth.x;
#else

    depth.xy = min(depth.xy, depth.zw);
    depth.x = min(depth.x, depth.y);
    return minZ < depth.x;
#endif
}

#endif