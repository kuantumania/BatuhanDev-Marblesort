Shader "Toon/ToonBox"
{
    Properties
    {
        [Header(Base)]
        _Color           ("Base Color", Color)              = (1, 0.85, 0.20, 1)
        _MainTex         ("Overlay (RGBA)", 2D)             = "white" {}
        _OverlayStrength ("Overlay Strength", Range(0, 1))  = 1.0

        [Header(Top Face Highlight)]
        _TopColor        ("Top Face Color", Color)          = (1, 1, 1, 1)
        _TopStrength     ("Top Highlight Strength", Range(0, 1)) = 0.35
        _TopPower        ("Top Highlight Falloff", Range(0.1, 16)) = 4

        [Header(Bottom Shade)]
        _BottomShade     ("Bottom Shade Color", Color)      = (0.55, 0.45, 0.30, 1)
        _BottomStrength  ("Bottom Shade Strength", Range(0, 1)) = 0.25
        _BottomPower     ("Bottom Shade Falloff", Range(0.1, 16)) = 3

        [Header(Toon Diffuse Ramp)]
        _RampStep        ("Ramp Step", Range(-1, 1))        = 0.0
        _RampSmoothness  ("Ramp Smoothness", Range(0.001, 0.5)) = 0.06
        _ShadowColor     ("Shadow Tint", Color)             = (0.55, 0.45, 0.55, 1)
        _ShadowStrength  ("Shadow Strength", Range(0, 1))   = 0.40

        [Header(Rim Light)]
        _RimColor        ("Rim Color", Color)               = (1, 1, 1, 1)
        _RimStep         ("Rim Step", Range(0, 1))          = 0.55
        _RimSmoothness   ("Rim Smoothness", Range(0.001, 0.5)) = 0.10
        _RimStrength     ("Rim Strength", Range(0, 1))      = 0.20

        [Header(Ambient)]
        _AmbientBoost    ("Ambient Boost", Range(0, 1))     = 0.30
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        // -------------------------------------------------------------
        // Forward Base
        // -------------------------------------------------------------
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float2 uv      : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldNormal: TEXCOORD1;
                float3 worldPos   : TEXCOORD2;
                SHADOW_COORDS(3)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _TopColor)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _BottomShade)
            UNITY_INSTANCING_BUFFER_END(Props)

            float  _OverlayStrength;
            float  _TopStrength;
            float  _TopPower;
            float  _BottomStrength;
            float  _BottomPower;

            float  _RampStep;
            float  _RampSmoothness;
            fixed4 _ShadowColor;
            float  _ShadowStrength;

            fixed4 _RimColor;
            float  _RimStep;
            float  _RimSmoothness;
            float  _RimStrength;

            float  _AmbientBoost;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos         = UnityObjectToClipPos(v.vertex);
                o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 baseCol   = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                fixed4 topCol    = UNITY_ACCESS_INSTANCED_PROP(Props, _TopColor);
                fixed4 bottomCol = UNITY_ACCESS_INSTANCED_PROP(Props, _BottomShade);

                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                // ---- World-up face mask (top vs side vs bottom)
                float upDot   = N.y;                        // -1..1
                float topMask = pow(saturate(upDot),       _TopPower);
                float botMask = pow(saturate(-upDot),      _BottomPower);

                // ---- Optional overlay texture (e.g. "?" mark)
                fixed4 ov     = tex2D(_MainTex, i.uv);
                fixed3 albedo = lerp(baseCol.rgb, ov.rgb, ov.a * _OverlayStrength);

                // Apply face-based gradients
                fixed3 grad = albedo;
                grad = lerp(grad, topCol.rgb    * albedo, topMask * _TopStrength);
                grad = lerp(grad, bottomCol.rgb * albedo, botMask * _BottomStrength);

                // ---- Toon diffuse ramp
                float NdotL  = dot(N, L);
                float shadow = SHADOW_ATTENUATION(i);
                float lit    = smoothstep(_RampStep - _RampSmoothness,
                                          _RampStep + _RampSmoothness,
                                          NdotL * shadow);

                fixed3 shadowSide = lerp(grad, _ShadowColor.rgb * grad, _ShadowStrength);
                fixed3 col        = lerp(shadowSide, grad, lit);

                // ---- Toon rim (lit side only, soft)
                float rimRaw = 1.0 - saturate(dot(N, V));
                float rim    = smoothstep(_RimStep - _RimSmoothness,
                                          _RimStep + _RimSmoothness,
                                          rimRaw);
                col += rim * _RimColor.rgb * _RimStrength * lit;

                // ---- Ambient (SH)
                fixed3 ambient = ShadeSH9(float4(N, 1.0)) * albedo * _AmbientBoost;
                col += ambient;

                return fixed4(col, 1);
            }
            ENDCG
        }

        // -------------------------------------------------------------
        // Shadow Caster
        // -------------------------------------------------------------
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct v2fShadow { V2F_SHADOW_CASTER; UNITY_VERTEX_INPUT_INSTANCE_ID };

            v2fShadow vertShadow(appdata_base v)
            {
                v2fShadow o;
                UNITY_SETUP_INSTANCE_ID(v);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(v2fShadow i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}
