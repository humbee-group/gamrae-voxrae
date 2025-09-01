Shader "Voxrae/DebugFaces"
{
    Properties
    {
        _MainTexArray ("Texture Array", 2DArray) = "" {}
        _DebugFaces   ("Debug Mode (0=off,1=on)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_MainTexArray);
            SAMPLER(sampler_MainTexArray);
            float _DebugFaces;

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv0        : TEXCOORD0;
                float  layer      : TEXCOORD1;
                float  faceCode   : TEXCOORD2;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv0        = IN.uv0;
                OUT.layer      = IN.uv1.x;
                OUT.faceCode   = IN.uv1.y;
                return OUT;
            }

            half4 FaceColor(float c)
            {
                if (c < 1.5) return half4(1,0,0,1);
                if (c < 2.5) return half4(0,1,0,1);
                if (c < 3.5) return half4(0,0,1,1);
                if (c < 4.5) return half4(1,1,0,1);
                if (c < 5.5) return half4(1,0,1,1);
                return half4(0,1,1,1);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                int layer = (int)IN.layer;
                half4 texCol = SAMPLE_TEXTURE2D_ARRAY(_MainTexArray, sampler_MainTexArray, IN.uv0, layer);
                if (_DebugFaces > 0.5) return lerp(texCol, FaceColor(IN.faceCode), 0.3);
                return texCol;
            }
            ENDHLSL
        }
    }
}