// World-space (triplanar) aged paint shader for the ship's walls.
// The wall meshes have unreliable UVs (curved porthole pieces stretch any
// UV-mapped texture), so grime and detail normals are sampled by world
// position instead — flat, even coverage regardless of topology.
Shader "Cycles/AgedWall"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _GrimeMap("Grime (RGB stains, A sheen)", 2D) = "white" {}
        _DetailNormalMap("Detail Normal", 2D) = "bump" {}
        _WorldTile("Grime world tile (m)", Float) = 5.0
        _DetailTile("Detail world tile (m)", Float) = 1.4
        _DetailStrength("Detail strength", Range(0, 2)) = 0.6
        _Smoothness("Smoothness", Range(0, 1)) = 0.42
        _Metallic("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _WorldTile;
            float _DetailTile;
            half _DetailStrength;
            half _Smoothness;
            half _Metallic;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_GrimeMap);        SAMPLER(sampler_GrimeMap);
            TEXTURE2D(_DetailNormalMap); SAMPLER(sampler_DetailNormalMap);

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
                half   fogFactor  : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(v.normalOS);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = nrm.normalWS;
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half3 SampleDetail(float2 uv)
            {
                return UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, uv), _DetailStrength);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 nws = normalize(i.normalWS);
                float3 w = pow(abs(nws), 4.0);
                w /= (w.x + w.y + w.z);

                // grime sampled flat in world space on all three planes
                half4 grime =
                    SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, i.positionWS.zy / _WorldTile) * w.x +
                    SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, i.positionWS.xz / _WorldTile) * w.y +
                    SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, i.positionWS.xy / _WorldTile) * w.z;

                // triplanar detail normal (UDN-style blend — plenty for noise detail)
                half3 tnX = SampleDetail(i.positionWS.zy / _DetailTile);
                half3 tnY = SampleDetail(i.positionWS.xz / _DetailTile);
                half3 tnZ = SampleDetail(i.positionWS.xy / _DetailTile);
                float3 n = normalize(nws + float3(0, tnX.y, tnX.x) * w.x
                                         + float3(tnY.x, 0, tnY.y) * w.y
                                         + float3(tnZ.x, tnZ.y, 0) * w.z);

                SurfaceData s = (SurfaceData)0;
                s.albedo = _BaseColor.rgb * grime.rgb;
                s.metallic = _Metallic;
                s.smoothness = _Smoothness * grime.a;
                s.occlusion = 1.0;
                s.alpha = 1.0;

                InputData d = (InputData)0;
                d.positionWS = i.positionWS;
                d.normalWS = n;
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                d.fogCoord = i.fogFactor;
                d.bakedGI = SampleSH(n);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask = half4(1, 1, 1, 1);
                d.positionCS = i.positionCS;

                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, i.fogFactor);
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                o.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(normalize(i.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
