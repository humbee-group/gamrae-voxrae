Shader "Voxrae/VoxelArrayLit"
{
    Properties
    {
        _MainTexArray ("Texture Array", 2DArray) = "" {}
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
                float2 uv1        : TEXCOORD1; // uv1.x = tile index
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD2;
                float2 uv0        : TEXCOORD0;
                float  tile       : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv0        = IN.uv0;
                OUT.tile       = IN.uv1.x;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                int layer = (int)IN.tile;
                return SAMPLE_TEXTURE2D_ARRAY(_MainTexArray, sampler_MainTexArray, IN.uv0, layer);
            }
            ENDHLSL
        }
    }
}