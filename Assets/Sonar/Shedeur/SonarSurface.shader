Shader "Sonar/SonarSurface"
{
    Properties
    {
        _EdgeColor        ("Aretes revelees joueur",     Color)  = (1.0, 1.0, 1.0, 1.0)
        _EdgeWaveColor    ("Aretes pendant onde",        Color)  = (0.8, 1.0, 1.0, 1.0)
        _RingColor        ("Rebord onde joueur",         Color)  = (0.5, 0.9, 1.0, 1.0)
        _SurfaceColor     ("Surface onde joueur",        Color)  = (0.0, 0.3, 0.5, 1.0)
        _EnemyRingColor   ("Rebord onde ennemi",         Color)  = (1.0, 0.3, 0.1, 1.0)
        _WaveThickness    ("Epaisseur anneau (m)",       Float)  = 0.8
        _RingThickness    ("Epaisseur rebord",           Float)  = 0.15
        _WireThickness    ("Epaisseur trait (px)",       Float)  = 2.0
        _ConeEdgeSoftness ("Douceur bord cone",          Float)  = 0.05
        _FadeDuration     ("Duree trace (s)",            Float)  = 15.0
        _EdgeFadeMult     ("Multiplicateur duree",       Float)  = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        // PASSE 1 : Anneau de l'onde
        Pass
        {
            Name "WaveRingPass"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vertRing
            #pragma fragment fragRing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _WaveOrigin; float _WaveRadius; float _WaveActive;
            float4 _ConeForward; float _ConeHalfAngleCos; float _ConeEdgeSoftness;
            float4 _RingColor; float4 _SurfaceColor;
            float  _WaveThickness; float _RingThickness;

            float4 _EnemyOrigin0; float _EnemyRadius0; float _EnemyActive0; float4 _EnemyColor0;
            float4 _EnemyOrigin1; float _EnemyRadius1; float _EnemyActive1; float4 _EnemyColor1;
            float4 _EnemyOrigin2; float _EnemyRadius2; float _EnemyActive2; float4 _EnemyColor2;
            float4 _EnemyOrigin3; float _EnemyRadius3; float _EnemyActive3; float4 _EnemyColor3;
            float4 _EnemyOrigin4; float _EnemyRadius4; float _EnemyActive4; float4 _EnemyColor4;
            float4 _EnemyOrigin5; float _EnemyRadius5; float _EnemyActive5; float4 _EnemyColor5;
            float4 _EnemyOrigin6; float _EnemyRadius6; float _EnemyActive6; float4 _EnemyColor6;
            float4 _EnemyOrigin7; float _EnemyRadius7; float _EnemyActive7; float4 _EnemyColor7;

            struct Attributes { float4 posOS : POSITION; };
            struct Varyings   { float4 posHCS : SV_POSITION; float3 posWS : TEXCOORD0; };

            Varyings vertRing(Attributes IN)
            {
                Varyings O;
                O.posWS  = TransformObjectToWorld(IN.posOS.xyz);
                O.posHCS = TransformWorldToHClip(O.posWS);
                return O;
            }

            half4 fragRing(Varyings IN) : SV_Target
            {
                float3 toPixel  = normalize(IN.posWS - _WaveOrigin.xyz);
                float  angleCos = dot(toPixel, normalize(_ConeForward.xyz));
                float  inCone   = smoothstep(_ConeHalfAngleCos - _ConeEdgeSoftness, _ConeHalfAngleCos, angleCos);
                float  dist     = distance(IN.posWS, _WaveOrigin.xyz);
                float  inner    = smoothstep(_WaveRadius - _WaveThickness, _WaveRadius, dist);
                float  outer    = smoothstep(_WaveRadius + _WaveThickness, _WaveRadius, dist);
                float  wave     = inner * outer * _WaveActive * inCone;
                float  ringI    = smoothstep(_WaveRadius - _RingThickness, _WaveRadius, dist);
                float  ringO    = smoothstep(_WaveRadius + _RingThickness, _WaveRadius, dist);
                float  ring     = ringI * ringO * _WaveActive * inCone;

                float3 eCol = float3(0,0,0);
                float  eAny = 0;
                #define ENEMY_RING(IDX) { \
                    float ed = distance(IN.posWS, _EnemyOrigin##IDX.xyz); \
                    float ei = smoothstep(_EnemyRadius##IDX - _WaveThickness, _EnemyRadius##IDX, ed); \
                    float eo = smoothstep(_EnemyRadius##IDX + _WaveThickness, _EnemyRadius##IDX, ed); \
                    float ew = ei * eo * _EnemyActive##IDX; \
                    eCol = lerp(eCol, _EnemyColor##IDX.rgb, ew); \
                    eAny = saturate(eAny + ew); \
                }
                ENEMY_RING(0) ENEMY_RING(1) ENEMY_RING(2) ENEMY_RING(3)
                ENEMY_RING(4) ENEMY_RING(5) ENEMY_RING(6) ENEMY_RING(7)

                if (saturate(wave + ring + eAny) < 0.01)
                    return half4(0, 0, 0, 1);

                float3 col = float3(0,0,0);
                col = lerp(col, _SurfaceColor.rgb, wave);
                col = lerp(col, _RingColor.rgb,    ring);
                col = lerp(col, eCol,              eAny);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // PASSE 2 : Aretes wireframe
        Pass
        {
            Name "SonarWire"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull   Back
            ZWrite Off
            ZTest  LEqual
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target   4.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _WaveOrigin; float _WaveRadius; float _WaveActive;
            float4 _ConeForward; float _ConeHalfAngleCos;
            float  _WaveFireTime; float _WaveMaxRadius; float _WaveFadeDuration;
            float4 _EdgeColor; float4 _EdgeWaveColor; float4 _RingColor;
            float  _WaveThickness; float _RingThickness; float _WireThickness;
            float  _FadeDuration; float _EdgeFadeMult;

            float4 _EnemyOrigin0; float _EnemyRadius0; float _EnemyActive0; float4 _EnemyColor0; float _EnemyFireTime0; float _EnemyMaxRad0; float _EnemyFadeDur0;
            float4 _EnemyOrigin1; float _EnemyRadius1; float _EnemyActive1; float4 _EnemyColor1; float _EnemyFireTime1; float _EnemyMaxRad1; float _EnemyFadeDur1;
            float4 _EnemyOrigin2; float _EnemyRadius2; float _EnemyActive2; float4 _EnemyColor2; float _EnemyFireTime2; float _EnemyMaxRad2; float _EnemyFadeDur2;
            float4 _EnemyOrigin3; float _EnemyRadius3; float _EnemyActive3; float4 _EnemyColor3; float _EnemyFireTime3; float _EnemyMaxRad3; float _EnemyFadeDur3;
            float4 _EnemyOrigin4; float _EnemyRadius4; float _EnemyActive4; float4 _EnemyColor4; float _EnemyFireTime4; float _EnemyMaxRad4; float _EnemyFadeDur4;
            float4 _EnemyOrigin5; float _EnemyRadius5; float _EnemyActive5; float4 _EnemyColor5; float _EnemyFireTime5; float _EnemyMaxRad5; float _EnemyFadeDur5;
            float4 _EnemyOrigin6; float _EnemyRadius6; float _EnemyActive6; float4 _EnemyColor6; float _EnemyFireTime6; float _EnemyMaxRad6; float _EnemyFadeDur6;
            float4 _EnemyOrigin7; float _EnemyRadius7; float _EnemyActive7; float4 _EnemyColor7; float _EnemyFireTime7; float _EnemyMaxRad7; float _EnemyFadeDur7;

            struct Attributes { float4 posOS : POSITION; float3 normOS : NORMAL; };
            struct GeoInput   { float4 posHCS : SV_POSITION; float3 posWS : TEXCOORD0; float3 normWS : TEXCOORD1; };
            struct Varyings   { float4 posHCS : SV_POSITION; float3 posWS : TEXCOORD0; float3 bary : TEXCOORD1; };

            GeoInput vert(Attributes IN)
            {
                GeoInput O;
                O.posWS  = TransformObjectToWorld(IN.posOS.xyz);
                O.posHCS = TransformWorldToHClip(O.posWS);
                O.normWS = TransformObjectToWorldNormal(IN.normOS);
                return O;
            }

            [maxvertexcount(3)]
            void geom(triangle GeoInput IN[3], inout TriangleStream<Varyings> stream)
            {
                Varyings O;
                O.posHCS = IN[0].posHCS; O.posWS = IN[0].posWS; O.bary = float3(1,0,0); stream.Append(O);
                O.posHCS = IN[1].posHCS; O.posWS = IN[1].posWS; O.bary = float3(0,1,0); stream.Append(O);
                O.posHCS = IN[2].posHCS; O.posWS = IN[2].posWS; O.bary = float3(0,0,1); stream.Append(O);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 deltas  = fwidth(IN.bary);
                float3 smooth3 = smoothstep(float3(0,0,0), deltas * max(_WireThickness, 0.1), IN.bary);
                float  wire    = 1.0 - min(smooth3.x, min(smooth3.y, smooth3.z));
                if (wire < 0.01) return half4(0,0,0,0);

                // Onde joueur
                float3 toPixel  = normalize(IN.posWS - _WaveOrigin.xyz);
                float  angleCos = dot(toPixel, normalize(_ConeForward.xyz));
                float  inCone   = step(_ConeHalfAngleCos, angleCos);
                float  dist     = distance(IN.posWS, _WaveOrigin.xyz);
                float  inner    = smoothstep(_WaveRadius - _WaveThickness, _WaveRadius, dist);
                float  outer    = smoothstep(_WaveRadius + _WaveThickness, _WaveRadius, dist);
                float  wave     = inner * outer * _WaveActive * inCone;
                float  ringI    = smoothstep(_WaveRadius - _RingThickness, _WaveRadius, dist);
                float  ringO    = smoothstep(_WaveRadius + _RingThickness, _WaveRadius, dist);
                float  ring     = ringI * ringO * _WaveActive * inCone;

                // Trace joueur
                float waveDur     = max(_WaveFadeDuration, 0.001);
                float delay       = (dist / max(_WaveMaxRadius, 0.001)) * waveDur;
                float arrivalTime = _WaveFireTime + delay;
                float firedOnce   = step(0.001, _WaveFireTime);
                float waveArrived = step(arrivalTime, _Time.y);
                float inRange     = step(dist, _WaveMaxRadius);
                float wasSwept    = firedOnce * waveArrived * inRange * inCone;
                float waveEndTime = _WaveFireTime + waveDur;
                float fadeDur     = _FadeDuration * _EdgeFadeMult;
                float elapsed     = max(0.0, _Time.y - waveEndTime);
                float fadeOut     = 1.0 - smoothstep(fadeDur * 0.8, fadeDur, elapsed);
                float trailFade   = wasSwept * fadeOut;

                // 8 emetteurs ennemis
                float  eTrailAny = 0;
                float  eWaveAny  = 0;
                float3 eTrailCol = float3(0,0,0);

                #define ENEMY_WIRE(IDX) { \
                    float ed     = distance(IN.posWS, _EnemyOrigin##IDX.xyz); \
                    float ewi    = smoothstep(_EnemyRadius##IDX - _WaveThickness, _EnemyRadius##IDX, ed); \
                    float ewo    = smoothstep(_EnemyRadius##IDX + _WaveThickness, _EnemyRadius##IDX, ed); \
                    float ew     = ewi * ewo * _EnemyActive##IDX; \
                    eWaveAny     = saturate(eWaveAny + ew); \
                    float ewd    = max(_EnemyFadeDur##IDX, 0.001); \
                    float edel   = (ed / max(_EnemyMaxRad##IDX, 0.001)) * ewd; \
                    float earr   = _EnemyFireTime##IDX + edel; \
                    float efire  = step(0.001, _EnemyFireTime##IDX); \
                    float earriv = step(earr, _Time.y); \
                    float einr   = step(ed, _EnemyMaxRad##IDX); \
                    float eswept = efire * earriv * einr; \
                    float eend   = _EnemyFireTime##IDX + ewd; \
                    float eelaps = max(0.0, _Time.y - eend); \
                    float efout  = 1.0 - smoothstep(fadeDur*0.8, fadeDur, eelaps); \
                    float etf    = eswept * efout; \
                    eTrailCol    = lerp(eTrailCol, _EnemyColor##IDX.rgb, etf); \
                    eTrailAny    = saturate(eTrailAny + etf); \
                }
                ENEMY_WIRE(0) ENEMY_WIRE(1) ENEMY_WIRE(2) ENEMY_WIRE(3)
                ENEMY_WIRE(4) ENEMY_WIRE(5) ENEMY_WIRE(6) ENEMY_WIRE(7)

                float revealed = saturate(wave + ring + trailFade + eWaveAny + eTrailAny);
                if (revealed < 0.01) return half4(0,0,0,0);

                float3 col = _EdgeColor.rgb * trailFade;
                col = lerp(col, eTrailCol,          eTrailAny);
                col = lerp(col, _EdgeWaveColor.rgb, wave);
                col = lerp(col, _RingColor.rgb,     ring);
                col = lerp(col, eTrailCol,          eWaveAny);

                return half4(col * wire, wire);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
