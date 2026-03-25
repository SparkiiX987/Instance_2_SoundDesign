Shader "Custom/StifledEdge_Sonar"
{
    Properties
    {
        _EdgeColor       ("Edge Color",         Color)        = (1,1,1,1)
        _EdgeWaveColor   ("Edge Wave Color",     Color)        = (0.8,1,1,1)
        _EdgeThickness   ("Edge Thickness",      Float)        = 1.2
        _DepthThreshold  ("Depth Threshold",     Range(0,0.1)) = 0.008
        _NormalThreshold ("Normal Threshold",    Range(0,2))   = 0.3
        _IntersectMin    ("Intersection Min",    Range(0,0.1)) = 0.002
        _IntersectMax    ("Intersection Max",    Range(0,0.5)) = 0.05
        _FadeDuration    ("Duree trace (s)",     Float)        = 15.0
        _EdgeFadeMult    ("Multiplicateur fade", Float)        = 1.0
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

            // Globaux sonar joueur (cri)
            float4 _WaveOrigin;
            float  _WaveRadius;
            float  _WaveActive;
            float4 _ConeForward;
            float  _ConeHalfAngleCos;
            float  _WaveFireTime;
            float  _WaveMaxRadius;
            float  _WaveFadeDuration;

            // Globaux onde de mouvement (cercle independant)
            float4 _MoveWaveOrigin;
            float  _MoveWaveRadius;
            float  _MoveWaveActive;
            float  _MoveWaveFireTime;
            float  _MoveWaveMaxRadius;
            float  _MoveWaveFadeDuration;

            // Globaux sonar ennemis (8 slots)
            float4 _EnemyOrigin0; float _EnemyRadius0; float _EnemyActive0; float4 _EnemyColor0; float _EnemyFireTime0; float _EnemyMaxRad0; float _EnemyFadeDur0;
            float4 _EnemyOrigin1; float _EnemyRadius1; float _EnemyActive1; float4 _EnemyColor1; float _EnemyFireTime1; float _EnemyMaxRad1; float _EnemyFadeDur1;
            float4 _EnemyOrigin2; float _EnemyRadius2; float _EnemyActive2; float4 _EnemyColor2; float _EnemyFireTime2; float _EnemyMaxRad2; float _EnemyFadeDur2;
            float4 _EnemyOrigin3; float _EnemyRadius3; float _EnemyActive3; float4 _EnemyColor3; float _EnemyFireTime3; float _EnemyMaxRad3; float _EnemyFadeDur3;
            float4 _EnemyOrigin4; float _EnemyRadius4; float _EnemyActive4; float4 _EnemyColor4; float _EnemyFireTime4; float _EnemyMaxRad4; float _EnemyFadeDur4;
            float4 _EnemyOrigin5; float _EnemyRadius5; float _EnemyActive5; float4 _EnemyColor5; float _EnemyFireTime5; float _EnemyMaxRad5; float _EnemyFadeDur5;
            float4 _EnemyOrigin6; float _EnemyRadius6; float _EnemyActive6; float4 _EnemyColor6; float _EnemyFireTime6; float _EnemyMaxRad6; float _EnemyFadeDur6;
            float4 _EnemyOrigin7; float _EnemyRadius7; float _EnemyActive7; float4 _EnemyColor7; float _EnemyFireTime7; float _EnemyMaxRad7; float _EnemyFadeDur7;
            float4 _EnemyOrigin8; float _EnemyRadius8; float _EnemyActive8; float4 _EnemyColor8; float _EnemyFireTime8; float _EnemyMaxRad8; float _EnemyFadeDur8;
            float4 _EnemyOrigin9; float _EnemyRadius9; float _EnemyActive9; float4 _EnemyColor9; float _EnemyFireTime9; float _EnemyMaxRad9; float _EnemyFadeDur9;
            float4 _EnemyOrigin10; float _EnemyRadius10; float _EnemyActive10; float4 _EnemyColor10; float _EnemyFireTime10; float _EnemyMaxRad10; float _EnemyFadeDur10;
            float4 _EnemyOrigin11; float _EnemyRadius11; float _EnemyActive11; float4 _EnemyColor11; float _EnemyFireTime11; float _EnemyMaxRad11; float _EnemyFadeDur11;
            float4 _EnemyOrigin12; float _EnemyRadius12; float _EnemyActive12; float4 _EnemyColor12; float _EnemyFireTime12; float _EnemyMaxRad12; float _EnemyFadeDur12;
            float4 _EnemyOrigin13; float _EnemyRadius13; float _EnemyActive13; float4 _EnemyColor13; float _EnemyFireTime13; float _EnemyMaxRad13; float _EnemyFadeDur13;
            float4 _EnemyOrigin14; float _EnemyRadius14; float _EnemyActive14; float4 _EnemyColor14; float _EnemyFireTime14; float _EnemyMaxRad14; float _EnemyFadeDur14;




            
            float4 _EdgeColor;
            float4 _EdgeWaveColor;
            float  _EdgeThickness;
            float  _DepthThreshold;
            float  _NormalThreshold;
            float  _IntersectMin;
            float  _IntersectMax;
            float  _FadeDuration;
            float  _EdgeFadeMult;

            // ── Reconstruction position monde depuis depth ────────────
            float3 ReconstructWorldPos(float2 uv, float rawDepth)
            {
                float4 ndc = float4(uv * 2.0 - 1.0, rawDepth, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                    ndc.y = -ndc.y;
                #endif
                float4 worldPos = mul(UNITY_MATRIX_I_VP, ndc);
                return worldPos.xyz / worldPos.w;
            }

            // ── Depth Sobel ──────────────────────────────────────────
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

            // ── Reconstruction normale depuis depth ──────────────────
            float3 ReconstructNormal(float2 uv, float2 off)
            {
                float dc = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float dr = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(off.x, 0)).r;
                float du = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv + float2(0, off.y)).r;
                float3 c = float3(uv,                     dc);
                float3 r = float3(uv + float2(off.x, 0),  dr);
                float3 u = float3(uv + float2(0, off.y),  du);
                return normalize(cross(u - c, r - c));
            }

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

            float IntersectionEdge(float2 uv, float2 off)
            {
                float center = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float maxDelta = 0;
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) { continue; }
                    float neighbor = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture,
                                     uv + float2(x, y) * off).r;
                    maxDelta = max(maxDelta, abs(center - neighbor));
                }
                return step(_IntersectMin, maxDelta) * step(maxDelta, _IntersectMax);
            }

            // ── Fragment principal ────────────────────────────────────
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv        = input.texcoord;
                float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                float2 off       = texelSize * _EdgeThickness;

                // Detection aretes
                float depthEdge  = SobelDepth(uv, off);
                float normalEdge = SobelNormalFromDepth(uv, off);
                float interEdge  = IntersectionEdge(uv, off);
                float edge       = saturate(
                    step(_DepthThreshold, depthEdge) +
                    step(_NormalThreshold, normalEdge)
                );
                float finalEdge  = saturate(edge + interEdge * 2.0);

                // Pas une arete = pixel noir pur
                if (finalEdge < 0.01)
                {
                    return half4(0, 0, 0, 1);
                }

                // Position monde du pixel
                float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float3 posWS   = ReconstructWorldPos(uv, rawDepth);

                // ── Onde joueur ───────────────────────────────────────
                float3 toPixel  = normalize(posWS - _WaveOrigin.xyz);
                float  angleCos = dot(toPixel, normalize(_ConeForward.xyz));
                float  inCone   = step(_ConeHalfAngleCos, angleCos);
                float  dist     = distance(posWS, _WaveOrigin.xyz);

                // Anneau pendant propagation
                float inner  = smoothstep(_WaveRadius - 0.8, _WaveRadius, dist);
                float outer  = smoothstep(_WaveRadius + 0.8, _WaveRadius, dist);
                float wave   = inner * outer * _WaveActive * inCone;

                // Trace residuelle joueur
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

                // ── Onde de mouvement (cercle autour du joueur) ──────
                float moveDist    = distance(posWS, _MoveWaveOrigin.xyz);
                float moveInner   = smoothstep(_MoveWaveRadius - 0.8, _MoveWaveRadius, moveDist);
                float moveOuter   = smoothstep(_MoveWaveRadius + 0.8, _MoveWaveRadius, moveDist);
                float moveWave    = moveInner * moveOuter * _MoveWaveActive;

                float mWaveDur    = max(_MoveWaveFadeDuration, 0.001);
                float mDelay      = (moveDist / max(_MoveWaveMaxRadius, 0.001)) * mWaveDur;
                float mArrival    = _MoveWaveFireTime + mDelay;
                float mFired      = step(0.001, _MoveWaveFireTime);
                float mArrived    = step(mArrival, _Time.y);
                float mInRange    = step(moveDist, _MoveWaveMaxRadius);
                float mSwept      = mFired * mArrived * mInRange;
                float mEnd        = _MoveWaveFireTime + mWaveDur;
                float mElapsed    = max(0.0, _Time.y - mEnd);
                float mFadeOut    = 1.0 - smoothstep(fadeDur * 0.8, fadeDur, mElapsed);
                float moveTrail   = mSwept * mFadeOut;

                // ── 8 emetteurs ennemis ───────────────────────────────
                float  eTrailAny = 0;
                float  eWaveAny  = 0;
                float3 eTrailCol = float3(0,0,0);

                #define ENEMY_POST(IDX) { \
                    float ed     = distance(posWS, _EnemyOrigin##IDX.xyz); \
                    float ewi    = smoothstep(_EnemyRadius##IDX - 0.8, _EnemyRadius##IDX, ed); \
                    float ewo    = smoothstep(_EnemyRadius##IDX + 0.8, _EnemyRadius##IDX, ed); \
                    float ew     = ewi * ewo * _EnemyActive##IDX; \
                    eWaveAny     = saturate(eWaveAny + ew); \
                    float ewd    = max(_EnemyFadeDur##IDX, 0.001); \
                    float edel   = (ed / max(_EnemyMaxRad##IDX, 0.001)) * ewd; \
                    float earr   = _EnemyFireTime##IDX + edel; \
                    float efire  = step(0.001, _EnemyFireTime##IDX); \
                    float earriv = step(earr, _Time.y); \
                    float einr   = step(ed, _EnemyMaxRad##IDX); \
                    /* Occlusion : verifier que le pixel est du cote visible de l'onde */ \
                    /* La surface normale pointe vers l'ennemi si le pixel est expose  */ \
                    float3 eDir     = normalize(posWS - _EnemyOrigin##IDX.xyz); \
                    float3 surfNorm = ReconstructNormal(uv, off); \
                    float  facing   = saturate(dot(-eDir, surfNorm) + 0.5); \
                    float eswept = efire * earriv * einr * step(0.01, facing); \
                    float eend   = _EnemyFireTime##IDX + ewd; \
                    float eelaps = max(0.0, _Time.y - eend); \
                    float efout  = 1.0 - smoothstep(fadeDur*0.8, fadeDur, eelaps); \
                    float etf    = eswept * efout; \
                    eTrailCol    = lerp(eTrailCol, _EnemyColor##IDX.rgb, etf); \
                    eTrailAny    = saturate(eTrailAny + etf); \
                }
                ENEMY_POST(0) ENEMY_POST(1) ENEMY_POST(2) ENEMY_POST(3)
                ENEMY_POST(4) ENEMY_POST(5) ENEMY_POST(6) ENEMY_POST(7)

                // Rien de revele = noir
                float revealed = saturate(wave + trailFade + moveWave + moveTrail + eWaveAny + eTrailAny);
                if (revealed < 0.01)
                {
                    return half4(0, 0, 0, 1);
                }

                // Composition couleur
                float3 col = _EdgeColor.rgb * trailFade;
                col = lerp(col, _EdgeColor.rgb * moveTrail, moveTrail);
                col = lerp(col, eTrailCol,              eTrailAny);
                col = lerp(col, _EdgeWaveColor.rgb,     wave);
                col = lerp(col, _EdgeWaveColor.rgb,     moveWave);
                col = lerp(col, eTrailCol,              eWaveAny);

                return half4(col * finalEdge, 1.0);
            }
            ENDHLSL
        }
    }
}
