Shader "Cryptforge/Foundry Actor Xray"
{
    // Enemy bodies in the foundry. The first pass is Sprites/Default. The second draws only where a structure in front
    // wrote its depth (ZTest Greater), as an ember outline over a faint fill, so an enemy behind a boiler still shows
    // where it is. Without structure depth - ordinary halls, or everywhere the floor is open - the second pass draws nothing.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _XrayColor ("Hidden silhouette colour", Color) = (1,.46,.2,1)
        _XrayFill ("Hidden fill opacity", Range(0,1)) = .3
        _XrayEdge ("Hidden outline opacity", Range(0,1)) = .95
        _XrayWidth ("Outline width in screen pixels", Float) = 2.5
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
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            ENDCG
        }
        Pass
        {
            ZTest Greater
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            fixed4 _XrayColor;
            fixed _XrayFill;
            fixed _XrayEdge;
            float _XrayWidth;
            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                fixed a = SampleSpriteTexture(uv).a;
                // An inner outline a few screen pixels wide, whatever the sprite's texel density.
                float2 reach = fwidth(uv) * _XrayWidth;
                fixed inner = min(min(SampleSpriteTexture(uv + float2(reach.x, 0)).a, SampleSpriteTexture(uv - float2(reach.x, 0)).a),
                                  min(SampleSpriteTexture(uv + float2(0, reach.y)).a, SampleSpriteTexture(uv - float2(0, reach.y)).a));
                fixed edge = saturate(a - inner);
                // Vertex alpha carries fades (death, spawn); the hit tint in rgb is ignored so the silhouette stays one colour.
                fixed alpha = step(.5, a) * lerp(_XrayFill, _XrayEdge, edge) * IN.color.a;
                return fixed4(_XrayColor.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
