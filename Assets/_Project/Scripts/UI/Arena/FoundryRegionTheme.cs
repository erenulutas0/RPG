using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        private static readonly int ColourId=Shader.PropertyToID("_Color"),TextureId=Shader.PropertyToID("_MainTex"),
            AtmosphereId=Shader.PropertyToID("_Atmosphere");
        private const float IceAtmosphere=.1f;
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
        private MaterialPropertyBlock _themeBlock;

        // The ice dress worked out ahead of the floor change (PrepareTheme), so ApplyTheme only binds it: the foundry's
        // vertex colours kept and their ice grades computed on a worker thread, each renderer's block planned, the cold
        // texture variants loaded asynchronously (their GPU upload with them). The main thread's share runs in slices of
        // about PrepareBudgetMs a frame (Cryptforge.Theme.Prepare). A change that comes first finishes it at once.
        private const double PrepareBudgetMs=1;
        private struct IceBlock
        {
            public MeshRenderer Renderer;
            public Texture Texture;
            public Color Colour;
            public bool Atmosphere;
            // A Resources/Ice variant still to resolve (null when none is asked for), and the wall-art fallback.
            public string Variant;
            public bool WallArt;
        }
        private readonly List<IceBlock> _iceBlocks=new List<IceBlock>();
        private readonly List<Task<Color[]>> _iceGrades=new List<Task<Color[]>>();
        private readonly Dictionary<string,ResourceRequest> _iceRequests=new Dictionary<string,ResourceRequest>();
        private IEnumerator _icePreparation;
        private Coroutine _iceCoroutine;
        private bool _icePrepared,_iceDraining;

        public FloorTheme Theme=>_theme;
        public bool IsThemePrepared(FloorTheme theme)=>theme!=FloorTheme.Ice||_icePrepared;
        public bool IsPreparingTheme=>_icePreparation!=null&&!_icePrepared;

        // Starts working out the theme's dress over the next frames; nothing on show changes. Ice only (Foundry is what
        // Build made); asking again, or once it is ready, does nothing.
        public void PrepareTheme(FloorTheme theme,PlatformMaterialSet set)
        {
            if(theme!=FloorTheme.Ice||_icePrepared||_icePreparation!=null)return;
            _icePreparation=PrepareIce(set);
            ResumeIcePreparation();
        }

        // Runs a pending preparation as a coroutine whenever the region is active (asked while inactive, or re-enabled).
        private void ResumeIcePreparation()
        {
            if(_icePreparation!=null&&!_icePrepared&&_iceCoroutine==null&&isActiveAndEnabled)
                _iceCoroutine=StartCoroutine(_icePreparation);
        }
        private void OnEnable()=>ResumeIcePreparation();
        // A deactivated object's coroutines stop; the same iterator goes on where it stood when it is enabled again.
        private void OnDisable()
        {
            if(_iceCoroutine!=null){StopCoroutine(_iceCoroutine);_iceCoroutine=null;}
        }

        // The preparation finished now, in this frame: the rest of what PrepareTheme spreads over frames, a texture still
        // loading forced to complete and a grade still running waited for.
        private void FinishIcePreparation(PlatformMaterialSet set)
        {
            if(_icePrepared)return;
            if(_icePreparation==null)_icePreparation=PrepareIce(set);
            if(_iceCoroutine!=null){StopCoroutine(_iceCoroutine);_iceCoroutine=null;}
            _iceDraining=true;
            try{while(!_icePrepared&&_icePreparation.MoveNext()){}}
            finally{_iceDraining=false;}
        }

        private IEnumerator PrepareIce(PlatformMaterialSet set)
        {
            MeshFilter[] filters;
            using(PerformanceMarkers.ThemePrepare.Auto())
            {
                filters=GetComponentsInChildren<MeshFilter>(true);
                foreach(var sprite in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if(sprite.name==FoundryVisibilityView.WarmUpName)continue;
                    _themeSprites.Add(sprite);_foundrySpriteColours.Add(sprite.color);
                }
            }
            yield return null;
            // The foundry's colours kept and each mesh's grade handed to a worker, a slice a frame.
            for(int i=0;i<filters.Length;)
            {
                using(PerformanceMarkers.ThemePrepare.Auto())
                {
                    long slice=Stopwatch.GetTimestamp();
                    for(;i<filters.Length&&Within(slice);i++)KeepMesh(filters[i],set);
                }
                if(i<filters.Length)yield return null;
            }
            yield return null;
            // Every renderer's block planned and the cold variants asked for.
            MeshRenderer[] renderers;
            using(PerformanceMarkers.ThemePrepare.Auto())renderers=GetComponentsInChildren<MeshRenderer>(true);
            for(int i=0;i<renderers.Length;)
            {
                using(PerformanceMarkers.ThemePrepare.Auto())
                {
                    long slice=Stopwatch.GetTimestamp();
                    for(;i<renderers.Length&&Within(slice);i++)
                    {
                        if(!PlanBlock(renderers[i],set,out IceBlock block))continue;
                        if(!string.IsNullOrEmpty(block.Variant)&&!_iceVariants.ContainsKey(block.Variant)&&!_iceRequests.ContainsKey(block.Variant))
                            _iceRequests.Add(block.Variant,Resources.LoadAsync<Texture2D>("Ice/"+block.Variant));
                        _iceBlocks.Add(block);
                    }
                }
                if(i<renderers.Length)yield return null;
            }
            // The loads finish in the background; a change that cannot wait takes them now.
            foreach(var pair in _iceRequests)
            {
                while(!_iceDraining&&!pair.Value.isDone)yield return null;
                using(PerformanceMarkers.ThemePrepare.Auto())_iceVariants[pair.Key]=pair.Value.asset as Texture2D;
            }
            _iceRequests.Clear();
            using(PerformanceMarkers.ThemePrepare.Auto())
                for(int i=0;i<_iceBlocks.Count;i++)_iceBlocks[i]=Resolve(_iceBlocks[i],set);
            // The grades: the worker's result, or the same grade here if the worker failed.
            for(int i=0;i<_iceGrades.Count;i++)
            {
                Task<Color[]> grade=_iceGrades[i];
                if(grade==null)continue;
                while(!_iceDraining&&!grade.IsCompleted)yield return null;
                using(PerformanceMarkers.ThemePrepare.Auto())
                {
                    Color[] graded=null;
                    try{graded=grade.Result;}
                    catch(System.Exception){graded=null;}
                    if(graded==null&&_themeMeshes[i]!=null)graded=GradeOf(_themeMeshes[i],_foundryColours[i],_themeDress[i]);
                    _iceColours[i]=graded;
                }
            }
            _iceGrades.Clear();
            _icePrepared=true;
            _iceCoroutine=null;
        }

        private static bool Within(long since)=>(Stopwatch.GetTimestamp()-since)*1000d/Stopwatch.Frequency<PrepareBudgetMs;

        // One owned mesh's foundry colours kept, and its ice grade started on a worker from copies of what it reads.
        private void KeepMesh(MeshFilter filter,PlatformMaterialSet set)
        {
            if(filter==null)return;
            var mesh=filter.sharedMesh;
            if(mesh==null||!_owned.Contains(mesh)||_themeMeshes.Contains(mesh))return;
            var colours=mesh.colors;
            if(colours==null||colours.Length==0)return;
            var renderer=filter.GetComponent<MeshRenderer>();
            IceDress dress=renderer!=null&&renderer.sharedMaterial!=null?DressOf(renderer.sharedMaterial,set):IceDress.Slate;
            _themeMeshes.Add(mesh);_foundryColours.Add(colours);_iceColours.Add(null);_themeDress.Add(dress);
            ReadGradeInputs(mesh,dress,out Vector2[] uvs,out Vector3[] positions,out int[] triangles);
            _iceGrades.Add(Task.Run(()=>Grade(colours,dress,uvs,positions,triangles)));
        }

        private static void ReadGradeInputs(Mesh mesh,IceDress dress,out Vector2[] uvs,out Vector3[] positions,out int[] triangles)
        {
            uvs=null;positions=null;triangles=null;
            if(dress==IceDress.Bare){uvs=mesh.uv;positions=mesh.vertices;}
            // Rime needs the structure's faces; a mesh with no depth flags is not a structure (UpwardVertices).
            else if(dress!=IceDress.Glow&&dress!=IceDress.Paving&&mesh.uv2!=null&&mesh.uv2.Length>0)
            {positions=mesh.vertices;triangles=mesh.triangles;}
        }

        private static Color[] GradeOf(Mesh mesh,Color[] source,IceDress dress)
        {
            ReadGradeInputs(mesh,dress,out Vector2[] uvs,out Vector3[] positions,out int[] triangles);
            return Grade(source,dress,uvs,positions,triangles);
        }

        // A mesh's ice vertex colours from its foundry ones. Pure: it runs on a worker thread.
        private static Color[] Grade(Color[] source,IceDress dress,Vector2[] uvs,Vector3[] positions,int[] triangles)
        {
            var graded=new Color[source.Length];
            if(dress==IceDress.Bare)
            {
                // The coping sheet's rail is its only metal: brass there becomes mirror-ice, silver on the compass seal;
                // every other decal vertex is the kerbs' and grooves' stone.
                for(int v=0;v<source.Length;v++)
                {
                    if(uvs==null||v>=uvs.Length||!OnCopingRail(uvs[v])){graded[v]=IceTint(source[v],true);continue;}
                    FoundrySurface.ToDesign(positions[v].x,positions[v].y,out float u,out float w);
                    graded[v]=IceRail(source[v],Vector2.Distance(new Vector2(u,w),CourtSealCentre)<CourtStoneRadius+.1f);
                }
                return graded;
            }
            for(int v=0;v<source.Length;v++)graded[v]=IceTint(source[v],false,dress==IceDress.Glow);
            // Rime: the upward faces of raised structures (parapet and wall caps, crenellations, plinths, domes) go
            // toward frost; the walkable floor and decals are not structures and keep none.
            if(positions!=null&&triangles!=null)
            {
                bool[] top=UpwardVertices(positions,triangles);
                for(int v=0;v<graded.Length&&v<top.Length;v++)if(top[v])graded[v]=Rime(graded[v]);
            }
            return graded;
        }

        // A renderer's ice block, before its variant is known; false for one the dress leaves alone.
        private bool PlanBlock(MeshRenderer renderer,PlatformMaterialSet set,out IceBlock block)
        {
            block=default;
            if(renderer==null)return false;
            var material=renderer.sharedMaterial;
            if(material==null||!material.HasProperty(ColourId))return false;
            block.Renderer=renderer;
            block.Colour=material.GetColor(ColourId);
            switch(DressOf(material,set))
            {
                case IceDress.Paving:
                    if(set.IcePaving!=null)block.Texture=set.IcePaving;else block.Colour*=IceSlate;
                    break;
                case IceDress.Masonry:
                    if(set.IceMasonry!=null)block.Texture=set.IceMasonry;else block.Colour*=IceSlate;
                    break;
                case IceDress.Bare:
                    block.Texture=Texture2D.whiteTexture;
                    break;
                default:
                    // A glowing texture with an ice variant (fires, the pit, the baked boilers' windows): the variant is
                    // already cold, so no tint; anything else darkens toward slate (Resolve).
                    Texture main=material.mainTexture;
                    block.Variant=main!=null?main.name:string.Empty;
                    // The distant works borrow the foundry wall's art in their own atmospheric material.
                    block.WallArt=set!=null&&set.IceMasonry!=null&&set.FoundryWall!=null&&main==set.FoundryWall.mainTexture;
                    // Their violet atmospheric blend read magenta against the ice; only a trace of it stays.
                    block.Atmosphere=material.HasProperty(AtmosphereId);
                    break;
            }
            return true;
        }

        private IceBlock Resolve(IceBlock block,PlatformMaterialSet set)
        {
            if(block.Variant==null)return block;
            Texture2D variant=block.Variant.Length>0&&_iceVariants.TryGetValue(block.Variant,out var loaded)?loaded:null;
            if(variant!=null)block.Texture=variant;
            else if(block.WallArt)block.Texture=set.IceMasonry;
            else block.Colour*=IceSlate;
            block.Variant=null;
            return block;
        }

        private void Bind(IceBlock block)
        {
            if(block.Renderer==null)return;
            _themeBlock.Clear();
            if(block.Texture!=null)_themeBlock.SetTexture(TextureId,block.Texture);
            if(block.Atmosphere)_themeBlock.SetFloat(AtmosphereId,IceAtmosphere);
            _themeBlock.SetColor(ColourId,block.Colour);
            block.Renderer.SetPropertyBlock(_themeBlock);
        }

        // The ice objects built on the change itself, dressed as the coping (their material) with the rest.
        private void BindIceObject(GameObject ice,PlatformMaterialSet set)
        {
            if(ice==null)return;
            var renderer=ice.GetComponent<MeshRenderer>();
            // Their own material is the coping (untextured): bound straight away, without DressOf's name compares.
            if(renderer!=null&&set!=null&&set.Coping!=null&&renderer.sharedMaterial==set.Coping&&set.Coping.HasProperty(ColourId))
            {
                Bind(new IceBlock{Renderer=renderer,Texture=Texture2D.whiteTexture,Colour=set.Coping.GetColor(ColourId)});
                return;
            }
            if(!PlanBlock(renderer,set,out IceBlock block))return;
            if(block.Variant!=null)
            {
                if(block.Variant.Length>0&&!_iceVariants.ContainsKey(block.Variant))
                    _iceVariants.Add(block.Variant,Resources.Load<Texture2D>("Ice/"+block.Variant));
                block=Resolve(block,set);
            }
            Bind(block);
        }

        public void ApplyTheme(FloorTheme theme,PlatformMaterialSet set)
        {
            if(theme==_theme)return;
            bool ice=theme==FloorTheme.Ice;
            // Normally done frames ago (PrepareTheme); otherwise all of it now.
            FinishIcePreparation(set);
            if(_themeBlock==null)_themeBlock=new MaterialPropertyBlock();
            using(PerformanceMarkers.ThemeRetint.Auto())
                for(int i=0;i<_themeMeshes.Count;i++)
                {
                    if(_themeMeshes[i]==null)continue;
                    Color[] colours=ice?_iceColours[i]:_foundryColours[i];
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
                if(ice)
                {
                    for(int i=0;i<_iceBlocks.Count;i++)Bind(_iceBlocks[i]);
                    BindIceObject(_iceCrystals,set);
                    BindIceObject(_icePanels,set);
                    BindIceObject(_iceDrifts,set);
                }
                else
                    foreach(var renderer in GetComponentsInChildren<MeshRenderer>(true))renderer.SetPropertyBlock(null);
            }
            using(PerformanceMarkers.ThemeRetint.Auto())
                for(int i=0;i<_themeSprites.Count;i++)
                    if(_themeSprites[i]!=null)_themeSprites[i].color=ice?_foundrySpriteColours[i]*IceSprite:_foundrySpriteColours[i];
            _theme=theme;
        }

        // A structure mesh's vertices on upward faces. Walls are extruded straight down the screen, so each wall triangle
        // has an exactly vertical edge; a cap, a crenellation top or a dome facet has none. A vertex is rimed only when
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
        // Toward frost #B9CCDA on a slate texture (a multiplier near 2 lifts its ~.35 value to ~.7).
        private static Color Rime(Color c)=>new Color(Mathf.Min(c.r*1.75f,2),Mathf.Min(c.g*1.9f,2),Mathf.Min(c.b*2f,2),c.a);

        // Resources/Ice/<name>: the cold variant of a foundry texture (Tools/ArtReview/GenerateIceGlowTextures.py), loaded
        // asynchronously once an ice floor is ahead (PrepareTheme); a run whose floors are all foundry never loads one.
        private readonly Dictionary<string,Texture2D> _iceVariants=new Dictionary<string,Texture2D>();

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
                    // A light: lava (red, little green) to a darker quicksilver glow, a flame or brazier to frost-light.
                    :max>1.05f?(c.g<c.r*.55f?QuicksilverGlow*.7f:FrostLight)*Mathf.Min(max,1.5f):FrostLight*luminance;
                graded=Color.LerpUnclamped(stone,ice,warm);
            }
            graded.r=Mathf.Min(graded.r,2);graded.g=Mathf.Min(graded.g,2);graded.b=Mathf.Min(graded.b,2);
            graded.a=c.a;
            return graded;
        }

        // A rail vertex of the untextured coping batch (its colour is final): inlaid mirror-ice #5FA8C8 at about 50-55%
        // of its value, a faint ice-edge #BFE6F2 sheen only where the floor light is strong; on the compass seal silver
        // #A9B4BF to ice edge.
        public static Color IceRail(Color c,bool seal)
        {
            float luminance=c.r*.299f+c.g*.587f+c.b*.114f;
            Color graded=seal?Color.Lerp(Silver,IceEdge,Mathf.Clamp01((luminance-.7f)*1.5f))*Mathf.Clamp(luminance*1.15f,.85f,1.4f)
                :Color.Lerp(MirrorIce,IceEdge,Mathf.Clamp01((luminance-1f)*1.2f)*.45f)*Mathf.Clamp(luminance*.56f,.45f,.68f);
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
