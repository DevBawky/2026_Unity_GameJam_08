// 바람에 흔들리는 스프라이트.
// 기본(나무 한 그루): 밑동(uv.y < _Anchor)은 고정하고 위로 갈수록 꼭짓점을 좌우로 흔든다.
// _Ripple(나무·풀이 여러 개 그려진 배경 띠): 꼭짓점이 4개뿐이라 대신 픽셀 샘플 위치를 물결처럼 흔든다.
// 월드 x로 위상을 바꾸고 느린 돌풍(_Gust)을 곱해 바람이 지나가는 것처럼 보이게 한다.
// _PixelArt를 켜면 도트 그래픽용으로 흔들림을 픽셀 단위로 끊고 시간도 _PixelFps 단위로 끊는다.
Shader "GameJam/Wind Sway Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        _SwayStrength ("Sway strength (world units at top)", Range(0, 0.5)) = 0.18
        _SwaySpeed ("Sway speed (rad/s)", Range(0, 6)) = 1.8
        _WaveScale ("Phase per world unit", Range(0, 2)) = 0.35
        _Anchor ("Fixed bottom (uv.y)", Range(0, 0.9)) = 0.25
        _Gust ("Gust (0 = steady)", Range(0, 1)) = 0.5
        [Toggle] _Ripple ("Ripple mode (background strip)", Float) = 0
        _RipplePixels ("Ripple: texture pixels", Range(0, 8)) = 3
        _RippleRows ("Ripple: waves per world unit (y)", Range(0, 8)) = 2.5
        [Toggle] _PixelArt ("Pixel art mode", Float) = 0
        _PixelsPerUnit ("Pixel art: pixels per unit", Float) = 32
        _PixelFps ("Pixel art: sway frames per second", Range(1, 30)) = 8
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
            #pragma vertex WindVertex
            #pragma fragment WindFragment
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
                float2 windWS : TEXCOORD4;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _SwayStrength;
                float _SwaySpeed;
                float _WaveScale;
                float _Anchor;
                float _Gust;
                float _Ripple;
                float _RipplePixels;
                float _RippleRows;
                float _PixelArt;
                float _PixelsPerUnit;
                float _PixelFps;
            CBUFFER_END

            float WindTime()
            {
                float time = _Time.y;
                if (_PixelArt > 0.5)
                    time = floor(time * _PixelFps) / _PixelFps;
                return time;
            }

            // -1~1 흔들림에 돌풍 세기를 곱한 값. 돌풍은 느리게 x 방향으로 지나간다.
            float WindWave(float time, float x, float extraPhase)
            {
                float phase = time * _SwaySpeed + x * _WaveScale + extraPhase;
                float sway = sin(phase) * 0.7 + sin(phase * 2.3 + 1.1) * 0.3;
                float gust = 1.0 - _Gust + _Gust * (0.5 + 0.5 * sin(time * 0.6 - x * 0.12));
                return sway * gust;
            }

            Varyings WindVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);

                float3 world = TransformObjectToWorld(input.positionOS);
                output.windWS = world.xy;

                if (_Ripple < 0.5)
                {
                    float height = saturate((input.uv.y - _Anchor) / max(1.0 - _Anchor, 0.001));
                    float offset = _SwayStrength * height * WindWave(WindTime(), world.x, 0.0);
                    if (_PixelArt > 0.5)
                        offset = round(offset * _PixelsPerUnit) / _PixelsPerUnit;
                    world.x += offset;
                    output.positionCS = TransformWorldToHClip(world);
                }

                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 WindFragment(Varyings input) : SV_Target
            {
                if (_Ripple > 0.5)
                {
                    float rows = input.windWS.y * _RippleRows * 6.2831853;
                    float pixels = _RipplePixels * WindWave(WindTime(), input.windWS.x * 3.0, rows);
                    if (_PixelArt > 0.5)
                        pixels = round(pixels);
                    input.uv.x += pixels * _MainTex_TexelSize.x;
                }
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
