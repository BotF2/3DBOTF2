//
// Created :    Spring 2023
// Author :     SeungGeon Kim (keithrek@hanmail.net)
// Project :    FogWar
// Filename :   FogPlane.shader (cg shader)
// 
// All Content (C) 2022 Unlimited Fischl Works, all rights reserved.
//

// This shader is based on an implementation of fog shader by rito15
// https://rito15.github.io/posts/fog-of-war/
// The main difference is that the lerping of fragment shader happens in csFogWar (CPU), not in an additional shader pass

Shader "FogWar/FogPlane"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _BlurOffset("BlurOffset", Range(0, 10)) = 1

        // Bright ring drawn exactly at the fog/clear boundary (see frag() below) - the flat fog
        // tint alone gives poor contrast wherever the underlying galaxy image is mostly black,
        // since a translucent dark color over black still reads as close to black. The rim is
        // independent of what's under either side, so the edge of a clearing stays visible over
        // any background.
        _RimColor("Rim Color", Color) = (0.4, 0.95, 1, 1)
        _RimIntensity("Rim Intensity", Range(0, 3)) = 0.75

        // How tightly the rim hugs the exact 50%-visibility line - independent of _BlurOffset
        // (which controls the base fog fade's own softness) so narrowing the rim doesn't also
        // harden the underlying fog-to-clear transition. Default of 8 is ~10x narrower than the
        // original 4*a*(1-a) band (see frag() below).
        _RimWidth("Rim Width", Range(1, 30)) = 8
    }

    CGINCLUDE

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
    float4 _MainTex_TexelSize;
    float4 _Color;
    float _BlurOffset;
    float4 _RimColor;
    float _RimIntensity;
    float _RimWidth;

    v2f vert(appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
        // This normalizes the offset to the uv-coordinates scale, having range of [0, 1]
        float offset = _BlurOffset * _MainTex_TexelSize;

        // 3x3 gaussian kernel
        // https://homepages.inf.ed.ac.uk/rbf/HIPR2/gsmooth.htm
        // Above link may be a good reference of what is going on
        half GaussianKernel[9] =
        {
            1,2,1,
            2,4,2,
            1,2,1
        };

        // Color accumulator
        fixed4 col = fixed4(0,0,0,0);

        // UV index slightly going out of range is fine, texture wrap mode (clamp) will deal with that
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                col +=
                tex2D(_MainTex, i.uv + fixed2(x - 1, y - 1) * offset) *
                GaussianKernel[x * 1 + y * 3];
            }
        }

        // Adding up all elements in the 3x3 kernel results in 16
        col /= 16;

        // col.a is the blurred visibility value: ~0 deep in a cleared area, ~1 deep in fog, and
        // only strictly between the two in the transition band right at a clearing's edge (the
        // blur is what turns the raw per-tile visibility into a soft gradient there instead of a
        // hard cut). distFromEdge is 0 exactly at the 50% midpoint and 1 at either extreme;
        // _RimWidth scales how fast that falls off to zero, so it directly controls how many
        // texels wide the visible rim band is, independent of _BlurOffset.
        float distFromEdge = abs(col.a - 0.5) * 2.0;
        float edge = 1.0 - smoothstep(0.0, 1.0, distFromEdge * _RimWidth);

        fixed4 result = col * _Color;

        // Boost alpha along the edge too, not just RGB - otherwise the rim would inherit
        // whatever (possibly very low) alpha col.a happens to have there and barely show up,
        // defeating the point of a boundary marker that's visible regardless of the fog's own
        // contrast against the background on either side.
        float rimStrength = edge * _RimIntensity;
        result.rgb += _RimColor.rgb * rimStrength;
        result.a = max(result.a, rimStrength);

        return result;
    }

    ENDCG

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        CULL BACK
        ZWrite OFF
        ZTest Always
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}