using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Cryptforge.UI
{
    // The region's floor dress (FloorTheme). Foundry is what Build made and is never touched; Ice (docs/49) and Gloam
    // (docs/51) dress it in place, once per change, with no geometry, no new material and no new draw, through one shared
    // path that reads the theme's row of the palette table (ThemePalette):
    // - property blocks on the mesh renderers: the court flagstones take the theme's paving (IcePavingV1, GloamPavingV1)
    //   and the masonry its masonry; the coping and inlay batch draws untextured (white), so its brass lines take the
    //   theme's rail colour (mirror-ice, a dim sap channel) from the vertex colours alone; a glowing texture takes its
    //   variant from Resources/<theme>/; every other material is darkened toward the theme's stone. The shared materials
    //   are never edited.
    // - every owned mesh's vertex colours through Tint: brass, amber and gold to the theme's metal, lava to its glow
    //   (quicksilver, gloamlight), flames to its light (frost-light, a seed-lantern), neutral stone to its stone, and the
    //   upward faces of structures to its cap (rime, moss).
    // - the sprites' colour toward the theme's stone.
    // Foundry restores the kept vertex arrays and sprite colours and clears the blocks. Leaving a theme releases its
    // Resources variants (unbound first), so a run through floors 1-3 never holds two themes' variant sets.
    public sealed partial class FoundryRegionView
    {
        private static readonly int ColourId=Shader.PropertyToID("_Color"),TextureId=Shader.PropertyToID("_MainTex"),
            AtmosphereId=Shader.PropertyToID("_Atmosphere");
        // docs/49's palette.
        internal static readonly Color MirrorIce=Hex(0x5FA8C8),IceEdge=Hex(0xBFE6F2),Silver=Hex(0xA9B4BF),
            QuicksilverGlow=Hex(0x7FE3FF),FrostLight=Hex(0xA6F0FF),Haze=Hex(0x6E8299),Frost=Hex(0xB9CCDA);
        // docs/51's palette.
        internal static readonly Color Gloamlight=Hex(0xC6EE6A),GloamlightCore=Hex(0xE2F7A0),Verdigris=Hex(0x6FA38C),
            Lichen=Hex(0x8FA35A),Heartwood=Hex(0x5B4838),AshHaze=Hex(0x767A68),AshDrift=Hex(0xA7A99C);
        private static readonly Color SkyGloam=Hex(0x0B110F),SkyDusk=Hex(0x23302A);
        private static readonly Color BackdropDeep=new Color(.055f,.10f,.18f),BackdropTeal=new Color(.12f,.29f,.36f);

        // One theme's row of the table: every number the shared grade, rail, atmosphere and backdrop bodies read, so Ice
        // and Gloam run the same code with different colours (never a copied body per theme). Immutable once built: the
        // grade reads it on worker threads.
        private sealed class ThemePalette
        {
            public FloorTheme Theme;
            // Resources/<Folder>/<foundry texture name>: the theme's variants of the glowing and baked textures.
            public string Folder;
            // Neutral stone's multiplier (the textures carry the floor's colour; saturation stays under 0.2).
            public Color Stone;
            // Untextured coping stone (kerbs, grooves): a little darker than the paving.
            public Color BareStone;
            // Stone in a fire's pool: barely lit by the theme's light, never brighter than a lit flag.
            public Color LitStone;
            // Materials without a theme texture (the base paving, iron, pit, baked props) and sprites: darker, the stone's hue.
            public Color Untextured,Sprite;
            // On the embers material: lava (red, little green) to the theme's glow, a flame or brazier to its light.
            public Color LavaGlow,FlameLight;
            // The coping's brass rail and the compass seal (Rail).
            public Color Rail,RailEdge,Seal,SealEdge;
            // Per-channel multiplier on the upward faces of raised structures (rime on ice, moss in the garden).
            public Color Cap;
            // FoundryAtmosphereView: smoke to haze; lava, bright sparks and dim embers to the three lights.
            public Color Haze,AtmosphereLava,AtmosphereBright,AtmosphereMote;
            // The atmosphere's embers are the theme's motes, told apart by the view (one in five lit), not by their colour.
            public bool OwnMotes;
            // The distant works' violet atmospheric blend: only a trace stays.
            public float Atmosphere;
            // The painted backdrop's tint when the theme has no plate of its own.
            public Color BackdropTint;
            // The hero's contact patch on this floor (docs/49 7.1, docs/51 7.1).
            public float HeroShadow;
            // Floor 2's own props (crystals, mirror panels, snow drifts) stand only on this theme.
            public bool IceProps;
        }

        private static readonly ThemePalette IcePalette=new ThemePalette
        {
            Theme=FloorTheme.Ice,Folder="Ice/",
            Stone=new Color(.93f,.96f,1f),BareStone=new Color(.30f,.33f,.39f),LitStone=new Color(.86f,.97f,1.04f),
            Untextured=new Color(.66f,.70f,.77f),Sprite=new Color(.62f,.67f,.76f),
            LavaGlow=QuicksilverGlow*.7f,FlameLight=FrostLight,
            Rail=MirrorIce,RailEdge=IceEdge,Seal=Silver,SealEdge=IceEdge,
            // Toward frost #B9CCDA on a slate texture (a multiplier near 2 lifts its ~.35 value to ~.7).
            Cap=new Color(1.75f,1.9f,2f),
            Haze=FoundryRegionView.Haze,AtmosphereLava=QuicksilverGlow,AtmosphereBright=FrostLight,AtmosphereMote=Frost,OwnMotes=false,
            Atmosphere=.1f,BackdropTint=Color.Lerp(BackdropDeep,BackdropTeal,.5f)*2.4f,HeroShadow=1.25f,IceProps=true,
        };

        private static readonly ThemePalette GloamPalette=new ThemePalette
        {
            Theme=FloorTheme.Gloam,Folder="Gloam/",
            // Ash loam: barely olive, the textures carry the moss.
            Stone=new Color(.97f,.98f,.90f),BareStone=new Color(.30f,.31f,.27f),LitStone=new Color(.92f,1f,.84f),
            Untextured=new Color(.68f,.69f,.62f),Sprite=new Color(.64f,.66f,.58f),
            // The root well's sap at the ice pit's .65; seed-lanterns dimmer than the foundry's fires (docs/51 5).
            LavaGlow=Gloamlight*.65f,FlameLight=GloamlightCore*.8f,
            // The sap channel: bark-brown with gloamlight, dim (about half of #C6EE6A after the rail's own scale); the seal
            // verdigris to lichen. No orchid anywhere (docs/51 7.3).
            Rail=Color.Lerp(Heartwood,Gloamlight,.5f),RailEdge=Gloamlight,Seal=Verdigris,SealEdge=Lichen,
            // Moss cap: ash-loam stone toward moss #3F5A32-#4F6B3A on caps and crenellations.
            Cap=new Color(1.3f,1.65f,1.05f),
            Haze=AshHaze,AtmosphereLava=Gloamlight*.65f,AtmosphereBright=GloamlightCore,AtmosphereMote=AshDrift,OwnMotes=true,
            Atmosphere=.1f,BackdropTint=Color.Lerp(SkyGloam,SkyDusk,.5f)*2.4f,HeroShadow=1.25f,IceProps=false,
        };

        private static ThemePalette PaletteOf(FloorTheme theme)=>
            theme==FloorTheme.Ice?IcePalette:theme==FloorTheme.Gloam?GloamPalette:null;

        // What a renderer's material is to the dress.
        private enum Dress{Slate,Paving,Masonry,Bare,Glow}
        private FloorTheme _theme=FloorTheme.Foundry;
        // The foundry's own colours, kept once for every theme: an owned mesh, its colours and its dress, index for index.
        private readonly List<Mesh> _themeMeshes=new List<Mesh>();
        private readonly Dictionary<Mesh,int> _themeMeshIndex=new Dictionary<Mesh,int>();
        private readonly List<Dress> _themeDress=new List<Dress>();
        private readonly List<Color[]> _foundryColours=new List<Color[]>();
        private readonly List<SpriteRenderer> _themeSprites=new List<SpriteRenderer>();
        private readonly List<Color> _foundrySpriteColours=new List<Color>();
        private bool _spritesKept;
        private MaterialPropertyBlock _themeBlock;

        // A theme's dress worked out ahead of the floor change (PrepareTheme), so ApplyTheme only binds it: its grade of
        // every kept mesh computed on a worker thread, each renderer's block planned, its texture variants loaded
        // asynchronously (their GPU upload with them). The main thread's share runs in slices of about PrepareBudgetMs a
        // frame (Cryptforge.Theme.Prepare). A change that comes first finishes it at once.
        private const double PrepareBudgetMs=1;
        private struct ThemeBlock
        {
            public MeshRenderer Renderer;
            public Texture Texture;
            public Color Colour;
            public bool Atmosphere;
            // A Resources/<theme> variant still to resolve (null when none is asked for), and the wall-art fallback.
            public string Variant;
            public bool WallArt;
        }
        private sealed class ThemeDress
        {
            public ThemePalette Palette;
            public readonly List<ThemeBlock> Blocks=new List<ThemeBlock>();
            // Index for index with _themeMeshes; null where not graded yet.
            public readonly List<Color[]> Colours=new List<Color[]>();
            public readonly List<Task<Color[]>> Grades=new List<Task<Color[]>>();
            public readonly Dictionary<string,ResourceRequest> Requests=new Dictionary<string,ResourceRequest>();
            // Resources/<theme>/<name>: the theme's variant of a foundry texture (Tools/ArtReview/GenerateIceGlowTextures.py,
            // GenerateGloamGlowTextures.py), loaded once its floor is ahead; a run that never reaches it never loads one.
            public readonly Dictionary<string,Texture2D> Variants=new Dictionary<string,Texture2D>();
            public IEnumerator Preparation;
            public Coroutine Coroutine;
            public bool Prepared,Draining;
        }
        private readonly Dictionary<FloorTheme,ThemeDress> _dresses=new Dictionary<FloorTheme,ThemeDress>();

        public FloorTheme Theme=>_theme;
        public bool IsThemePrepared(FloorTheme theme)=>
            PaletteOf(theme)==null||(_dresses.TryGetValue(theme,out ThemeDress dress)&&dress.Prepared);
        public bool IsPreparingTheme
        {
            get
            {
                foreach(var dress in _dresses.Values)if(dress.Preparation!=null&&!dress.Prepared)return true;
                return false;
            }
        }
        // How many of the theme's Resources variants the region holds loaded: none before its floor is ahead, none once
        // its floor is left.
        public int HeldVariantCount(FloorTheme theme)
        {
            if(!_dresses.TryGetValue(theme,out ThemeDress dress))return 0;
            int held=0;
            foreach(var variant in dress.Variants.Values)if(variant!=null)held++;
            return held;
        }

        // Starts working out the theme's dress over the next frames; nothing on show changes. Foundry is what Build made;
        // asking again, or once it is ready, does nothing.
        public void PrepareTheme(FloorTheme theme,PlatformMaterialSet set)
        {
            ThemePalette palette=PaletteOf(theme);
            if(palette==null||_dresses.ContainsKey(theme))return;
            var dress=new ThemeDress{Palette=palette};
            _dresses.Add(theme,dress);
            dress.Preparation=Prepare(dress,set);
            ResumePreparations();
        }

        // Runs every pending preparation as a coroutine whenever the region is active (asked while inactive, or re-enabled).
        private void ResumePreparations()
        {
            if(!isActiveAndEnabled)return;
            foreach(var dress in _dresses.Values)
                if(dress.Preparation!=null&&!dress.Prepared&&!dress.Draining&&dress.Coroutine==null)
                    dress.Coroutine=StartCoroutine(dress.Preparation);
        }
        private void OnEnable()=>ResumePreparations();
        // A deactivated object's coroutines stop; the same iterator goes on where it stood when it is enabled again.
        private void OnDisable()
        {
            foreach(var dress in _dresses.Values)
                if(dress.Coroutine!=null){StopCoroutine(dress.Coroutine);dress.Coroutine=null;}
        }

        // The preparation finished now, in this frame: the rest of what PrepareTheme spreads over frames, a texture still
        // loading forced to complete and a grade still running waited for.
        private ThemeDress FinishPreparation(FloorTheme theme,PlatformMaterialSet set)
        {
            if(!_dresses.TryGetValue(theme,out ThemeDress dress))
            {
                dress=new ThemeDress{Palette=PaletteOf(theme)};
                _dresses.Add(theme,dress);
            }
            if(dress.Prepared)return dress;
            if(dress.Preparation==null)dress.Preparation=Prepare(dress,set);
            if(dress.Coroutine!=null){StopCoroutine(dress.Coroutine);dress.Coroutine=null;}
            dress.Draining=true;
            try{while(!dress.Prepared&&dress.Preparation.MoveNext()){}}
            finally{dress.Draining=false;}
            return dress;
        }

        private IEnumerator Prepare(ThemeDress dress,PlatformMaterialSet set)
        {
            MeshFilter[] filters;
            using(PerformanceMarkers.ThemePrepare.Auto())
            {
                filters=GetComponentsInChildren<MeshFilter>(true);
                KeepSprites();
            }
            yield return null;
            // The foundry's colours kept (once, for every theme) and each mesh's grade handed to a worker, a slice a frame.
            for(int i=0;i<filters.Length;)
            {
                using(PerformanceMarkers.ThemePrepare.Auto())
                {
                    long slice=Stopwatch.GetTimestamp();
                    for(;i<filters.Length&&Within(slice);i++)KeepMesh(filters[i],set,dress);
                }
                if(i<filters.Length)yield return null;
            }
            yield return null;
            // Every renderer's block planned and the theme's variants asked for.
            MeshRenderer[] renderers;
            using(PerformanceMarkers.ThemePrepare.Auto())renderers=GetComponentsInChildren<MeshRenderer>(true);
            for(int i=0;i<renderers.Length;)
            {
                using(PerformanceMarkers.ThemePrepare.Auto())
                {
                    long slice=Stopwatch.GetTimestamp();
                    for(;i<renderers.Length&&Within(slice);i++)
                    {
                        // Floor 2's own props have their own dress (BindIceObject) and are never another theme's.
                        if(renderers[i]==null||IsIceObject(renderers[i].gameObject))continue;
                        if(!PlanBlock(renderers[i],set,dress.Palette,out ThemeBlock block))continue;
                        if(!string.IsNullOrEmpty(block.Variant)&&!dress.Variants.ContainsKey(block.Variant)&&!dress.Requests.ContainsKey(block.Variant))
                            dress.Requests.Add(block.Variant,Resources.LoadAsync<Texture2D>(dress.Palette.Folder+block.Variant));
                        dress.Blocks.Add(block);
                    }
                }
                if(i<renderers.Length)yield return null;
            }
            // The loads finish in the background; a change that cannot wait takes them now.
            foreach(var pair in dress.Requests)
            {
                while(!dress.Draining&&!pair.Value.isDone)yield return null;
                using(PerformanceMarkers.ThemePrepare.Auto())dress.Variants[pair.Key]=pair.Value.asset as Texture2D;
            }
            dress.Requests.Clear();
            using(PerformanceMarkers.ThemePrepare.Auto())
                for(int i=0;i<dress.Blocks.Count;i++)dress.Blocks[i]=Resolve(dress.Blocks[i],set,dress);
            // The grades: the worker's result, or the same grade here if the worker failed.
            for(int i=0;i<dress.Grades.Count;i++)
            {
                Task<Color[]> grade=dress.Grades[i];
                if(grade==null)continue;
                while(!dress.Draining&&!grade.IsCompleted)yield return null;
                using(PerformanceMarkers.ThemePrepare.Auto())
                {
                    Color[] graded=null;
                    try{graded=grade.Result;}
                    catch(System.Exception){graded=null;}
                    if(graded==null&&_themeMeshes[i]!=null)graded=GradeOf(dress.Palette,_themeMeshes[i],_foundryColours[i],_themeDress[i]);
                    dress.Colours[i]=graded;
                    dress.Grades[i]=null;
                }
            }
            dress.Prepared=true;
            dress.Coroutine=null;
        }

        private static bool Within(long since)=>(Stopwatch.GetTimestamp()-since)*1000d/Stopwatch.Frequency<PrepareBudgetMs;

        private void KeepSprites()
        {
            if(_spritesKept)return;
            _spritesKept=true;
            foreach(var sprite in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(sprite.name==FoundryVisibilityView.WarmUpName)continue;
                _themeSprites.Add(sprite);_foundrySpriteColours.Add(sprite.color);
            }
        }

        private bool IsIceObject(GameObject candidate)=>candidate!=null&&
            (candidate==_iceCrystals||candidate==_icePanels||candidate==_iceDrifts);

        // One owned mesh's foundry colours kept (the first theme to see it keeps them), and the theme's grade of it started
        // on a worker from copies of what it reads.
        private void KeepMesh(MeshFilter filter,PlatformMaterialSet set,ThemeDress dress)
        {
            if(filter==null||IsIceObject(filter.gameObject))return;
            var mesh=filter.sharedMesh;
            if(mesh==null)return;
            if(!_themeMeshIndex.TryGetValue(mesh,out int index))
            {
                if(!_owned.Contains(mesh))return;
                var colours=mesh.colors;
                if(colours==null||colours.Length==0)return;
                var renderer=filter.GetComponent<MeshRenderer>();
                Dress kind=renderer!=null&&renderer.sharedMaterial!=null?DressOf(renderer.sharedMaterial,set):Dress.Slate;
                index=_themeMeshes.Count;
                _themeMeshes.Add(mesh);_themeMeshIndex.Add(mesh,index);_foundryColours.Add(colours);_themeDress.Add(kind);
            }
            while(dress.Colours.Count<_themeMeshes.Count){dress.Colours.Add(null);dress.Grades.Add(null);}
            if(dress.Colours[index]!=null||dress.Grades[index]!=null)return;
            Color[] source=_foundryColours[index];
            Dress meshDress=_themeDress[index];
            ThemePalette palette=dress.Palette;
            ReadGradeInputs(mesh,meshDress,out Vector2[] uvs,out Vector3[] positions,out int[] triangles);
            dress.Grades[index]=Task.Run(()=>Grade(palette,source,meshDress,uvs,positions,triangles));
        }

        private static void ReadGradeInputs(Mesh mesh,Dress dress,out Vector2[] uvs,out Vector3[] positions,out int[] triangles)
        {
            uvs=null;positions=null;triangles=null;
            if(dress==Dress.Bare){uvs=mesh.uv;positions=mesh.vertices;}
            // The cap needs the structure's faces; a mesh with no depth flags is not a structure (UpwardVertices).
            else if(dress!=Dress.Glow&&dress!=Dress.Paving&&mesh.uv2!=null&&mesh.uv2.Length>0)
            {positions=mesh.vertices;triangles=mesh.triangles;}
        }

        private static Color[] GradeOf(ThemePalette palette,Mesh mesh,Color[] source,Dress dress)
        {
            ReadGradeInputs(mesh,dress,out Vector2[] uvs,out Vector3[] positions,out int[] triangles);
            return Grade(palette,source,dress,uvs,positions,triangles);
        }

        // A mesh's themed vertex colours from its foundry ones. Pure: it runs on a worker thread.
        private static Color[] Grade(ThemePalette palette,Color[] source,Dress dress,Vector2[] uvs,Vector3[] positions,int[] triangles)
        {
            var graded=new Color[source.Length];
            if(dress==Dress.Bare)
            {
                // The coping sheet's rail is its only metal: brass there becomes the theme's rail, its seal on the compass
                // seal; every other decal vertex is the kerbs' and grooves' stone.
                for(int v=0;v<source.Length;v++)
                {
                    if(uvs==null||v>=uvs.Length||!OnCopingRail(uvs[v])){graded[v]=TintWith(palette,source[v],true,false);continue;}
                    FoundrySurface.ToDesign(positions[v].x,positions[v].y,out float u,out float w);
                    graded[v]=RailWith(palette,source[v],Vector2.Distance(new Vector2(u,w),CourtSealCentre)<CourtStoneRadius+.1f);
                }
                return graded;
            }
            for(int v=0;v<source.Length;v++)graded[v]=TintWith(palette,source[v],false,dress==Dress.Glow);
            // The cap (rime, moss): the upward faces of raised structures (parapet and wall caps, crenellations, plinths,
            // domes); the walkable floor and decals are not structures and keep none.
            if(positions!=null&&triangles!=null)
            {
                bool[] top=UpwardVertices(positions,triangles);
                for(int v=0;v<graded.Length&&v<top.Length;v++)if(top[v])graded[v]=CapOf(palette,graded[v]);
            }
            return graded;
        }

        // A renderer's block, before its variant is known; false for one the dress leaves alone.
        private static bool PlanBlock(MeshRenderer renderer,PlatformMaterialSet set,ThemePalette palette,out ThemeBlock block)
        {
            block=default;
            if(renderer==null)return false;
            var material=renderer.sharedMaterial;
            if(material==null||!material.HasProperty(ColourId))return false;
            block.Renderer=renderer;
            block.Colour=material.GetColor(ColourId);
            switch(DressOf(material,set))
            {
                case Dress.Paving:
                    Texture2D paving=set.PavingFor(palette.Theme);
                    if(paving!=null)block.Texture=paving;else block.Colour*=palette.Untextured;
                    break;
                case Dress.Masonry:
                    Texture2D masonry=set.MasonryFor(palette.Theme);
                    if(masonry!=null)block.Texture=masonry;else block.Colour*=palette.Untextured;
                    break;
                case Dress.Bare:
                    block.Texture=Texture2D.whiteTexture;
                    break;
                default:
                    // A glowing texture with a variant (fires, the pit, the baked boilers' windows): the variant is
                    // already themed, so no tint; anything else darkens toward the theme's stone (Resolve).
                    Texture main=material.mainTexture;
                    block.Variant=main!=null?main.name:string.Empty;
                    // The distant works borrow the foundry wall's art in their own atmospheric material.
                    block.WallArt=set!=null&&set.MasonryFor(palette.Theme)!=null&&set.FoundryWall!=null&&main==set.FoundryWall.mainTexture;
                    // Their violet atmospheric blend reads magenta against the theme; only a trace of it stays.
                    block.Atmosphere=material.HasProperty(AtmosphereId);
                    break;
            }
            return true;
        }

        private static ThemeBlock Resolve(ThemeBlock block,PlatformMaterialSet set,ThemeDress dress)
        {
            if(block.Variant==null)return block;
            Texture2D variant=block.Variant.Length>0&&dress.Variants.TryGetValue(block.Variant,out var loaded)?loaded:null;
            if(variant!=null)block.Texture=variant;
            else if(block.WallArt)block.Texture=set.MasonryFor(dress.Palette.Theme);
            else block.Colour*=dress.Palette.Untextured;
            block.Variant=null;
            return block;
        }

        private void Bind(ThemeBlock block,ThemePalette palette)
        {
            if(block.Renderer==null)return;
            _themeBlock.Clear();
            if(block.Texture!=null)_themeBlock.SetTexture(TextureId,block.Texture);
            if(block.Atmosphere)_themeBlock.SetFloat(AtmosphereId,palette.Atmosphere);
            _themeBlock.SetColor(ColourId,block.Colour);
            block.Renderer.SetPropertyBlock(_themeBlock);
        }

        // The ice objects built on the change itself, dressed as the coping (their material) with the rest.
        private void BindIceObject(GameObject ice,PlatformMaterialSet set,ThemeDress dress)
        {
            if(ice==null)return;
            var renderer=ice.GetComponent<MeshRenderer>();
            // Their own material is the coping (untextured): bound straight away, without DressOf's name compares.
            if(renderer!=null&&set!=null&&set.Coping!=null&&renderer.sharedMaterial==set.Coping&&set.Coping.HasProperty(ColourId))
            {
                Bind(new ThemeBlock{Renderer=renderer,Texture=Texture2D.whiteTexture,Colour=set.Coping.GetColor(ColourId)},dress.Palette);
                return;
            }
            if(!PlanBlock(renderer,set,dress.Palette,out ThemeBlock block))return;
            if(block.Variant!=null)
            {
                if(block.Variant.Length>0&&!dress.Variants.ContainsKey(block.Variant))
                    dress.Variants.Add(block.Variant,Resources.Load<Texture2D>(dress.Palette.Folder+block.Variant));
                block=Resolve(block,set,dress);
            }
            Bind(block,dress.Palette);
        }

        public void ApplyTheme(FloorTheme theme,PlatformMaterialSet set)
        {
            if(theme==_theme)return;
            ThemePalette palette=PaletteOf(theme);
            // Normally done frames ago (PrepareTheme); otherwise all of it now.
            ThemeDress next=palette!=null?FinishPreparation(theme,set):null;
            ThemeDress leaving=PaletteOf(_theme)!=null&&_dresses.TryGetValue(_theme,out ThemeDress shown)?shown:null;
            bool ice=palette!=null&&palette.IceProps;
            if(_themeBlock==null)_themeBlock=new MaterialPropertyBlock();
            using(PerformanceMarkers.ThemeRetint.Auto())
                for(int i=0;i<_themeMeshes.Count;i++)
                {
                    if(_themeMeshes[i]==null)continue;
                    Color[] colours=_foundryColours[i];
                    if(next!=null)
                    {
                        // A mesh kept after this theme was prepared (by another theme's preparation): graded now.
                        while(next.Colours.Count<=i){next.Colours.Add(null);next.Grades.Add(null);}
                        colours=next.Colours[i];
                        if(colours==null&&next.Grades[i]==null)
                            colours=next.Colours[i]=GradeOf(palette,_themeMeshes[i],_foundryColours[i],_themeDress[i]);
                    }
                    if(colours!=null)_themeMeshes[i].colors=colours;
                }
            // Built before the blocks are set, so the crystals take the coping's untextured dress with the rest.
            using(PerformanceMarkers.ThemeCrystals.Auto())
            {
                if(ice)BuildIceCrystals(set);
                if(_iceCrystals!=null)_iceCrystals.SetActive(ice);
            }
            using(PerformanceMarkers.ThemeProps.Auto())
            {
                if(ice)BuildIceProps(set);
                if(_icePanels!=null)_icePanels.SetActive(ice);
                if(_iceDrifts!=null)_iceDrifts.SetActive(ice);
            }
            using(PerformanceMarkers.ThemeTextures.Auto())
            {
                if(next!=null)
                {
                    // The theme on show unbound first: nothing keeps a block on a variant about to be released.
                    if(leaving!=null)Unbind(leaving);
                    for(int i=0;i<next.Blocks.Count;i++)Bind(next.Blocks[i],palette);
                    if(ice)
                    {
                        BindIceObject(_iceCrystals,set,next);
                        BindIceObject(_icePanels,set,next);
                        BindIceObject(_iceDrifts,set,next);
                    }
                }
                else
                    foreach(var renderer in GetComponentsInChildren<MeshRenderer>(true))renderer.SetPropertyBlock(null);
            }
            using(PerformanceMarkers.ThemeRetint.Auto())
                for(int i=0;i<_themeSprites.Count;i++)
                    if(_themeSprites[i]!=null)_themeSprites[i].color=next!=null?_foundrySpriteColours[i]*palette.Sprite:_foundrySpriteColours[i];
            _theme=theme;
            if(leaving!=null&&leaving!=next)Release(leaving);
        }

        // The blocks a theme bound, cleared: its planned renderers and, on the ice, its own props.
        private void Unbind(ThemeDress dress)
        {
            for(int i=0;i<dress.Blocks.Count;i++)if(dress.Blocks[i].Renderer!=null)dress.Blocks[i].Renderer.SetPropertyBlock(null);
            if(!dress.Palette.IceProps)return;
            foreach(var ice in new[]{_iceCrystals,_icePanels,_iceDrifts})
                if(ice!=null&&ice.TryGetComponent(out MeshRenderer renderer))renderer.SetPropertyBlock(null);
        }

        // A theme left: its variants unloaded (nothing binds them now) and its grades dropped; the foundry's kept colours
        // stay for every theme. Coming back to it prepares it again.
        private void Release(ThemeDress dress)
        {
            if(dress.Coroutine!=null){StopCoroutine(dress.Coroutine);dress.Coroutine=null;}
            foreach(var request in dress.Requests.Values)
                if(request.isDone&&request.asset is Texture2D loading)Resources.UnloadAsset(loading);
            foreach(var variant in dress.Variants.Values)
                if(variant!=null)Resources.UnloadAsset(variant);
            dress.Variants.Clear();dress.Requests.Clear();dress.Blocks.Clear();dress.Colours.Clear();dress.Grades.Clear();
            _dresses.Remove(dress.Palette.Theme);
        }

        // A structure mesh's vertices on upward faces. Walls are extruded straight down the screen, so each wall triangle
        // has an exactly vertical edge; a cap, a crenellation top or a dome facet has none. A vertex is capped only when
        // no wall triangle uses it. Asked only of a structure (ReadGradeInputs: a mesh with depth flags; not floor, decals, below deck).
        private static bool[] UpwardVertices(Vector3[] positions,int[] triangles)
        {
            var top=new bool[positions.Length];var wall=new bool[positions.Length];
            for(int t=0;t+2<triangles.Length;t+=3)
            {
                int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                bool side=Vertical(positions[a],positions[b])||Vertical(positions[b],positions[c])||Vertical(positions[c],positions[a]);
                if(side){wall[a]=wall[b]=wall[c]=true;}else{top[a]=top[b]=top[c]=true;}
            }
            for(int v=0;v<top.Length;v++)top[v]&=!wall[v];
            return top;
        }
        private static bool Vertical(Vector3 a,Vector3 b)=>Mathf.Abs(a.x-b.x)<1e-4f&&Mathf.Abs(a.y-b.y)>.02f;
        private static Color CapOf(ThemePalette p,Color c)=>
            new Color(Mathf.Min(c.r*p.Cap.r,2),Mathf.Min(c.g*p.Cap.g,2),Mathf.Min(c.b*p.Cap.b,2),c.a);

        // A renderer's material, or the structure variant made from it (FoundryVisibilityView.ForStructure).
        private static Dress DressOf(Material material,PlatformMaterialSet set)
        {
            if(set==null)return Dress.Slate;
            if(Is(material,set.FoundryFlagstone))return Dress.Paving;
            if(Is(material,set.FoundryWall)||Is(material,set.Wall))return Dress.Masonry;
            if(Is(material,set.Coping))return Dress.Bare;
            if(Is(material,set.FoundryEmbers))return Dress.Glow;
            return Dress.Slate;
        }
        private static bool Is(Material material,Material source)=>source!=null&&
            (material==source||material.name==source.name+" (scene cutaway)");

        // A vertex colour (a multiplier on the texture, under the shaders' 2x ceiling) on a themed floor. bare: the batch
        // draws untextured, so the colour is the final one (kerb stone; its rail goes through Rail). glow: the embers
        // material (fires, lantern panes, the molten pool), whose warm colours become the theme's light; anywhere else a
        // warm vertex colour is a fire's pool on stone and becomes stone faintly lit, capped so no pool blows out.
        // Foundry: the colour itself.
        public static Color Tint(FloorTheme theme,Color c,bool bare=false,bool glow=false)
        {
            ThemePalette palette=PaletteOf(theme);
            return palette!=null?TintWith(palette,c,bare,glow):c;
        }
        public static Color IceTint(Color c,bool bare=false,bool glow=false)=>TintWith(IcePalette,c,bare,glow);
        public static Color GloamTint(Color c,bool bare=false,bool glow=false)=>TintWith(GloamPalette,c,bare,glow);

        // The one body every theme's vertex colours go through.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Color TintWith(ThemePalette p,Color c,bool bare,bool glow)
        {
            float max=Mathf.Max(c.r,Mathf.Max(c.g,c.b)),min=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
            float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
            Color graded;
            if(bare)graded=p.BareStone*Mathf.Clamp(luminance,.5f,1.2f);
            else
            {
                float warm=Warmth(c,max,min);
                Color stone=p.Stone*Mathf.Min(luminance,1.25f);
                Color lit=!glow?p.LitStone*Mathf.Min(luminance,1.1f)
                    // A light: lava (red, little green) to the theme's glow, a flame or brazier to its light.
                    :max>1.05f?(c.g<c.r*.55f?p.LavaGlow:p.FlameLight)*Mathf.Min(max,1.5f):p.FlameLight*luminance;
                graded=Color.LerpUnclamped(stone,lit,warm);
            }
            graded.r=Mathf.Min(graded.r,2);graded.g=Mathf.Min(graded.g,2);graded.b=Mathf.Min(graded.b,2);
            graded.a=c.a;
            return graded;
        }

        // A rail vertex of the untextured coping batch (its colour is final): the theme's inlay at about 50-55% of its
        // value, a faint edge sheen only where the floor light is strong; on the compass seal the theme's metal to its
        // edge. Ice: mirror-ice #5FA8C8 with an ice-edge #BFE6F2 sheen, silver #A9B4BF on the seal (docs/49). Gloam: a dim
        // sap channel (bark-brown and gloamlight), verdigris #6FA38C to lichen on the seal (docs/51).
        public static Color Rail(FloorTheme theme,Color c,bool seal)
        {
            ThemePalette palette=PaletteOf(theme);
            return palette!=null?RailWith(palette,c,seal):c;
        }
        public static Color IceRail(Color c,bool seal)=>RailWith(IcePalette,c,seal);
        public static Color GloamRail(Color c,bool seal)=>RailWith(GloamPalette,c,seal);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Color RailWith(ThemePalette p,Color c,bool seal)
        {
            float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
            Color graded=seal?Color.Lerp(p.Seal,p.SealEdge,Mathf.Clamp01((luminance-.7f)*1.5f))*Mathf.Clamp(luminance*1.15f,.85f,1.4f)
                :Color.Lerp(p.Rail,p.RailEdge,Mathf.Clamp01((luminance-1f)*1.2f)*.45f)*Mathf.Clamp(luminance*.56f,.45f,.68f);
            graded.a=c.a;
            return graded;
        }

        // FoundryAtmosphereView's mote kinds: a colour read by its own warmth and brightness, or an ember disc the view
        // names (a theme with OwnMotes draws its embers as ash flakes, one in five a lit pollen mote).
        internal const int ByColour=0,AshMote=1,LitMote=2;

        // FoundryAtmosphereView on a themed floor: smoke to the theme's haze, embers to its motes, the pit's glow to its
        // glow. Ice: cold haze, frost motes, quicksilver. Gloam: ash haze, grey ash flakes and gloamlight pollen, the root
        // well's gloamlight. Foundry: the colour itself.
        public static Color Atmosphere(FloorTheme theme,Color c,int mote=ByColour)
        {
            ThemePalette palette=PaletteOf(theme);
            return palette!=null?AtmosphereWith(palette,c,mote):c;
        }
        public static Color ColdAtmosphere(Color c)=>AtmosphereWith(IcePalette,c,ByColour);
        // lit: one of the motes the view draws as pollen; otherwise read by colour (an ember is a grey ash flake).
        public static Color GloamAtmosphere(Color c,bool lit=false)=>AtmosphereWith(GloamPalette,c,lit?LitMote:ByColour);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Color AtmosphereWith(ThemePalette p,Color c,int mote)
        {
            float max=Mathf.Max(c.r,Mathf.Max(c.g,c.b)),min=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
            float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
            float warm=Warmth(c,max,min);
            Color haze=p.Haze*Mathf.Min(luminance*1.9f,1.6f);
            if(!p.OwnMotes)mote=ByColour;
            Color tone=mote==LitMote?p.AtmosphereBright:mote==AshMote?p.AtmosphereMote
                :c.g<c.r*.5f?p.AtmosphereLava:max>1.2f?p.AtmosphereBright:p.AtmosphereMote;
            Color light=tone*Mathf.Clamp(max,.6f,1.8f);
            Color themed=Color.LerpUnclamped(haze,light,warm);
            themed.a=c.a;
            return themed;
        }

        // How warm and saturated: brass, amber, ember, lava near 1; neutral stone and smoke near 0.
        private static float Warmth(Color c,float max,float min)
        {
            if(max<=1e-4f)return 0;
            float saturation=(max-min)/max;
            return Mathf.Clamp01((c.r-c.b)/max*1.6f)*Mathf.Clamp01(saturation*2.2f);
        }

        // The painted backdrop's tint on a themed floor without a plate of its own: Ice deep navy to teal, Gloam sky gloam
        // to dusk, over the plate's own value. Foundry: the colour itself.
        public static Color Backdrop(FloorTheme theme,Color foundry)
        {
            ThemePalette palette=PaletteOf(theme);
            if(palette==null)return foundry;
            Color tint=palette.BackdropTint;
            return new Color(foundry.r*tint.r,foundry.g*tint.g,foundry.b*tint.b,foundry.a);
        }
        public static Color IceBackdrop(Color foundry)=>Backdrop(FloorTheme.Ice,foundry);
        public static Color GloamBackdrop(Color foundry)=>Backdrop(FloorTheme.Gloam,foundry);

        // The hero's contact patch on the floor, against its own (1 on the foundry).
        public static float HeroShadowScale(FloorTheme theme)
        {
            ThemePalette palette=PaletteOf(theme);
            return palette!=null?palette.HeroShadow:1f;
        }

        private static Color Hex(int rgb)=>new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f,1);
    }
}
