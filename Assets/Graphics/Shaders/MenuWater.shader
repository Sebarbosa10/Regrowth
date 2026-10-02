// Mar nocturno del menu principal. Barato para Quest: sin iluminacion de URP, sin texturas,
// sin textura de profundidad ni de color. Olas por vertice + ondas finas y brillo de luna por pixel.
Shader "Regrowth/MenuWater"
{
    Properties
    {
        _DeepColor ("Color agua (mirando hacia abajo)", Color) = (0.01, 0.03, 0.07, 1)
        _HorizonColor ("Color horizonte", Color) = (0.05, 0.10, 0.21, 1)
        _GlintColor ("Color del brillo de luna", Color) = (0.7, 0.9, 1, 1)
        _WaveHeight ("Altura de ola (m)", Float) = 0.12
        _WaveLength ("Longitud de ola (m)", Float) = 9
        _WaveSpeed ("Velocidad de ola", Float) = 0.6
        _RippleScale ("Escala de ondas finas", Float) = 1.3
        _RippleStrength ("Fuerza de ondas finas", Range(0, 1)) = 0.25
        _Glossiness ("Concentracion del brillo", Range(8, 400)) = 120
        _FadeStart ("Inicio de fundido al horizonte (m)", Float) = 15
        _FadeEnd ("Fin de fundido al horizonte (m)", Float) = 42
        _MoonDir ("Direccion de la luna", Vector) = (0.2, 0.35, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
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
                half4 _DeepColor;
                half4 _HorizonColor;
                half4 _GlintColor;
                float4 _MoonDir;
                float _WaveHeight;
                float _WaveLength;
                float _WaveSpeed;
                float _RippleScale;
                float _RippleStrength;
                float _Glossiness;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 slope : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // Tres olas senoidales en direcciones distintas; se guarda tambien su pendiente
                float t = _Time.y * _WaveSpeed;
                float k = 6.2831853 / max(_WaveLength, 0.01);
                float2 d1 = float2(0.944, 0.330);
                float2 d2 = float2(-0.514, 0.857);
                float2 d3 = float2(0.316, -0.949);
                float k2 = k * 1.7;
                float k3 = k * 2.6;
                float p1 = dot(d1, positionWS.xz) * k + t;
                float p2 = dot(d2, positionWS.xz) * k2 + t * 1.3;
                float p3 = dot(d3, positionWS.xz) * k3 + t * 1.9;

                positionWS.y += _WaveHeight * (sin(p1) + 0.5 * sin(p2) + 0.25 * sin(p3));
                output.slope = _WaveHeight * (cos(p1) * k * d1 + 0.5 * cos(p2) * k2 * d2 + 0.25 * cos(p3) * k3 * d3);

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 toCamera = GetCameraPositionWS() - input.positionWS;
                float dist = length(toCamera);
                float3 viewDir = toCamera / max(dist, 0.0001);

                // Ondas finas por pixel (se apagan con la distancia para que no parpadeen a lo lejos)
                float2 xz = input.positionWS.xz * _RippleScale;
                float t = _Time.y;
                float2 ripple = cos(dot(xz, float2(1.0, 0.6)) + t * 1.1) * float2(1.0, 0.6);
                ripple += cos(dot(xz, float2(-0.7, 1.3)) * 1.7 - t * 1.4) * float2(-0.7, 1.3) * 0.6;
                ripple += cos(dot(xz, float2(1.9, -1.1)) * 2.3 + t * 1.9) * float2(1.9, -1.1) * 0.35;
                float rippleFade = saturate(1.0 - dist / max(_FadeEnd, 0.01));

                float2 slope = input.slope + ripple * _RippleStrength * rippleFade;
                float3 normal = normalize(float3(-slope.x, 1.0, -slope.y));

                // Mas oscuro mirando hacia abajo, color de horizonte en angulos rasantes
                half fresnel = pow(1.0 - saturate(dot(normal, viewDir)), 4.0);
                half3 color = lerp(_DeepColor.rgb, _HorizonColor.rgb, fresnel);

                // Brillo de la luna
                float3 moonDir = normalize(_MoonDir.xyz);
                float3 halfDir = normalize(moonDir + viewDir);
                half glint = pow(saturate(dot(normal, halfDir)), _Glossiness);
                color += _GlintColor.rgb * glint;

                // El borde del plano se funde con el color del horizonte del cielo
                half fade = smoothstep(_FadeStart, _FadeEnd, dist);
                color = lerp(color, _HorizonColor.rgb, fade);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
