Shader "SecurityCenter/MM_CRTStatic"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Intensity ("Static Intensity", Range(0, 1)) = 0.08
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.22
        _RollSpeed ("Rolling Bar Speed", Float) = 0.16
        _NoiseScale ("Noise Scale", Float) = 420.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

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

            sampler2D _MainTex;
            fixed4 _Color;
            float _Intensity;
            float _ScanlineStrength;
            float _RollSpeed;
            float _NoiseScale;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float t = floor(_Time.y * 28.0);

                // Animated UV jump + horizontal tear on _MainTex (works in both Edit Mode and Play Mode!)
                float2 animOffset = float2(frac(t * 0.371), frac(t * 0.619));
                float tearLine = hash21(float2(floor(uv.y * 64.0), t + 1.0));
                float jitterX = (tearLine > 0.94) ? (tearLine - 0.94) * 0.45 : 0.0;

                fixed4 texNoise = tex2D(_MainTex, frac(uv * float2(1.35, 1.35) + animOffset + float2(jitterX, 0.0)));
                fixed4 texNoise2 = tex2D(_MainTex, frac(uv * float2(2.1, 2.1) - animOffset * 1.3));
                float n = lerp(texNoise.r, texNoise2.r, 0.45);

                // Rolling sync bar (Ref 3)
                float rollPos = frac(0.62 - _Time.y * _RollSpeed);
                float rollDist = abs(uv.y - rollPos);
                float rollBar = smoothstep(0.12, 0.0, rollDist) * 0.18;

                // Fine CRT scanlines
                float scan = 1.0 - _ScanlineStrength * (0.5 + 0.5 * sin(uv.y * 960.0));

                // Subtle CRT flicker
                float flicker = 0.96 + 0.04 * sin(_Time.y * 47.0);

                float val = saturate((n + rollBar) * scan * flicker);
                return fixed4(val * i.color.rgb, saturate(_Intensity) * i.color.a);
            }
            ENDCG
        }
    }
}
