Shader "Cryptforge/Foundry Cavern"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Atmosphere ("Violet atmospheric blend", Range(0,1)) = .10
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
            half _Atmosphere;
            fixed4 CavernFrag(v2f input) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(input.texcoord) * input.color;
                // One sample in the existing backdrop draw. The approved concept's void is a saturated violet haze
                // (value ~.25-.29, saturation ~.35) lit by lava falls; the former 72 % desaturation, dark indigo blend
                // and dimming tint left it near .17 and grey. Keep the painting's own saturation, lift its haze a little
                // toward the concept's violet and let molten light through. No full-screen overlay or bloom.
                half luminance = dot(c.rgb, half3(.2126,.7152,.0722));
                c.rgb = lerp(luminance.xxx, c.rgb, 1.08);
                c.rgb = lerp(c.rgb, half3(.20,.15,.30), _Atmosphere);
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
