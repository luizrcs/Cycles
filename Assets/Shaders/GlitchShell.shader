// The replay double is a glitch in time — but the failure language is a dying
// ANALOG television, not a GPU: pale desaturated static (snow), horizontal
// tearing whose offset varies along each slice (torn, never slab-like), a
// faint warm/cool fringe instead of saturated red/cyan, band flutter, and
// rare vertical-hold slips where the whole image pops downward for a tick.
// Rendered as a thin shell over the normally lit body; everything quantizes
// on hashed time ticks — it SNAPS, never eases. Driven by _Intensity
// (0 = barely an outline, 1 = the picture barely holds together). Applied at
// runtime by AntiPlayerGlitch (and ParadoxBleed) to shell renderers that
// share the original skeleton.
Shader "Cycles/GlitchShell"
{
    Properties
    {
        _Intensity("Glitch Intensity", Range(0, 1)) = 0.15
        _Inflate("Shell inflate (m)", Float) = 0.012
        _SliceScale("Slices per meter", Float) = 22.0
        _TickRate("Glitch ticks per second", Float) = 13.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "GlitchShell"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Intensity;
                float _Inflate;
                float _SliceScale;
                float _TickRate;
            CBUFFER_END

            float Hash(float x)
            {
                return frac(sin(x * 12.9898) * 43758.5453);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  band       : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);

                // Discrete time tick: all randomness snaps on this, giving
                // the broken-signal stutter instead of smooth animation.
                float tick = floor(_Time.y * _TickRate);

                // Horizontal scan band of this vertex (world-height slices).
                float band = floor(positionWS.y * _SliceScale + Hash(tick) * 3.0);

                // Tearing: a hashed few bands slip sideways each tick, and the
                // offset VARIES along the slice (noise by world x/z) so tears
                // look ripped, not extruded slabs.
                float h = Hash(band * 7.13 + tick * 57.0);
                float gate = step(1.0 - (0.05 + 0.4 * _Intensity), h);
                float along = 0.4 + 0.6 * Hash(band + floor(positionWS.x * 3.0 + positionWS.z * 2.0) + tick);
                float2 dir = float2(Hash(band + tick * 3.7) - 0.5, Hash(band * 1.7 + tick) - 0.5) * 2.0;
                positionWS.xz += dir * gate * (0.015 + 0.11 * _Intensity) * along;

                // Vertical hold slipping: rare ticks pop the whole picture
                // down a few centimeters, like a frame losing sync.
                float vhold = step(0.93, Hash(tick * 3.37));
                positionWS.y -= vhold * (0.03 + 0.10 * _Intensity) * Hash(tick * 9.1);

                positionWS += normalWS * _Inflate;

                o.positionWS = positionWS;
                o.normalWS = normalWS;
                o.band = band;
                o.positionCS = TransformWorldToHClip(positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float tick = floor(_Time.y * _TickRate);

                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float fresnel = pow(saturate(1.0 - dot(n, v)), 2.5);

                // Per-pixel snow: TV static grain, re-rolled every tick.
                float2 px = floor(i.positionCS.xy / 2.0);
                float snow = Hash(px.x * 7.31 + px.y * 53.71 + tick * 91.0);

                // Band flutter: some scan bands sit dimmer this tick.
                float flutter = lerp(0.45, 1.0, step(0.35, Hash(floor(i.positionWS.y * 38.0) + tick * 31.0)));

                // Pale phosphor static with a FAINT warm/cool fringe per band
                // (analog color drift, not RGB channel slabs).
                float side = Hash(i.band * 3.31 + floor(tick / 3.0));
                half3 fringe = side < 0.5 ? half3(1.06, 0.94, 0.88) : half3(0.88, 0.97, 1.06);
                half3 color = half3(0.82, 0.87, 0.92) * fringe * (0.55 + 0.9 * _Intensity)
                            + step(0.92, snow) * 0.5; // sparse bright snow specks

                // Crush saturation hard — this should read gray-white, aged.
                half lum = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(half3(lum, lum, lum), color, 0.4);

                half alpha = saturate(fresnel * (0.08 + 0.7 * _Intensity) * flutter * (0.5 + 0.5 * snow));

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
