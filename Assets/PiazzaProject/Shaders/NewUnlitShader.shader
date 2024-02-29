Shader "Custom/URP_CustomShader"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _AlphaMap ("Alpha Texture", 2D) = "white" {}
        _Threshold ("Alpha Threshold", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

#include "UnityCG.cginc"

struct appdata_t
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
    float4 vertex : SV_POSITION;
};

sampler2D _BaseMap;
sampler2D _AlphaMap;
float _Threshold;

v2f vert(appdata_t v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    UNITY_TRANSFER_FOG(o, o.vertex);
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    fixed4 baseColor = tex2D(_BaseMap, i.uv);
    float alphaValue = tex2D(_AlphaMap, i.uv).r;
    clip(alphaValue - _Threshold);
    baseColor.a *= alphaValue;
    return baseColor;
}
            ENDCG
        }
    }
}
