//
// ShipShield.shader
//
// The "shields absorbed a hit" shell effect: an additive fresnel glow over an egg-shaped sphere
// wrapped around the ship (see ShipShieldEffect.cs, which builds and scales the mesh at runtime).
// Fresnel alone (rim bright, face-on nearly invisible) reads fine from a side-on view, but combat's
// camera looks down at a steep angle, so the sphere's top cap faces the camera almost dead-on and
// fresnel goes near-zero there - only the equator (a thin ring) would ever be visible, which is
// exactly the "flat ring around the ship" look this shader deliberately avoids by adding _BaseGlow,
// a small view-independent floor so the whole dome has some translucent presence and the rim just
// glows brighter on top of it.
//
// Additive blending (Blend SrcAlpha One) is used instead of standard alpha blending so overlapping
// transparent geometry (other shields, beams, explosions) never needs draw-order sorting - at
// _FlashIntensity = 0 the shell contributes pure black, i.e. nothing, so it's visually safe to
// leave enabled at zero intensity, though ShipShieldEffect.cs also fully disables the GameObject
// between flashes for zero per-frame cost.
//
Shader "BOTF3D/ShipShield"
{
    Properties
    {
        _ShieldColor("Shield Color", Color) = (0.3, 0.7, 1, 1)
        // Lower = softer/broader glow spread across more of the sphere; higher = a thinner rim.
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 1.75
        // View-independent minimum glow so the shell reads as an enclosing dome from any camera
        // angle (including steep top-down) instead of just a bright ring at the silhouette edge.
        _BaseGlow("Base Glow", Range(0, 1)) = 0.25
        // Driven per-instance at runtime via MaterialPropertyBlock (ShipShieldEffect.cs) so every
        // ship's shield can flash independently while all sharing this one Material - keeps the
        // SRP batcher/GPU instancing happy instead of paying for a unique Material per ship.
        _FlashIntensity("Flash Intensity", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        Blend SrcAlpha One
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _ShieldColor;
            float _FresnelPower;
            float _BaseGlow;
            float _FlashIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 normal = normalize(i.worldNormal);
                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), _FresnelPower);
                float shellGlow = saturate(fresnel + _BaseGlow);

                float glow = shellGlow * _FlashIntensity;
                return fixed4(_ShieldColor.rgb * glow, glow);
            }
            ENDCG
        }
    }
}
