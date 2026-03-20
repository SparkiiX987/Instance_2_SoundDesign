Shader "Custom/StifledEdge"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _EdgeThickness ("Edge Thickness", Float) = 1.5
        _DepthThreshold ("Depth Threshold", Range(0,1)) = 0.01
        _NormalThreshold ("Normal Threshold", Range(0,1)) = 0.4
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
            TEXTURE2D_X(_CameraNormalsTexture);
            SAMPLER(sampler_CameraDepthTexture);
            SAMPLER(sampler_CameraNormalsTexture);

            float4 _EdgeColor;
            float  _EdgeThickness;
            float  _DepthThreshold;
            float  _NormalThreshold;

            // Noyau Sobel — retourne la magnitude du gradient
            float SobelDepth(float2 uv, float2 texelSize)
            {
                float t = _EdgeThickness;
                float2 off = texelSize * t;

                float d00 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(-off.x,  off.y)).r;
                float d10 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( 0,       off.y)).r;
                float d20 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( off.x,  off.y)).r;
                float d01 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(-off.x,  0     )).r;
                float d21 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( off.x,  0     )).r;
                float d02 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(-off.x, -off.y)).r;
                float d12 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( 0,      -off.y)).r;
                float d22 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2( off.x, -off.y)).r;

                float gx = -d00 - 2*d01 - d02 + d20 + 2*d21 + d22;
                float gy = -d00 - 2*d10 - d20 + d02 + 2*d12 + d22;

                return sqrt(gx*gx + gy*gy);
            }

            float SobelNormal(float2 uv, float2 texelSize)
            {
                float t = _EdgeThickness;
                float2 off = texelSize * t;

                float3 n00 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + float2(-off.x,  off.y)).rgb;
                float3 n10 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + float2( 0,       off.y)).rgb;
                float3 n20 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + float2( off.x,  off.y)).rgb;
                float3 n01 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + float2(-off.x,  0     )).rgb;
                float3 n21 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + float2( off.x,  0     )).rgb;
                float3 n02 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + float2(-off.x, -off.y)).rgb;
                float3 n12 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + float2( 0,      -off.y)).rgb;
                float3 n22 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + float2( off.x, -off.y)).rgb;

                float3 gx = -n00 - 2*n01 - n02 + n20 + 2*n21 + n22;
                float3 gy = -n00 - 2*n10 - n20 + n02 + 2*n12 + n22;

                return sqrt(dot(gx,gx) + dot(gy,gy));
            }

            // Détection d'intersection : zone où la depth change brutalement
            // sur un voisin très proche → deux surfaces s'interpénètrent
            float IntersectionEdge(float2 uv, float2 texelSize)
            {
                float2 off = texelSize * _EdgeThickness;
                float center = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;

                float maxDelta = 0;
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    float neighbor = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture,
                                     uv + float2(x,y) * off).r;
                    maxDelta = max(maxDelta, abs(center - neighbor));
                }
                // Seuil serré = seulement les vraies intersections
                return step(0.002, maxDelta) * step(maxDelta, 0.05);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

                float depthEdge  = SobelDepth(uv, texelSize);
                float normalEdge = SobelNormal(uv, texelSize);
                float interEdge  = IntersectionEdge(uv, texelSize);

                // Combine : contour si depth OU normale dépasse le seuil
                float edge = step(_DepthThreshold, depthEdge)
                           + step(_NormalThreshold, normalEdge);
                edge = saturate(edge);

                // Intersections toujours visibles (ligne plus brillante)
                float finalEdge = saturate(edge + interEdge * 1.5);

                return half4(_EdgeColor.rgb * finalEdge, finalEdge);
            }
            ENDHLSL
        }
    }
}