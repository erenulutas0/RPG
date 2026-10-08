Shader "Cryptforge/Foundry Ground"
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
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" "DisableBatching"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SurfaceVert
            #pragma fragment SpriteFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _Surface, _Region;
            v2f SurfaceVert(appdata_t input)
            {
                v2f output = SpriteVert(input);
                float4 world = mul(unity_ObjectToWorld, UnityFlipSprite(input.vertex, _Flip));
                float u = ((world.x - _Region.x) + 2 * (world.y - _Region.y)) * _Surface.w;
                world.y += saturate((u - _Surface.x) * _Surface.y) * _Surface.z;
                output.vertex = mul(UNITY_MATRIX_VP, world);
                return output;
            }
            ENDCG
        }
    }
}
