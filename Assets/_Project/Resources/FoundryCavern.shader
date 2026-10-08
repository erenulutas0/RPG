Shader "Cryptforge/Foundry Cavern"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment CavernFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            fixed4 CavernFrag(v2f input) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(input.texcoord) * input.color;
                // One sample in the existing backdrop draw: distant furnace light loses
                // saturation and contrast into violet air. No full-screen overlay or bloom.
                half luminance = dot(c.rgb, half3(.2126,.7152,.0722));
                c.rgb = lerp(luminance.xxx, c.rgb, .72);
                c.rgb = c.rgb * .78 + half3(.072,.067,.115) * .22;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
