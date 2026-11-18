Shader "GameJam/Crew Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [PerRendererData] _LocalOutline ("Local player outline", Float) = 0
        _OutlinePixels ("Outline width (pixels)", Range(1, 5)) = 3.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex CrewVertex
            #pragma fragment CrewFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _LocalOutline;
                float _OutlinePixels;
            CBUFFER_END

            float4 _MainTex_TexelSize;

            Varyings CrewVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half AlphaAt(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 CrewFragment(Varyings input) : SV_Target
            {
                half4 sprite = CommonUnlitFragment(input, input.color);
                if (_LocalOutline < 0.5)
                    return sprite;

                float2 delta = min(fwidth(input.uv) * _OutlinePixels, _MainTex_TexelSize.xy * 8.0);
                if (sprite.a >= 0.995)
                    return sprite;
                float2 x = float2(delta.x, 0);
                float2 y = float2(0, delta.y);
                float2 diagonal = delta * 0.7071;
                float2 opposite = float2(diagonal.x, -diagonal.y);
                half around = max(
                    max(max(AlphaAt(input.uv + x), AlphaAt(input.uv - x)),
                        max(AlphaAt(input.uv + y), AlphaAt(input.uv - y))),
                    max(max(AlphaAt(input.uv + diagonal), AlphaAt(input.uv - diagonal)),
                        max(AlphaAt(input.uv + opposite), AlphaAt(input.uv - opposite))));
                // Ignore the translucent ground shadow in the crew art.
                half outline = smoothstep(0.5, 0.8, around) * input.color.a * (1.0 - sprite.a);
                half alpha = sprite.a + outline;
                return half4((sprite.rgb * sprite.a + outline) / max(alpha, 0.001), alpha);
            }
            ENDHLSL
        }
    }
}
