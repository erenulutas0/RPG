using System.Collections.Generic;
using Cryptforge.Content;
using UnityEngine;

namespace Cryptforge.UI
{
    // The region's floor dress (FloorTheme). Foundry is what Build made and is never touched; Ice re-tints in place, once
    // per change: every owned mesh's vertex colours through IceTint (brass, amber, lava and flame to ice and cold cyan,
    // everything else to a darker desaturated blue-grey), a property-block tint on each mesh renderer's material (the
    // textures cool too; the materials themselves stay shared and unedited), and the sprites' colour. No geometry, no new
    // material, no new draw. Foundry restores the kept arrays and clears the blocks. First palette: the 2026-11-12 brief
    // (ground #5E6E80 / #8FA3B5 / #1C2633, frost #DDEBF5, ice #7FD4F0, cold light #9FE8FF, backdrop #0E1A2E-#1F4A5C).
    public sealed partial class FoundryRegionView
    {
        private static readonly int ColourId=Shader.PropertyToID("_Color");
        // Neutral stone keeps its value and leans to the ground's blue-grey; a material tint darkens and cools textures.
        private static readonly Color IceStone=new Color(.80f,.90f,1.06f),IceMaterial=new Color(.64f,.74f,.88f);
        // #7FD4F0 and #9FE8FF at unit luminance: a warm colour keeps its brightness and takes the ice hue.
        private static readonly Color IceInlay=new Color(.67f,1.12f,1.26f),IceGlow=new Color(.75f,1.09f,1.2f);
        private static readonly Color IceSprite=new Color(.62f,.76f,.95f);
        private static readonly Color BackdropDeep=new Color(.055f,.10f,.18f),BackdropTeal=new Color(.12f,.29f,.36f);
        private FloorTheme _theme=FloorTheme.Foundry;
        private readonly List<Mesh> _themeMeshes=new List<Mesh>();
        private readonly List<Color[]> _foundryColours=new List<Color[]>(),_iceColours=new List<Color[]>();
        private readonly List<SpriteRenderer> _themeSprites=new List<SpriteRenderer>();
        private readonly List<Color> _foundrySpriteColours=new List<Color>();
        private bool _themeKept;
        private MaterialPropertyBlock _themeBlock;

        public FloorTheme Theme=>_theme;

        public void ApplyTheme(FloorTheme theme)
        {
            if(theme==_theme)return;
            KeepFoundryColours();
            bool ice=theme==FloorTheme.Ice;
            for(int i=0;i<_themeMeshes.Count;i++)
            {
                if(_themeMeshes[i]==null)continue;
                if(ice&&_iceColours[i]==null)
                {
                    var source=_foundryColours[i];var graded=new Color[source.Length];
                    for(int v=0;v<source.Length;v++)graded[v]=IceTint(source[v]);
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
                _themeBlock.Clear();
                _themeBlock.SetColor(ColourId,material.GetColor(ColourId)*IceMaterial);
                renderer.SetPropertyBlock(_themeBlock);
            }
            for(int i=0;i<_themeSprites.Count;i++)
                if(_themeSprites[i]!=null)_themeSprites[i].color=ice?_foundrySpriteColours[i]*IceSprite:_foundrySpriteColours[i];
            _theme=theme;
        }

        private void KeepFoundryColours()
        {
            if(_themeKept)return;
            _themeKept=true;
            foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;
                if(mesh==null||!_owned.Contains(mesh)||_themeMeshes.Contains(mesh))continue;
                var colours=mesh.colors;
                if(colours==null||colours.Length==0)continue;
                _themeMeshes.Add(mesh);_foundryColours.Add(colours);_iceColours.Add(null);
            }
            foreach(var sprite in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(sprite.name==FoundryVisibilityView.WarmUpName)continue;
                _themeSprites.Add(sprite);_foundrySpriteColours.Add(sprite.color);
            }
        }

        // A vertex colour (a multiplier on the texture, up to the shaders' 2x ceiling) on the Ice floor.
        public static Color IceTint(Color c)
        {
            float max=Mathf.Max(c.r,Mathf.Max(c.g,c.b)),min=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
            float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
            float saturation=max>1e-4f?(max-min)/max:0;
            // Warm and saturated: brass, amber, ember, lava. Neutral stone stays stone.
            float warm=max>1e-4f?Mathf.Clamp01((c.r-c.b)/max*1.6f)*Mathf.Clamp01(saturation*2.2f):0;
            Color stone=IceStone*luminance;
            Color ice=(max>1.05f?IceGlow:IceInlay)*luminance;
            Color graded=Color.LerpUnclamped(stone,ice,warm);
            graded.r=Mathf.Min(graded.r,2);graded.g=Mathf.Min(graded.g,2);graded.b=Mathf.Min(graded.b,2);
            graded.a=c.a;
            return graded;
        }

        // The painted backdrop's tint on the Ice floor: deep navy to teal over the plate's own value.
        public static Color IceBackdrop(Color foundry)
        {
            Color tint=Color.Lerp(BackdropDeep,BackdropTeal,.5f)*2.4f;
            return new Color(foundry.r*tint.r,foundry.g*tint.g,foundry.b*tint.b,foundry.a);
        }
    }
}
