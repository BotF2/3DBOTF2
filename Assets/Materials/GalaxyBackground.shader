//
// GalaxyBackground.shader
//
// Renders the galaxy map's background sprite (GalaxyHighRes.png) plus a procedurally-generated
// grid overlay, instead of relying on the grid baked into that texture. The baked grid is thin,
// high-frequency detail that suffers badly once the source image (6144x4803) is downsampled to
// its 2048 import max and BC-compressed - exactly the same failure mode as small bright stars in
// the skybox starmap (see starmap_8k's import settings). A grid drawn in the shader is generated
// at full screen resolution every frame regardless of the base texture's own resolution, so it
// stays crisp and equally visible over any part of the image, dark or bright.
//
// Grid line distance-to-pixel conversion uses screen-space derivatives (fwidth) for automatic
// anti-aliasing at any zoom level - a standard, cheap technique (a handful of ALU ops, no extra
// texture samples) with no meaningful performance cost for a single background plane.
//
Shader "BOTF3D/GalaxyBackground"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)

        [Header(Procedural Grid)]
        _GridColor("Grid Color", Color) = (0.3, 1, 0.5, 1)
        // In UV units (0-1 across the whole sprite) - tune this in the Inspector to match the
        // baked grid's spacing by eye; there's no way to measure the baked grid's exact pixel
        // pitch from code alone.
        _GridSpacing("Grid Spacing (UV)", Range(0.0002, 0.02)) = 0.002
        _GridLineWidth("Grid Line Width (px)", Range(0.5, 4)) = 1.5
        _GridIntensity("Grid Intensity", Range(0, 3)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off
        ZTest LEqual
        LOD 100

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GridColor;
            float _GridSpacing;
            float _GridLineWidth;
            float _GridIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;

                // Distance from this pixel to the nearest grid line, in screen pixels (dividing
                // the UV-space distance by fwidth(coord) - the per-pixel rate of change of coord -
                // converts it from UV units into pixels), so the line stays a constant width in
                // pixels regardless of zoom instead of getting thinner/thicker with the camera.
                float2 coord = i.uv / _GridSpacing;
                float2 gridDistPixels = abs(frac(coord - 0.5) - 0.5) / fwidth(coord);
                float lineDistPixels = min(gridDistPixels.x, gridDistPixels.y);
                float lineMask = 1.0 - saturate(lineDistPixels - (_GridLineWidth - 1.0));

                fixed3 result = baseColor.rgb + _GridColor.rgb * lineMask * _GridIntensity;
                return fixed4(result, baseColor.a);
            }
            ENDCG
        }
    }
}
