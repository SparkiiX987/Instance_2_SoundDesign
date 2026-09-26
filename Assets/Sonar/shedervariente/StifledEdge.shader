Shader "Custom/StifledEdge_Sonar"
{
    Properties
    {
        [Header(Aretes)]
        _EdgeColor       ("Couleur trace",          Color)          = (1,1,1,1)
        _EdgeWaveColor   ("Couleur crete onde",     Color)          = (0.8,1,1,1)
        _EnemyRingColor  ("Couleur onde ennemi",    Color)          = (1,0.3,0.1,1)
        _EdgeThickness   ("Epaisseur (texels)",     Range(0.5, 4))  = 1.2
        _DepthThreshold  ("Seuil silhouette",       Range(0, 0.2))  = 0.02
        _NormalThreshold ("Seuil arete",            Range(0, 2))    = 0.25

        [Header(Onde)]
        _WaveWidth       ("Largeur crete (m)",      Range(0.1, 6))  = 1.5
        _WaveBrightness  ("Intensite crete",        Range(1, 5))    = 2.0
        _ConeSoftness    ("Douceur bord cone",      Range(0, 0.5))  = 0.08
        _FadeDuration    ("Duree trace (s)",        Float)          = 4.0
        _EdgeFadeMult    ("Multiplicateur duree",   Float)          = 1.0
        _DistanceFalloff ("Attenuation distance",   Range(0, 1))    = 0.6
        _TrailFloor      ("Trace minimale",         Range(0, 1))    = 0.0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Globaux sonar joueur (cri).
            // _WaveOrigin et _ConeForward restent FIGES sur le tir tant que
            // la trace vit (cf. Sonar.PushShaderGlobals).
            float4 _WaveOrigin;
            float  _WaveRadius;
            float  _WaveActive;
            float4 _ConeForward;
            float  _ConeHalfAngleCos;
            float  _WaveFireTime;
            float  _WaveMaxRadius;
            float  _WaveFadeDuration;

            // Onde de mouvement (pas du renard) : cercle, sans cone.
            float4 _MoveWaveOrigin;
            float  _MoveWaveRadius;
            float  _MoveWaveActive;
            float  _MoveWaveFireTime;
            float  _MoveWaveMaxRadius;
            float  _MoveWaveFadeDuration;

            // Onde d echolocalisation de l ennemi (EnemyEcholocation.cs).
            float4 _EnemyWaveOrigin;
            float  _EnemyWaveRadius;
            float  _EnemyWaveActive;
            float  _EnemyWaveFireTime;
            float  _EnemyWaveMaxRadius;
            float  _EnemyWaveFadeDuration;

            // Emetteurs sonar (SonarEmitterManager, 15 slots).
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
            float4 _EnemyRingColor;
            float  _EdgeThickness;
            float  _DepthThreshold;
            float  _NormalThreshold;
            float  _WaveWidth;
            float  _WaveBrightness;
            float  _ConeSoftness;
            float  _FadeDuration;
            float  _EdgeFadeMult;
            float  _DistanceFalloff;
            float  _TrailFloor;

            // ---------------------------------------------------------
            //  Revelation : facteur commun a toutes les ondes
            // ---------------------------------------------------------
            // Chaque pixel s allume au moment ou le front d onde l atteint
            // (arrivee = tir + distance/vitesse) puis decroit depuis CE
            // moment-la. La revelation recule donc du proche vers le loin
            // au lieu de s eteindre d un bloc.
            //
            //  dist      distance pixel <-> origine de l onde
            //  fireTime  _Time.y du tir (0 = jamais tire)
            //  maxRadius portee de l onde
            //  travel    duree de propagation sur toute la portee
            //  mask      masque supplementaire (cone, orientation) ; 1 si aucun
            float TrailFactor(float dist, float fireTime, float maxRadius, float travel, float mask)
            {
                float fired   = step(0.001, fireTime);
                float inRange = step(dist, maxRadius);

                float delay   = (dist / max(maxRadius, 0.001)) * max(travel, 0.001);
                float since   = _Time.y - (fireTime + delay);   // < 0 : onde pas encore arrivee
                float arrived = step(0.0, since);

                float fadeDur = max(_FadeDuration * _EdgeFadeMult, 0.001);
                float fade    = saturate(1.0 - since / fadeDur);
                fade          = fade * fade;                    // decroissance douce

                // Le lointain revient moins fort : donne la lecture de profondeur.
                float falloff = lerp(1.0, 1.0 - saturate(dist / max(maxRadius, 0.001)), _DistanceFalloff);

                return fired * inRange * arrived * mask * fade * falloff;
            }

            // Crete lumineuse du front d onde, pendant la propagation.
            float CrestFactor(float dist, float radius, float active, float mask)
            {
                float c = 1.0 - saturate(abs(dist - radius) / max(_WaveWidth, 0.01));
                return c * c * active * mask;
            }

            // ---------------------------------------------------------
            //  Fragment
            // ---------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv  = input.texcoord;
                float2 off = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * _EdgeThickness;

                float rawC = SampleSceneDepth(uv);
                float eyeC = LinearEyeDepth(rawC, _ZBufferParams);

                // Ciel / plan lointain : rien a reveler.
                if (eyeC >= _ProjectionParams.z * 0.99)
                {
                    return half4(0, 0, 0, 1);
                }

                float3 posC = ComputeWorldSpacePosition(uv, rawC, UNITY_MATRIX_I_VP);

                // Normale monde du GBuffer. Contrairement a une normale
                // reconstruite depuis la depth, elle ne subit ni la
                // triangulation des meshes ni le raccourci perspectif des
                // surfaces vues en rasant.
                float3 nC   = SampleSceneNormals(uv);
                float  nLen = length(nC);
                nC = nLen > 0.1 ? nC / nLen : float3(0, 1, 0);

                // ── 1. Silhouettes ───────────────────────────────────
                // Ecart de profondeur relatif a la distance, pour qu un
                // meme seuil tienne a 2 m comme a 40 m.
                float eyeR = LinearEyeDepth(SampleSceneDepth(uv + float2( off.x, 0)), _ZBufferParams);
                float eyeL = LinearEyeDepth(SampleSceneDepth(uv + float2(-off.x, 0)), _ZBufferParams);
                float eyeU = LinearEyeDepth(SampleSceneDepth(uv + float2(0,  off.y)), _ZBufferParams);
                float eyeD = LinearEyeDepth(SampleSceneDepth(uv + float2(0, -off.y)), _ZBufferParams);
                float depthDelta = max(max(abs(eyeR - eyeC), abs(eyeL - eyeC)),
                                       max(abs(eyeU - eyeC), abs(eyeD - eyeC)));
                float depthEdge  = depthDelta / max(eyeC, 0.001);

                // Un sol vu en rasant creuse un enorme ecart de profondeur
                // entre deux pixels voisins sans qu il y ait la moindre
                // arete : on relache le seuil proportionnellement.
                float3 viewDir = normalize(GetCameraPositionWS() - posC);
                float  NdotV   = saturate(dot(nC, viewDir));
                float  tolerance = _DepthThreshold / max(NdotV, 0.06);
                float  eDepth  = smoothstep(tolerance, tolerance * 2.0, depthEdge);

                // ── 2. Aretes vives : croix de Roberts sur les normales ──
                // Sur une surface plane les deux normales opposees sont
                // identiques (produit scalaire = 1, donc 0) quel que soit
                // l angle de vue. Sur une arete, elles divergent.
                float3 nA = SampleSceneNormals(uv + float2( off.x,  off.y));
                float3 nB = SampleSceneNormals(uv + float2(-off.x, -off.y));
                float3 nE = SampleSceneNormals(uv + float2(-off.x,  off.y));
                float3 nF = SampleSceneNormals(uv + float2( off.x, -off.y));
                float  normalEdge = (1.0 - dot(nA, nB)) + (1.0 - dot(nE, nF));
                float  eNormal = smoothstep(_NormalThreshold, _NormalThreshold * 2.0, normalEdge);

                float edge = saturate(eDepth + eNormal);

                if (edge < 0.01)
                {
                    return half4(0, 0, 0, 1);
                }

                // ══ Onde du joueur (cri) : cone fige au moment du tir ══
                float3 coneF  = _ConeForward.xyz;
                float  coneLn = length(coneF);
                coneF = coneLn > 1e-4 ? coneF / coneLn : float3(0, 0, 1);

                float  dist    = distance(posC, _WaveOrigin.xyz);
                float3 toPixel = (posC - _WaveOrigin.xyz) / max(dist, 1e-4);
                // Bord adouci : le cone etant fige dans le monde, sa limite
                // balaie l ecran quand on tourne la tete. En dur elle se
                // lisait comme une coupure nette au milieu du decor.
                float  inCone  = smoothstep(_ConeHalfAngleCos - _ConeSoftness,
                                            _ConeHalfAngleCos + _ConeSoftness * 0.25,
                                            dot(toPixel, coneF));

                float trailFade = TrailFactor(dist, _WaveFireTime, _WaveMaxRadius, _WaveFadeDuration, inCone);
                float crest     = CrestFactor(dist, _WaveRadius, _WaveActive, inCone);

                // ══ Onde de mouvement, omnidirectionnelle ═════════════
                float moveDist  = distance(posC, _MoveWaveOrigin.xyz);
                float moveTrail = TrailFactor(moveDist, _MoveWaveFireTime, _MoveWaveMaxRadius, _MoveWaveFadeDuration, 1.0);
                float moveCrest = CrestFactor(moveDist, _MoveWaveRadius, _MoveWaveActive, 1.0);

                // ══ Echolocalisation de l ennemi ══════════════════════
                float enemyDist  = distance(posC, _EnemyWaveOrigin.xyz);
                float enemyTrail = TrailFactor(enemyDist, _EnemyWaveFireTime, _EnemyWaveMaxRadius, _EnemyWaveFadeDuration, 1.0);
                float enemyCrest = CrestFactor(enemyDist, _EnemyWaveRadius, _EnemyWaveActive, 1.0);

                // ══ Emetteurs sonar (jouets, pieges...) ═══════════════
                float  eTrailAny = 0;
                float3 eTrailCol = float3(0, 0, 0);

                #define ENEMY_POST(IDX) { \
                    float ed = distance(posC, _EnemyOrigin##IDX.xyz); \
                    /* Attenue les surfaces qui tournent le dos a l emetteur */ \
                    float3 eDir   = (posC - _EnemyOrigin##IDX.xyz) / max(ed, 1e-4); \
                    float  facing = saturate(dot(-eDir, nC) * 0.5 + 0.6); \
                    float  et = TrailFactor(ed, _EnemyFireTime##IDX, _EnemyMaxRad##IDX, _EnemyFadeDur##IDX, facing); \
                    float  ec = CrestFactor(ed, _EnemyRadius##IDX, _EnemyActive##IDX, facing); \
                    float  e  = saturate(et + ec * _WaveBrightness); \
                    eTrailCol = lerp(eTrailCol, _EnemyColor##IDX.rgb, e); \
                    eTrailAny = max(eTrailAny, e); \
                }
                ENEMY_POST(0)  ENEMY_POST(1)  ENEMY_POST(2)  ENEMY_POST(3)  ENEMY_POST(4)
                ENEMY_POST(5)  ENEMY_POST(6)  ENEMY_POST(7)  ENEMY_POST(8)  ENEMY_POST(9)
                ENEMY_POST(10) ENEMY_POST(11) ENEMY_POST(12) ENEMY_POST(13) ENEMY_POST(14)

                // ══ Composition ═══════════════════════════════════════
                float3 col = float3(0, 0, 0);
                col += _EdgeColor.rgb      * max(max(trailFade, moveTrail), _TrailFloor);
                col += _EdgeWaveColor.rgb  * saturate(crest + moveCrest) * _WaveBrightness;
                col += _EnemyRingColor.rgb * saturate(enemyTrail + enemyCrest * _WaveBrightness);
                col += eTrailCol           * eTrailAny;

                return half4(col * edge, 1.0);
            }
            ENDHLSL
        }
    }
}
