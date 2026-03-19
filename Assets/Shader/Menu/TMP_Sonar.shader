Shader "Custom/TMP_Sonar"
{
    Properties
    {
        _FaceTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)

        _MousePos ("Mouse Position", Vector) = (0,0,0,0)
        _Radius ("Radius", Float) = 0
        _Width ("Wave Width", Float) = 0.1
        _Softness ("Softness", Float) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _FaceTex;
            float4 _FaceColor;

            float4 _MousePos;
            float _Radius;
            float _Width;
            float _Softness;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = world.xy;

                return o;
            }

            float smoothRing(float dist, float radius, float width, float softness)
            {
                float inner = smoothstep(radius - width - softness, radius - width, dist);
                float outer = smoothstep(radius, radius + softness, dist);
                return inner * (1 - outer);
            }

            float pow6(float x)
            {
                return pow(x, 6);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.worldPos, _MousePos.xy);

                _Radius = 1 - pow((frac(_Time.y) - 1), 6);

                float wave = smoothRing(dist, _Radius, _Width, _Softness);

                fixed4 col = tex2D(_FaceTex, i.uv) * _FaceColor;

                col.a *= wave;

                return col;
            }
            ENDCG
        }
    }
}