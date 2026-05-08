Shader "Custom/Toon/ShockwaveRing"
{
    // Built-in Render Pipeline (BiRP) icin yazilmis stylized expanding ring shader.
    // Projende URP yok - manifest.json'a goz attim, com.unity.render-pipelines.universal yok.
    // Bu yuzden CGPROGRAM + UnityCG.cginc kullanildi (mevcut ToonBall/ToonBox ile ayni stack).
    //
    // Hedef: Casual/Toon stylized expanding ring.
    // - Sert ic ve dis kenar (cartoon outline)
    // - Mid-band'da yumusak gradient (Color1 -> Color2)
    // - _Alpha ile MagnetCollector'in shockwave fade'i kontrol edilir
    //
    // Onerilen kullanim:
    //   Quad mesh + bu shader. localScale baslangici 0.2, sonu 2.6.
    //   MagnetCollector.shockwaveRingPrefab'a ata.
    //
    // Mobil dostu: unlit, depth write off, additive blend.

    Properties
    {
        _Color1            ("Inner Color (HDR)", Color) = (1, 1, 1, 1)
        _Color2            ("Outer Color (HDR)", Color) = (0.4, 0.85, 1, 1)
        _InnerRadius       ("Inner Radius",      Range(0, 0.5)) = 0.20
        _OuterRadius       ("Outer Radius",      Range(0, 0.5)) = 0.45
        _EdgeSoftness      ("Edge Softness",     Range(0.001, 0.2)) = 0.04
        _RingPower         ("Ring Power (gradient curve)", Range(0.5, 4)) = 1.6
        _Alpha             ("Master Alpha",      Range(0, 1)) = 1.0
        _IntensityBoost    ("Intensity Boost",   Range(1, 8)) = 2.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"
        }
        LOD 100

        Pass
        {
            Name "ShockwaveRingPass"

            Blend SrcAlpha One       // Additive (cartoon glow feel)
            ZWrite Off
            Cull Off
            ColorMask RGB

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color1;
            float4 _Color2;
            float  _InnerRadius;
            float  _OuterRadius;
            float  _EdgeSoftness;
            float  _RingPower;
            float  _Alpha;
            float  _IntensityBoost;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Quad UV merkezini (0.5, 0.5) yap, distance from center hesapla
                float2 centered = i.uv - 0.5;
                float  d        = length(centered);

                // Ring bandi: _InnerRadius ile _OuterRadius arasi
                // Smoothstep ile ic ve dis kenarlar yumusak
                float innerEdge = smoothstep(_InnerRadius - _EdgeSoftness, _InnerRadius + _EdgeSoftness, d);
                float outerEdge = 1.0 - smoothstep(_OuterRadius - _EdgeSoftness, _OuterRadius + _EdgeSoftness, d);
                float ringMask  = saturate(innerEdge * outerEdge);

                // Ring icindeki gradient: ic kenara yakin _Color1, dis kenara _Color2
                float t       = saturate((d - _InnerRadius) / max(0.0001, _OuterRadius - _InnerRadius));
                t             = pow(t, _RingPower);
                fixed4 ringCol = lerp(_Color1, _Color2, t);

                // Master alpha + intensity boost (HDR-ish)
                float a       = ringMask * _Alpha;
                fixed3 finalC = ringCol.rgb * _IntensityBoost * a;

                return fixed4(finalC, a);
            }
            ENDCG
        }
    }

    Fallback Off
}
