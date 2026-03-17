Shader "Custom/SharpStarParticle"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _Sharpness ("Sharpness", Range(0.01, 1.0)) = 0.9
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One  // Additive blending for glow
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _TintColor;
            float _Sharpness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _TintColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Convert UV to center-based coordinates
                float2 center = i.uv - 0.5;
                float dist = length(center);
                
                // Sharp circle with smooth edge
                float circle = 1.0 - smoothstep(_Sharpness - 0.1, _Sharpness, dist * 2.0);
                
                return i.color * circle;
            }
            ENDCG
        }
    }
}