using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Content;
using UnityEngine;

namespace Cryptforge.UI
{
    // The region's floor dress (FloorTheme). Foundry is what Build made and is never touched; Ice (docs/49) dresses it in
    // place, once per change, with no geometry, no new material and no new draw:
    // - property blocks on the mesh renderers: the court flagstones take IcePavingV1 and the masonry IceMasonryV1; the
    //   coping and inlay batch draws untextured (white), so its brass lines become bright mirror-ice from the vertex
    //   colours alone; every other material is darkened toward slate. The shared materials are never edited.
    // - every owned mesh's vertex colours through IceTint: brass, amber and gold to mirror-ice and silver, lava to
    //   quicksilver glow, flames to frost-light, neutral stone to a desaturated slate.
    // - the sprites' colour toward slate.
    // Foundry restores the kept vertex arrays and sprite colours and clears the blocks.
    public sealed partial class FoundryRegionView
    {
        private static readonly int ColourId=Shader.PropertyToID("_Color"),TextureId=Shader.PropertyToID("_MainTex");
        // docs/49's palette.
        internal static readonly Color MirrorIce=Hex(0x5FA8C8),IceEdge=Hex(0xBFE6F2),Silver=Hex(0xA9B4BF),
            QuicksilverGlow=Hex(0x7FE3FF),FrostLight=Hex(0xA6F0FF),Haze=Hex(0x6E8299),Frost=Hex(0xB9CCDA);
        // Neutral stone keeps its value, barely cooled: the textures carry the slate, and saturation stays under 0.2.
        private static readonly Color IceStone=new Color(.93f,.96f,1f);
        // Untextured coping stone (kerbs, grooves): a little darker than the paving, near docs/49's slate #3B4350.
        private static readonly Color IceBareStone=new Color(.30f,.33f,.39f);
        // Stone in a fire's pool: barely lit by frost-light, never brighter than a lit flag.
        private static readonly Color FrostLitStone=new Color(.86f,.97f,1.04f);
        // Materials without an ice texture yet (the base paving, iron, pit, baked props) and sprites: darker, cool grey.
        private static readonly Color IceSlate=new Color(.66f,.70f,.77f),IceSprite=new Color(.62f,.67f,.76f);
        private static readonly Color BackdropDeep=new Color(.055f,.10f,.18f),BackdropTeal=new Color(.12f,.29f,.36f);
        private enum IceDress{Slate,Paving,Masonry,Bare,Glow}
        private FloorTheme _theme=FloorTheme.Foundry;
        private readonly List<Mesh> _themeMeshes=new List<Mesh>();
        private readonly List<IceDress> _themeDress=new List<IceDress>();
        private readonly List<Color[]> _foundryColours=new List<Color[]>(),_iceColours=new List<Color[]>();
        private readonly List<SpriteRenderer> _themeSprites=new List<SpriteRenderer>();
        private readonly List<Color> _foundrySpriteColours=new List<Color>();
        private bool _themeKept;
        private MaterialPropertyBlock _themeBlock;

        public FloorTheme Theme=>_theme;

        public void ApplyTheme(FloorTheme theme,PlatformMaterialSet set)
        {
            if(theme==_theme)return;
            KeepFoundryColours(set);
            bool ice=theme==FloorTheme.Ice;
            for(int i=0;i<_themeMeshes.Count;i++)
            {
                if(_themeMeshes[i]==null)continue;
                if(ice&&_iceColours[i]==null)
                {
                    var source=_foundryColours[i];var graded=new Color[source.Length];
                    IceDress dress=_themeDress[i];
                    if(dress==IceDress.Bare)
                    {
                        // The coping sheet's rail is its only metal: brass there becomes mirror-ice, silver on the
                        // compass seal; every other decal vertex is the kerbs' and grooves' stone.
                        var uvs=_themeMeshes[i].uv;var positions=_themeMeshes[i].vertices;
                        for(int v=0;v<source.Length;v++)
                        {
                            if(v>=uvs.Length||!OnCopingRail(uvs[v])){graded[v]=IceTint(source[v],true);continue;}
                            FoundrySurface.ToDesign(positions[v].x,positions[v].y,out float u,out float w);
                            graded[v]=IceRail(source[v],Vector2.Distance(new Vector2(u,w),CourtSealCentre)<CourtStoneRadius+.1f);
                        }
                    }
                    else for(int v=0;v<source.Length;v++)graded[v]=IceTint(source[v],false,dress==IceDress.Glow);
                    _iceColours[i]=graded;
                }
                _themeMeshes[i].colors=ice?_iceColours[i]:_foundryColours[i];
            }
            if(_themeBlock==null)_themeBlock=new MaterialPropertyBlock();
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!ice){renderer.SetPropertyBlock(null);continue;}
                var material=renderer.sharedMaterial;
                if(material==null||!material.HasProperty(ColourId))continue;
                IceDress dress=DressOf(material,set);
                Color colour=material.GetColor(ColourId);
                _themeBlock.Clear();
                switch(dress)
                {
                    case IceDress.Paving:
                        if(set.IcePaving!=null)_themeBlock.SetTexture(TextureId,set.IcePaving);else colour*=IceSlate;
                        break;
                    case IceDress.Masonry:
                        if(set.IceMasonry!=null)_themeBlock.SetTexture(TextureId,set.IceMasonry);else colour*=IceSlate;
                        break;
                    case IceDress.Bare:
                        _themeBlock.SetTexture(TextureId,Texture2D.whiteTexture);
                        break;
                    default:
                        colour*=IceSlate;
                        break;
                }
                _themeBlock.SetColor(ColourId,colour);
                renderer.SetPropertyBlock(_themeBlock);
            }
            for(int i=0;i<_themeSprites.Count;i++)
                if(_themeSprites[i]!=null)_themeSprites[i].color=ice?_foundrySpriteColours[i]*IceSprite:_foundrySpriteColours[i];
            _theme=theme;
        }

        private void KeepFoundryColours(PlatformMaterialSet set)
        {
            if(_themeKept)return;
            _themeKept=true;
            foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;
                if(mesh==null||!_owned.Contains(mesh)||_themeMeshes.Contains(mesh))continue;
                var colours=mesh.colors;
                if(colours==null||colours.Length==0)continue;
                var renderer=filter.GetComponent<MeshRenderer>();
                _themeMeshes.Add(mesh);_foundryColours.Add(colours);_iceColours.Add(null);
                _themeDress.Add(renderer!=null&&renderer.sharedMaterial!=null?DressOf(renderer.sharedMaterial,set):IceDress.Slate);
            }
            foreach(var sprite in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(sprite.name==FoundryVisibilityView.WarmUpName)continue;
                _themeSprites.Add(sprite);_foundrySpriteColours.Add(sprite.color);
            }
        }

        // A renderer's material, or the structure variant made from it (FoundryVisibilityView.ForStructure).
        private static IceDress DressOf(Material material,PlatformMaterialSet set)
        {
            if(set==null)return IceDress.Slate;
            if(Is(material,set.FoundryFlagstone))return IceDress.Paving;
            if(Is(material,set.FoundryWall)||Is(material,set.Wall))return IceDress.Masonry;
            if(Is(material,set.Coping))return IceDress.Bare;
            if(Is(material,set.FoundryEmbers))return IceDress.Glow;
            return IceDress.Slate;
        }
        private static bool Is(Material material,Material source)=>source!=null&&
            (material==source||material.name==source.name+" (scene cutaway)");

        // A vertex colour (a multiplier on the texture, under the shaders' 2x ceiling) on the Ice floor. bare: the batch
        // draws untextured, so the colour is the final one (kerb stone; its rail goes through IceRail). glow: the embers
        // material (fires, lantern panes, the molten pool), whose warm colours become cold light; anywhere else a warm
        // vertex colour is a fire's pool on stone and becomes stone faintly frost-lit, capped so no pool blows out.
        public static Color IceTint(Color c,bool bare=false,bool glow=false)
        {
            float max=Mathf.Max(c.r,Mathf.Max(c.g,c.b)),min=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
            float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
            Color graded;
            if(bare)graded=IceBareStone*Mathf.Clamp(luminance,.5f,1.2f);
            else
            {
                float warm=Warmth(c,max,min);
                Color stone=IceStone*Mathf.Min(luminance,1.25f);
                Color ice=!glow?FrostLitStone*Mathf.Min(luminance,1.1f)
                    // A light: lava (red, little green) to quicksilver glow, a flame or brazier to frost-light.
                    :max>1.05f?(c.g<c.r*.55f?QuicksilverGlow:FrostLight)*Mathf.Min(max,1.5f):FrostLight*luminance;
                graded=Color.LerpUnclamped(stone,ice,warm);
            }
            graded.r=Mathf.Min(graded.r,2);graded.g=Mathf.Min(graded.g,2);graded.b=Mathf.Min(graded.b,2);
            graded.a=c.a;
            return graded;
        }

        // A rail vertex of the untextured coping batch (its colour is final): inlaid mirror-ice #5FA8C8 at about 60-70%
        // of its value, a faint ice-edge #BFE6F2 sheen only where the floor light is strong; on the compass seal silver
        // #A9B4BF to ice edge.
        public static Color IceRail(Color c,bool seal)
        {
            float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
            Color graded=seal?Color.Lerp(Silver,IceEdge,Mathf.Clamp01((luminance-.7f)*1.5f))*Mathf.Clamp(luminance*1.15f,.85f,1.4f)
                :Color.Lerp(MirrorIce,IceEdge,Mathf.Clamp01((luminance-1f)*1.2f)*.45f)*Mathf.Clamp(luminance*.7f,.55f,.85f);
            graded.a=c.a;
            return graded;
        }

        // FoundryAtmosphereView on the Ice floor: smoke to cold haze, embers to frost motes, the pit's glow to quicksilver.
        public static Color ColdAtmosphere(Color c)
        {
            float max=Mathf.Max(c.r,Mathf.Max(c.g,c.b)),min=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
            float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
            float warm=Warmth(c,max,min);
            Color haze=Haze*Mathf.Min(luminance*1.9f,1.6f);
            Color light=(c.g<c.r*.5f?QuicksilverGlow:max>1.2f?FrostLight:Frost)*Mathf.Clamp(max,.6f,1.8f);
            Color cold=Color.LerpUnclamped(haze,light,warm);
            cold.a=c.a;
            return cold;
        }

        // How warm and saturated: brass, amber, ember, lava near 1; neutral stone and smoke near 0.
        private static float Warmth(Color c,float max,float min)
        {
            if(max<=1e-4f)return 0;
            float saturation=(max-min)/max;
            return Mathf.Clamp01((c.r-c.b)/max*1.6f)*Mathf.Clamp01(saturation*2.2f);
        }

        // The painted backdrop's tint on the Ice floor: deep navy to teal over the plate's own value.
        public static Color IceBackdrop(Color foundry)
        {
            Color tint=Color.Lerp(BackdropDeep,BackdropTeal,.5f)*2.4f;
            return new Color(foundry.r*tint.r,foundry.g*tint.g,foundry.b*tint.b,foundry.a);
        }

        private static Color Hex(int rgb)=>new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f,1);
    }
}
