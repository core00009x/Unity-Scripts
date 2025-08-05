Shader "Custom/SkidMarkShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,5)) = 1
        _Tiling ("Tiling", Float) = 1
        _FadeFactor ("Fade Factor", Range(0,1)) = 1
        _SurfaceBlend ("Surface Blend", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        float4 _Color;
        float _NormalStrength;
        float _Tiling;
        float _FadeFactor;
        float _SurfaceBlend;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Apply tiling
            float2 tiledUV = IN.uv_MainTex * float2(_Tiling, 1);
            
            // Sample main texture
            fixed4 c = tex2D (_MainTex, tiledUV) * _Color;
            o.Albedo = c.rgb;
            
            // Apply fading
            o.Alpha = c.a * _FadeFactor;
            
            // Sample and apply normal map
            float3 normal = UnpackNormal(tex2D(_BumpMap, tiledUV));
            normal.xy *= _NormalStrength;
            o.Normal = normal;
            
            // Blend with surface normals
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            float3 surfaceNormal = WorldNormalVector(IN, float3(0,0,1));
            o.Normal = lerp(worldNormal, surfaceNormal, _SurfaceBlend);
        }
        ENDCG
    }
    FallBack "Diffuse"
}