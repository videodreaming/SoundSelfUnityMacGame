Shader "Custom/SimpleGrabPassBlur"
{
    // Blur is pre-computed and cached by UIBlurManager into _UIBlurGrabTexture.
    // This shader just samples that cached texture in screen space.
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Pass
        {
            Tags { "LightMode" = "Always" }

            Cull Off
            Lighting Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uvgrab : TEXCOORD0;
                float2 uvmain : TEXCOORD1;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            float4 _MainTex_ST;
            sampler2D _UIBlurGrabTexture;
            sampler2D _MainTex;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvgrab = ComputeGrabScreenPos(o.vertex);
                o.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Divide out the perspective, then flip Y.
                // Graphics.Blit stores Y=0 at the bottom of the RT, while
                // ComputeGrabScreenPos was designed for GrabPass (backbuffer capture)
                // which has the opposite Y convention on Metal/OpenGL.
                float2 uv = i.uvgrab.xy / i.uvgrab.w;
                uv.y = 1.0 - uv.y;
                half4 blurred = tex2D(_UIBlurGrabTexture, uv);
                half4 tint = tex2D(_MainTex, i.uvmain) * i.color;
                return half4(blurred.rgb * tint.rgb, tint.a);
            }
            ENDCG
        }
    }
}
