Shader "Custom/StifledEdge"
{
    Properties
    {
        _EdgeColor      ("Edge Color",        Color)        = (1,1,1,1)
        _EdgeThickness  ("Edge Thickness",    Float)        = 1.2
        _DepthThreshold ("Depth Threshold",   Range(0,0.1)) = 0.008
        _NormalThreshold("Normal Threshold",  Range(0,2))   = 0.3
        _IntersectMin   ("Intersection Min",  Range(0,0.1)) = 0.002
        _IntersectMax   ("Intersection Max",  Range(0,0.5)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            float4 _EdgeColor;
            float  _EdgeThickness;
            float  _DepthThreshold;
            float  _NormalThreshold;
            float  _IntersectMin;
            float  _IntersectMax;

            // --- Depth Sobel ---
            float SobelDepth(float2 uv, float2 off)
            {
                float d00 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(-off.x,  off.y)).r;
                float d10 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( 0,       off.y)).r;
                float d20 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( off.x,  off.y)).r;
                float d01 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(-off.x,  0    )).r;
                float d21 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( off.x,  0    )).r;
                float d02 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(-off.x, -off.y)).r;
                float d12 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( 0,      -off.y)).r;
                float d22 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( off.x, -off.y)).r;

                float gx = -d00 - 2*d01 - d02 + d20 + 2*d21 + d22;
                float gy = -d00 - 2*d10 - d20 + d02 + 2*d12 + d22;
                return sqrt(gx*gx + gy*gy);
            }

            // --- Reconstruction normale depuis depth ---
            float3 ReconstructNormal(float2 uv, float2 off)
            {
                float dc = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float dr = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(off.x, 0)).r;
                float du = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(0, off.y)).r;

                float3 c = float3(uv,                    dc);
                float3 r = float3(uv + float2(off.x, 0), dr);
                float3 u = float3(uv + float2(0, off.y), du);

                return normalize(cross(u - c, r - c));
            }

            // --- Sobel sur normales reconstruites ---
            float SobelNormalFromDepth(float2 uv, float2 off)
            {
                float3 n00 = ReconstructNormal(uv + float2(-off.x,  off.y), off);
                float3 n10 = ReconstructNormal(uv + float2( 0,       off.y), off);
                float3 n20 = ReconstructNormal(uv + float2( off.x,  off.y), off);
                float3 n01 = ReconstructNormal(uv + float2(-off.x,  0    ), off);
                float3 n21 = ReconstructNormal(uv + float2( off.x,  0    ), off);
                float3 n02 = ReconstructNormal(uv + float2(-off.x, -off.y), off);
                float3 n12 = ReconstructNormal(uv + float2( 0,      -off.y), off);
                float3 n22 = ReconstructNormal(uv + float2( off.x, -off.y), off);

                float3 gx = -n00 - 2*n01 - n02 + n20 + 2*n21 + n22;
                float3 gy = -n00 - 2*n10 - n20 + n02 + 2*n12 + n22;
                return sqrt(dot(gx,gx) + dot(gy,gy));
            }

            // --- Detection intersection entre objets ---
            float IntersectionEdge(float2 uv, float2 off)
            {
                float center = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;

                float maxDelta = 0;
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    float neighbor = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture,
                                     uv + float2(x, y) * off).r;
                    maxDelta = max(maxDelta, abs(center - neighbor));
                }
                return step(_IntersectMin, maxDelta) * step(maxDelta, _IntersectMax);
            }

            // --- Fragment principal ---
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv       = input.texcoord;
                float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                float2 off      = texelSize * _EdgeThickness;

                float depthEdge  = SobelDepth(uv, off);
                float normalEdge = SobelNormalFromDepth(uv, off);
                float interEdge  = IntersectionEdge(uv, off);

                // Arete = depth OU normale depasse le seuil
                float edge = step(_DepthThreshold, depthEdge)
                           + step(_NormalThreshold, normalEdge);
                edge = saturate(edge);

                // Intersection = ligne plus brillante
                float finalEdge = saturate(edge + interEdge * 2.0);

                // Fond noir total, lignes en EdgeColor
                return half4(_EdgeColor.rgb * finalEdge, 1.0);
            }
            ENDHLSL
        }
    }
}
