Shader "Toon/HighlightSweepOverlay"
{
    // Transparent additive overlay that draws a textured horizontal "scan band"
    // sweeping along world-Y. Place this material as the SECOND slot on the
    // renderer's Materials list -> Unity will draw the same submesh again on top
    // of the base material.
    //
    // Texture authoring tip:
    //   - Vertical (e.g. 64x256), RGBA.
    //   - Bottom row alpha=0, middle bright yellow line + glow, top row alpha=0.
    //   - The whole vertical span maps to _SweepHeight world-units centered on _SweepY.

    Properties
    {
        [Header(Sweep Texture)]
        _MainTex      ("Sweep Texture (RGBA)", 2D) = "white" {}
        _Tint         ("Tint", Color)              = (1, 0.92, 0.25, 1)
        _Strength     ("Strength", Range(0, 8))    = 1.5

        [Header(Sweep Position)]
        _SweepY       ("Sweep World Y (animate)", Float) = -9999
        _SweepHeight  ("Sweep Height (world units)", Float) = 0.6

        [Header(UV)]
        _UvXScale     ("UV X Scale (world->u)", Float) = 1.0
        _UvXOffset    ("UV X Offset", Float)           = 0.0
        _UseObjectUV  ("Use Object UV.x (0=worldX 1=uv.x)", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+10"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"
        }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha One                 // premultiplied additive (texture alpha drives intensity)

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            float  _SweepHeight;
            fixed4 _Tint;
            float  _UvXScale;
            float  _UvXOffset;
            float  _UseObjectUV;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _SweepY)
                UNITY_DEFINE_INSTANCED_PROP(float, _Strength)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uv       = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float sweepY   = UNITY_ACCESS_INSTANCED_PROP(Props, _SweepY);
                float strength = UNITY_ACCESS_INSTANCED_PROP(Props, _Strength);

                // V coordinate maps the band height onto the texture vertically.
                // 0 = bottom edge of the band, 1 = top edge.
                float halfH = max(_SweepHeight * 0.5, 0.0001);
                float v     = (i.worldPos.y - (sweepY - halfH)) / _SweepHeight;

                // Discard everything outside the band -> zero overdraw cost.
                if (v < 0.0 || v > 1.0) discard;

                // U coordinate: choose object-UV.x or world-X scrolled.
                float u = lerp(i.worldPos.x * _UvXScale + _UvXOffset,
                               i.uv.x      * _UvXScale + _UvXOffset,
                               _UseObjectUV);

                float2 sampleUv = float2(u, v) * _MainTex_ST.xy + _MainTex_ST.zw;
                fixed4 tex      = tex2D(_MainTex, sampleUv);

                fixed3 rgb   = tex.rgb * _Tint.rgb;
                float  alpha = tex.a   * _Tint.a * strength;

                // Premultiply for SrcAlpha-One additive: brighter alpha = brighter contribution.
                return fixed4(rgb * alpha, alpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
