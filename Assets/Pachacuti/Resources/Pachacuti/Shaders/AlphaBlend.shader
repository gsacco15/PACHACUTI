// Unlit alpha-blended vertex-coloured geometry: the dark-cloud llama and its
// confidence smudge on the sky-map, fog cards, crowd billboards in the
// Cajamarca shot. _Fog > 0.5 applies scene fog (for ground-level cards).
Shader "Pachacuti/AlphaBlend"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (1,1,1,1)
        _Fog ("Apply Fog", Float) = 0
        _SoftEdge ("Soft Edge (uv.y)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "AlphaBlend"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Fog;
                float _SoftEdge;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 c = IN.color * _BaseColor;
                if (_SoftEdge > 0)
                    c.a *= saturate((1.0 - abs(IN.uv.y * 2.0 - 1.0)) / _SoftEdge);
                if (_Fog > 0.5)
                    c.rgb = MixFog(c.rgb, IN.fogFactor);
                return c;
            }
            ENDHLSL
        }
    }
}
