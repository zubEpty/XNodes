Shader "Outsiders/UI/Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1; // UV1: x=progress, y=thickness, z=angle(rad), w=softness
                float4 texcoord2 : TEXCOORD2; // UV2: x=shineCount, y=gap, z=shineColorR, w=shineColorG
                float4 texcoord3 : TEXCOORD3; // UV3: x=shineColorB, y=shineColorA, z=unused, w=unused
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 shineParams : TEXCOORD2;  // x=progress, y=thickness, z=angle(rad), w=softness
                float4 shineParams2 : TEXCOORD3; // x=shineCount, y=gap, z=shineColorR, w=shineColorG
                float2 shineParams3 : TEXCOORD4; // x=shineColorB, y=shineColorA
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                OUT.shineParams = v.texcoord1;
                OUT.shineParams2 = v.texcoord2;
                OUT.shineParams3 = v.texcoord3.xy;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = tex2D(_MainTex, IN.texcoord);
                color *= IN.color;

                // Extract shine parameters
                float progress = IN.shineParams.x;
                float thickness = IN.shineParams.y;
                float angle = IN.shineParams.z;
                float softness = IN.shineParams.w;
                int shineCount = (int)IN.shineParams2.x;
                float gap = IN.shineParams2.y;
                float4 shineColor = float4(IN.shineParams2.z, IN.shineParams2.w, IN.shineParams3.x, IN.shineParams3.y);

                // Only apply shine if progress is active (> 0 means animating)
                if (progress > -0.5)
                {
                    // Calculate the rotated coordinate for the shine line
                    float cosA = cos(angle);
                    float sinA = sin(angle);
                    float2 uv = IN.texcoord - 0.5;
                    float rotatedX = uv.x * cosA + uv.y * sinA;
                    // Map rotated coordinate to 0-1 range (accounting for rotation extending range)
                    float range = abs(cosA) * 0.5 + abs(sinA) * 0.5;
                    float normalizedPos = (rotatedX + range) / (2.0 * range);

                    float totalShineAlpha = 0.0;

                    // Calculate total band width for all shines
                    float totalBandWidth = shineCount * thickness + (shineCount - 1) * gap;
                    // Progress sweeps the band from fully off-screen left to fully off-screen right
                    float bandStart = lerp(-totalBandWidth, 1.0, progress);

                    for (int i = 0; i < shineCount && i < 5; i++)
                    {
                        float shineCenter = bandStart + i * (thickness + gap) + thickness * 0.5;
                        float dist = abs(normalizedPos - shineCenter);
                        float halfThickness = thickness * 0.5;
                        float edgeSoftness = halfThickness * softness;
                        float shineAlpha = 1.0 - smoothstep(halfThickness - edgeSoftness, halfThickness, dist);
                        totalShineAlpha += shineAlpha;
                    }

                    totalShineAlpha = saturate(totalShineAlpha);

                    // Blend shine on top of the original color (only where original alpha > 0)
                    float shineContribution = totalShineAlpha * shineColor.a * color.a;
                    color.rgb += shineColor.rgb * shineContribution;
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                color.rgb *= color.a;

                return color;
            }
            ENDCG
        }
    }
}
