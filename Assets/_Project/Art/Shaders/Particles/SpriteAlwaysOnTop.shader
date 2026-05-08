Shader "Marble Sort/Sprites/Default Always On Top"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Toggle] _UseBrightnessBoost ("Use Brightness Boost", Float) = 0
        _Brightness ("Boost Brightness", Float) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFragBright
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            half _UseBrightnessBoost;
            half _Brightness;

            fixed4 SpriteFragBright(v2f IN) : SV_Target
            {
                fixed4 color = SpriteFrag(IN);
                half boostAmount = step(0.5h, _UseBrightnessBoost);
                color.rgb *= lerp(1.0h, _Brightness, boostAmount);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
