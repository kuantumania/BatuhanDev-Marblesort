Shader "Toon/ToonBall"
{
    Properties
    {
        [Header(Base Color)]
        _Color           ("Base Color", Color)              = (1, 0.55, 0.20, 1)

        [Header(World Up Gradient)]
        _TopColor        ("Top Tint", Color)                = (1.00, 0.92, 0.78, 1)
        _BottomColor     ("Bottom Tint", Color)             = (0.55, 0.25, 0.10, 1)
        _GradientPower   ("Gradient Power", Range(0.1, 6))  = 1.4
        _GradientStrength("Gradient Strength", Range(0, 1)) = 0.85

        [Header(Toon Diffuse Ramp)]
        _RampStep        ("Ramp Step", Range(-1, 1))        = 0.05
        _RampSmoothness  ("Ramp Smoothness", Range(0.001, 0.5)) = 0.04
        _ShadowColor     ("Shadow Tint", Color)             = (0.55, 0.40, 0.55, 1)
        _ShadowStrength  ("Shadow Strength", Range(0, 1))   = 0.55

        [Header(Toon Specular)]
        _ToonSpecColor   ("Specular Color", Color)          = (1, 1, 1, 1)
        _SpecGloss       ("Specular Gloss", Range(1, 256))  = 96
        _SpecStep        ("Specular Step", Range(0, 1))     = 0.55
        _SpecSmoothness  ("Specular Smoothness", Range(0.001, 0.5)) = 0.03
        _SpecStrength    ("Specular Strength", Range(0, 2)) = 1.0

        [Header(Rim Light)]
        _RimColor        ("Rim Color", Color)               = (1, 1, 1, 1)
        _RimStep         ("Rim Step", Range(0, 1))          = 0.62
        _RimSmoothness   ("Rim Smoothness", Range(0.001, 0.5)) = 0.05
        _RimStrength     ("Rim Strength", Range(0, 1))      = 0.35

        [Header(Ambient)]
        _AmbientBoost    ("Ambient Boost", Range(0, 1))     = 0.20
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

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos        : SV_POSITION;
                float3 worldNormal: TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                SHADOW_COORDS(2)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _TopColor)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _BottomColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            float  _GradientPower;
            float  _GradientStrength;

            float  _RampStep;
            float  _RampSmoothness;
            fixed4 _ShadowColor;
            float  _ShadowStrength;

            fixed4 _ToonSpecColor;
            float  _SpecGloss;
            float  _SpecStep;
            float  _SpecSmoothness;
            float  _SpecStrength;

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
                fixed4 bottomCol = UNITY_ACCESS_INSTANCED_PROP(Props, _BottomColor);

                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 H = normalize(L + V);

                // ---- World-up vertical gradient (top lighter, bottom darker)
                float upMask = saturate(N.y * 0.5 + 0.5);          // 0..1 from bottom to top
                float topW   = pow(upMask, _GradientPower);        // bias toward top
                float botW   = pow(1.0 - upMask, _GradientPower);  // bias toward bottom
                fixed3 grad  = baseCol.rgb;
                grad = lerp(grad, bottomCol.rgb * baseCol.rgb, botW * _GradientStrength);
                grad = lerp(grad, topCol.rgb     * baseCol.rgb, topW * _GradientStrength);

                // ---- Toon diffuse ramp (cel band)
                float NdotL  = dot(N, L);
                float shadow = SHADOW_ATTENUATION(i);
                float lit    = smoothstep(_RampStep - _RampSmoothness,
                                          _RampStep + _RampSmoothness,
                                          NdotL * shadow);

                fixed3 shadowSide = lerp(grad, _ShadowColor.rgb * grad, _ShadowStrength);
                fixed3 col        = lerp(shadowSide, grad, lit);

                // ---- Toon specular dot (Blinn-Phong, stepped)
                float NdotH = saturate(dot(N, H));
                float specRaw = pow(NdotH, _SpecGloss);
                float spec   = smoothstep(_SpecStep - _SpecSmoothness,
                                          _SpecStep + _SpecSmoothness,
                                          specRaw);
                col += spec * _ToonSpecColor.rgb * _SpecStrength * lit;

                // ---- Toon rim (only on the lit side, fresnel-style)
                float rimRaw = 1.0 - saturate(dot(N, V));
                float rim    = smoothstep(_RimStep - _RimSmoothness,
                                          _RimStep + _RimSmoothness,
                                          rimRaw);
                col += rim * _RimColor.rgb * _RimStrength * lit;

                // ---- Ambient (SH, soft)
                fixed3 ambient = ShadeSH9(float4(N, 1.0)) * baseCol.rgb * _AmbientBoost;
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
