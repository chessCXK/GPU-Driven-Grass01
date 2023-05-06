Shader "Unlit/outline"
{
    Properties
    {
        _BaseMap("Base Texture", 2D) = "white"{}
        _BaseColor("Color", color) = (0, 0, 0, 0)
    }
        SubShader
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
            }
            Cull Back

            Pass
            {
                // 这个是urp的默认渲染pass，里面可以处理多光源，自发光，以及环境光和雾等等。
                Tags{"LightMode" = "UniversalForward"}

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #pragma target 4.5
                #pragma multi_compile_instancing
                #pragma instancing_options procedural:setup

                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
               

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD;
                    float3 normal : NORMAL;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };
                struct Varings
                {
                    float4 positionCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float3 positionWS : TEXCOORD1;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct InstanceBuffer
                {
                    float4x4 worldMatrix;
                    float4x4 worldInverseMatrix;
                };

                float _ResultOffset;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<InstanceBuffer> _InstanceBuffer;
                StructuredBuffer<uint> _ResultBuffer;
#endif
                

                void setup()
                {
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint instanceIndex = _ResultBuffer[_ResultOffset + unity_InstanceID];
                    InstanceBuffer instanceData = _InstanceBuffer[instanceIndex];
                    unity_ObjectToWorld = instanceData.worldMatrix;
                    unity_WorldToObject = instanceData.worldInverseMatrix;
#endif
                }

                Varings vert(Attributes IN)
                {
                    Varings OUT;
                    UNITY_SETUP_INSTANCE_ID(IN);
                    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                    VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                    OUT.positionCS = positionInputs.positionCS;
                    OUT.positionWS = positionInputs.positionWS;

                    OUT.uv = TRANSFORM_TEX(IN.uv,_BaseMap);


                    return OUT;
                }

                float4 frag(Varings IN) :SV_Target
                {
                    float4 SHADOW_COORDS = TransformWorldToShadowCoord(IN.positionWS);

                    Light mainLight = GetMainLight(SHADOW_COORDS);
                    half shadow = MainLightRealtimeShadow(SHADOW_COORDS);

                    half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                    return baseMap * _BaseColor * shadow;
                }
                ENDHLSL
            }

             Pass
            {   
                Name "ShadowCaster"
                Tags{"LightMode" = "ShadowCaster"}

                ZWrite On
                ZTest LEqual
                ColorMask 0
                Cull[_Cull]

                HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma target 4.5

                // -------------------------------------
                // Material Keywords
                #define _ALPHATEST_ON 1

                //--------------------------------------
                // GPU Instancing
                #pragma multi_compile_instancing
                #pragma instancing_options procedural:setup

                #pragma vertex ShadowPassVertex
                #pragma fragment ShadowPassFragment

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

                float3 _LightDirection;
                half4 _BaseMap_ST;
                half _Cutoff;
                struct Attributes
                {
                    float4 positionOS   : POSITION;
                    float3 normalOS     : NORMAL;
                    #if defined(_ALPHATEST_ON)
                    float2 texcoord     : TEXCOORD0;
                    #endif
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    #if defined(_ALPHATEST_ON)
                    float2 uv           : TEXCOORD0;
                    #endif
                    float4 positionCS   : SV_POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                float4 GetShadowPositionHClip(Attributes input)
                {
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                    return positionCS;
                }

                struct InstanceBuffer
                {
                    float4x4 worldMatrix;
                    float4x4 worldInverseMatrix;
                };

                float _CSMOffset0;

                float _ResultShadowOffset;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<InstanceBuffer> _InstanceBuffer;
                StructuredBuffer<uint> _ResultShadowBuffer;
#endif
                void setup()
                {
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint instanceIndex = _ResultShadowBuffer[_ResultShadowOffset + _CSMOffset0 + unity_InstanceID];
                    InstanceBuffer instanceData = _InstanceBuffer[instanceIndex];
                    unity_ObjectToWorld = instanceData.worldMatrix;
                    unity_WorldToObject = instanceData.worldInverseMatrix;
#endif
                    
                }
                Varyings ShadowPassVertex(Attributes input)
                {
                    #if defined(_USING_VERTEX_SCALE)
                    input.positionOS.xyz *= _VertexScale.xyz;
                    #endif

                    Varyings output;
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);

                    #if defined(_ALPHATEST_ON)
                    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
                    #endif
                    output.positionCS = GetShadowPositionHClip(input);
                    return output;
                }

                half4 ShadowPassFragment(Varyings input) : SV_TARGET
                {
                    #if defined(_ALPHATEST_ON)
                    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy);
                    AlphaDiscard(baseColor.a, _Cutoff);
                    #endif
                    return 0;
                }
                ENDHLSL
            }

            Pass
            {
                Name "ShadowCaster1"
                Tags{"LightMode" = "ShadowCaster"}

                ZWrite On
                ZTest LEqual
                ColorMask 0
                Cull[_Cull]

                HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma target 4.5

                // -------------------------------------
                // Material Keywords
                #define _ALPHATEST_ON 1

                //--------------------------------------
                // GPU Instancing
                #pragma multi_compile_instancing
                #pragma instancing_options procedural:setup

                #pragma vertex ShadowPassVertex
                #pragma fragment ShadowPassFragment

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

                float3 _LightDirection;
                half4 _BaseMap_ST;
                half _Cutoff;
                struct Attributes
                {
                    float4 positionOS   : POSITION;
                    float3 normalOS     : NORMAL;
                    #if defined(_ALPHATEST_ON)
                    float2 texcoord     : TEXCOORD0;
                    #endif
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    #if defined(_ALPHATEST_ON)
                    float2 uv           : TEXCOORD0;
                    #endif
                    float4 positionCS   : SV_POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                float4 GetShadowPositionHClip(Attributes input)
                {
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                    return positionCS;
                }

                struct InstanceBuffer
                {
                    float4x4 worldMatrix;
                    float4x4 worldInverseMatrix;
                };

                float _CSMOffset1;

                float _ResultShadowOffset;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<InstanceBuffer> _InstanceBuffer;
                StructuredBuffer<uint> _ResultShadowBuffer;
#endif
                void setup()
                {
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint instanceIndex = _ResultShadowBuffer[_ResultShadowOffset + _CSMOffset1 + unity_InstanceID];
                    InstanceBuffer instanceData = _InstanceBuffer[instanceIndex];
                    unity_ObjectToWorld = instanceData.worldMatrix;
                    unity_WorldToObject = instanceData.worldInverseMatrix;
#endif

                }
                Varyings ShadowPassVertex(Attributes input)
                {
                    #if defined(_USING_VERTEX_SCALE)
                    input.positionOS.xyz *= _VertexScale.xyz;
                    #endif

                    Varyings output;
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);

                    #if defined(_ALPHATEST_ON)
                    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
                    #endif
                    output.positionCS = GetShadowPositionHClip(input);
                    return output;
                }

                half4 ShadowPassFragment(Varyings input) : SV_TARGET
                {
                    #if defined(_ALPHATEST_ON)
                    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy);
                    AlphaDiscard(baseColor.a, _Cutoff);
                    #endif
                    return 0;
                }
                ENDHLSL
            }
            Pass
            {
                Name "ShadowCaster2"
                Tags{"LightMode" = "ShadowCaster"}

                ZWrite On
                ZTest LEqual
                ColorMask 0
                Cull[_Cull]

                HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma target 4.5

                // -------------------------------------
                // Material Keywords
                #define _ALPHATEST_ON 1

                //--------------------------------------
                // GPU Instancing
                #pragma multi_compile_instancing
                #pragma instancing_options procedural:setup

                #pragma vertex ShadowPassVertex
                #pragma fragment ShadowPassFragment

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

                float3 _LightDirection;
                half4 _BaseMap_ST;
                half _Cutoff;
                struct Attributes
                {
                    float4 positionOS   : POSITION;
                    float3 normalOS     : NORMAL;
                    #if defined(_ALPHATEST_ON)
                    float2 texcoord     : TEXCOORD0;
                    #endif
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    #if defined(_ALPHATEST_ON)
                    float2 uv           : TEXCOORD0;
                    #endif
                    float4 positionCS   : SV_POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                float4 GetShadowPositionHClip(Attributes input)
                {
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                    return positionCS;
                }

                struct InstanceBuffer
                {
                    float4x4 worldMatrix;
                    float4x4 worldInverseMatrix;
                };

                float _CSMOffset2;

                float _ResultShadowOffset;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<InstanceBuffer> _InstanceBuffer;
                StructuredBuffer<uint> _ResultShadowBuffer;
#endif
                void setup()
                {
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint instanceIndex = _ResultShadowBuffer[_ResultShadowOffset + _CSMOffset2 + unity_InstanceID];
                    InstanceBuffer instanceData = _InstanceBuffer[instanceIndex];
                    unity_ObjectToWorld = instanceData.worldMatrix;
                    unity_WorldToObject = instanceData.worldInverseMatrix;
#endif

                }
                Varyings ShadowPassVertex(Attributes input)
                {
                    #if defined(_USING_VERTEX_SCALE)
                    input.positionOS.xyz *= _VertexScale.xyz;
                    #endif

                    Varyings output;
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);

                    #if defined(_ALPHATEST_ON)
                    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
                    #endif
                    output.positionCS = GetShadowPositionHClip(input);
                    return output;
                }

                half4 ShadowPassFragment(Varyings input) : SV_TARGET
                {
                    #if defined(_ALPHATEST_ON)
                    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy);
                    AlphaDiscard(baseColor.a, _Cutoff);
                    #endif
                    return 0;
                }
                ENDHLSL
            }

            Pass
            {
                Name "DepthOnly"
                Tags{"LightMode" = "DepthOnly"}

                ZWrite On
                ColorMask A
                Cull off

                HLSLPROGRAM
                #pragma exclude_renderers gles glcore
                #pragma target 4.5
                #pragma multi_compile_instancing
                #pragma instancing_options procedural:setup


                #define _ALPHATEST_ON
                #pragma enable_d3d11_debug_symbols

                #pragma vertex DepthOnlyVertex
                #pragma fragment DepthOnlyFragment

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                half4 _BaseMap_ST;
                half _Cutoff;
            
                struct Attributes
                {
                    float4 positionOS   : POSITION;
                    float3 normalOS     : NORMAL;
                    #if defined(_ALPHATEST_ON)
                    float2 texcoord     : TEXCOORD0;
                    #endif
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    #if defined(_ALPHATEST_ON)
                    float2 uv           : TEXCOORD0;
                    #endif
                    float4 positionCS   : SV_POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct InstanceBuffer
                {
                    float4x4 worldMatrix;
                    float4x4 worldInverseMatrix;
                };

                float _ResultOffset;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<InstanceBuffer> _InstanceBuffer;
                StructuredBuffer<uint> _ResultBuffer;
#endif
                void setup()
                {
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint instanceIndex = _ResultBuffer[_ResultOffset + unity_InstanceID];
                    InstanceBuffer instanceData = _InstanceBuffer[instanceIndex];
                    unity_ObjectToWorld = instanceData.worldMatrix;
                    unity_WorldToObject = instanceData.worldInverseMatrix;
#endif
                }

                Varyings DepthOnlyVertex(Attributes input)
                {
                    Varyings output = (Varyings)0;
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);

                    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                    float3 positionWS = vertexInput.positionWS;

                 
                    output.positionCS = TransformWorldToHClip(positionWS);
                    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

                    return output;

                }

                half4 DepthOnlyFragment(Varyings input) : SV_TARGET
                {
                   UNITY_SETUP_INSTANCE_ID(input);
               
                   half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                   AlphaDiscard(baseColor.a, _Cutoff);
                   return half4(0,0,0,input.positionCS.z);
                }
                ENDHLSL
            }
        }
}
