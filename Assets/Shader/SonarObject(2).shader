Shader "Sonar/SonarObject"
{
    Properties
    {
        [Header(Sonar)]
        _EdgeColor        ("Aretes apres onde",           Color) = (1.0, 1.0, 1.0, 1.0)
        _EdgeWaveColor    ("Aretes pendant onde",         Color) = (0.8, 1.0, 1.0, 1.0)
        _RingColor        ("Couleur rebord onde",         Color) = (0.5, 0.9, 1.0, 1.0)
        _SurfaceColor     ("Couleur surface onde",        Color) = (0.0, 0.3, 0.5, 1.0)
        _EnemyRingColor   ("Couleur onde ennemi",         Color) = (1.0, 0.3, 0.1, 1.0)
        _WaveThickness    ("Epaisseur anneau (m)",        Float) = 0.8
        _RingThickness    ("Epaisseur rebord",            Float) = 0.15
        _FadeDuration     ("Duree trace (s)",             Float) = 15.0
        _EdgeFadeMult     ("Multiplicateur duree aretes", Float) = 4.0
        _WireThickness    ("Epaisseur trait (px)",        Float) = 2.0
        _SilhouetteCull   ("Supression silhouette (0-1)", Float) = 0.0
        _ConeEdgeSoftness ("Douceur bord cone",           Float) = 0.05

        [Header(Noise Displacement)]
        _NoiseStrength    ("Force bruit",                 Range(0, 1)) = 0.64
        _NoiseSpeed       ("Vitesse bruit",               Float) = 0.0
        _NoiseScale       ("Echelle bruit",               Float) = 500.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        // ════════════════════════════════════════════════════════
        // Code partage entre les 2 passes
        // ════════════════════════════════════════════════════════
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

        // ── Sonar joueur ──
        float4 _WaveOrigin;
        float  _WaveRadius;
        float  _WaveActive;
        float4 _ConeForward;
        float  _ConeHalfAngleCos;
        float  _WaveFireTime;
        float  _WaveMaxRadius;
        float  _WaveFadeDuration;
        float  _ConeEdgeSoftness;

        // ── Sonar ennemi ──
        float4 _EnemyWaveOrigin;
        float  _EnemyWaveRadius;
        float  _EnemyWaveActive;

        // ── Couleurs / epaisseurs ──
        float4 _EdgeColor;
        float4 _EdgeWaveColor;
        float4 _RingColor;
        float4 _SurfaceColor;
        float4 _EnemyRingColor;
        float  _WaveThickness;
        float  _RingThickness;
        float  _FadeDuration;
        float  _EdgeFadeMult;
        float  _WireThickness;
        float  _SilhouetteCull;

        // ── Noise displacement ──
        float _NoiseStrength;
        float _NoiseSpeed;
        float _NoiseScale;

        // ────────────────────────────────────────────────────────
        // Bruit deterministe (meme algo que TestShaderProps)
        // ────────────────────────────────────────────────────────
        float ValueNoise(float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);

            float r0; Hash_Tchou_2_1_float(i + float2(0, 0), r0);
            float r1; Hash_Tchou_2_1_float(i + float2(1, 0), r1);
            float r2; Hash_Tchou_2_1_float(i + float2(0, 1), r2);
            float r3; Hash_Tchou_2_1_float(i + float2(1, 1), r3);

            return lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);
        }

        float SimpleNoise(float2 uv, float scale)
        {
            float result = 0.0;
            result += ValueNoise(uv * (scale / 1.0)) * 0.125;
            result += ValueNoise(uv * (scale / 2.0)) * 0.25;
            result += ValueNoise(uv * (scale / 4.0)) * 0.5;
            return result;
        }

        // Reproduit exactement TestShaderProps :
        //   animatedPos = (Time * Speed) + WorldPosition
        //   noise = SimpleNoise(animatedPos.xy, 500)
        //   Position = noise * Strength * WorldNormal   (object space)
        //
        // posOS  = position object-space du sommet
        // posWS  = position world-space (pour sampler le bruit)
        // normWS = normale world-space
        // Retourne la nouvelle position OBJECT SPACE
        float3 NoiseDisplace(float3 posOS, float3 posWS, float3 normWS)
        {
            float3 animated = posWS + (_NoiseSpeed * _Time.y);
            float  n = SimpleNoise(animated.xy, _NoiseScale);
            float  d = n * _NoiseStrength;
            return posOS + normWS * d;
        }

        // ────────────────────────────────────────────────────────
        // Sonar joueur
        // ────────────────────────────────────────────────────────
        struct SonarResult
        {
            float wave;
            float ring;
            float inCone;
            float dist;
        };

        SonarResult ComputePlayerSonar(float3 posWS)
        {
            SonarResult s;
            float3 toPixel  = normalize(posWS - _WaveOrigin.xyz);
            float  angleCos = dot(toPixel, normalize(_ConeForward.xyz));
            s.inCone = smoothstep(_ConeHalfAngleCos - _ConeEdgeSoftness,
                                  _ConeHalfAngleCos, angleCos);
            s.dist = distance(posWS, _WaveOrigin.xyz);

            float inner = smoothstep(_WaveRadius - _WaveThickness, _WaveRadius, s.dist);
            float outer = smoothstep(_WaveRadius + _WaveThickness, _WaveRadius, s.dist);
            s.wave = inner * outer * _WaveActive * s.inCone;

            float ringI = smoothstep(_WaveRadius - _RingThickness, _WaveRadius, s.dist);
            float ringO = smoothstep(_WaveRadius + _RingThickness, _WaveRadius, s.dist);
            s.ring = ringI * ringO * _WaveActive * s.inCone;

            return s;
        }

        // ────────────────────────────────────────────────────────
        // Sonar ennemi
        // ────────────────────────────────────────────────────────
        struct EnemySonarResult
        {
            float wave;
            float ring;
        };

        EnemySonarResult ComputeEnemySonar(float3 posWS)
        {
            EnemySonarResult e;
            float eDist  = distance(posWS, _EnemyWaveOrigin.xyz);
            float eInner = smoothstep(_EnemyWaveRadius - _WaveThickness, _EnemyWaveRadius, eDist);
            float eOuter = smoothstep(_EnemyWaveRadius + _WaveThickness, _EnemyWaveRadius, eDist);
            e.wave = eInner * eOuter * _EnemyWaveActive;

            float eRingI = smoothstep(_EnemyWaveRadius - _RingThickness, _EnemyWaveRadius, eDist);
            float eRingO = smoothstep(_EnemyWaveRadius + _RingThickness, _EnemyWaveRadius, eDist);
            e.ring = eRingI * eRingO * _EnemyWaveActive;

            return e;
        }
        ENDHLSL

        // ════════════════════════════════════════════════════════
        // Pass 1 : Surface (faces pleines revelees par l'onde)
        // ════════════════════════════════════════════════════════
        Pass
        {
            Name "WaveRingPass"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vertRing
            #pragma fragment fragRing

            struct AppDataRing
            {
                float4 posOS  : POSITION;
                float3 normOS : NORMAL;
            };

            struct V2FRing
            {
                float4 posHCS : SV_POSITION;
                float3 posWS  : TEXCOORD0;
            };

            V2FRing vertRing(AppDataRing IN)
            {
                V2FRing O;
                float3 wPos  = TransformObjectToWorld(IN.posOS.xyz);
                float3 wNorm = TransformObjectToWorldNormal(IN.normOS);
                float3 displaced = NoiseDisplace(IN.posOS.xyz, wPos, wNorm);
                float3 finalWS = TransformObjectToWorld(displaced);

                O.posWS  = finalWS;
                O.posHCS = TransformWorldToHClip(finalWS);
                return O;
            }

            half4 fragRing(V2FRing IN) : SV_Target
            {
                SonarResult      s = ComputePlayerSonar(IN.posWS);
                EnemySonarResult e = ComputeEnemySonar(IN.posWS);

                if (saturate(s.wave + s.ring + e.wave + e.ring) < 0.01)
                    return half4(0, 0, 0, 1);

                float3 col = _SurfaceColor.rgb * s.wave;
                col = lerp(col, _RingColor.rgb,      s.ring);
                col = lerp(col, _EnemyRingColor.rgb, e.wave);
                col = lerp(col, _EnemyRingColor.rgb, e.ring);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // ════════════════════════════════════════════════════════
        // Pass 2 : Wireframe (aretes revelees par l'onde)
        // ════════════════════════════════════════════════════════
        Pass
        {
            Name "SonarWire"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull   Off
            ZWrite Off
            ZTest  LEqual
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vertWire
            #pragma geometry geomWire
            #pragma fragment fragWire
            #pragma target   4.0

            struct AppDataWire
            {
                float4 posOS  : POSITION;
                float3 normOS : NORMAL;
            };

            struct GeoInput
            {
                float4 posHCS : SV_POSITION;
                float3 posWS  : TEXCOORD0;
                float3 normWS : TEXCOORD1;
                float  inCone : TEXCOORD2;
            };

            struct V2FWire
            {
                float4 posHCS : SV_POSITION;
                float3 posWS  : TEXCOORD0;
                float3 bary   : TEXCOORD1;
                float  inCone : TEXCOORD2;
            };

            GeoInput vertWire(AppDataWire IN)
            {
                GeoInput O;
                float3 wPos  = TransformObjectToWorld(IN.posOS.xyz);
                float3 wNorm = TransformObjectToWorldNormal(IN.normOS);
                float3 displaced = NoiseDisplace(IN.posOS.xyz, wPos, wNorm);
                float3 finalWS = TransformObjectToWorld(displaced);

                O.posWS  = finalWS;
                O.posHCS = TransformWorldToHClip(finalWS);
                O.normWS = wNorm;

                float3 toVert   = normalize(O.posWS - _WaveOrigin.xyz);
                float  angleCos = dot(toVert, normalize(_ConeForward.xyz));
                O.inCone = smoothstep(_ConeHalfAngleCos - _ConeEdgeSoftness,
                                      _ConeHalfAngleCos, angleCos);
                return O;
            }

            [maxvertexcount(3)]
            void geomWire(triangle GeoInput IN[3], inout TriangleStream<V2FWire> stream)
            {
                V2FWire O;
                O.posHCS = IN[0].posHCS; O.posWS = IN[0].posWS; O.bary = float3(1,0,0); O.inCone = IN[0].inCone; stream.Append(O);
                O.posHCS = IN[1].posHCS; O.posWS = IN[1].posWS; O.bary = float3(0,1,0); O.inCone = IN[1].inCone; stream.Append(O);
                O.posHCS = IN[2].posHCS; O.posWS = IN[2].posWS; O.bary = float3(0,0,1); O.inCone = IN[2].inCone; stream.Append(O);
            }

            half4 fragWire(V2FWire IN) : SV_Target
            {
                // Wireframe via coordonnees barycentriques
                float3 deltas  = fwidth(IN.bary);
                float3 smooth3 = smoothstep(float3(0,0,0), deltas * max(_WireThickness, 0.1), IN.bary);
                float  wire    = 1.0 - min(smooth3.x, min(smooth3.y, smooth3.z));
                if (wire < 0.01) return half4(0, 0, 0, 0);

                // Sonar joueur (inCone interpole depuis vertex)
                float  inCone = IN.inCone;
                float  dist   = distance(IN.posWS, _WaveOrigin.xyz);

                float inner = smoothstep(_WaveRadius - _WaveThickness, _WaveRadius, dist);
                float outer = smoothstep(_WaveRadius + _WaveThickness, _WaveRadius, dist);
                float wave  = inner * outer * _WaveActive * inCone;

                float ringI = smoothstep(_WaveRadius - _RingThickness, _WaveRadius, dist);
                float ringO = smoothstep(_WaveRadius + _RingThickness, _WaveRadius, dist);
                float ring  = ringI * ringO * _WaveActive * inCone;

                float firedOnce     = step(0.5, _WaveFireTime);
                float waveHasPassed = step(dist, _WaveRadius) * firedOnce;
                float wasSwept      = step(dist, _WaveMaxRadius) * inCone * waveHasPassed;

                // Sonar ennemi
                EnemySonarResult e = ComputeEnemySonar(IN.posWS);

                float revealed = saturate(wave + ring + wasSwept + e.wave + e.ring);
                if (revealed < 0.01) return half4(0, 0, 0, 0);

                // Couleur
                float3 col = _EdgeColor.rgb * wasSwept;
                col = lerp(col, _EdgeWaveColor.rgb,  wave);
                col = lerp(col, _RingColor.rgb,      ring);
                col = lerp(col, _EnemyRingColor.rgb, e.wave);
                col = lerp(col, _EnemyRingColor.rgb, e.ring);

                return half4(col * wire, wire);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
