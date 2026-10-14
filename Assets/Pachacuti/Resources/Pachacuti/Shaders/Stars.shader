// Star field. Each star is a quad whose four vertices share the star's
// position; UV0 holds the corner (-1..1) and UV1.x the angular radius in
// radians. The vertex shader expands the quad in view space so stars stay
// round and constant-size at any distance. Additive.
Shader "Pachacuti/Stars"
{
    Properties
    {
        _Brightness ("Brightness", Float) = 1
        _SizeScale ("Size Scale", Float) = 1
        _Twinkle ("Twinkle", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Background+10" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Stars"
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Brightness;
                float _SizeScale;
                float _Twinkle;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 corner : TEXCOORD0;
                float2 sizeSeed : TEXCOORD1;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 corner : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 centerVS = TransformWorldToView(TransformObjectToWorld(IN.positionOS.xyz));
                float dist = length(centerVS);
                centerVS.xy += IN.corner * IN.sizeSeed.x * _SizeScale * dist;
                OUT.positionHCS = TransformWViewToHClip(centerVS);
                float tw = 1.0 - _Twinkle * (0.5 + 0.5 * sin(_Time.y * (2.0 + IN.sizeSeed.y * 3.0) + IN.sizeSeed.y * 40.0));
                OUT.color = IN.color * (_Brightness * tw);
                OUT.corner = IN.corner;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float d = length(IN.corner);
                float a = saturate(1.0 - d);
                a = a * a * a;
                return half4(IN.color.rgb * a * IN.color.a, 1);
            }
            ENDHLSL
        }
    }
}
