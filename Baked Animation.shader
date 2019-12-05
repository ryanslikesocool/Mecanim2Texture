Shader "ifelse/Baked Animation"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" { }
        _AnimationTex ("Animation Texture", 2D) = "white" { }
        _LightEuler ("Light Euler", Vector) = (30.000000,90.000000,-60.000000,0.000000)
        _ShadowColor ("Shadow Color", Color) = (0.188679,0.188679,0.188679,0.000000)
        _FPS ("FPS", Int) = 60
        _AnimationFrameCount ("Animation Frame Count", Float) = 39.000000
        _VertexCount ("Vertex Count", Int) = 660
        _TexSize ("Texture Size", Vector) = (256.000000,256.000000,0.000000,0.000000)
        _Scaler ("Scaler", Float) = 0.500000
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "HLSLSupport.cginc"

            struct appdata
            {
                float2 uv : TEXCOORD0;
                uint vid: SV_VERTEXID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
            };

            float _FPS;
            texture2D _AnimationTex;
            int _VertexCount;
            float _AnimationFrameCount;
            float2 _TexSize;
            float _Scaler;

            SamplerState _AnimationSampler
            {
                Filter = MIN_MAG_MIP_POINT;
                AddressU = Wrap;
                AddressV = Wrap;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = v.uv;

                int frameCount = floor(_Time.y * _FPS);
                frameCount = floor(fmod(frameCount, _AnimationFrameCount));
                int pixelOffset = floor(frameCount * _VertexCount);

                float2 uvPos = float2(0.5, 0.5);
                uvPos.x += floor((v.vid + pixelOffset) / _TexSize.x);
                uvPos.y += fmod(v.vid + pixelOffset, _TexSize.y);
                uvPos /= _TexSize;

                float4 positions = _AnimationTex.SampleLevel(_AnimationSampler, uvPos, 0);
                positions -= float4(0.5, 0.5, 0.5, 0);
                positions /= _Scaler;
                o.vertex = UnityObjectToClipPos(positions);

                return o;
            }

            texture2D _MainTex;
            
            SamplerState _MainTexSampler
            {
                Filter = MIN_MAG_MIP_POINT;
                AddressU = Clamp;
                AddressV = Clamp;
            };

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _MainTex.Sample(_MainTexSampler, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
