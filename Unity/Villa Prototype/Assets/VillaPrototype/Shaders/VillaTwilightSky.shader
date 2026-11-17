Shader "FamiliarDread/TwilightSky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.085, 0.12, 0.25, 1)
        _HorizonColor ("Horizon", Color) = (0.37, 0.29, 0.36, 1)
        _GroundColor ("Below Horizon", Color) = (0.12, 0.145, 0.21, 1)
        _AfterglowColor ("Afterglow", Color) = (0.52, 0.29, 0.24, 1)
        _AfterglowDirection ("Afterglow Direction", Vector) = (-0.8, 0, 0.3, 0)
        _Exposure ("Exposure", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _ZenithColor, _HorizonColor, _GroundColor, _AfterglowColor;
            float4 _AfterglowDirection;
            float _Exposure;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.direction = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 direction = normalize(i.direction);
                // A brighter horizon blends into the blue-violet upper sky.
                float height = pow(saturate(direction.y), 0.42);
                float3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, height);
                sky = lerp(sky, _GroundColor.rgb, saturate(-direction.y * 4));

                // Only a faint directional afterglow remains; no sun disc or stars.
                float2 horizontal = direction.xz / max(length(direction.xz), 0.001);
                float2 glowDirection = normalize(_AfterglowDirection.xz);
                float glow = pow(saturate(dot(horizontal, glowDirection)), 6);
                float band = exp(-pow(direction.y / 0.12, 2));
                sky = lerp(sky, _AfterglowColor.rgb, glow * band * 0.45);
                return fixed4(sky * _Exposure, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
