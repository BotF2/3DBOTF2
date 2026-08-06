// The nebula art (Nebula*.png, OmarianNebula.png, ORIONNEBULA.png) bakes a fully-opaque
// near-black halo around the cloud art instead of a clean alpha cutout, which reads as a
// visible dark patch against the galaxy map's non-black background. Premultiplied-alpha
// additive blending sidesteps that without touching the source art: black contributes
// nothing to the framebuffer regardless of what's behind it, and multiplying by the sprite's
// own alpha keeps the existing soft outer edge fading to nothing instead of a hard cutoff.
Shader "Custom/NebulaAdditive"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend One One

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed3 rgb = tex.rgb * tex.a * i.color.rgb * i.color.a;
                return fixed4(rgb, 0);
            }
            ENDCG
        }
    }
}
