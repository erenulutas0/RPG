using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
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
    //   upward faces of structures to its cap (rime, moss) - laid over the mesh's own as an additional vertex stream, so
    //   the foundry's colours never leave the meshes.
    // - the sprites' colour toward the theme's stone.
    // Foundry takes the streams away, restores the sprite colours and clears the blocks. Leaving a theme releases its
    // streams and Resources variants over the next frames, so a run through floors 1-3 never holds two themes' sets.
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
            // Floor 3's own growth (roots, fungi, moss: FoundryRegionGloamGrowth.cs) stands only on this theme.
            public bool GardenProps;
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
            Atmosphere=.1f,BackdropTint=Color.Lerp(BackdropDeep,BackdropTeal,.5f)*2.4f,HeroShadow=1.25f,IceProps=true,GardenProps=false,
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
            Atmosphere=.1f,BackdropTint=Color.Lerp(SkyGloam,SkyDusk,.5f)*2.4f,HeroShadow=1.25f,IceProps=false,GardenProps=true,
        };

        private static ThemePalette PaletteOf(FloorTheme theme)=>
            theme==FloorTheme.Ice?IcePalette:theme==FloorTheme.Gloam?GloamPalette:null;

        // What a renderer's material is to the dress.
        private enum Dress{Slate,Paving,Masonry,Bare,Glow}
        private FloorTheme _theme=FloorTheme.Foundry;
        // Every owned mesh a theme re-colours, index for index: the mesh, its renderer, its dress, and whether its colours
        // are written in place (direct: a mesh some script on its object re-shapes - the banners, flames and gate door
        // motions - or every mesh when ColourStreams is off), with, for those, the foundry's own colours. Every other mesh
        // keeps the foundry's colours for good: a theme lays its own over them as the renderer's additional vertex stream
        // (MeshRenderer.additionalVertexStreams: the same positions, the theme's colours), built while the floor before
        // plays, and the foundry takes the stream away. A change of dress writes no vertex data.
        private readonly List<Mesh> _themeMeshes=new List<Mesh>();
        private readonly Dictionary<Mesh,int> _themeMeshIndex=new Dictionary<Mesh,int>();
        private readonly List<MeshRenderer> _themeRenderers=new List<MeshRenderer>();
        private readonly List<Dress> _themeDress=new List<Dress>();
        private readonly List<bool> _themeDirect=new List<bool>();
        private readonly List<Color[]> _foundryColours=new List<Color[]>();
        private readonly List<SpriteRenderer> _themeSprites=new List<SpriteRenderer>();
        private readonly List<Color> _foundrySpriteColours=new List<Color>();
        private bool _spritesKept;
        private MaterialPropertyBlock _themeBlock;
        // Off: every mesh's colours written in place on the change, as before the streams (a fallback, should a device
        // mishandle additional vertex streams).
        private static readonly bool ColourStreams=true;
        // A theme's stream vertex: the primary mesh's position, unchanged, and the theme's colour, both as the primary
        // stores them (Float32), so the result is exactly the colour written in place would give.
        [StructLayout(LayoutKind.Sequential)]
        private struct StreamVertex{public Vector3 Position;public Color Colour;}
        private static readonly VertexAttributeDescriptor[] StreamLayout=
        {
            new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3),
            new VertexAttributeDescriptor(VertexAttribute.Color,VertexAttributeFormat.Float32,4),
        };
        private const MeshUpdateFlags StreamFlags=MeshUpdateFlags.DontValidateIndices|MeshUpdateFlags.DontRecalculateBounds|
            MeshUpdateFlags.DontNotifyMeshUsers|MeshUpdateFlags.DontResetBoneBounds;

        // A theme's dress worked out ahead of the floor change (PrepareTheme), so ApplyTheme only binds it: each kept mesh
        // read through a read-only snapshot and graded on a worker thread, its stream built, each renderer's block planned,
        // the texture variants loaded asynchronously (their GPU upload with them), and the theme's own props built hidden.
        // The main thread's share runs in slices of about PrepareBudgetMs a frame (Cryptforge.Theme.Prepare). A change that
        // comes first finishes it at once.
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
        // One mesh's grade, as the worker hands it back: the colours read (the foundry's, kept for a direct mesh), and the
        // theme's colours in place (direct) or as stream vertices.
        private sealed class Graded{public Color[] Foundry;public Color[] Colours;public StreamVertex[] Stream;}
        private sealed class PendingGrade{public Task<Graded> Task;public Mesh.MeshDataArray Snapshot;}
        private sealed class ThemeDress
        {
            public ThemePalette Palette;
            public readonly List<ThemeBlock> Blocks=new List<ThemeBlock>();
            // Index for index with _themeMeshes: a direct mesh's colours, a streamed mesh's stream, a grade still running.
            public readonly List<Color[]> Colours=new List<Color[]>();
            public readonly List<Mesh> Streams=new List<Mesh>();
            public readonly List<PendingGrade> Pending=new List<PendingGrade>();
            public readonly Dictionary<string,ResourceRequest> Requests=new Dictionary<string,ResourceRequest>();
            // Resources/<theme>/<name>: the theme's variant of a foundry texture (Tools/ArtReview/GenerateIceGlowTextures.py,
            // GenerateGloamGlowTextures.py), loaded once its floor is ahead; a run that never reaches it never loads one.
            public readonly Dictionary<string,Texture2D> Variants=new Dictionary<string,Texture2D>();
            public IEnumerator Preparation;
            public Coroutine Coroutine;
            public bool Prepared,Draining;
            // Set when the theme is asked for again while this, its left dress, is being released: its textures stay.
            public bool KeepTextures;
            public void Fit(int count)
            {
                while(Colours.Count<count){Colours.Add(null);Streams.Add(null);Pending.Add(null);}
            }
        }
        private readonly Dictionary<FloorTheme,ThemeDress> _dresses=new Dictionary<FloorTheme,ThemeDress>();
        // Left dresses being released over the frames after the change (ReleaseLater), never inside it.
        private readonly List<ThemeDress> _releasing=new List<ThemeDress>();
        private Coroutine _releaseCoroutine;

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
        // its floor is left and the release has run (a few frames after the change).
        public int HeldVariantCount(FloorTheme theme)
        {
            int held=0;
            if(_dresses.TryGetValue(theme,out ThemeDress dress))
                foreach(var variant in dress.Variants.Values)if(variant!=null)held++;
            foreach(var left in _releasing)
                if(left.Palette.Theme==theme)
                    foreach(var variant in left.Variants.Values)if(variant!=null)held++;
            return held;
        }

        // Starts working out the theme's dress over the next frames; nothing on show changes. Foundry is what Build made
        // and needs nothing (its colours never leave the meshes); asking again, or once it is ready, does nothing.
        public void PrepareTheme(FloorTheme theme,PlatformMaterialSet set)
        {
            ThemePalette palette=PaletteOf(theme);
            if(palette==null||_dresses.ContainsKey(theme))return;
            KeepReleasedTextures(theme);
            var dress=new ThemeDress{Palette=palette};
            _dresses.Add(theme,dress);
            dress.Preparation=Prepare(dress,set);
            ResumePreparations();
        }

        // Runs every pending preparation, and the release of a left dress, as coroutines whenever the region is active
        // (asked while inactive, or re-enabled).
        private void ResumePreparations()
        {
            if(!isActiveAndEnabled)return;
            foreach(var dress in _dresses.Values)
                if(dress.Preparation!=null&&!dress.Prepared&&!dress.Draining&&dress.Coroutine==null)
                    dress.Coroutine=StartCoroutine(dress.Preparation);
            if(_releasing.Count>0&&_releaseCoroutine==null)_releaseCoroutine=StartCoroutine(ReleaseLater());
        }
        private void OnEnable()=>ResumePreparations();
        // A deactivated object's coroutines stop; the same iterator goes on where it stood when it is enabled again.
        private void OnDisable()
        {
            foreach(var dress in _dresses.Values)
                if(dress.Coroutine!=null){StopCoroutine(dress.Coroutine);dress.Coroutine=null;}
            if(_releaseCoroutine!=null){StopCoroutine(_releaseCoroutine);_releaseCoroutine=null;}
        }

        // The preparation finished now, in this frame: the rest of what PrepareTheme spreads over frames, a texture still
        // loading forced to complete and a grade still running waited for.
        private ThemeDress FinishPreparation(FloorTheme theme,PlatformMaterialSet set)
        {
            if(!_dresses.TryGetValue(theme,out ThemeDress dress))
            {
                KeepReleasedTextures(theme);
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
            Task<PropGeometry[]> geometry;
            using(PerformanceMarkers.ThemePrepare.Auto())
            {
                filters=GetComponentsInChildren<MeshFilter>(true);
                KeepSprites();
                geometry=StartPropGeometry(dress.Palette);
            }
            yield return null;
            // Each kept mesh's snapshot taken and its grade handed to a worker, a slice a frame.
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
                        // A theme's own props have their own dress (BindIceObject, BindGrowth) and are never another theme's.
                        if(renderers[i]==null||IsThemeObject(renderers[i].gameObject))continue;
                        if(!PlanBlock(renderers[i],set,dress.Palette,out ThemeBlock block))continue;
                        if(!string.IsNullOrEmpty(block.Variant)&&!dress.Variants.ContainsKey(block.Variant)&&!dress.Requests.ContainsKey(block.Variant))
                            dress.Requests.Add(block.Variant,Resources.LoadAsync<Texture2D>(dress.Palette.Folder+block.Variant));
                        dress.Blocks.Add(block);
                    }
                }
                if(i<renderers.Length)yield return null;
            }
            // The garden's growth atlas, loaded as one of its variants (released with them).
            if(dress.Palette.GardenProps&&!dress.Variants.ContainsKey(GloamGrowthAtlas)&&!dress.Requests.ContainsKey(GloamGrowthAtlas))
                dress.Requests.Add(GloamGrowthAtlas,Resources.LoadAsync<Texture2D>(dress.Palette.Folder+GloamGrowthAtlas));
            // The loads finish in the background; a change that cannot wait takes them now.
            foreach(var pair in dress.Requests)
            {
                while(!dress.Draining&&!pair.Value.isDone)yield return null;
                using(PerformanceMarkers.ThemePrepare.Auto())dress.Variants[pair.Key]=pair.Value.asset as Texture2D;
            }
            dress.Requests.Clear();
            using(PerformanceMarkers.ThemePrepare.Auto())
                for(int i=0;i<dress.Blocks.Count;i++)dress.Blocks[i]=Resolve(dress.Blocks[i],set,dress);
            // The grades as the workers finish them: a direct mesh's colours kept, a streamed mesh's stream built (its GPU
            // upload with it), a slice a frame.
            for(int at=0;at<dress.Pending.Count;)
            {
                using(PerformanceMarkers.ThemePrepare.Auto())
                {
                    long slice=Stopwatch.GetTimestamp();
                    while(at<dress.Pending.Count&&(dress.Draining||Within(slice)))
                    {
                        PendingGrade pending=dress.Pending[at];
                        if(pending!=null&&!dress.Draining&&!pending.Task.IsCompleted)break;
                        if(pending!=null)Consume(dress,at);
                        at++;
                    }
                }
                if(at<dress.Pending.Count)yield return null;
            }
            // The theme's own props, from the geometry the worker made: built hidden, so the change only shows them.
            if(geometry!=null)
            {
                while(!dress.Draining&&!geometry.IsCompleted)yield return null;
                PropGeometry[] made=null;
                try{made=geometry.Result;}
                catch(System.Exception){made=null;}
                if(made!=null)
                {
                    using(PerformanceMarkers.ThemePrepare.Auto())MakeProp(dress,set,made,0);
                    if(!dress.Draining)yield return null;
                    using(PerformanceMarkers.ThemePrepare.Auto())MakeProp(dress,set,made,1);
                }
            }
            dress.Prepared=true;
            dress.Coroutine=null;
        }

        private static bool Within(long since)=>(Stopwatch.GetTimestamp()-since)*1000d/Stopwatch.Frequency<PrepareBudgetMs;

        // A theme's own geometry, worked out on a worker (FoundryRegionIceCrystals, FoundryRegionIceProps,
        // FoundryRegionGloamGrowth); null when the theme has none or it already stands.
        private Task<PropGeometry[]> StartPropGeometry(ThemePalette palette)
        {
            if(palette.GardenProps)
            {
                if(_gloamGrowth!=null)return null;
                var garden=new List<Vector2Int>(_cells);
                return Task.Run(()=>new[]{GloamGrowthGeometry(garden)});
            }
            if(!palette.IceProps||(_iceCrystals!=null&&_icePanels!=null&&_iceDrifts!=null))return null;
            var cells=new List<Vector2Int>(_cells);
            bool crystals=_iceCrystals==null,panels=_icePanels==null,drifts=_iceDrifts==null;
            return Task.Run(()=>new[]{crystals?IceCrystalGeometry(cells):null,panels?IcePanelGeometry():null,drifts?IceDriftGeometry():null});
        }

        // Its objects from that geometry, hidden unless the theme is on show, and dressed once: the ice's as the coping
        // (their own untextured material), the garden's with its atlas. Step 0 the crystals or the growth, step 1 the
        // panels and drifts.
        private void MakeProp(ThemeDress dress,PlatformMaterialSet set,PropGeometry[] made,int step)
        {
            bool shown=_theme==dress.Palette.Theme;
            if(_themeBlock==null)_themeBlock=new MaterialPropertyBlock();
            if(dress.Palette.GardenProps)
            {
                if(step!=0||_gloamGrowth!=null||made[0]==null)return;
                MakeGloamGrowth(made[0],set);
                if(_gloamGrowth==null)return;
                _gloamGrowth.SetActive(shown);
                BindGrowth(set,dress);
                return;
            }
            if(!dress.Palette.IceProps)return;
            if(step==0)
            {
                if(_iceCrystals!=null||made[0]==null)return;
                MakeIceCrystals(made[0],set);
                if(_iceCrystals==null)return;
                _iceCrystals.SetActive(shown);
                BindIceObject(_iceCrystals,set,dress);
                return;
            }
            bool panels=_icePanels==null,drifts=_iceDrifts==null;
            MakeIceProps(made[1],made[2],set);
            if(panels&&_icePanels!=null){_icePanels.SetActive(shown);BindIceObject(_icePanels,set,dress);}
            if(drifts&&_iceDrifts!=null){_iceDrifts.SetActive(shown);BindIceObject(_iceDrifts,set,dress);}
        }

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

        private bool IsThemeObject(GameObject candidate)=>candidate!=null&&
            (candidate==_iceCrystals||candidate==_icePanels||candidate==_iceDrifts||candidate==_gloamGrowth);

        // The growth dressed with the garden's atlas on its block (the coping material's colour kept); the atlas loaded
        // now if the preparation never reached it.
        private void BindGrowth(PlatformMaterialSet set,ThemeDress dress)
        {
            if(_gloamGrowth==null||set==null||set.Coping==null||!_gloamGrowth.TryGetComponent(out MeshRenderer renderer))return;
            if(!dress.Variants.TryGetValue(GloamGrowthAtlas,out Texture2D atlas)||atlas==null)
                dress.Variants[GloamGrowthAtlas]=atlas=Resources.Load<Texture2D>(dress.Palette.Folder+GloamGrowthAtlas);
            if(_themeBlock==null)_themeBlock=new MaterialPropertyBlock();
            Color colour=set.Coping.HasProperty(ColourId)?set.Coping.GetColor(ColourId):Color.white;
            Bind(new ThemeBlock{Renderer=renderer,Texture=atlas!=null?atlas:Texture2D.whiteTexture,Colour=colour},dress.Palette);
        }

        // One owned mesh kept (the first theme to see it), and the theme's grade of it started on a worker from a
        // read-only snapshot of the mesh: no array is copied on the main thread.
        private void KeepMesh(MeshFilter filter,PlatformMaterialSet set,ThemeDress dress)
        {
            if(filter==null||IsThemeObject(filter.gameObject))return;
            var mesh=filter.sharedMesh;
            if(mesh==null)return;
            if(!_themeMeshIndex.TryGetValue(mesh,out int index))
            {
                if(!_owned.Contains(mesh)||mesh.vertexCount==0||!mesh.HasVertexAttribute(VertexAttribute.Color))return;
                var renderer=filter.GetComponent<MeshRenderer>();
                Dress kind=renderer!=null&&renderer.sharedMaterial!=null?DressOf(renderer.sharedMaterial,set):Dress.Slate;
                // A script on the object (a motion) re-shapes the mesh: a stream's positions would freeze it.
                bool direct=!ColourStreams||renderer==null||filter.GetComponent<MonoBehaviour>()!=null;
                index=_themeMeshes.Count;
                _themeMeshes.Add(mesh);_themeMeshIndex.Add(mesh,index);_themeRenderers.Add(renderer);_themeDress.Add(kind);
                _themeDirect.Add(direct);_foundryColours.Add(null);
            }
            dress.Fit(_themeMeshes.Count);
            if(dress.Colours[index]!=null||dress.Streams[index]!=null||dress.Pending[index]!=null)return;
            dress.Pending[index]=StartGrade(dress.Palette,index);
        }

        private PendingGrade StartGrade(ThemePalette palette,int index)
        {
            Mesh.MeshDataArray snapshot=Mesh.AcquireReadOnlyMeshData(_themeMeshes[index]);
            Mesh.MeshData data=snapshot[0];
            Color[] foundry=_foundryColours[index];
            Dress kind=_themeDress[index];
            bool direct=_themeDirect[index];
            return new PendingGrade{Snapshot=snapshot,Task=Task.Run(()=>GradeSnapshot(palette,data,foundry,kind,direct))};
        }

        // A finished grade taken in: the snapshot released, a direct mesh's colours (and the foundry's) kept, a streamed
        // mesh's stream built. A failed worker's grade is done here.
        private void Consume(ThemeDress dress,int index)
        {
            PendingGrade pending=dress.Pending[index];
            dress.Pending[index]=null;
            Graded graded=null;
            try{graded=pending.Task.Result;}
            catch(System.Exception){graded=null;}
            pending.Snapshot.Dispose();
            if(graded==null)graded=GradeNow(dress.Palette,index);
            if(graded==null)return;
            Take(dress,index,graded);
        }

        private void Take(ThemeDress dress,int index,Graded graded)
        {
            if(graded.Foundry!=null&&_foundryColours[index]==null)_foundryColours[index]=graded.Foundry;
            if(graded.Colours!=null)dress.Colours[index]=graded.Colours;
            if(graded.Stream!=null&&_themeMeshes[index]!=null)dress.Streams[index]=MakeStream(_themeMeshes[index],graded.Stream);
        }

        // The same grade on this thread (a worker failed, or a mesh no preparation reached).
        private Graded GradeNow(ThemePalette palette,int index)
        {
            if(_themeMeshes[index]==null)return null;
            Mesh.MeshDataArray snapshot=Mesh.AcquireReadOnlyMeshData(_themeMeshes[index]);
            try{return GradeSnapshot(palette,snapshot[0],_foundryColours[index],_themeDress[index],_themeDirect[index]);}
            finally{snapshot.Dispose();}
        }

        // A mesh's themed colours from a snapshot of it. Pure (it reads only the snapshot): it runs on a worker thread.
        // foundry: the foundry's colours when already kept (a direct mesh may show a theme's by now), else read here.
        private static Graded GradeSnapshot(ThemePalette palette,Mesh.MeshData data,Color[] foundry,Dress kind,bool direct)
        {
            int count=data.vertexCount;
            Color[] source=foundry;
            if(source==null)
            {
                var colours=new NativeArray<Color>(count,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
                try{data.GetColors(colours);source=colours.ToArray();}
                finally{colours.Dispose();}
            }
            // The cap needs the structure's faces; a mesh with no depth flags is not a structure (UpwardVertices).
            bool cap=kind!=Dress.Glow&&kind!=Dress.Paving&&kind!=Dress.Bare&&data.HasVertexAttribute(VertexAttribute.TexCoord1);
            Vector3[] positions=kind==Dress.Bare||cap||!direct?Positions(data):null;
            Vector2[] uvs=kind==Dress.Bare?Uvs(data):null;
            int[] triangles=cap?Triangles(data):null;
            Color[] graded=Grade(palette,source,kind,uvs,positions,triangles);
            var result=new Graded{Foundry=direct?source:null};
            if(direct)result.Colours=graded;
            else
            {
                var stream=new StreamVertex[count];
                for(int v=0;v<count;v++){stream[v].Position=positions[v];stream[v].Colour=graded[v];}
                result.Stream=stream;
            }
            return result;
        }

        private static Vector3[] Positions(Mesh.MeshData data)
        {
            var native=new NativeArray<Vector3>(data.vertexCount,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
            try{data.GetVertices(native);return native.ToArray();}
            finally{native.Dispose();}
        }
        private static Vector2[] Uvs(Mesh.MeshData data)
        {
            if(!data.HasVertexAttribute(VertexAttribute.TexCoord0))return new Vector2[0];
            var native=new NativeArray<Vector2>(data.vertexCount,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
            try{data.GetUVs(0,native);return native.ToArray();}
            finally{native.Dispose();}
        }
        // Every sub-mesh's indices in order, the base vertex applied (Mesh.triangles).
        private static int[] Triangles(Mesh.MeshData data)
        {
            int total=0;
            for(int s=0;s<data.subMeshCount;s++)total+=data.GetSubMesh(s).indexCount;
            var triangles=new int[total];
            int at=0;
            for(int s=0;s<data.subMeshCount;s++)
            {
                int length=data.GetSubMesh(s).indexCount;
                if(length==0)continue;
                var native=new NativeArray<int>(length,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
                try{data.GetIndices(native,s,true);NativeArray<int>.Copy(native,0,triangles,at,length);}
                finally{native.Dispose();}
                at+=length;
            }
            return triangles;
        }

        // A theme's colours as an additional vertex stream of the primary mesh: its positions and the theme's colours, no
        // indices; uploaded once and dropped from the CPU.
        private Mesh MakeStream(Mesh primary,StreamVertex[] vertices)
        {
            var stream=new Mesh{name=primary.name+" (theme colours)"};
            stream.SetVertexBufferParams(vertices.Length,StreamLayout);
            stream.SetVertexBufferData(vertices,0,0,vertices.Length,0,StreamFlags);
            stream.bounds=primary.bounds;
            stream.UploadMeshData(true);
            _owned.Add(stream);
            return stream;
        }

        // A mesh's colours or stream for a theme, graded now when no preparation reached it (a mesh kept after the theme
        // was prepared, by another theme's preparation).
        private Color[] ColoursOf(ThemeDress dress,int index)
        {
            dress.Fit(index+1);
            if(dress.Pending[index]!=null)Consume(dress,index);
            if(dress.Colours[index]==null){Graded graded=GradeNow(dress.Palette,index);if(graded!=null)Take(dress,index,graded);}
            return dress.Colours[index];
        }
        private Mesh StreamOf(ThemeDress dress,int index)
        {
            dress.Fit(index+1);
            if(dress.Pending[index]!=null)Consume(dress,index);
            if(dress.Streams[index]==null){Graded graded=GradeNow(dress.Palette,index);if(graded!=null)Take(dress,index,graded);}
            return dress.Streams[index];
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

        // An ice object dressed as the coping (its material) with the rest; its block stays when it is hidden.
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

        // The change itself: only binds what the preparation made. Streams swapped on each renderer (or a direct mesh's
        // colours written), the theme's props shown or hidden, the blocks bound or cleared, the sprites tinted. The left
        // dress is released over the next frames (ReleaseLater). Back to the foundry nothing is prepared: its colours never
        // left the meshes, and its blocks are none.
        public void ApplyTheme(FloorTheme theme,PlatformMaterialSet set)
        {
            if(theme==_theme)return;
            ThemePalette palette=PaletteOf(theme);
            // Normally done frames ago (PrepareTheme); otherwise all of it now.
            ThemeDress next=palette!=null?FinishPreparation(theme,set):null;
            ThemeDress leaving=PaletteOf(_theme)!=null&&_dresses.TryGetValue(_theme,out ThemeDress shown)?shown:null;
            bool ice=palette!=null&&palette.IceProps,garden=palette!=null&&palette.GardenProps;
            if(_themeBlock==null)_themeBlock=new MaterialPropertyBlock();
            using(PerformanceMarkers.ThemeRetint.Auto())
                for(int i=0;i<_themeMeshes.Count;i++)
                {
                    if(_themeMeshes[i]==null)continue;
                    if(_themeDirect[i])
                    {
                        Color[] colours=next!=null?ColoursOf(next,i):_foundryColours[i];
                        if(colours!=null)_themeMeshes[i].SetColors(colours);
                    }
                    else if(_themeRenderers[i]!=null)
                        _themeRenderers[i].additionalVertexStreams=next!=null?StreamOf(next,i):null;
                }
            // Built during the preparation; only when it never ran are they built here.
            using(PerformanceMarkers.ThemeCrystals.Auto())
            {
                if(ice&&_iceCrystals==null){BuildIceCrystals(set);BindIceObject(_iceCrystals,set,next);}
                if(_iceCrystals!=null&&_iceCrystals.activeSelf!=ice)_iceCrystals.SetActive(ice);
                // The garden's growth on the crystals' slots: its atlas bound from this dress (a dress prepared again
                // after a release loads its own), cleared as the garden is left so nothing holds a released atlas.
                if(garden)
                {
                    if(_gloamGrowth==null)BuildGloamGrowth(set);
                    BindGrowth(set,next);
                }
                else if(_gloamGrowth!=null&&_gloamGrowth.TryGetComponent(out MeshRenderer growth))growth.SetPropertyBlock(null);
                if(_gloamGrowth!=null&&_gloamGrowth.activeSelf!=garden)_gloamGrowth.SetActive(garden);
            }
            using(PerformanceMarkers.ThemeProps.Auto())
            {
                if(ice&&(_icePanels==null||_iceDrifts==null))
                {
                    BuildIceProps(set);
                    BindIceObject(_icePanels,set,next);
                    BindIceObject(_iceDrifts,set,next);
                }
                if(_icePanels!=null&&_icePanels.activeSelf!=ice)_icePanels.SetActive(ice);
                if(_iceDrifts!=null&&_iceDrifts.activeSelf!=ice)_iceDrifts.SetActive(ice);
            }
            using(PerformanceMarkers.ThemeTextures.Auto())
            {
                if(next!=null)
                {
                    // The same renderers as the dress on show: each block overwritten, no ice variant left bound.
                    for(int i=0;i<next.Blocks.Count;i++)Bind(next.Blocks[i],palette);
                }
                else if(leaving!=null)
                {
                    for(int i=0;i<leaving.Blocks.Count;i++)
                        if(leaving.Blocks[i].Renderer!=null)leaving.Blocks[i].Renderer.SetPropertyBlock(null);
                }
                else
                    foreach(var renderer in GetComponentsInChildren<MeshRenderer>())renderer.SetPropertyBlock(null);
            }
            using(PerformanceMarkers.ThemeRetint.Auto())
                for(int i=0;i<_themeSprites.Count;i++)
                    if(_themeSprites[i]!=null)_themeSprites[i].color=next!=null?_foundrySpriteColours[i]*palette.Sprite:_foundrySpriteColours[i];
            _theme=theme;
            if(leaving!=null&&leaving!=next)ScheduleRelease(leaving);
        }

        // A theme left: out of the dresses at once (coming back to it prepares it again), its streams destroyed and its
        // variants unloaded over the next frames, a slice a frame (Cryptforge.Theme.Release), nothing of it in the change.
        private void ScheduleRelease(ThemeDress dress)
        {
            if(dress.Coroutine!=null){StopCoroutine(dress.Coroutine);dress.Coroutine=null;}
            _dresses.Remove(dress.Palette.Theme);
            _releasing.Add(dress);
            if(isActiveAndEnabled){if(_releaseCoroutine==null)_releaseCoroutine=StartCoroutine(ReleaseLater());}
            else while(_releasing.Count>0)ReleaseStep(_releasing[0],double.MaxValue);
        }

        private IEnumerator ReleaseLater()
        {
            // The change's own frame is the floor's.
            yield return null;
            while(_releasing.Count>0)
            {
                using(PerformanceMarkers.ThemeRelease.Auto())ReleaseStep(_releasing[0],PrepareBudgetMs);
                if(_releasing.Count>0)yield return null;
            }
            _releaseCoroutine=null;
        }

        // Some of a left dress's release, within the budget; the dress leaves the list once nothing of it is held.
        private void ReleaseStep(ThemeDress dress,double budgetMs)
        {
            long slice=Stopwatch.GetTimestamp();
            bool Left()=>(Stopwatch.GetTimestamp()-slice)*1000d/Stopwatch.Frequency<budgetMs;
            for(int i=0;i<dress.Pending.Count;i++)
                if(dress.Pending[i]!=null)
                {
                    try{dress.Pending[i].Task.Wait();}catch(System.Exception){}
                    dress.Pending[i].Snapshot.Dispose();
                    dress.Pending[i]=null;
                }
            for(int i=0;i<dress.Streams.Count;i++)
            {
                if(dress.Streams[i]==null)continue;
                if(!Left())return;
                _owned.Remove(dress.Streams[i]);
                Destroy(dress.Streams[i]);
                dress.Streams[i]=null;
            }
            var names=new List<string>(dress.Variants.Keys);
            foreach(string name in names)
            {
                if(!Left())return;
                Texture2D variant=dress.Variants[name];
                dress.Variants.Remove(name);
                // Nothing binds it any more (ApplyTheme overwrote or cleared every block of the dress); unless its theme
                // is wanted again meanwhile, it goes.
                if(variant!=null&&!dress.KeepTextures)Resources.UnloadAsset(variant);
            }
            dress.Requests.Clear();dress.Blocks.Clear();dress.Colours.Clear();dress.Streams.Clear();dress.Pending.Clear();
            _releasing.Remove(dress);
        }

        // A theme asked for again while its left dress is being released: its textures are not unloaded (a new load
        // returns the same objects).
        private void KeepReleasedTextures(FloorTheme theme)
        {
            foreach(var left in _releasing)if(left.Palette.Theme==theme)left.KeepTextures=true;
        }

        // Every snapshot still read by a worker released as the region goes (the workers waited for).
        private void DropThemeSnapshots()
        {
            foreach(var dress in _dresses.Values)
                for(int i=0;i<dress.Pending.Count;i++)
                    if(dress.Pending[i]!=null)
                    {
                        try{dress.Pending[i].Task.Wait();}catch(System.Exception){}
                        dress.Pending[i].Snapshot.Dispose();
                        dress.Pending[i]=null;
                    }
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
