Shader "Custom/FogPlaneSoftEdge"
{
    Properties
    {
        _MainTex ("Fog Texture", 2D) = "white" {}
        _FogPlaneAlpha ("Fog Alpha", Range(0,1)) = 1
        _FogSoftness ("Fog Edge Softness", Range(0.001, 1)) = 0.1
        _Color ("Tint", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FogPlaneAlpha;
            float _FogSoftness;
            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float visibility = tex2D(_MainTex, i.uv).r;
                float alpha = smoothstep(0, _FogSoftness, visibility) * _FogPlaneAlpha;
                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
