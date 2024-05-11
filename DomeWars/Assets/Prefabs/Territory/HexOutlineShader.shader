Shader "Custom/HexOutlineShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "red" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.0, 0.1)) = 0.05
    }
    SubShader
    {
        Tags {"Queue"="Overlay" "RenderType"="Transparent"}
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
            #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 pos : POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _OutlineColor;
            float _OutlineThickness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                float2 offset[8] = float2[8](
                    float2(-1, -1), float2(0, -1), float2(1, -1),
                    float2(-1, 0),                  float2(1, 0),
                    float2(-1, 1), float2(0, 1), float2(1, 1)
                );
                
                float2 uv = i.texcoord;
                float2 pixelSize = _OutlineThickness / float2(_ScreenParams.x, _ScreenParams.y);
                half4 mainColor = tex2D(_MainTex, uv);
                half4 outlineColor = _OutlineColor;
                
                bool hasOutline = false;
                for (int j = 0; j < 8; j++)
                {
                    float2 sampleUV = uv + offset[j] * pixelSize;
                    half4 sampleColor = tex2D(_MainTex, sampleUV);
                    if (sampleColor.a < 0.1 && mainColor.a > 0.1)
                    {
                        hasOutline = true;
                        break;
                    }
                }
                
                return hasOutline ? outlineColor : mainColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}