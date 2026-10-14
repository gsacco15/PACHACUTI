// Fog-free unlit solid colour. Used for the landmark silhouettes that must
// read from every zone through the ground fog (pitch slice §4: "From every
// zone the player can see the Coricancha's roofline and the hill of
// Sacsayhuaman (silhouette only). This replaces a map."), and for the
// valley ridge cards.
Shader "Pachacuti/Silhouette"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.01,0.012,0.02,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+50" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Silhouette"
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
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
                return half4(_BaseColor.rgb * IN.color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
