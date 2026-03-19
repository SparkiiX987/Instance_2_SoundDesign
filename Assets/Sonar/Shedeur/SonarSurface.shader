Shader "Sonar/SonarSurface"
{
    Properties
    {
        _EdgeColor       ("Aretes apres onde",           Color) = (1.0, 1.0, 1.0, 1.0)
        _EdgeWaveColor   ("Aretes pendant onde",         Color) = (0.8, 1.0, 1.0, 1.0)
        _RingColor       ("Couleur rebord onde",         Color) = (0.5, 0.9, 1.0, 1.0)
        _SurfaceColor    ("Couleur surface onde",        Color) = (0.0, 0.3, 0.5, 1.0)
        _EnemyRingColor  ("Couleur onde ennemi",         Color) = (1.0, 0.3, 0.1, 1.0)
        _WaveThickness   ("Epaisseur anneau (m)",        Float) = 0.8
        _RingThickness   ("Epaisseur rebord",            Float) = 0.15
        _FadeDuration    ("Duree trace (s)",             Float) = 15.0
        _EdgeFadeMult    ("Multiplicateur duree aretes", Float) = 4.0
        _WireThickness   ("Epaisseur trait (px)",        Float) = 2.0
        _SilhouetteCull  ("Supression silhouette (0-1)", Float) = 0.0
        _ConeEdgeSoftness ("Douceur bord cone",          Float) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _WaveOrigin; float _WaveRadius; float _WaveActive;
            float4 _ConeForward; float _ConeHalfAngleCos;
            float  _WaveFireTime; float _WaveMaxRadius; float _WaveFadeDuration;
            float  _ConeEdgeSoftness;

            float4 _EnemyWaveOrigin; float _EnemyWaveRadius; float _EnemyWaveActive;
            float  _EnemyWaveFireTime; float _EnemyWaveMaxRadius; float _EnemyWaveFadeDuration;

            float4 _RingColor; float4 _SurfaceColor; float4 _EnemyRingColor;
            float  _WaveThickness; float _RingThickness; float _FadeDuration;

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
                // Onde joueur
                float3 toPixel  = normalize(IN.posWS - _WaveOrigin.xyz);
                float  angleCos = dot(toPixel, normalize(_ConeForward.xyz));
                float  inCone   = smoothstep(_ConeHalfAngleCos - _ConeEdgeSoftness, _ConeHalfAngleCos, angleCos);
                float  dist     = distance(IN.posWS, _WaveOrigin.xyz);

                float inner = smoothstep(_WaveRadius - _WaveThickness, _WaveRadius, dist);
                float outer = smoothstep(_WaveRadius + _WaveThickness, _WaveRadius, dist);
                float wave  = inner * outer * _WaveActive * inCone;

                float ringInner = smoothstep(_WaveRadius - _RingThickness, _WaveRadius, dist);
                float ringOuter = smoothstep(_WaveRadius + _RingThickness, _WaveRadius, dist);
                float ring      = ringInner * ringOuter * _WaveActive * inCone;

                // Onde ennemi
                float eDist      = distance(IN.posWS, _EnemyWaveOrigin.xyz);
                float eInner     = smoothstep(_EnemyWaveRadius - _WaveThickness, _EnemyWaveRadius, eDist);
                float eOuter     = smoothstep(_EnemyWaveRadius + _WaveThickness, _EnemyWaveRadius, eDist);
                float eWave      = eInner * eOuter * _EnemyWaveActive;
                float eRingInner = smoothstep(_EnemyWaveRadius - _RingThickness, _EnemyWaveRadius, eDist);
                float eRingOuter = smoothstep(_EnemyWaveRadius + _RingThickness, _EnemyWaveRadius, eDist);
                float eRing      = eRingInner * eRingOuter * _EnemyWaveActive;

                if (saturate(wave + ring + eWave + eRing) < 0.01)
                    return half4(0, 0, 0, 1);

                float3 col = _SurfaceColor.rgb * wave;
                col = lerp(col, _RingColor.rgb,      ring);
                col = lerp(col, _EnemyRingColor.rgb, eWave);
                col = lerp(col, _EnemyRingColor.rgb, eRing);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "SonarWire"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull   Off
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

            float4 _EnemyWaveOrigin; float _EnemyWaveRadius; float _EnemyWaveActive;
            float  _EnemyWaveFireTime; float _EnemyWaveMaxRadius; float _EnemyWaveFadeDuration;

            float4 _EdgeColor; float4 _EdgeWaveColor; float4 _RingColor; float4 _EnemyRingColor;
            float  _WaveThickness; float _RingThickness;
            float  _FadeDuration; float _EdgeFadeMult;
            float  _WireThickness; float _SilhouetteCull;

            struct Attributes { float4 posOS : POSITION; float3 normOS : NORMAL; };

            struct GeoInput
            {
                float4 posHCS : SV_POSITION;
                float3 posWS  : TEXCOORD0;
                float3 normWS : TEXCOORD1;
            };

            struct Varyings
            {
                float4 posHCS : SV_POSITION;
                float3 posWS  : TEXCOORD0;
                float3 bary   : TEXCOORD1;
            };

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
                // Wireframe
                float3 deltas  = fwidth(IN.bary);
                float3 smooth3 = smoothstep(float3(0,0,0), deltas * max(_WireThickness, 0.1), IN.bary);
                float  wire    = 1.0 - min(smooth3.x, min(smooth3.y, smooth3.z));
                if (wire < 0.01) return half4(0,0,0,0);

                // ── Onde joueur ───────────────────────────────────────
                float3 toPixel  = normalize(IN.posWS - _WaveOrigin.xyz);
                float  angleCos = dot(toPixel, normalize(_ConeForward.xyz));
                float  inCone   = step(_ConeHalfAngleCos, angleCos);
                float  dist     = distance(IN.posWS, _WaveOrigin.xyz);

                float inner = smoothstep(_WaveRadius - _WaveThickness, _WaveRadius, dist);
                float outer = smoothstep(_WaveRadius + _WaveThickness, _WaveRadius, dist);
                float wave  = inner * outer * _WaveActive * inCone;

                float ringInner = smoothstep(_WaveRadius - _RingThickness, _WaveRadius, dist);
                float ringOuter = smoothstep(_WaveRadius + _RingThickness, _WaveRadius, dist);
                float ring      = ringInner * ringOuter * _WaveActive * inCone;

                // Fade joueur
                float fadeDurBase = max(_WaveFadeDuration, _FadeDuration);
                float fadeDurEdge = fadeDurBase * _EdgeFadeMult;
                float firedOnce     = step(0.5, _WaveFireTime);
                float waveHasPassed = step(dist, _WaveRadius) * firedOnce;
                float wasSwept      = step(dist, _WaveMaxRadius) * inCone * waveHasPassed;
                float delay         = (dist / max(_WaveMaxRadius, 0.001)) * (fadeDurBase * 0.1);
                float elapsed       = max(0.0, _Time.y - _WaveFireTime - delay);
                float fadeOut       = 1.0 - smoothstep(fadeDurEdge * 0.8, fadeDurEdge, elapsed);
                float trailFade     = wasSwept * fadeOut;

                // ── Onde ennemi — meme logique, sans cone ─────────────
                float eDist      = distance(IN.posWS, _EnemyWaveOrigin.xyz);
                float eInner     = smoothstep(_EnemyWaveRadius - _WaveThickness, _EnemyWaveRadius, eDist);
                float eOuter     = smoothstep(_EnemyWaveRadius + _WaveThickness, _EnemyWaveRadius, eDist);
                float eWave      = eInner * eOuter * _EnemyWaveActive;
                float eRingInner = smoothstep(_EnemyWaveRadius - _RingThickness, _EnemyWaveRadius, eDist);
                float eRingOuter = smoothstep(_EnemyWaveRadius + _RingThickness, _EnemyWaveRadius, eDist);
                float eRing      = eRingInner * eRingOuter * _EnemyWaveActive;

                // Fade ennemi (meme courbe, sans cone)
                float eFadeDurBase  = max(_EnemyWaveFadeDuration, _FadeDuration);
                float eFadeDurEdge  = eFadeDurBase * _EdgeFadeMult;
                float eFiredOnce    = step(0.5, _EnemyWaveFireTime);
                float eHasPassed    = step(eDist, _EnemyWaveRadius) * eFiredOnce;
                float eWasSwept     = step(eDist, _EnemyWaveMaxRadius) * eHasPassed;
                float eDelay        = (eDist / max(_EnemyWaveMaxRadius, 0.001)) * (eFadeDurBase * 0.1);
                float eElapsed      = max(0.0, _Time.y - _EnemyWaveFireTime - eDelay);
                float eFadeOut      = 1.0 - smoothstep(eFadeDurEdge * 0.8, eFadeDurEdge, eElapsed);
                float eTrailFade    = eWasSwept * eFadeOut;

                float revealed = saturate(wave + ring + trailFade + eWave + eRing + eTrailFade);
                if (revealed < 0.01) return half4(0,0,0,0);

                float3 col = _EdgeColor.rgb      * trailFade;
                col = lerp(col, _EdgeWaveColor.rgb,  wave);
                col = lerp(col, _RingColor.rgb,      ring);
                col = lerp(col, _EnemyRingColor.rgb, eTrailFade);
                col = lerp(col, _EnemyRingColor.rgb, eWave);
                col = lerp(col, _EnemyRingColor.rgb, eRing);

                return half4(col * wire, wire);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
