// Night sky dome. Vertex colour RGB = base sky (gradient + ground-fog band near
// the horizon, baked in C#); vertex colour A = Milky Way brightness.
// The hard cut to the vision (pitch slice §9) is a uniform swap on this
// material: _SkyTint / _MilkyWayGain / _MilkyWayColor.
Shader "Pachacuti/SkyDome"
{
    Properties
    {
        _SkyTint ("Sky Tint", Color) = (1,1,1,1)
        _MilkyWayColor ("Milky Way Colour", Color) = (0.55,0.6,0.75,1)
        _MilkyWayGain ("Milky Way Gain", Float) = 1
        _Exposure ("Exposure", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" "PreviewType"="Skybox" }
        Pass
        {
            Name "SkyDome"
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyTint;
                half4 _MilkyWayColor;
                float _MilkyWayGain;
                float _Exposure;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionHCS : SV_POSITION; half4 color : COLOR; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half3 sky = IN.color.rgb * _SkyTint.rgb;
                half3 mw = IN.color.a * _MilkyWayColor.rgb * _MilkyWayGain;
                return half4((sky + mw) * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
