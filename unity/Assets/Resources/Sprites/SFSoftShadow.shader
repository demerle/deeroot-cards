Shader "Sprites/SFSoftShadow" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Tint ("Tint and Transparency", Vector) = (1,1,1,1)
		_SoftHardMix ("Unshadowed/Shadowed Mix", Range(0, 1)) = 0
		_AmbientOnlyMix ("Lit/Ambient Mix", Range(0, 1)) = 0
		_Glow ("Self Illumination", Vector) = (0,0,0,0)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
}