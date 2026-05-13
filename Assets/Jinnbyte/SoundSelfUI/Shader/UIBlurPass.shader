Shader "Hidden/UIBlurPass"
{
    // Used internally by UIBlurManager for separated Gaussian blur via Graphics.Blit.
    // Pass 0 = horizontal, Pass 1 = vertical.
    Properties { _MainTex ("", 2D) = "" {} }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    float _BlurSize;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv  : TEXCOORD0;
    };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv  = v.texcoord;
        return o;
    }

    // 9-tap separated Gaussian (sigma ≈ 1.5), weights sum to 1.
    half4 BlurH(float2 uv)
    {
        float d = _MainTex_TexelSize.x * _BlurSize;
        half4 c  = tex2D(_MainTex, uv)                    * 0.2270270270;
        c += tex2D(_MainTex, uv + float2( d,   0)) * 0.1945945946;
        c += tex2D(_MainTex, uv + float2(-d,   0)) * 0.1945945946;
        c += tex2D(_MainTex, uv + float2( d*2, 0)) * 0.1216216216;
        c += tex2D(_MainTex, uv + float2(-d*2, 0)) * 0.1216216216;
        c += tex2D(_MainTex, uv + float2( d*3, 0)) * 0.0540540541;
        c += tex2D(_MainTex, uv + float2(-d*3, 0)) * 0.0540540541;
        c += tex2D(_MainTex, uv + float2( d*4, 0)) * 0.0162162162;
        c += tex2D(_MainTex, uv + float2(-d*4, 0)) * 0.0162162162;
        return c;
    }

    half4 BlurV(float2 uv)
    {
        float d = _MainTex_TexelSize.y * _BlurSize;
        half4 c  = tex2D(_MainTex, uv)                    * 0.2270270270;
        c += tex2D(_MainTex, uv + float2(0,  d  )) * 0.1945945946;
        c += tex2D(_MainTex, uv + float2(0, -d  )) * 0.1945945946;
        c += tex2D(_MainTex, uv + float2(0,  d*2)) * 0.1216216216;
        c += tex2D(_MainTex, uv + float2(0, -d*2)) * 0.1216216216;
        c += tex2D(_MainTex, uv + float2(0,  d*3)) * 0.0540540541;
        c += tex2D(_MainTex, uv + float2(0, -d*3)) * 0.0540540541;
        c += tex2D(_MainTex, uv + float2(0,  d*4)) * 0.0162162162;
        c += tex2D(_MainTex, uv + float2(0, -d*4)) * 0.0162162162;
        return c;
    }

    half4 fragH(v2f i) : SV_Target { return BlurH(i.uv); }
    half4 fragV(v2f i) : SV_Target { return BlurV(i.uv); }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass // 0 - Horizontal
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragH
            ENDCG
        }

        Pass // 1 - Vertical
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragV
            ENDCG
        }
    }
}
