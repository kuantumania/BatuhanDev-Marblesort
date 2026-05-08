// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MG/Almighty_Additive"
{
	Properties
	{
		_Main_Tex("Main_Tex", 2D) = "white" {}
		[HDR]_Main_Color("Main_Color", Color) = (1,1,1,1)
		_Brightness("Brightness", Float) = 1
		_Alpha("Alpha", Float) = 1
		_Main_UV("Main_UV", Vector) = (0,0,0,0)
		_Tex_2("Tex_2", 2D) = "white" {}
		_Tex_2_UV("Tex_2_UV", Vector) = (0,0,0,0)
		[Toggle(_UVSWITCH_ON)] _UVSwitch("UV Switch", Float) = 0
		_Polar_UV("Polar_UV", Vector) = (0,0,0,0)
		[Toggle(_POLAR_UV_OFFSET_ON)] _Polar_UV_offset("Polar_UV_offset", Float) = 0
		[Toggle(_TEX_2_ALPHA_ON)] _Tex_2_Alpha("Tex_2_Alpha", Float) = 0
		_Diss_Noise("Diss_Noise", 2D) = "white" {}
		_Diss_value("Diss_value", Float) = 0
		_Soft_value("Soft_value", Float) = 0
		_Diss_UV("Diss_UV", Vector) = (0,0,0,0)
		_Turb_Noise("Turb_Noise", 2D) = "white" {}
		_Turb_Value("Turb_Value", Float) = 0
		_Turb_UV("Turb_UV", Vector) = (0,0,0,0)
		_Mask("Mask", 2D) = "white" {}
		[Toggle(_MASK_R_ON)] _Mask_R("Mask_R", Float) = 1
		[Toggle(_DISS_REVERSE_ON)] _Diss_Reverse("Diss_Reverse", Float) = 0
		[Toggle(_VER_REVERSE_ON)] _Ver_Reverse("Ver_Reverse", Float) = 0
		_Vertex_offset("Vertex_offset", 2D) = "white" {}
		_displacment_Intensity("displacment_Intensity", Float) = 0
		_Displacment("Displacment", Vector) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		ZWrite Off
		Blend SrcAlpha One
		BlendOp Add
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _UVSWITCH_ON
		#pragma shader_feature_local _POLAR_UV_OFFSET_ON
		#pragma shader_feature _DISS_REVERSE_ON
		#pragma shader_feature _VER_REVERSE_ON
		#pragma shader_feature _MASK_R_ON
		#pragma shader_feature _TEX_2_ALPHA_ON
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform sampler2D _Vertex_offset;
		uniform float2 _Displacment;
		uniform float4 _Vertex_offset_ST;
		uniform float _displacment_Intensity;
		uniform sampler2D _Main_Tex;
		uniform sampler2D _Turb_Noise;
		uniform float4 _Turb_Noise_ST;
		uniform float2 _Turb_UV;
		uniform float _Turb_Value;
		uniform float2 _Main_UV;
		uniform float4 _Main_Tex_ST;
		uniform float4 _Main_Color;
		uniform float _Brightness;
		uniform sampler2D _Tex_2;
		uniform float2 _Tex_2_UV;
		uniform float4 _Tex_2_ST;
		uniform float4 _Polar_UV;
		uniform float _Diss_value;
		uniform float _Soft_value;
		uniform sampler2D _Diss_Noise;
		uniform float2 _Diss_UV;
		uniform float4 _Diss_Noise_ST;
		uniform float _Alpha;
		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 uv_Vertex_offset = v.texcoord.xy * _Vertex_offset_ST.xy + _Vertex_offset_ST.zw;
			float2 panner102 = ( 1.0 * _Time.y * _Displacment + uv_Vertex_offset);
			float3 ase_vertexNormal = v.normal.xyz;
			v.vertex.xyz += ( tex2Dlod( _Vertex_offset, float4( panner102, 0, 0.0) ) * float4( ase_vertexNormal , 0.0 ) * _displacment_Intensity ).rgb;
			v.vertex.w = 1;
		}

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			float2 uv_Turb_Noise = i.uv_texcoord * _Turb_Noise_ST.xy + _Turb_Noise_ST.zw;
			float4 appendResult135 = (float4(( uv_Turb_Noise.x + 0.0 ) , uv_Turb_Noise.y , 0.0 , 0.0));
			float2 panner82 = ( 1.0 * _Time.y * _Turb_UV + uv_Turb_Noise);
			float2 uv_Main_Tex = i.uv_texcoord * _Main_Tex_ST.xy + _Main_Tex_ST.zw;
			float2 panner90 = ( 1.0 * _Time.y * _Main_UV + uv_Main_Tex);
			float4 appendResult61 = (float4(( ( tex2D( _Turb_Noise, ( appendResult135 + float4( panner82, 0.0 , 0.0 ) ).xy ).r * _Turb_Value ) + panner90 ) , 0.0 , 0.0));
			float4 tex2DNode11 = tex2D( _Main_Tex, appendResult61.xy );
			#ifdef _VER_REVERSE_ON
				float staticSwitch96 = ( 1.0 - i.vertexColor.a );
			#else
				float staticSwitch96 = i.vertexColor.a;
			#endif
			float temp_output_8_0 = ( staticSwitch96 + _Diss_value );
			float2 uv_Diss_Noise = i.uv_texcoord * _Diss_Noise_ST.xy + _Diss_Noise_ST.zw;
			float2 panner84 = ( 1.0 * _Time.y * _Diss_UV + uv_Diss_Noise);
			float smoothstepResult12 = smoothstep( temp_output_8_0 , ( temp_output_8_0 + _Soft_value ) , tex2D( _Diss_Noise, panner84 ).r);
			#ifdef _DISS_REVERSE_ON
				float staticSwitch93 = smoothstepResult12;
			#else
				float staticSwitch93 = ( 1.0 - smoothstepResult12 );
			#endif
			float2 uv_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float4 tex2DNode66 = tex2D( _Mask, uv_Mask );
			#ifdef _MASK_R_ON
				float staticSwitch70 = tex2DNode66.a;
			#else
				float staticSwitch70 = tex2DNode66.r;
			#endif
			float2 uv_Tex_2 = i.uv_texcoord * _Tex_2_ST.xy + _Tex_2_ST.zw;
			float2 panner86 = ( 1.0 * _Time.y * _Tex_2_UV + uv_Tex_2);
			float2 CenteredUV15_g1 = ( uv_Tex_2 - float2( 0.5,0.5 ) );
			float2 break17_g1 = CenteredUV15_g1;
			float2 appendResult23_g1 = (float2(( length( CenteredUV15_g1 ) * _Polar_UV.x * 2.0 ) , ( atan2( break17_g1.x , break17_g1.y ) * ( 1.0 / 6.28318548202515 ) * _Polar_UV.y )));
			float2 appendResult112 = (float2(_Polar_UV.z , _Polar_UV.w));
			float2 panner125 = ( 1.0 * _Time.y * appendResult112 + float2( 0,0 ));
			#ifdef _POLAR_UV_OFFSET_ON
				float2 staticSwitch133 = panner125;
			#else
				float2 staticSwitch133 = appendResult112;
			#endif
			#ifdef _UVSWITCH_ON
				float2 staticSwitch115 = ( appendResult23_g1 + staticSwitch133 );
			#else
				float2 staticSwitch115 = panner86;
			#endif
			float4 tex2DNode71 = tex2D( _Tex_2, staticSwitch115 );
			#ifdef _TEX_2_ALPHA_ON
				float staticSwitch97 = 1.0;
			#else
				float staticSwitch97 = tex2DNode71.a;
			#endif
			c.rgb = 0;
			c.a = ( tex2DNode11.a * staticSwitch93 * _Main_Color.a * _Alpha * staticSwitch70 * staticSwitch97 * i.vertexColor.a );
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			float2 uv_Turb_Noise = i.uv_texcoord * _Turb_Noise_ST.xy + _Turb_Noise_ST.zw;
			float4 appendResult135 = (float4(( uv_Turb_Noise.x + 0.0 ) , uv_Turb_Noise.y , 0.0 , 0.0));
			float2 panner82 = ( 1.0 * _Time.y * _Turb_UV + uv_Turb_Noise);
			float2 uv_Main_Tex = i.uv_texcoord * _Main_Tex_ST.xy + _Main_Tex_ST.zw;
			float2 panner90 = ( 1.0 * _Time.y * _Main_UV + uv_Main_Tex);
			float4 appendResult61 = (float4(( ( tex2D( _Turb_Noise, ( appendResult135 + float4( panner82, 0.0 , 0.0 ) ).xy ).r * _Turb_Value ) + panner90 ) , 0.0 , 0.0));
			float4 tex2DNode11 = tex2D( _Main_Tex, appendResult61.xy );
			float2 uv_Tex_2 = i.uv_texcoord * _Tex_2_ST.xy + _Tex_2_ST.zw;
			float2 panner86 = ( 1.0 * _Time.y * _Tex_2_UV + uv_Tex_2);
			float2 CenteredUV15_g1 = ( uv_Tex_2 - float2( 0.5,0.5 ) );
			float2 break17_g1 = CenteredUV15_g1;
			float2 appendResult23_g1 = (float2(( length( CenteredUV15_g1 ) * _Polar_UV.x * 2.0 ) , ( atan2( break17_g1.x , break17_g1.y ) * ( 1.0 / 6.28318548202515 ) * _Polar_UV.y )));
			float2 appendResult112 = (float2(_Polar_UV.z , _Polar_UV.w));
			float2 panner125 = ( 1.0 * _Time.y * appendResult112 + float2( 0,0 ));
			#ifdef _POLAR_UV_OFFSET_ON
				float2 staticSwitch133 = panner125;
			#else
				float2 staticSwitch133 = appendResult112;
			#endif
			#ifdef _UVSWITCH_ON
				float2 staticSwitch115 = ( appendResult23_g1 + staticSwitch133 );
			#else
				float2 staticSwitch115 = panner86;
			#endif
			float4 tex2DNode71 = tex2D( _Tex_2, staticSwitch115 );
			o.Emission = ( tex2DNode11 * _Main_Color * _Brightness * i.vertexColor * tex2DNode71 ).rgb;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				half4 color : COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.color = v.color;
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.vertexColor = IN.color;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				UnityGI gi;
				UNITY_INITIALIZE_OUTPUT( UnityGI, gi );
				o.Alpha = LightingStandardCustomLighting( o, worldViewDir, gi ).a;
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=18800
1920;-1;1920;1019;3133.181;1304.816;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;88;-3154.691,-548.5042;Inherit;False;1576.927;477.8428;Turb;10;82;137;135;39;35;36;9;136;91;34;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;2;-1523.899,-556.3491;Inherit;False;801.2869;670.6885;Base;8;16;14;13;11;6;4;61;96;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;34;-3105.392,-433.8767;Inherit;False;0;9;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;91;-2877.671,-214.9008;Float;False;Property;_Turb_UV;Turb_UV;18;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;136;-2810.171,-442.2906;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;4;-1485.058,-66.26558;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;129;-2990.729,-1049.462;Inherit;False;Property;_Polar_UV;Polar_UV;9;0;Create;True;0;0;0;False;0;False;0,0,0,0;1,0,0.2,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;6;-1280.299,99.34604;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;112;-2741.495,-977.7371;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;3;-1526.738,169.7933;Inherit;False;918.1165;503.5822;Diss;7;15;12;10;8;7;5;41;;1,1,1,1;0;0
Node;AmplifyShaderEditor.DynamicAppendNode;135;-2642.669,-405.5181;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PannerNode;82;-2674.347,-232.4403;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;108;-2447.228,-1292.925;Inherit;False;0;71;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;85;-1903.993,255.8584;Float;False;Property;_Diss_UV;Diss_UV;15;0;Create;True;0;0;0;False;0;False;0,0;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;125;-2568.632,-886.0621;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;26;-1975.265,110.4086;Inherit;False;0;41;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;137;-2457.441,-405.5181;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1348.885,442.6707;Float;False;Property;_Diss_value;Diss_value;13;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;96;-1116.767,21.59497;Float;False;Property;_Ver_Reverse;Ver_Reverse;22;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;47;-2504.571,-6.486951;Inherit;False;0;11;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;133;-2375.981,-981.1493;Inherit;False;Property;_Polar_UV_offset;Polar_UV_offset;10;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;107;-2152.179,-1172.352;Inherit;False;Polar Coordinates;-1;;1;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;78;-2103.77,-829.7653;Inherit;False;0;71;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;9;-2280.65,-433.7116;Inherit;True;Property;_Turb_Noise;Turb_Noise;16;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;7;-1346.863,556.2814;Float;False;Property;_Soft_value;Soft_value;14;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;89;-2438.24,117.0552;Float;False;Property;_Main_UV;Main_UV;5;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;36;-2154.954,-230.4614;Float;False;Property;_Turb_Value;Turb_Value;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;8;-1054.54,294.8782;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;84;-1701.045,238.3631;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;87;-2030.702,-698.4099;Float;False;Property;_Tex_2_UV;Tex_2_UV;7;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;41;-1478.004,210.3752;Inherit;True;Property;_Diss_Noise;Diss_Noise;12;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;90;-2243.296,98.9464;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;110;-1866.513,-1172.838;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-1927.309,-405.9158;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;10;-923.2366,352.4848;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;86;-1850.703,-715.4099;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;115;-1611.766,-819.8003;Inherit;False;Property;_UVSwitch;UV Switch;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode;12;-777.6218,235.7441;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;100;-1548.186,1097.04;Inherit;False;0;104;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;101;-1471.469,1242.022;Float;False;Property;_Displacment;Displacment;25;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;39;-1762.164,-406.4522;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;67;-1397.474,763.6462;Inherit;False;0;66;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;66;-1112.926,741.827;Inherit;True;Property;_Mask;Mask;19;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;61;-1497.858,-482.4957;Inherit;False;FLOAT4;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.OneMinusNode;94;-600.1143,325.7834;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;102;-1273.647,1223.709;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;71;-1359.02,-842.2697;Inherit;True;Property;_Tex_2;Tex_2;6;0;Create;True;0;0;0;False;0;False;-1;None;89839960d5c179b4bb16dd77c536e32f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;98;-683.6008,-376.1202;Float;False;Constant;_Float0;Float 0;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;11;-1315.306,-511.6489;Inherit;True;Property;_Main_Tex;Main_Tex;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NormalVertexDataNode;103;-947.3342,1407.677;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;13;-1197.044,-290.208;Float;False;Property;_Brightness;Brightness;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;97;-529.7269,-149.3895;Float;False;Property;_Tex_2_Alpha;Tex_2_Alpha;11;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;14;-1127.562,-153.8986;Float;False;Property;_Main_Color;Main_Color;2;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;15;-760.2797,415.6429;Float;False;Property;_Alpha;Alpha;4;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;70;-785.5483,764.6188;Float;False;Property;_Mask_R;Mask_R;20;0;Create;True;0;0;0;False;0;False;0;1;1;True;;Toggle;2;Key0;Key1;Create;False;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;104;-1058.997,1197.02;Inherit;True;Property;_Vertex_offset;Vertex_offset;23;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;99;-745.3303,934.4855;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;93;-444.0592,200.9752;Float;False;Property;_Diss_Reverse;Diss_Reverse;21;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;105;-999.8774,1575.337;Float;False;Property;_displacment_Intensity;displacment_Intensity;24;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-866.2346,-333.4609;Inherit;False;5;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-183.6074,183.609;Inherit;False;7;7;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;106;-683.9445,1201.589;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;1;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;CustomLighting;MG/Almighty_Additive;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;2;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0;True;True;0;True;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;8;5;False;-1;1;False;-1;0;0;False;-1;0;False;-1;1;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;136;0;34;1
WireConnection;6;0;4;4
WireConnection;112;0;129;3
WireConnection;112;1;129;4
WireConnection;135;0;136;0
WireConnection;135;1;34;2
WireConnection;82;0;34;0
WireConnection;82;2;91;0
WireConnection;125;2;112;0
WireConnection;137;0;135;0
WireConnection;137;1;82;0
WireConnection;96;1;4;4
WireConnection;96;0;6;0
WireConnection;133;1;112;0
WireConnection;133;0;125;0
WireConnection;107;1;108;0
WireConnection;107;3;129;1
WireConnection;107;4;129;2
WireConnection;9;1;137;0
WireConnection;8;0;96;0
WireConnection;8;1;5;0
WireConnection;84;0;26;0
WireConnection;84;2;85;0
WireConnection;41;1;84;0
WireConnection;90;0;47;0
WireConnection;90;2;89;0
WireConnection;110;0;107;0
WireConnection;110;1;133;0
WireConnection;35;0;9;1
WireConnection;35;1;36;0
WireConnection;10;0;8;0
WireConnection;10;1;7;0
WireConnection;86;0;78;0
WireConnection;86;2;87;0
WireConnection;115;1;86;0
WireConnection;115;0;110;0
WireConnection;12;0;41;1
WireConnection;12;1;8;0
WireConnection;12;2;10;0
WireConnection;39;0;35;0
WireConnection;39;1;90;0
WireConnection;66;1;67;0
WireConnection;61;0;39;0
WireConnection;94;0;12;0
WireConnection;102;0;100;0
WireConnection;102;2;101;0
WireConnection;71;1;115;0
WireConnection;11;1;61;0
WireConnection;97;1;71;4
WireConnection;97;0;98;0
WireConnection;70;1;66;1
WireConnection;70;0;66;4
WireConnection;104;1;102;0
WireConnection;93;1;94;0
WireConnection;93;0;12;0
WireConnection;16;0;11;0
WireConnection;16;1;14;0
WireConnection;16;2;13;0
WireConnection;16;3;4;0
WireConnection;16;4;71;0
WireConnection;17;0;11;4
WireConnection;17;1;93;0
WireConnection;17;2;14;4
WireConnection;17;3;15;0
WireConnection;17;4;70;0
WireConnection;17;5;97;0
WireConnection;17;6;99;4
WireConnection;106;0;104;0
WireConnection;106;1;103;0
WireConnection;106;2;105;0
WireConnection;1;2;16;0
WireConnection;1;9;17;0
WireConnection;1;11;106;0
ASEEND*/
//CHKSM=5E518D7B829140EB93EC982B235A08ABDD1BC9E1
