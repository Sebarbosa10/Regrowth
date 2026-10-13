// Halo suave alrededor de la basura. Quad que el vertex shader orienta siempre hacia la camara
// (billboard), aditivo y sin escribir profundidad: el objeto tapa el centro y el brillo queda en el borde.
// La niebla lo apaga a lo lejos igual que al resto de la escena.
Shader "Regrowth/TrashGlow"
{
    Properties
    {
        _GlowColor ("Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensidad", Range(0, 4)) = 1
        _Softness ("Suavidad del borde", Range(0.5, 4)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _Intensity;
                half _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Centro del objeto en espacio de vista + el quad en el plano de la pantalla.
                // El tamano sale de la escala del objeto (eje X), asi la rotacion no le afecta.
                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float scale = length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));
                float3 positionVS = TransformWorldToView(centerWS) + float3(input.positionOS.xy * scale, 0.0);

                output.positionCS = TransformWViewToHClip(positionVS);
                output.uv = input.positionOS.xy * 2.0;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half glow = pow(saturate(1.0 - length(input.uv)), _Softness) * _Intensity;
                half3 color = _GlowColor.rgb * glow;

                // Aditivo: con niebla se funde hacia negro (no suma nada)
                color = MixFogColor(color, half3(0.0, 0.0, 0.0), input.fogFactor);
                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
