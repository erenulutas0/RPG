Shader "Cryptforge/Rendered Character Flash"
{
    // The pre-rendered hero's hit flash (RenderedCharacterAnimator): the shown frame's silhouette filled with the
    // renderer's colour - white for a hit, gold for a critical one - drawn over the frame. A sprite tint can only darken,
    // and Ilya's robe is already light. Blending, depth and atlas use are Sprites/Default's.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            fixed4 frag(v2f IN) : SV_Target
            {
                // Only the texture's coverage is kept; the colour is the renderer's, premultiplied as Sprites/Default's.
                fixed alpha = SampleSpriteTexture(IN.texcoord).a * IN.color.a;
                return fixed4(IN.color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
