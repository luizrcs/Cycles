// The mist is poison. Fullscreen treatment drawn on a camera-attached quad,
// resampling the opaque scene color: a ghosted double image (like needing
// glasses you do not have), the picture darkening, draining of color and
// drifting toward a sick green — all scaling with _Nausea (0..1), driven by
// MistNausea from fog density × time exposed.
Shader "Cycles/NauseaVision"
{
    Properties
    {
        _Nausea("Nausea", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+200" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "NauseaVision"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Nausea;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionCS.xy / _ScaledScreenParams.xy;

                half3 scene = SampleSceneColor(uv);

                // The ghost image wanders — eyes that cannot converge.
                float t = _Time.y;
                float2 ghostOffset = float2(
                    (sin(t * 0.7) * 0.5 + 0.5) * 0.018 + 0.006,
                    sin(t * 0.47) * 0.007) * _Nausea;
                half3 ghost = SampleSceneColor(uv + ghostOffset);
                half3 mixed = lerp(scene, max(scene, ghost), 0.6 * _Nausea);

                // Drain and sicken: darker, grayer, green-shifted.
                half lum = dot(mixed, half3(0.299, 0.587, 0.114));
                half3 sick = lerp(mixed, lum.xxx * half3(0.82, 1.0, 0.88), 0.55 * _Nausea);
                sick *= 1.0 - 0.28 * _Nausea;

                return half4(sick, saturate(_Nausea * 1.4));
            }
            ENDHLSL
        }
    }
}
