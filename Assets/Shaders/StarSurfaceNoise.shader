// Warps the star glow sprite's own UVs with scrolling value noise so its baked-in
// granulation/corona detail visibly churns in place ("boiling"), instead of pulsing the
// whole sprite's brightness uniformly. Alpha is sampled at the un-warped UV so the sprite's
// silhouette (disc + corona) stays crisp and doesn't ripple at the edges.
Shader "Custom/StarSurfaceNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 3.0
        _NoiseSpeed ("Noise Speed", Float) = 0.15
        _DistortStrength ("Distortion Strength", Range(0, 0.3)) = 0.02
        _BrightnessNoiseStrength ("Brightness Noise Strength", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float seed : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _NoiseScale;
            float _NoiseSpeed;
            float _DistortStrength;
            float _BrightnessNoiseStrength;

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                // Per-object seed (constant across the sprite's quad) so same-type stars
                // don't boil in lockstep - derived from world position instead of a
                // per-instance C# uniform, so no MaterialPropertyBlock bookkeeping is needed.
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                o.seed = hash(worldPos.xz) * 100.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float baseAlpha = tex2D(_MainTex, i.uv).a;

                float2 noiseUV = i.uv * _NoiseScale + i.seed;
                float t = _Time.y * _NoiseSpeed;

                float nx = valueNoise(noiseUV + float2(t, -t * 0.6));
                float ny = valueNoise(noiseUV * 1.7 + float2(-t * 0.8, t) + 17.0);
                float2 distortion = (float2(nx, ny) - 0.5) * _DistortStrength;

                // Negative mip bias forces a sharper mip level for the warped sample - without
                // it, mipmapping (enabled on these star textures) blurs away small/fine-grained
                // UV displacement before it's visible at typical on-screen star sizes.
                fixed4 col = tex2Dbias(_MainTex, float4(i.uv + distortion, 0, -1.5));

                float nb = valueNoise(noiseUV * 2.1 - float2(t * 1.3, t * 0.9) + 91.0);
                float brightness = 1.0 + (nb - 0.5) * 2.0 * _BrightnessNoiseStrength;

                col.rgb *= brightness;
                col *= i.color;
                col.a = baseAlpha * i.color.a;
                return col;
            }
            ENDCG
        }
    }
}
