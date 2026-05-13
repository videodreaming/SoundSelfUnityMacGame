Shader "Custom/SimpleGrabPassBlur"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Size ("Size", Range(0, 20)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        GrabPass
        {
            "_UIBlurGrabTexture"
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

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvgrab = ComputeGrabScreenPos(o.vertex);
                o.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            sampler2D _UIBlurGrabTexture;
            float4 _UIBlurGrabTexture_TexelSize;
            sampler2D _MainTex;
            half _Size;

            half4 GrabSample(float4 uvgrab, float2 offset)
            {
                float4 uv = uvgrab;
                uv.xy += offset * uvgrab.w;
                return tex2Dproj(_UIBlurGrabTexture, UNITY_PROJ_COORD(uv));
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 blurStep = _UIBlurGrabTexture_TexelSize.xy * _Size * 2.0;

                half4 sum = half4(0, 0, 0, 0);
                sum += GrabSample(i.uvgrab, float2(0, 0)) * 0.20;
                sum += GrabSample(i.uvgrab, float2( blurStep.x, 0)) * 0.12;
                sum += GrabSample(i.uvgrab, float2(-blurStep.x, 0)) * 0.12;
                sum += GrabSample(i.uvgrab, float2(0,  blurStep.y)) * 0.12;
                sum += GrabSample(i.uvgrab, float2(0, -blurStep.y)) * 0.12;
                sum += GrabSample(i.uvgrab, float2( blurStep.x,  blurStep.y)) * 0.08;
                sum += GrabSample(i.uvgrab, float2(-blurStep.x,  blurStep.y)) * 0.08;
                sum += GrabSample(i.uvgrab, float2( blurStep.x, -blurStep.y)) * 0.08;
                sum += GrabSample(i.uvgrab, float2(-blurStep.x, -blurStep.y)) * 0.08;

                half4 tint = tex2D(_MainTex, i.uvmain) * i.color;
                return half4(sum.rgb * tint.rgb, tint.a);
            }
            ENDCG
        }
    }
}
