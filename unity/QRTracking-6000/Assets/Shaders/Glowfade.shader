Shader "Custom/GlowFade"
{
    Properties
    {
        _GlowColor("Glow Color", Color) = (1,1,1,1)
        _GlowAmount("Glow Amount", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 _GlowColor;
            float _GlowAmount;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed alpha = 1 - _GlowAmount;
                return fixed4(_GlowColor.rgb * _GlowAmount, alpha);
            }
            ENDCG
        }
    }
}
