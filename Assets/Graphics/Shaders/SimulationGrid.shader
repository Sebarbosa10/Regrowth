// Sala de simulacion: rejilla procedural sin iluminacion + dissolve por baldosas.
// Usa la misma propiedad _Dissolve (0 = visible, 2 = oculto) que Dissolve.ShaderGraph,
// asi FirstGrabDissolveEvent la controla sin cambios.
Shader "Regrowth/SimulationGrid"
{
    Properties
    {
        _BaseColor ("Color de fondo", Color) = (0.01, 0.02, 0.05, 1)
        _LineColor ("Color de lineas", Color) = (0.1, 0.9, 1, 1)
        _CellSize ("Tamano de celda (m)", Float) = 0.5
        _LineWidth ("Grosor de linea", Range(0.001, 0.2)) = 0.02
        _EdgeColor ("Color de baldosa al disolverse", Color) = (1, 1, 1, 1)
        _EdgeWidth ("Ancho del destello", Range(0, 0.3)) = 0.08
        _Dissolve ("Dissolve (0 visible, 2 oculto)", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _LineColor;
                half4 _EdgeColor;
                float _CellSize;
                float _LineWidth;
                float _EdgeWidth;
                float _Dissolve;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                return output;
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Coordenadas 2D de la cara (en celdas) segun hacia donde mira la pared
                float3 p = input.positionOS / max(_CellSize, 0.0001);
                float3 n = abs(input.normalOS);
                float2 uv = n.x > 0.5 ? p.zy : (n.y > 0.5 ? p.xz : p.xy);

                // Dissolve por baldosa: cada celda tiene un umbral aleatorio
                float faceId = dot(input.normalOS, float3(1.0, 2.0, 3.0));
                float noise = Hash21(floor(uv) + faceId * 17.0);
                float threshold = _Dissolve * 0.5;
                float diff = noise - threshold;
                clip(diff);

                // Lineas de la rejilla con antialiasing
                float2 d = abs(frac(uv - 0.5) - 0.5);
                float2 aa = fwidth(uv);
                float2 lines = 1.0 - smoothstep(_LineWidth, _LineWidth + aa, d);
                half grid = max(lines.x, lines.y);

                half3 color = lerp(_BaseColor.rgb, _LineColor.rgb, grid);

                // Destello de las baldosas que estan a punto de desaparecer
                half edge = (1.0 - smoothstep(0.0, max(_EdgeWidth, 0.0001), diff)) * step(0.0001, threshold);
                color = lerp(color, _EdgeColor.rgb, edge);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
