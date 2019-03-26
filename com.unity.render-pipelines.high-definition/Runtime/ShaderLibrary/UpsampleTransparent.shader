﻿Shader "Hidden/HDRP/DownsampleDepth"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma multi_compile_local BILINEAR NEAREST_DEPTH
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        TEXTURE2D_X(_LowResTransparent);
#ifdef NEAREST_DEPTH
        TEXTURE2D_X_FLOAT(_LowResDepthTexture);
#endif

#define NEIGHBOUR_SEARCH 4

        float4 Frag(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;

            float2 fullResTexelSize = _ScreenSize.zw;
            float2 halfResTexelSize = 2.0f * fullResTexelSize;

        #ifdef BILINEAR
            return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_linear_clamp_sampler, ClampAndScaleUVForBilinear(uv, halfResTexelSize), 0.0);
        #elif NEAREST_DEPTH
            float4 lowResDepths = GATHER_RED_TEXTURE2D_X(_LowResDepthTexture, s_linear_clamp_sampler, ClampAndScaleUVForBilinear(uv, halfResTexelSize));
            
            float2 topLeftUV = uv - 0.5f * halfResTexelSize; 

            float2 UVs[NEIGHBOUR_SEARCH] = {
              topLeftUV + float2(0.0f,             halfResTexelSize.y),
              topLeftUV + float2(halfResTexelSize.x, halfResTexelSize.y),
              topLeftUV + float2(halfResTexelSize.x, 0.0f),
              topLeftUV,
            };

            float linearFullResDepth = LinearEyeDepth(LoadCameraDepth(input.positionCS.xy), _ZBufferParams);
            float depthDiffThresh = 0.1f; // make this a param?

            float minDiff = 1e12f;
            float2 nearestUV = uv;
            int countBelowThresh = 0;

            [unroll]
            for (int i = 0; i < NEIGHBOUR_SEARCH; ++i)
            {
                float depthDiff = abs(linearFullResDepth - LinearEyeDepth(lowResDepths[i], _ZBufferParams));
                if (depthDiff < minDiff)
                {
                    minDiff = depthDiff;
                    nearestUV = UVs[i];
                }
                countBelowThresh += (depthDiff < depthDiffThresh);
            }

            if (countBelowThresh == NEIGHBOUR_SEARCH)
            {
                // Bilinear.
                return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_linear_clamp_sampler, ClampAndScaleUVForBilinear(uv, halfResTexelSize), 0.0);
            }
            else
            {
                // Edge
                return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_point_clamp_sampler, ClampAndScaleUVForPoint(nearestUV), 0.0);
            }

        #else
            // Nothing to see here yet.
            return 0.0f;
        #endif

        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off
            Blend One SrcAlpha
            BlendOp Add

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
