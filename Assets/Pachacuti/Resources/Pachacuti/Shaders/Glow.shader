// Additive emissive lines and shapes: ceque threads, the order thread, the
// zigzag channel's liquid light, torch flames, gold. UV0.x runs along a line
// (0..1) so a travelling tension pulse can be animated (pitch slice §9 VFX:
// "emissive gold material and a vertex-shader tension pulse").
// _Reveal hides everything past a point along the line (for "light running
// down the channel"). Not affected by fog.
Shader "Pachacuti/Glow"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (1,0.8,0.35,1)
        _Intensity ("Intensity", Float) = 1
        _PulseSpeed ("Pulse Speed", Float) = 0
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0
        _PulseWidth ("Pulse Width", Range(0.01,1)) = 0.15
        _Reveal ("Reveal (0..1 along line)", Range(0,1)) = 1
        _Wobble ("Vertex Wobble (m)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Glow"
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Intensity;
                float _PulseSpeed;
                float _PulseAmount;
                float _PulseWidth;
                float _Reveal;
                float _Wobble;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 p = IN.positionOS.xyz;
                // Tension hum: a small travelling vertical wobble along the thread.
                p.y += _Wobble * sin(IN.uv.x * 60.0 - _Time.y * 9.0) * sin(IN.uv.x * 3.14159);
                OUT.positionHCS = TransformObjectToHClip(p);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float along = IN.uv.x;
                clip(_Reveal - along);
                float phase = frac(along - _Time.y * _PulseSpeed);
                float pulse = 1.0 + _PulseAmount * 3.0 * saturate(1.0 - abs(phase - 0.5) / _PulseWidth);
                float edge = 1.0 - abs(IN.uv.y * 2.0 - 1.0);   // soft across the strip
                half3 c = IN.color.rgb * _BaseColor.rgb * (_Intensity * pulse * edge * IN.color.a * _BaseColor.a);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
