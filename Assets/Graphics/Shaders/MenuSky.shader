// Cielo nocturno del menu principal: degradado, estrellas y luna, todo procedural.
// Se dibuja sobre un cubo alrededor del jugador, antes que todo lo demas y sin escribir profundidad.
Shader "Regrowth/MenuSky"
{
    Properties
    {
        _ZenithColor ("Color cenit", Color) = (0.005, 0.01, 0.03, 1)
        _HorizonColor ("Color horizonte", Color) = (0.05, 0.10, 0.21, 1)
        _Exponent ("Curva del degradado", Range(0.1, 3)) = 0.6
        _StarColor ("Color estrellas", Color) = (0.85, 0.92, 1, 1)
        _StarDensity ("Densidad de estrellas", Float) = 45
        _StarThreshold ("Rareza de estrellas", Range(0.9, 1)) = 0.985
        _MoonColor ("Color luna", Color) = (0.9, 0.95, 1, 1)
        _MoonSize ("Tamano luna (coseno)", Range(0.99, 0.9999)) = 0.9985
        _MoonDir ("Direccion de la luna", Vector) = (0.2, 0.35, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Background"
            "Queue" = "Background"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _StarColor;
                half4 _MoonColor;
                float4 _MoonDir;
                float _Exponent;
                float _StarDensity;
                float _StarThreshold;
                float _MoonSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float Hash31(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float height = saturate(dir.y);

                half3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(height, _Exponent));

                // Estrellas: una por celda de una rejilla 3D, solo en algunas celdas
                float3 starPos = dir * _StarDensity;
                float3 cell = floor(starPos);
                float3 local = frac(starPos) - 0.5;
                float hash = Hash31(cell);
                half star = step(_StarThreshold, hash) * smoothstep(0.25, 0.0, length(local));
                half twinkle = 0.7 + 0.3 * sin(_Time.y * 2.0 + hash * 100.0);
                color += _StarColor.rgb * star * twinkle * saturate(dir.y * 4.0);

                // Luna: disco + halo suave
                float moonDot = dot(dir, normalize(_MoonDir.xyz));
                half disc = smoothstep(_MoonSize, _MoonSize + 0.0006, moonDot);
                half halo = pow(saturate(moonDot), 250.0) * 0.35;
                color += _MoonColor.rgb * (disc + halo);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
