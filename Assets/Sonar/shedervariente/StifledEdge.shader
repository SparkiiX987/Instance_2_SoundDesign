Shader "Custom/StifledEdge_Sonar"
{
    Properties
    {
        [Header(Aretes)]
        _EdgeColor       ("Couleur trace",          Color)          = (1,1,1,1)
        _EdgeWaveColor   ("Couleur crete onde",     Color)          = (0.8,1,1,1)
        _EnemyRingColor  ("Couleur onde ennemi",    Color)          = (1,0.3,0.1,1)
        _EdgeFarColor    ("Couleur retour lointain", Color)          = (0.35,0.5,0.7,1)
        _EdgeThickness   ("Epaisseur (texels)",     Range(0.5, 4))  = 1.2
        _EdgeThreshold   ("Sensibilite aretes",     Range(0.01, 1)) = 0.08

        [Header(Onde)]
        _CrestWidth      ("Largeur crete (part portee)", Range(0.02, 1)) = 0.15
        _WaveBrightness  ("Intensite crete",        Range(1, 5))    = 2.0
        _ConeSoftness    ("Douceur bord cone",      Range(0, 0.5))  = 0.25
        _FadeDuration    ("Duree trace (s)",        Float)          = 4.0
        _EdgeFadeMult    ("Multiplicateur duree",   Float)          = 1.0
        _DistanceFalloff ("Attenuation distance",   Range(0, 1))    = 0.6
        _RangeSoftness   ("Douceur bord portee",     Range(0.02, 1)) = 0.4
        _TrailStrength   ("Force de la trace",       Range(0, 1))    = 0.3

        [Header(Impulsion)]
        _WaveEase        ("Deceleration du front",   Range(1, 2.5))  = 1.35
        _EchoDecay       ("Intensite des repliques", Range(0, 1))    = 0.45
        _EchoSpacing     ("Ecart des repliques",     Range(1, 6))    = 2.5
        _BurstSize       ("Taille eclat depart",     Range(0, 0.5))  = 0.12
        _BurstDecay      ("Duree eclat depart (s)",  Range(0.05, 1)) = 0.25

        [Header(Emetteurs du decor)]
        // Un prop de trois metres et un cri de douze ne sont pas le meme
        // phenomene : une crete a 15 pour cent de la portee fait 1,8 m sur
        // le cri mais 45 cm sur un conduit, ou elle ne se voit plus.
        _EmitterCrestWidth ("Largeur crete emetteurs", Range(0.02, 1)) = 0.5
        _EmitterTrailForce ("Force trace emetteurs",   Range(0, 1))    = 0.8
        _TrailFloor      ("Trace minimale",         Range(0, 1))    = 0.0

        [Header(Debug)]
        // 0 = rendu normal
        // 1 = masque des aretes seul, sans le sonar
        // 2 = masque de revelation seul, sans les aretes
        [Enum(Rendu,0,Aretes,1,Revelation,2)] _DebugMode ("Mode debug", Float) = 0
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

            // Emetteurs sonar (SonarEmitterManager, 20 slots).
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
            float4 _EnemyOrigin15; float _EnemyRadius15; float _EnemyActive15; float4 _EnemyColor15; float _EnemyFireTime15; float _EnemyMaxRad15; float _EnemyFadeDur15;
            float4 _EnemyOrigin16; float _EnemyRadius16; float _EnemyActive16; float4 _EnemyColor16; float _EnemyFireTime16; float _EnemyMaxRad16; float _EnemyFadeDur16;
            float4 _EnemyOrigin17; float _EnemyRadius17; float _EnemyActive17; float4 _EnemyColor17; float _EnemyFireTime17; float _EnemyMaxRad17; float _EnemyFadeDur17;
            float4 _EnemyOrigin18; float _EnemyRadius18; float _EnemyActive18; float4 _EnemyColor18; float _EnemyFireTime18; float _EnemyMaxRad18; float _EnemyFadeDur18;
            float4 _EnemyOrigin19; float _EnemyRadius19; float _EnemyActive19; float4 _EnemyColor19; float _EnemyFireTime19; float _EnemyMaxRad19; float _EnemyFadeDur19;

            float4 _EdgeColor;
            float4 _EdgeWaveColor;
            float4 _EnemyRingColor;
            float4 _EdgeFarColor;
            float  _EdgeThickness;
            float  _EdgeThreshold;
            float  _CrestWidth;
            float  _WaveBrightness;
            float  _ConeSoftness;
            float  _FadeDuration;
            float  _EdgeFadeMult;
            float  _DistanceFalloff;
            float  _RangeSoftness;
            float  _TrailStrength;
            float  _WaveEase;
            float  _EchoDecay;
            float  _EchoSpacing;
            float  _BurstSize;
            float  _BurstDecay;
            float  _EmitterCrestWidth;
            float  _EmitterTrailForce;
            float  _TrailFloor;
            float  _DebugMode;

            // ---------------------------------------------------------
            //  Revelation : facteur commun a toutes les ondes
            // ---------------------------------------------------------
            // Chaque pixel s allume au moment ou le front d onde l atteint
            // (arrivee = tir + distance/vitesse) puis decroit depuis CE
            // moment-la. La revelation recule donc du proche vers le loin
            // au lieu de s eteindre d un bloc.
            //
            // La crete n est pas un objet separe : c est la tete de la
            // trace, un pic bref sur la meme base de temps. Avant, elle
            // etait pilotee par le rayon vivant du tween et par un flag
            // "active" que le C# remettait a zero des la fin de la
            // propagation : elle disparaissait donc d une frame a l autre,
            // et sa largeur etait une valeur en metres commune au cri du
            // joueur (20 m de portee) et aux petits emetteurs (3 m), ou
            // elle couvrait la moitie de la sphere. D ou deux etats
            // distincts, un fort coupe net puis un faible.
            //
            //  dist      distance pixel <-> origine de l onde
            //  fireTime  _Time.y du tir (0 = jamais tire)
            //  maxRadius portee de l onde
            //  travel    duree de propagation sur toute la portee
            //  mask      masque supplementaire (cone, orientation) ; 1 si aucun
            //
            //  retour .x intensite totale, de 0 a _WaveBrightness
            //  retour .y poids de la crete, de 0 a 1, pour la teinte
            // Une bande de crete, centree sur since = offset.
            float CrestAt(float since, float width)
            {
                float c = saturate(1.0 - abs(since) / width);
                return c * c;
            }

            //  retour .x intensite totale
            //  retour .y poids de la crete, de 0 a 1, pour la teinte
            //  retour .z distance normalisee, de 0 a 1, pour la couleur
            float3 WaveFactor(float dist, float fireTime, float maxRadius, float travel, float mask,
                              float crestWidth, float trailForce)
            {
                float fired = step(0.001, fireTime);
                float d01   = saturate(dist / max(maxRadius, 0.001));

                // Le front decelere en s eloignant : il part vif puis prend
                // son temps sur le lointain, ce qui laisse lire la
                // profondeur au lieu de tout balayer d un coup.
                float delay   = pow(d01, _WaveEase) * max(travel, 0.001);
                float since   = _Time.y - (fireTime + delay);   // < 0 : onde pas encore arrivee
                float arrived = step(0.0, since);

                // La trace ne commence qu au passage du front et decroit
                // lentement derriere lui.
                float fadeDur = max(_FadeDuration * _EdgeFadeMult, 0.001);
                float trail   = arrived * saturate(1.0 - since / fadeDur);
                trail         = trail * trail;                  // decroissance douce

                // La crete est une bande SYMETRIQUE autour du front : elle
                // deborde autant devant que derriere, ce qui lui donne sa
                // lecture de vague qui voyage. C est la meme chose que
                // |dist - radius| < largeur, mais exprimee en temps :
                //
                //     since = (radius - dist) * travel / maxRadius
                //
                // donc |since| * maxRadius / travel = |radius - dist|.
                //
                // L ecrire ainsi plutot qu a partir du rayon vivant du tween
                // a deux consequences. La crete ne depend plus du flag
                // _WaveActive, que le C# remet a zero des la fin de la
                // propagation : elle ne peut plus etre coupee net. Et sa
                // largeur, une fraction du temps de propagation, vaut la
                // meme fraction de la portee : elle reste lisible aussi bien
                // sur le cri du joueur que sur un prop de deux metres.
                float crestT = max(crestWidth * max(travel, 0.001), 0.01);

                // Un cri n est pas une impulsion pure : on fait suivre le
                // front de deux repliques de plus en plus faibles. C est ce
                // train d ondes qui donne sa matiere a l echo.
                float gap   = crestT * _EchoSpacing;
                float crest = CrestAt(since, crestT)
                            + CrestAt(since - gap,       crestT) * _EchoDecay
                            + CrestAt(since - gap * 2.0, crestT) * _EchoDecay * _EchoDecay;

                // Eclat au depart : l onde nait a la gueule du renard.
                // Independant de la distance parcourue, il marque l instant
                // du cri lui-meme.
                float tSince = _Time.y - fireTime;
                float burst  = step(0.0, tSince)
                             * exp(-tSince / max(_BurstDecay, 0.01))
                             * (1.0 - smoothstep(0.0, max(_BurstSize, 0.001), d01));

                // Le lointain revient moins fort : donne la lecture de profondeur.
                float falloff = lerp(1.0, 1.0 - d01, _DistanceFalloff);

                // Extinction douce sur la fin de la portee. Avec un simple
                // step(dist, maxRadius), l intensite tombait d un coup de
                // (1 - _DistanceFalloff) a zero : un anneau franc au bord de
                // la zone revelee.
                float rangeFade = 1.0 - smoothstep(1.0 - _RangeSoftness, 1.0, d01);

                // arrived est deja porte par trail : la crete doit pouvoir
                // eclairer juste devant le front.
                float live = fired * mask * falloff * rangeFade;

                // La trace derriere le front n est qu une memoire sourde :
                // c est le contraste avec la crete qui fait lire une onde qui
                // voyage plutot qu un eclairage global qui s eteint.
                return float3(live * (trail * trailForce + (crest + burst) * _WaveBrightness),
                              live * saturate(crest + burst),
                              d01);
            }

            float3 DepthToWorld(float2 uv)
            {
                return ComputeWorldSpacePosition(uv, SampleSceneDepth(uv), UNITY_MATRIX_I_VP);
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

                // ══ Detection d aretes : test de colinearite ══════════
                // Une droite de l ecran se reprojette en droite sur une
                // surface plane, quelle que soit son inclinaison. Les trois
                // points gauche / centre / droite sont donc exactement
                // alignes, et les deux directions exactement opposees : le
                // terme vaut 0 meme sur un sol vu en rasant. Seule une
                // vraie arete les fait diverger.
                //
                // C est ce qui manquait aux deux versions precedentes : le
                // Sobel accrochait la triangulation des meshes, et l ecart
                // au plan tangent explosait des que le raccourci perspectif
                // ecrasait un voisin.
                float3 dR = DepthToWorld(uv + float2( off.x, 0)) - posC;
                float3 dL = DepthToWorld(uv + float2(-off.x, 0)) - posC;
                float3 dU = DepthToWorld(uv + float2(0,  off.y)) - posC;
                float3 dD = DepthToWorld(uv + float2(0, -off.y)) - posC;

                float3 uR = dR / max(length(dR), 1e-6);
                float3 uL = dL / max(length(dL), 1e-6);
                float3 uU = dU / max(length(dU), 1e-6);
                float3 uD = dD / max(length(dD), 1e-6);

                // 0 sur une surface plane, ~1 sur un angle droit,
                // jusqu a 2 sur une silhouette franche.
                float curvH = 1.0 + dot(uR, uL);
                float curvV = 1.0 + dot(uU, uD);
                float edge  = smoothstep(_EdgeThreshold, _EdgeThreshold * 2.0, max(curvH, curvV));

                if (_DebugMode > 0.5 && _DebugMode < 1.5)
                {
                    return half4(edge, edge, edge, 1.0);
                }

                if (edge < 0.01 && _DebugMode < 1.5)
                {
                    return half4(0, 0, 0, 1);
                }

                // Normale approchee, seulement pour orienter les emetteurs.
                float3 nRaw = cross(uU, uR);
                float  nLen = length(nRaw);
                float3 nC   = nLen > 1e-6 ? nRaw / nLen : float3(0, 1, 0);

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

                float3 cri   = WaveFactor(dist, _WaveFireTime, _WaveMaxRadius, _WaveFadeDuration, inCone, _CrestWidth, _TrailStrength);

                // ══ Onde de mouvement, omnidirectionnelle ═════════════
                float  moveDist = distance(posC, _MoveWaveOrigin.xyz);
                float3 pas      = WaveFactor(moveDist, _MoveWaveFireTime, _MoveWaveMaxRadius, _MoveWaveFadeDuration, 1.0, _CrestWidth, _TrailStrength);

                // ══ Echolocalisation de l ennemi ══════════════════════
                float  enemyDist = distance(posC, _EnemyWaveOrigin.xyz);
                float3 ennemi    = WaveFactor(enemyDist, _EnemyWaveFireTime, _EnemyWaveMaxRadius, _EnemyWaveFadeDuration, 1.0, _CrestWidth, _TrailStrength);

                // ══ Emetteurs sonar (jouets, pieges...) ═══════════════
                float  eTrailAny = 0;
                float3 eTrailCol = float3(0, 0, 0);

                #define ENEMY_POST(IDX) { \
                    float ed = distance(posC, _EnemyOrigin##IDX.xyz); \
                    /* Attenue les surfaces qui tournent le dos a l emetteur */ \
                    float3 eDir   = (posC - _EnemyOrigin##IDX.xyz) / max(ed, 1e-4); \
                    float  facing = saturate(dot(-eDir, nC) * 0.5 + 0.6); \
                    float3 ew = WaveFactor(ed, _EnemyFireTime##IDX, _EnemyMaxRad##IDX, _EnemyFadeDur##IDX, facing, _EmitterCrestWidth, _EmitterTrailForce); \
                    float  e  = saturate(ew.x); \
                    eTrailCol = lerp(eTrailCol, _EnemyColor##IDX.rgb, e); \
                    eTrailAny = max(eTrailAny, e); \
                }
                ENEMY_POST(0)  ENEMY_POST(1)  ENEMY_POST(2)  ENEMY_POST(3)  ENEMY_POST(4)
                ENEMY_POST(5)  ENEMY_POST(6)  ENEMY_POST(7)  ENEMY_POST(8)  ENEMY_POST(9)
                ENEMY_POST(10)  ENEMY_POST(11)  ENEMY_POST(12)  ENEMY_POST(13)  ENEMY_POST(14)
                ENEMY_POST(15)  ENEMY_POST(16)  ENEMY_POST(17)  ENEMY_POST(18)  ENEMY_POST(19)

                // Masque de revelation seul : vert = trace du cri,
                // rouge = onde de mouvement, bleu = ennemis et emetteurs.
                if (_DebugMode >= 1.5)
                {
                    return half4(saturate(pas.x), saturate(cri.x),
                                 saturate(ennemi.x + eTrailAny), 1.0);
                }

                // ══ Composition ═══════════════════════════════════════
                // La teinte de crete se fond dans la trace au lieu de
                // former un palier : plus de rupture visible entre les deux.
                // Le retour lointain ne sonne pas comme le proche : on le
                // teinte differemment pour que la distance se lise d un coup
                // d oeil, sans avoir a attendre l arrivee du front.
                float3 dom    = (cri.x >= pas.x) ? cri : pas;
                float3 proche = lerp(_EdgeColor.rgb, _EdgeFarColor.rgb, dom.z);

                float3 col = lerp(proche, _EdgeWaveColor.rgb, saturate(cri.y + pas.y))
                           * max(max(cri.x, pas.x), _TrailFloor);
                col += _EnemyRingColor.rgb * ennemi.x;
                col += eTrailCol           * eTrailAny;

                return half4(col * edge, 1.0);
            }
            ENDHLSL
        }
    }
}
