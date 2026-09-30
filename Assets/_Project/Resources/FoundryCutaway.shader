Shader "Cryptforge/Foundry Cutaway"
{
    // Above-floor foundry structures. Colour keeps painter order (ZTest Always, sorting orders unchanged); a second,
    // depth-only pass writes each structure's ground-contact depth (vertex z) so actors behind it fail their default
    // LEqual test. UV1: x = the vertex belongs to a known structure (writes depth), y = the structure may fade.
    // _HeroFocus: xy = hero feet + (0,.35), z = hero depth, w = enabled. Only structures in front of the hero fade,
    // only over his body, and a faded fragment writes no depth, so he shows through them.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HeroFocus ("Hero world focus, depth and enable", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        fixed4 _Color;
        float4 _HeroFocus;
        struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 flags : TEXCOORD1; fixed4 color : COLOR; };
        struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; float2 flags : TEXCOORD2; fixed4 color : COLOR; };
        Output vert(Input v)
        {
            Output o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.uv = v.uv;
            o.flags = v.flags;
            o.color = v.color * _Color;
            return o;
        }
        fixed4 Shade(Output i)
        {
            fixed4 c = tex2D(_MainTex, i.uv) * i.color;
            // An ellipse over the hero's body, from just below his feet to above his head.
            float2 offset = (i.world.xy - _HeroFocus.xy - float2(0, .4)) / float2(1.25, 1.6);
            float body = 1 - smoothstep(.6, 1, length(offset));
            float aboveFeet = smoothstep(-.55, -.3, i.world.y - _HeroFocus.y);
            float inFront = saturate((_HeroFocus.z - i.world.z) * 5);
            float cut = body * aboveFeet * inFront * i.flags.y * _HeroFocus.w;
            c.a *= 1 - cut * .92;
            return c;
        }
        ENDCG

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 c = Shade(i);
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
        Pass
        {
            ZWrite On
            ZTest Always
            ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(Output i) : SV_Target
            {
                clip(min(Shade(i).a, i.flags.x) - .5);
                return 0;
            }
            ENDCG
        }
    }
}
