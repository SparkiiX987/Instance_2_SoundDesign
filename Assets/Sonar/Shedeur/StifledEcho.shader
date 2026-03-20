Shader "Custom/StifledDepthOutlineURP_Fix"
{
    Properties
    {
        _Pulse ("Pulse", Float) = 0
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _EdgeThreshold ("Edge Threshold", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP moderne
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Propriétés exposées
            float _Pulse;
            float4 _EdgeColor;
            float _EdgeThreshold;

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

           

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Vertex shader : transforme objet -> clip
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex); // URP moderne
                o.uv = o.pos.xy / o.pos.w * 0.5 + 0.5;
                return o;
            }

            // Depth linéaire
            float SampleLinearDepth(float2 uv)
            {
                float rawDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                return Linear01Depth(rawDepth, _ZBufferParams);
            }

            // Sobel simple sur 4 voisins
            float ComputeEdge(v2f i)
            {
                float center = SampleLinearDepth(i.uv);
                float edge = 0;
                float2 offsets[4] = { float2(1,0), float2(-1,0), float2(0,1), float2(0,-1) };
                float pixelOffset = 1.0 / 512.0; // ajuste selon résolution
                for(int j=0;j<4;j++)
                {
                    float neighbor = SampleLinearDepth(i.uv + offsets[j]*pixelOffset);
                    edge += abs(center - neighbor);
                }
                return edge;
            }

            float4 frag(v2f i) : SV_Target
            {
                float edge = ComputeEdge(i);
                float mask = step(_EdgeThreshold, edge) * _Pulse;
                return float4(_EdgeColor.rgb * mask, 1);
            }

            ENDHLSL
        }
    }
}