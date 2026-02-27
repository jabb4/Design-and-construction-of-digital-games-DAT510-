Shader "Custom/DissolveLit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1.0
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)

        [Header(Dissolve)]
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0, 0.35)) = 0.08
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.35
        _EdgePulseSpeed ("Edge Pulse Speed", Range(0, 20)) = 7
        _EdgePulseAmount ("Edge Pulse Amount", Range(0, 1)) = 0.25
        _EdgeFresnelPower ("Edge Fresnel Power", Range(0.25, 8)) = 3
        _EdgeFresnelBoost ("Edge Fresnel Boost", Range(0, 3)) = 0.9
        [HDR] _EdgeInnerColor ("Edge Inner Color (Hot)", Color) = (16, 14, 10, 1)
        [HDR] _EdgeColor ("Edge Outer Color", Color) = (8, 2, 0.2, 1)
        [HDR] _EdgeCoolColor ("Edge Cool Color", Color) = (0.22, 0.04, 0.02, 1)
        _HeatExponent ("Heat Ramp Exponent", Range(0.25, 4)) = 1.25
        _NoiseScale ("Noise Scale", Float) = 4.2
        _DissolveNoiseWarp ("Noise Warp", Range(0, 1)) = 0.35
        _NoiseScrollSpeed ("Noise Scroll Speed", Range(0, 3)) = 0.35
        _DissolveHeightStrength ("Height Bias", Range(0, 1)) = 0.45
        [Header(Flakes)]
        _FlakeBandWidth ("Flake Band Width", Range(0, 0.2)) = 0.06
        _FlakeCutout ("Flake Cutout", Range(0, 1)) = 0.22
        _FlakeNoiseScale ("Flake Noise Scale", Float) = 18
        _FlakeFlickerSpeed ("Flake Flicker Speed", Range(0, 20)) = 8
        _FlakeSparkDensity ("Flake Spark Density", Range(0, 1)) = 0.18
        [HDR] _FlakeSparkColor ("Flake Spark Color", Color) = (14, 7, 1.2, 1)
        _FlakeSparkIntensity ("Flake Spark Intensity", Range(0, 5)) = 1.4
        [HideInInspector] _DissolveSeed ("Dissolve Seed", Float) = 0
        [HideInInspector] _DissolveHeightMin ("Dissolve Height Min", Float) = 0
        [HideInInspector] _DissolveHeightInvRange ("Dissolve Height Inv Range", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "DissolveCommon.hlsl"

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap);   SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float2 uv            : TEXCOORD0;
                float3 positionWS    : TEXCOORD1;
                float3 normalWS      : TEXCOORD2;
                float4 tangentWS     : TEXCOORD3;
                float  fogFactor     : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
            };

            Varyings vert(Attributes input)
            {
                Varyings o;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS   = normInputs.normalWS;
                o.tangentWS  = float4(normInputs.tangentWS, input.tangentOS.w);
                o.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normalWS, o.vertexSH);

                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Dissolve clip + edge distance.
                float distToEdge;
                DissolveClip(input.positionWS, distToEdge);

                // Surface data (matches URP Lit).
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(baseColor.a - _Cutoff);
                #endif

                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float sgn = input.tangentWS.w * GetOddNegativeScale();
                float3 bitangent = sgn * cross(input.normalWS, input.tangentWS.xyz);
                half3 normalWS = TransformTangentToWorld(normalTS,
                    half3x3(input.tangentWS.xyz, bitangent, input.normalWS));
                normalWS = NormalizeNormalPerPixel(normalWS);

                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                occlusion = lerp(1.0, occlusion, _OcclusionStrength);

                half3 emissionBase = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb
                                     * _EmissionColor.rgb;

                // Edge glow: smoother response with pulse and fresnel boost.
                float edgeFactor = 1.0 - smoothstep(0.0, max(_EdgeWidth, 0.001), distToEdge);
                edgeFactor = pow(saturate(edgeFactor), lerp(2.25, 0.75, _EdgeSoftness));
                edgeFactor *= step(0.001, _DissolveAmount);

                float pulseWave = 0.5 + 0.5 * sin((_Time.y + _DissolveSeed * 11.3) * _EdgePulseSpeed);
                float pulse = lerp(1.0, 0.7 + pulseWave * 0.6, _EdgePulseAmount);
                edgeFactor *= pulse;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _EdgeFresnelPower);
                float fresnelBoost = 1.0 + fresnel * _EdgeFresnelBoost;

                float heat = pow(saturate(edgeFactor), _HeatExponent);
                half3 edgeColor = lerp(_EdgeCoolColor.rgb, _EdgeColor.rgb, saturate(heat * 1.35));
                edgeColor = lerp(edgeColor, _EdgeInnerColor.rgb, saturate((heat - 0.45) * 1.85));
                half3 dissolveEmission = edgeColor * edgeFactor * fresnelBoost;

                float flakeTime = (_Time.y + _DissolveSeed * 7.91) * _FlakeFlickerSpeed;
                float3 flakePos = input.positionWS * (_FlakeNoiseScale * 1.37)
                                + float3(flakeTime * 0.73, -flakeTime * 1.11, flakeTime * 1.29);
                float flakeNoise = DissolveValueNoise3D(flakePos);
                float flakeBand = saturate(1.0 - distToEdge / max(_FlakeBandWidth * 1.2, 0.0001));
                float flakeSpark = step(1.0 - _FlakeSparkDensity, flakeNoise) * flakeBand;
                dissolveEmission += _FlakeSparkColor.rgb * flakeSpark * _FlakeSparkIntensity;

                // URP PBR lighting.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                SurfaceData surfaceData = (SurfaceData)0;
                float charMask = saturate(pow(edgeFactor, 0.75) * 1.2);
                surfaceData.albedo = lerp(baseColor.rgb,
                                          baseColor.rgb * 0.08 + _EdgeCoolColor.rgb * 0.025,
                                          charMask * 0.9);
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = lerp(_Smoothness, _Smoothness * 0.03, charMask);
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emissionBase + dissolveEmission;
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = 1.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "DissolveCommon.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes_SC
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings_SC
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv         : TEXCOORD1;
            };

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings_SC vertShadow(Attributes_SC input)
            {
                Varyings_SC o;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - posWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float3 posWSBiased = ApplyShadowBias(posWS, normWS, lightDirectionWS);
                o.positionCS = ApplyShadowClamping(TransformWorldToHClip(posWSBiased));
                o.positionWS = posWS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return o;
            }

            half4 fragShadow(Varyings_SC input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                float distToEdge;
                DissolveClip(input.positionWS, distToEdge);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DissolveCommon.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes_DO
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings_DO
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv         : TEXCOORD1;
            };

            Varyings_DO vertDepth(Attributes_DO input)
            {
                Varyings_DO o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 fragDepth(Varyings_DO input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                float distToEdge;
                DissolveClip(input.positionWS, distToEdge);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
