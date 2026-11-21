Shader "FamiliarDread/WorldText" { Properties { _MainTex ("Font",2D)="white" {} } SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" } Pass { Tags { "LightMode"="SRPDefaultUnlit" } ZWrite Off ZTest LEqual Cull Off Blend SrcAlpha OneMinusSrcAlpha HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; }; struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; }; sampler2D _MainTex; v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;} half4 frag(v2f i):SV_Target{return half4(i.color.rgb,i.color.a*tex2D(_MainTex,i.uv).a);}
ENDHLSL } } }