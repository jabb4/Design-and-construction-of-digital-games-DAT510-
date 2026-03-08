Shader "Custom/HealthBarBothFade"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillAmount ("Fill Amount", Range(0,1)) = 1
        _FadeWidth ("Fade Width", Range(0,0.3)) = 0.04
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; float4 color : COLOR; };

            sampler2D _MainTex;
            float _FillAmount;
            float _FadeWidth;

            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // How far each edge has eaten inward
                float edge = (1.0 - _FillAmount) * 0.5;

                // Fade in from the left inward edge
                float leftFade  = smoothstep(edge, edge + _FadeWidth, i.uv.x);
                // Fade in from the right inward edge
                float rightFade = smoothstep(1.0 - edge, 1.0 - edge - _FadeWidth, i.uv.x);

                col.a *= leftFade * rightFade;
                clip(col.a - 0.001);
                return col;
            }
            ENDCG
        }
    }
}