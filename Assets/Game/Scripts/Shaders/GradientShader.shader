Shader "Custom/ExplosionRadialGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GradientColor1 ("Center Color", Color) = (1,1,0,1)
        _GradientColor2 ("Edge Color", Color) = (1,0,0,1)
        _GradientPower ("Gradient Power", Range(0.1, 5)) = 1
        _GradientScale ("Gradient Scale", Range(0.1, 2)) = 1
        _GradientOffset ("Gradient Offset", Range(-0.5, 0.5)) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            fixed4 _Color;
            fixed4 _GradientColor1;
            fixed4 _GradientColor2;
            float _GradientPower;
            float _GradientScale;
            float _GradientOffset;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;
            
            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord);
                float2 centerVec = IN.texcoord - float2(0.5, 0.5);
                float dist = length(centerVec) * 2.0;
                dist = (dist - _GradientOffset) * _GradientScale;
                dist = pow(saturate(dist), _GradientPower);
                fixed4 gradientColor = lerp(_GradientColor1, _GradientColor2, dist);
                fixed4 finalColor = gradientColor * texColor.a;
                finalColor.a = texColor.a * IN.color.a;
                finalColor.rgb *= finalColor.a;
                return finalColor;
            }
            ENDCG
        }
    }
}