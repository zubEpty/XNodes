Shader "CyberSecHex/DispatchNode/CircuitSurface"
{
    Properties
    {
        [Header(Surface)]
        _BaseColor ("Base Color", Color) = (0.015, 0.07, 0.08, 0.72)
        _TintColor ("Glow Tint", Color) = (0.35, 1.0, 0.95, 1.0)
        _Alpha ("Overall Alpha", Range(0, 1)) = 0.82

        [Header(Grid)]
        _GridScale ("Fine Grid Scale", Range(8, 160)) = 72
        _GridLineWidth ("Fine Grid Width", Range(0.002, 0.08)) = 0.012
        _GridIntensity ("Fine Grid Intensity", Range(0, 3)) = 0.55
        _MajorGridEvery ("Major Grid Every N Cells", Range(2, 16)) = 6
        _MajorGridIntensity ("Major Grid Intensity", Range(0, 4)) = 1.25

        [Header(Circuit)]
        _CircuitScale ("Circuit Cell Scale", Range(2, 20)) = 7
        _NodeSize ("Node Size", Range(0.04, 0.45)) = 0.2
        _NodeBorderWidth ("Node Border Width", Range(0.005, 0.12)) = 0.035
        _LineWidth ("Connector Width", Range(0.005, 0.18)) = 0.035
        _CircuitIntensity ("Circuit Intensity", Range(0, 8)) = 3.2

        [Header(Glow And Motion)]
        _GlowRadius ("Glow Radius", Range(0.001, 0.25)) = 0.065
        _GlowIntensity ("Glow Intensity", Range(0, 8)) = 2.5
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 1.25
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.18
        _WarningColor ("Low Time Warning Color", Color) = (1.0, 0.04, 0.02, 1.0)
        _WarningIntensity ("Low Time Warning Intensity", Range(0, 4)) = 0

        [Header(Player Reveal)]
        _PlayerUV ("Player UV", Vector) = (0.5, 0.5, 0, 0)
        _PlayerGlowRadius ("Player Glow Radius", Range(0, 1)) = 0.26
        _PlayerGlowSoftness ("Player Glow Softness", Range(0.01, 1)) = 0.22
        _PlayerGlowIntensity ("Player Glow Intensity", Range(0, 8)) = 2.8
        _OutsideDarkness ("Outside Darkness", Range(0, 1)) = 0.72

        [Header(Player Data Blocks)]
        _BlockLayerIntensity ("Block Layer Intensity", Range(0, 8)) = 3.5
        _BlockSize ("Block Size", Range(0.002, 0.08)) = 0.018
        _BlockDistance ("Block Distance From Player", Range(0.01, 0.25)) = 0.07
        _BlockGap ("Block Spacing", Range(0.002, 0.12)) = 0.035
        _BlockFadeRadius ("Block Fade Radius", Range(0.01, 0.3)) = 0.14
        _BlockBlinkSpeed ("Block Blink Speed", Range(0, 12)) = 3.0
        _BlockBorderWidth ("Block Border Width", Range(0.001, 0.04)) = 0.006
        _BlockIdleVisibility ("Block Idle Visibility", Range(0, 1)) = 1.0
        _BlockMotionBoost ("Block Motion Boost", Range(0, 3)) = 1.4
        _PlayerMotion ("Player Motion", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "CircuitSurface"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TintColor;
                half _Alpha;
                half _GridScale;
                half _GridLineWidth;
                half _GridIntensity;
                half _MajorGridEvery;
                half _MajorGridIntensity;
                half _CircuitScale;
                half _NodeSize;
                half _NodeBorderWidth;
                half _LineWidth;
                half _CircuitIntensity;
                half _GlowRadius;
                half _GlowIntensity;
                half _PulseSpeed;
                half _ScanlineStrength;
                half4 _WarningColor;
                half _WarningIntensity;
                float4 _PlayerUV;
                half _PlayerGlowRadius;
                half _PlayerGlowSoftness;
                half _PlayerGlowIntensity;
                half _OutsideDarkness;
                half _BlockLayerIntensity;
                half _BlockSize;
                half _BlockDistance;
                half _BlockGap;
                half _BlockFadeRadius;
                half _BlockBlinkSpeed;
                half _BlockBorderWidth;
                half _BlockIdleVisibility;
                half _BlockMotionBoost;
                half _PlayerMotion;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float LineMask(float value, float width)
            {
                float distToLine = abs(frac(value) - 0.5);
                return 1.0 - smoothstep(width, width + fwidth(value), distToLine);
            }

            float BoxBorderMask(float2 cellUv, float size, float borderWidth)
            {
                float2 centered = abs(cellUv - 0.5);
                float outer = 1.0 - smoothstep(size, size + fwidth(cellUv.x + cellUv.y), max(centered.x, centered.y));
                float innerSize = max(size - borderWidth, 0.001);
                float inner = 1.0 - smoothstep(innerSize, innerSize + fwidth(cellUv.x + cellUv.y), max(centered.x, centered.y));
                return saturate(outer - inner);
            }

            float BoxFillMask(float2 cellUv, float size)
            {
                float2 centered = abs(cellUv - 0.5);
                return 1.0 - smoothstep(size, size + fwidth(cellUv.x + cellUv.y), max(centered.x, centered.y));
            }

            float CircuitPattern(float2 uv)
            {
                float2 circuitUv = uv * _CircuitScale;
                float2 id = floor(circuitUv);
                float2 cellUv = frac(circuitUv);

                // Repeatable deterministic pattern: enough gaps to feel authored, cheap enough for VR.
                float patternSeed = frac(sin(dot(id, float2(17.17, 41.73))) * 43758.5453);
                float nodeEnabled = step(0.38, patternSeed);

                float nodeBorder = BoxBorderMask(cellUv, _NodeSize, _NodeBorderWidth) * nodeEnabled;
                float nodeFill = BoxFillMask(cellUv, max(_NodeSize - _NodeBorderWidth * 1.35, 0.001)) * nodeEnabled * 0.28;

                float horizontalLane = 1.0 - smoothstep(_LineWidth, _LineWidth + fwidth(cellUv.y), abs(cellUv.y - 0.5));
                float verticalLane = 1.0 - smoothstep(_LineWidth, _LineWidth + fwidth(cellUv.x), abs(cellUv.x - 0.5));

                float rightSeed = frac(sin(dot(id + float2(1.0, 0.0), float2(17.17, 41.73))) * 43758.5453);
                float upSeed = frac(sin(dot(id + float2(0.0, 1.0), float2(17.17, 41.73))) * 43758.5453);
                float rightNode = step(0.38, rightSeed);
                float upNode = step(0.38, upSeed);

                float horizontalGate = nodeEnabled * rightNode;
                float verticalGate = nodeEnabled * upNode;
                float connector = horizontalLane * horizontalGate + verticalLane * verticalGate;

                return saturate(nodeBorder + nodeFill + connector);
            }

            float SquareMask(float2 uv, float2 center, float halfSize)
            {
                float2 dist = abs(uv - center);
                float edge = max(dist.x, dist.y);
                return 1.0 - smoothstep(halfSize, halfSize + max(fwidth(uv.x), fwidth(uv.y)), edge);
            }

            float SquareBorderMask(float2 uv, float2 center, float halfSize, float borderWidth)
            {
                float outer = SquareMask(uv, center, halfSize);
                float inner = SquareMask(uv, center, max(halfSize - borderWidth, 0.001));
                return saturate(outer - inner);
            }

            float PlayerBlock(float2 uv, float2 playerUv, float2 offset, float index)
            {
                float phase = frac(sin(index * 12.9898) * 43758.5453);
                float shimmer = 0.55 + 0.45 * sin(_Time.y * _BlockBlinkSpeed + phase * 6.2831);
                float2 center = playerUv + normalize(offset) * _BlockDistance + offset * _BlockGap;
                float border = SquareBorderMask(uv, center, _BlockSize, _BlockBorderWidth);
                float fill = SquareMask(uv, center, max(_BlockSize - _BlockBorderWidth * 1.8, 0.001)) * 0.32;
                return (border + fill) * shimmer;
            }

            float PlayerBlockLayer(float2 uv, float2 playerUv)
            {
                float block = 0.0;
                float fade = 1.0 - smoothstep(_BlockFadeRadius * 0.55, _BlockFadeRadius, distance(uv, playerUv));
                float motionVisibility = saturate(_BlockIdleVisibility + _PlayerMotion * _BlockMotionBoost);

                block += PlayerBlock(uv, playerUv, float2(-1.0, -1.0), 1.0) * 0.8;
                block += PlayerBlock(uv, playerUv, float2(-1.0, 0.0), 2.0);
                block += PlayerBlock(uv, playerUv, float2(-1.0, 1.0), 3.0) * 0.75;
                block += PlayerBlock(uv, playerUv, float2(1.0, -1.0), 4.0) * 0.8;
                block += PlayerBlock(uv, playerUv, float2(1.0, 0.0), 5.0);
                block += PlayerBlock(uv, playerUv, float2(1.0, 1.0), 6.0) * 0.75;
                block += PlayerBlock(uv, playerUv, float2(-0.75, -2.0), 7.0) * 0.7;
                block += PlayerBlock(uv, playerUv, float2(0.0, -2.0), 8.0);
                block += PlayerBlock(uv, playerUv, float2(0.75, -2.0), 9.0) * 0.7;
                block += PlayerBlock(uv, playerUv, float2(-0.55, -3.0), 10.0) * 0.45;
                block += PlayerBlock(uv, playerUv, float2(0.55, -3.0), 11.0) * 0.45;

                return saturate(block * fade * motionVisibility);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float fineGrid = max(LineMask(uv.x * _GridScale, _GridLineWidth), LineMask(uv.y * _GridScale, _GridLineWidth));
                float majorScale = max(_GridScale / max(_MajorGridEvery, 1.0), 1.0);
                float majorGrid = max(LineMask(uv.x * majorScale, _GridLineWidth * 1.7), LineMask(uv.y * majorScale, _GridLineWidth * 1.7));

                float circuit = CircuitPattern(uv);
#if defined(SHADER_API_MOBILE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                float glow = circuit;
                float pulse = 0.82 + 0.18 * sin(_Time.y * _PulseSpeed + uv.x * 10.0);
                float scanlines = 1.0;
                float playerDistance = distance(uv, _PlayerUV.xy);
                float reveal = 1.0 - smoothstep(_PlayerGlowRadius, _PlayerGlowRadius + _PlayerGlowSoftness, playerDistance);
                float coreGlow = reveal * reveal;
                float darkness = lerp(1.0 - _OutsideDarkness, 1.0, reveal);
                float playerBlocks = 0.0;
#else
                float glow = smoothstep(0.0, 1.0, circuit);
                glow += CircuitPattern(uv + float2(_GlowRadius, 0.0)) * 0.25;
                glow += CircuitPattern(uv - float2(_GlowRadius, 0.0)) * 0.25;
                glow += CircuitPattern(uv + float2(0.0, _GlowRadius)) * 0.25;
                glow += CircuitPattern(uv - float2(0.0, _GlowRadius)) * 0.25;

                float pulse = 0.72 + 0.28 * sin(_Time.y * _PulseSpeed + uv.x * 18.0 + uv.y * 9.0);
                float scanlines = 1.0 - _ScanlineStrength * (0.5 + 0.5 * sin((uv.y + _Time.y * 0.06) * 900.0));
                float playerDistance = distance(uv, _PlayerUV.xy);
                float reveal = 1.0 - smoothstep(_PlayerGlowRadius, _PlayerGlowRadius + _PlayerGlowSoftness, playerDistance);
                float coreGlow = 1.0 - smoothstep(0.0, max(_PlayerGlowRadius * 0.55, 0.001), playerDistance);
                float darkness = lerp(1.0 - _OutsideDarkness, 1.0, reveal);
                float playerBlocks = PlayerBlockLayer(uv, _PlayerUV.xy);
#endif

                half3 color = _BaseColor.rgb;
                color += _TintColor.rgb * fineGrid * _GridIntensity * 0.18;
                color += _TintColor.rgb * majorGrid * _MajorGridIntensity * 0.2;
                color += _TintColor.rgb * fineGrid * _GridIntensity * reveal * 0.32;
                color += _TintColor.rgb * majorGrid * _MajorGridIntensity * reveal * 0.36;
                color += _TintColor.rgb * circuit * _CircuitIntensity * pulse * (0.38 + reveal * 0.95);
                color += _TintColor.rgb * glow * _GlowIntensity * 0.35 * pulse * (0.45 + reveal);
                color += _TintColor.rgb * (reveal * 0.55 + coreGlow * 0.75) * _PlayerGlowIntensity;
                color += _TintColor.rgb * playerBlocks * _BlockLayerIntensity;
                color *= darkness;
                color *= scanlines;

                float warningStrength = saturate(_WarningIntensity);
                float warningMask = saturate(fineGrid * 0.65 + majorGrid + circuit * 0.9 + playerBlocks * 1.25);
                half3 warningGlow = _WarningColor.rgb * warningMask * warningStrength;
                color = lerp(color, color + warningGlow * 2.4, warningStrength);

                half alpha = saturate(_BaseColor.a * _Alpha);
                alpha = saturate(alpha + fineGrid * 0.08 + majorGrid * 0.12 + circuit * 0.58 + glow * 0.16 + reveal * 0.18 + playerBlocks * 0.38);
                alpha = saturate(alpha + warningMask * warningStrength * 0.16);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
