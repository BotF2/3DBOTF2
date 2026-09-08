// Romulan/Klingon cloak arc (TechTree_Phase2_Design.md §5b, §8 II.3) - CloakingController creates
// one shared Material from this shader at runtime and swaps a cloaked fleet's insignia SpriteRenderer
// onto it (see FleetController.SetCloakActive/UpdateCloakVisual) so the OWNER can tell at a glance
// which of their own fleets are currently cloaked, without needing to open the Fleet menu.
//
// Same CGPROGRAM/UnityCG.cginc Built-in-style structure as FogPlane.shader (this project's other
// hand-written shader) rather than a URP Core.hlsl-based one, since that's the pattern already
// confirmed rendering correctly in this project's URP setup. Mirrors Sprites-Default's own
// appdata/v2f shape (vertex color = the SpriteRenderer's tint) so it's a drop-in replacement
// material for any sprite - only the fragment output differs, converting to luminance-based
// grayscale instead of passing the sampled color straight through.
Shader "BOTF/CloakGrayscaleSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * i.color;
                // Standard luminance weights - true per-pixel desaturation, not just a flat gray tint.
                fixed gray = dot(texColor.rgb, fixed3(0.299, 0.587, 0.114));
                return fixed4(gray, gray, gray, texColor.a);
            }
            ENDCG
        }
    }
}
