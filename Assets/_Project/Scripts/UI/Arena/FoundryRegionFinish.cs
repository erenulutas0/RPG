using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Authored, static illumination in vertex colours. This projected scenery has no
        // physical surface normals: ordinary 3D lights cannot illuminate it correctly.
        // Calculated at build only, using existing shared materials and batches.
        //
        // Forge light, calibrated 2026-09-30 against the approved concept (foundry-layout-01/foundry-v1.png): its open
        // court is a cool violet-grey (mean (81,76,88), hue ~263, saturation ~.13) and its warmth sits in tight pools
        // on the fires - a brazier lifts R-B by ~+17..+25/255 within 1-2 u, by about half that at ~2.5 and little past
        // ~3.5, and the pit throws no orange past its collar. The render here had 24% of the court orange (saturation
        // over .35) and a brown court mean (hue ~15): the pit and boiler pools stayed orange 3-4 u out. Area sources
        // (the furnace pit's rectangle, the four boilers' round plinths) keep the hottest light, now a ~2.5 u rim;
        // lanterns, braziers and beacons are weaker point sources. The paving samples the light on one lattice that
        // every stone shares - each whole unit plus the stone's own edges, halved where that misses FloorLight by over
        // .04 (AshlarLightStep) - and the bridge every 1 s / 2 d, so no falloff radius is below 3.4: a pool interpolates
        // as one soft gradient across the joints rather than a lit disc or a pool that steps stone by stone (the old 2x1
        // bond's staggered rows stepped by up to ~11/255 in R-B, its apron pools by ~14).
        private struct ForgeLamp { internal Vector4 Box; internal float Round,Radius,Strength; internal Color Tint; }
        // The furnace tint is amber (hue ~33); heat deepens it. A pool absorbs blue in proportion to its glow and green
        // in proportion to the glow's square, so the pit and boiler rims run forge orange (hue ~22, saturation ~.5).
        // The flame tint is redder (hue ~26): a lantern's weak spill over violet stone then reads rose (hue ~5,
        // saturation ~.2) like the concept's brazier pools, where the former yellower tint over blue slate read tan.
        private static readonly Color FurnaceLight=new Color(.66f,.38f,.03f,0),FlameLight=new Color(.52f,.24f,.03f,0);
        // Ambient: the open court and, within a fire's reach, ~5% brighter and a little less blue. Pixel = paving
        // texel (.358 mean) x _Color .8 x light, so the open court renders ~(66,62,72): the concept's hue and
        // saturation. Its value is lifted only ~9% from the former slate (.85,.895,1): the whole concept is ~40%
        // brighter than this render (image mean value .26 against .18, void haze .35 against ~.2), and the violet
        // golems' bodies (~(73,71,89)) would sink into a floor at the concept's full value.
        private static readonly Color CoolSlate=new Color(1f,.945f,1.09f),CourtAmbient=new Color(1.05f,.99f,1.11f);
        private static readonly Color FireAbsorb=new Color(0,.42f,.22f,0);
        // Existing flame sources, from BuildCourtBorders (the four lantern finials) and BuildDressing (beacons).
        private static readonly Vector2[] CourtLanternPosts={new Vector2(-18.5f,-6),new Vector2(2,-14.5f),new Vector2(-14,18.5f),new Vector2(10,18.5f)};
        private static readonly Vector2[] ForgeBeacons={new Vector2(-27.6f,-23),new Vector2(-23,-27.6f),
            new Vector2(-18.8f,-14.1f),new Vector2(-14.1f,-18.8f),new Vector2(32.5f,-2),new Vector2(32.5f,12)};
        private static ForgeLamp[] _forgeLamps;

        // Built lazily on first use: static initialisers in other partial files have run by then.
        private static ForgeLamp[] ForgeLamps()
        {
            if(_forgeLamps!=null)return _forgeLamps;
            var lamps=new System.Collections.Generic.List<ForgeLamp>();
            // A hot rim: forge orange (saturation ~.5) to ~1.5 off the pit, rose-amber by 2.5, the court's violet by
            // 3.5. It was orange to ~3.5 off and still warmed the seal centre, 4.5 away; the concept's pit lights no
            // floor at all.
            lamps.Add(Lamp(new Vector4(PitU0,PitV0,PitU1,PitV1),0,3.8f,1f,FurnaceLight));
            // The tall rear boilers' fire fronts face the apron and reach furthest onto it; the squat front ones turn
            // their grates away. Their plinths sit at or past the walkable edge (the rear ones .5 and 1.8 behind it),
            // so the pools must reach in to it: a ~2-2.5 wide orange band along each apron rim, not the whole apron.
            for(int i=0;i<BoilerCentres.Length;i++)
            {
                Vector2 c=BoilerCentres[i];
                lamps.Add(Lamp(new Vector4(c.x,c.y,c.x,c.y),BoilerPlinth,SquatBoiler(i)?4.2f:4.6f,
                    SquatBoiler(i)?.9f:1.4f,FurnaceLight));
            }
            // The pit collar's corner lanterns (.9 pedestals) only round off the pit's own glow at its corners.
            foreach(var p in new[]{new Vector2(PitU0+.45f,PitV0+.45f),new Vector2(PitU1-.45f,PitV0+.45f),
                new Vector2(PitU0+.45f,PitV1-.45f),new Vector2(PitU1-.45f,PitV1-.45f)})
                lamps.Add(Lamp(new Vector4(p.x,p.y,p.x,p.y),0,3.4f,.3f,FlameLight));
            // Rim lanterns and beacons are the concept's braziers: R-B ~+22/255 over the open court at 1-1.5 u, half by
            // ~2.2, gone by ~3.5 - about as strong as before, a little tighter, and rose instead of tan.
            foreach(var p in CourtLanternPosts)lamps.Add(Lamp(new Vector4(p.x,p.y,p.x,p.y),0,3.6f,.45f,FlameLight));
            // Balustrade lanterns: the same placement test BuildBalustrade applies, so no pool lacks its lamp.
            foreach(var box in RailLanterns)
            {
                if(!MassClear(box)||MassBlocked(box)||MassConflicts(box,2.8f,true))continue;
                float cu=(box.x+box.z)*.5f,cv=(box.y+box.w)*.5f;
                lamps.Add(Lamp(new Vector4(cu,cv,cu,cv),0,3.6f,.45f,FlameLight));
            }
            foreach(var p in ForgeBeacons)lamps.Add(Lamp(new Vector4(p.x,p.y,p.x,p.y),0,3.8f,.5f,FlameLight));
            return _forgeLamps=lamps.ToArray();
        }

        private static ForgeLamp Lamp(Vector4 box,float round,float radius,float strength,Color tint)=>
            new ForgeLamp{Box=box,Round=round,Radius=radius,Strength=strength,Tint=tint};

        private static float SmoothFall(float distance,float radius)
        {
            float t=Mathf.Clamp01(1-distance/radius);
            return t*t*(3-2*t);
        }

        // Soft shoulder on the brightest channel, scaling all three alike so a hot pool keeps its hue: the former
        // per-channel shoulder flattened red first while green kept climbing, bleaching saturated heat toward sand.
        // The floor is Sprites/Default: float vertex colours (Mesh.SetColors) times _Color .8 land in a fixed4
        // interpolator, whose guaranteed range is +/-2 on every mobile precision. Every FloorLight channel stays
        // below 1.74, so even the brightest bevel of the palest, warm-tinted seal flag (1.15 x 1.1 x 1.05 x 1.08 =
        // x1.435) stays below 1.74 x 1.435 x .8 = 1.998 in the shader (pools now peak at 1.62, at the pit's collar
        // corners and where the seal's corner flags meet the pit rim: 1.86); the old apron pools reached 2.5..3.8
        // there, outside that range. The open court (brightest channel, blue, at most 1.13, under the knee) is
        // untouched.
        private const float LightKnee=1.2f,LightCeiling=1.74f;
        private static float Shoulder(float c)=>c<=LightKnee?c:
            LightKnee+(LightCeiling-LightKnee)*(1-Mathf.Exp(-(c-LightKnee)/(LightCeiling-LightKnee)));

        // Saturated heat has to read as fire: a full pool adds about 2.5x the tint (the shoulder caps its brightest
        // channel, now without turning it yellow), and a fire's ambient reach ends at 1.2x its pool radius so the
        // open court keeps its violet-grey between fires.
        private const float FireGain=2.5f,FireReach=1.2f;
        private static Color FloorLight(float u,float v)
        {
            // Overlapping pools saturate smoothly (1 - e^-heat) instead of adding up to a flat bright plateau.
            float heat=0,reach=0;
            Color warm=Color.clear;
            foreach(var lamp in ForgeLamps())
            {
                float d=Mathf.Max(0,DistanceFromFloorBox(u,v,lamp.Box.x,lamp.Box.y,lamp.Box.z,lamp.Box.w)-lamp.Round);
                if(d>=lamp.Radius*FireReach)continue;
                float f=lamp.Strength*SmoothFall(d,lamp.Radius);
                heat+=f;warm+=lamp.Tint*f;
                reach+=lamp.Strength*SmoothFall(d,lamp.Radius*FireReach);
            }
            float glow=1-Mathf.Exp(-heat);
            if(heat>.0001f)warm*=FireGain*glow/heat;
            // About 5% darker and bluer where no fire reaches; no luminous combat markings.
            Color ambient=Color.Lerp(CoolSlate,CourtAmbient,1-Mathf.Exp(-2.2f*reach));
            Color light=ambient*(Color.white-new Color(0,FireAbsorb.g*glow*glow,FireAbsorb.b*glow,0))+warm;
            // Broad, quiet deposits cross slab boundaries so wear belongs to the yard,
            // not to a repeated tile. This only modulates the existing texture colours.
            float patina=FloorPatina(u*.19f,v*.19f);
            light*=Mathf.Lerp(.91f,1.025f,patina);
            // Soot belongs to working areas, not every slab. Smooth, irregular deposits
            // stay flush with the floor and never resemble cracks or blocking rubble.
            float shaftDistance=DistanceFromFloorBox(u,v,-12,2,-4,10);
            float stockDistance=DistanceFromFloorBox(u,v,-20.7f,5,-18.45f,8.4f);
            float soot=Mathf.Max(Mathf.Clamp01(1-shaftDistance/1.45f),
                Mathf.Clamp01(1-stockDistance/2.0f));
            soot=soot*soot*Mathf.Lerp(.55f,1,FloorPatina(u*.71f+9,v*.71f-4));
            light*=1-.17f*soot;
            // Contact shade ties the large gate/boiler masses to their stone apron.
            // Smooth local falloff avoids a painted rectangular outline around buildings.
            // The boilers' round plinths replaced the 3x3 furnace boxes this once traced.
            float contact=ContactShade(u,v,32,-3,35,13,1.4f);
            foreach(var c in BoilerCentres)
                contact=Mathf.Max(contact,ContactShade(u,v,c.x,c.y,c.x,c.y,1.05f,BoilerPlinth));
            light*=1-.16f*contact;
            // The established approach is gently worn through use. Keep this a broad
            // material value change, not a luminous path or a new gameplay marker.
            float lane=Mathf.Clamp01(1-Mathf.Abs(v-5)/3.5f);
            float along=Mathf.SmoothStep(0,1,Mathf.InverseLerp(8,13,u))*
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(29,32,u)));
            light*=1+.045f*lane*lane*along;
            // Wide flush shoulder courses frame the gate approach without bright
            // navigation lines. Fade the treatment into the court at its entrance.
            float shoulder=Mathf.Max(Mathf.Clamp01(1-Mathf.Abs(v-.5f)/.65f),
                Mathf.Clamp01(1-Mathf.Abs(v-9.5f)/.65f));
            float approach=Mathf.SmoothStep(0,1,Mathf.InverseLerp(8,12,u))*
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(31,32,u)));
            light*=1-.12f*shoulder*approach;
            float edge=0;
            foreach(var offset in EdgeSamples)
            {
                FoundryMap.ToFloor(u+offset.x,v+offset.y,out float x,out float y);
                if(!FoundryMap.Contains(x,y,0))edge+=.055f;
            }
            light*=1-edge;
            float peak=Mathf.Max(light.r,Mathf.Max(light.g,light.b));
            if(peak>LightKnee)light*=Shoulder(peak)/peak;
            light.a=1;
            return light;
        }

        private static readonly Vector2[] EdgeSamples =
        {
            new Vector2(.6f,0),new Vector2(-.6f,0),new Vector2(0,.6f),new Vector2(0,-.6f)
        };

        // round: a rounded box (a disc, for a degenerate box) - the distance is measured from its rounded edge.
        private static float ContactShade(float u,float v,float u0,float v0,float u1,float v1,float radius,float round=0)
        {
            float proximity=Mathf.Clamp01(1-Mathf.Max(0,DistanceFromFloorBox(u,v,u0,v0,u1,v1)-round)/radius);
            return proximity*proximity*(3-2*proximity);
        }

        private static float DistanceFromFloorBox(float u,float v,float u0,float v0,float u1,float v1)
        {
            float du=Mathf.Max(0,Mathf.Max(u0-u,u-u1));
            float dv=Mathf.Max(0,Mathf.Max(v0-v,v-v1));
            return Mathf.Sqrt(du*du+dv*dv);
        }

        private static float FloorPatina(float u,float v)
        {
            int x=Mathf.FloorToInt(u),y=Mathf.FloorToInt(v);
            float tx=u-x,ty=v-y;
            tx=tx*tx*(3-2*tx);ty=ty*ty*(3-2*ty);
            return Mathf.Lerp(Mathf.Lerp(StoneSample(x,y),StoneSample(x+1,y),tx),
                Mathf.Lerp(StoneSample(x,y+1),StoneSample(x+1,y+1),tx),ty);
        }

        private static float StoneSample(int x,int y)
        {
            uint hash=unchecked((uint)x*73856093u^(uint)y*19349663u^83492791u);
            hash^=hash>>13;
            hash=unchecked(hash*1274126177u);
            return (hash&65535u)/65535f;
        }

        // These regions describe flush stonework only; the shared walking union is unchanged.
        private static int PavingZone(Vector2Int cell)
        {
            if(cell.x>=-4&&cell.x<4&&cell.y>=-4&&cell.y<4)return 1;
            if(cell.x>=10&&cell.x<32&&cell.y>=0&&cell.y<10)return 2;
            if(cell.x>=-18&&cell.x<-14&&cell.y>=2&&cell.y<12)return 3;
            return 0;
        }

        // Court ashlar, 2026-10-01. The 2x1 running bond (the apron's 1x2) ran every course joint unbroken across the
        // court, 22-32 u long: the canonical brick bond at the portrait camera, where the paving fills 80-95% of the
        // view, against the concept's large dressed flags of mixed size with no joint running on. The court and the
        // apron are now random ashlar on a half-unit grid, laid once at load from a seeded hash: stones 1-3 u a side
        // (aspect at most 2.5, at most 6.25 cells) weighted to the 2.5-6 cell flags, half-unit infill only where nothing
        // else fits. A skyline packer fills the lowest, then leftmost free half cell with a weighted pick among its
        // best-scored stones: no new joint making a visible straight run over 4 u (brass and the void edge are covered
        // and end a run; a zone edge counts at most 2 u, so the T a convex zone corner forces stays short), no four-way
        // crossing, no half-unit step beside a neighbour, and no half cell left over in the row or under a forced joint
        // (only infill could take it). A stone whose top meets its neighbour's top, or the end of a forced line beside
        // it, is ten times likelier, so the next stone can bridge the joint between them: two columns leapfrogging each
        // other otherwise draw one long joint. Where every stone breaks a rule the least penalised is laid. The apron
        // keeps its crosswise lay - a stone wider than it is tall is a quarter as likely there. Seed 24, measured on the
        // whole floor: 398 court and apron stones (mean 2.8 cells, no infill); 94% of the visible free joints run 4 u or
        // less, 25 run 4.5-6.5 u, and no zone edge is carried on by more than 3 u. The brass lanes and the compass's axis
        // rays lie in forced joints, so no flag runs under an inlay. The seal inset's 2x2 flags and the west strip's 1x1
        // setts are laid exactly as before.
        private const int AshlarSub=2,AshlarRun=8,AshlarEdgeRun=4,AshlarLevel=10,AshlarSalt=24;
        private const byte AshlarFree=0,AshlarEdge=1,AshlarCovered=2;
        // Forced joints under the court's brass, (u0, v0, u1, v1) of each line: BuildLandmarks' lanes and BuildCourtCompass'
        // rays on the axes (r 4.1-8.5; the joint runs on from the seal inset's edge at 4).
        private static readonly Vector4[] CourtBrassJoints=
        {
            new Vector4(4,-1,30,-1),new Vector4(5,9,30,9),new Vector4(-16,-10,8,-10),new Vector4(-16,-10,-16,15),
            new Vector4(-16,15,10,15),new Vector4(4,0,8.5f,0),new Vector4(-8.5f,0,-4,0),new Vector4(0,4,0,8.5f),
            new Vector4(0,-8.5f,0,-4)
        };
        // Stone sizes in half units (w along u, h along v) and weights, in the packer's order: court, then the apron's
        // crosswise set. Infill comes last, at weight 1: it is only ever laid as the least penalised stone.
        private static readonly int[][] AshlarSizes={AshlarFamily(false),AshlarFamily(true)};
        private static int[] AshlarFamily(bool crosswise)
        {
            var sizes=new List<int>();
            for(int h=2;h<=6;h++)
                for(int w=2;w<=6;w++)
                {
                    if(Mathf.Max(w,h)*2>5*Mathf.Min(w,h)||w*h>25)continue;
                    int weight=w*h<6?3:w*h<10?6:8;
                    sizes.AddRange(new[]{w,h,crosswise&&w>h?Mathf.Max(1,weight/4):weight});
                }
            sizes.AddRange(new[]{2,1,1,1,2,1,1,1,1});
            return sizes.ToArray();
        }
        // I, J: the stone's corner in absolute half units (u0 = I / 2), W, H its size in half units.
        private struct AshlarStone { internal int I,J,W,H,Zone; }
        // The laid stones by the cell holding their (u0, v0) corner, laid on the first cell built and taken as each of
        // those cells is: every stone is built exactly once, whatever order Build visits the cells in.
        private Dictionary<Vector2Int,List<AshlarStone>> _ashlar;

        private void BuildPavingCell(Builder mesh,Vector2Int cell)
        {
            if(_ashlar==null)LayCourtAshlar();
            if(!_ashlar.TryGetValue(cell,out var stones))return;
            _ashlar.Remove(cell);
            foreach(var stone in stones)AshlarFlag(mesh,stone);
        }

        private void LayCourtAshlar()
        {
            int u0=int.MaxValue,v0=int.MaxValue,u1=int.MinValue,v1=int.MinValue;
            for(int r=0;r<FoundryMap.RectangleCount;r++)
            {
                u0=Mathf.Min(u0,FoundryMap.Bound(r,0));v0=Mathf.Min(v0,FoundryMap.Bound(r,1));
                u1=Mathf.Max(u1,FoundryMap.Bound(r,2));v1=Mathf.Max(v1,FoundryMap.Bound(r,3));
            }
            var zone=new sbyte[(u1-u0)*AshlarSub,(v1-v0)*AshlarSub];
            for(int i=0;i<zone.GetLength(0);i++)for(int j=0;j<zone.GetLength(1);j++)zone[i,j]=-1;
            foreach(var cell in _cells)
                for(int a=0;a<AshlarSub;a++)for(int b=0;b<AshlarSub;b++)
                    zone[(cell.x-u0)*AshlarSub+a,(cell.y-v0)*AshlarSub+b]=(sbyte)PavingZone(cell);
            _ashlar=new Dictionary<Vector2Int,List<AshlarStone>>();
            foreach(var laid in LayAshlar(zone,u0,v0,AshlarSalt))
            {
                var stone=laid;stone.I+=u0*AshlarSub;stone.J+=v0*AshlarSub;
                var cell=new Vector2Int(Mathf.FloorToInt(stone.I/(float)AshlarSub),Mathf.FloorToInt(stone.J/(float)AshlarSub));
                if(!_ashlar.TryGetValue(cell,out var list))_ashlar[cell]=list=new List<AshlarStone>();
                list.Add(stone);
            }
        }

        // zone: PavingZone of each half cell of the rooms' bounding box from (u0, v0), -1 off the floor. Returns the
        // stones relative to that box. Every paved half cell is covered exactly once: a single half cell always fits.
        private static List<AshlarStone> LayAshlar(sbyte[,] zone,int u0,int v0,int salt)
        {
            var lay=new Ashlar(zone);
            foreach(var line in CourtBrassJoints)
            {
                if(line.y==line.w)lay.Cover(true,Mathf.RoundToInt((line.y-v0)*AshlarSub),
                    Mathf.RoundToInt((line.x-u0)*AshlarSub),Mathf.RoundToInt((line.z-u0)*AshlarSub));
                else lay.Cover(false,Mathf.RoundToInt((line.x-u0)*AshlarSub),
                    Mathf.RoundToInt((line.y-v0)*AshlarSub),Mathf.RoundToInt((line.w-v0)*AshlarSub));
            }
            // The seal inset's sixteen 2x2 flags and the west strip's setts first, then the court, then the apron.
            for(int i=0;i<lay.W;i+=AshlarSub)
                for(int j=0;j<lay.H;j+=AshlarSub)
                {
                    if(zone[i,j]==1&&((i/AshlarSub+u0)&1)==0&&((j/AshlarSub+v0)&1)==0)lay.Place(i,j,2*AshlarSub,2*AshlarSub,1);
                    else if(zone[i,j]==3)lay.Place(i,j,AshlarSub,AshlarSub,3);
                }
            lay.Fill(0,salt);lay.Fill(2,salt);
            return lay.Stones;
        }

        private static uint PavingHash(int x,int y,int salt)
        {
            uint hash=unchecked((uint)x*73856093u^(uint)y*19349663u^(uint)salt*83492791u);
            hash^=hash>>13;
            hash=unchecked(hash*1274126177u);
            return hash^hash>>16;
        }

        // The packer's half-unit grid. A joint segment is counted once per stone outline it lies on, plus once if forced;
        // its kind says whether it is free, a zone edge (seen) or covered (under brass or on the void edge, never seen).
        private sealed class Ashlar
        {
            internal readonly int W,H;
            internal readonly List<AshlarStone> Stones=new List<AshlarStone>();
            private readonly sbyte[,] _zone;
            private readonly int[,] _top;
            // Horizontal segment (i, line j) under half cell (i, j); vertical segment (line i, j) left of half cell (i, j).
            private readonly byte[,] _hc,_hk,_vc,_vk;
            private readonly List<int> _pool=new List<int>();

            internal Ashlar(sbyte[,] zone)
            {
                _zone=zone;W=zone.GetLength(0);H=zone.GetLength(1);
                _top=new int[W,H];
                for(int i=0;i<W;i++)for(int j=0;j<H;j++)_top[i,j]=-1;
                _hc=new byte[W,H+1];_hk=new byte[W,H+1];_vc=new byte[W+1,H];_vk=new byte[W+1,H];
                for(int i=0;i<W;i++)for(int j=0;j<=H;j++)Bound(true,i,j,Z(i,j-1),Z(i,j));
                for(int i=0;i<=W;i++)for(int j=0;j<H;j++)Bound(false,i,j,Z(i-1,j),Z(i,j));
            }

            private int Z(int i,int j)=>i>=0&&i<W&&j>=0&&j<H?_zone[i,j]:-1;
            private bool Free(int i,int j,int z)=>i>=0&&i<W&&j>=0&&j<H&&_zone[i,j]==z&&_top[i,j]<0;

            private void Bound(bool horizontal,int i,int j,int a,int b)
            {
                if(a==b||Mathf.Max(a,b)<0)return;
                byte kind=Mathf.Min(a,b)<0?AshlarCovered:AshlarEdge;
                if(horizontal){_hc[i,j]=1;_hk[i,j]=kind;}
                else{_vc[i,j]=1;_vk[i,j]=kind;}
            }

            // Forces covered joints along a grid line, from..to in half cells, wherever either side is paved.
            internal void Cover(bool horizontal,int line,int from,int to)
            {
                for(int k=from;k<to;k++)
                    if(horizontal){if(Mathf.Max(Z(k,line-1),Z(k,line))>=0){_hc[k,line]=1;_hk[k,line]=AshlarCovered;}}
                    else if(Mathf.Max(Z(line-1,k),Z(line,k))>=0){_vc[line,k]=1;_vk[line,k]=AshlarCovered;}
            }

            internal void Place(int i0,int j0,int w,int h,int z)
            {
                Stones.Add(new AshlarStone{I=i0,J=j0,W=w,H=h,Zone=z});
                for(int i=i0;i<i0+w;i++)
                {
                    for(int j=j0;j<j0+h;j++)_top[i,j]=j0+h;
                    _hc[i,j0]++;_hc[i,j0+h]++;
                }
                for(int j=j0;j<j0+h;j++){_vc[i0,j]++;_vc[i0+w,j]++;}
            }

            internal void Fill(int z,int salt)
            {
                for(int j=0;j<H;j++)for(int i=0;i<W;i++)if(_zone[i,j]==z&&_top[i,j]<0)Lay(i,j,z,salt);
            }

            // One stone at the lowest, leftmost free half cell (i, j): everything below and to its left is laid.
            private void Lay(int i,int j,int z,int salt)
            {
                int span=1;
                while(Free(i+span,j,z)&&_vk[i+span,j]!=AshlarCovered)span++;
                // What bounds the span: a laid neighbour a later stone could bridge (its top row), or a wall - a forced
                // joint line - whose top is where that line stops being forced.
                int left=Neighbour(i-1,j,z,i)?_top[i-1,j]:Wall(i,j),right=Neighbour(i+span,j,z,i+span)?_top[i+span,j]:Wall(i+span,j);
                int[] sizes=AshlarSizes[z==2?1:0];
                int best=int.MaxValue,total=0;
                _pool.Clear();
                for(int s=0;s<sizes.Length;s+=3)
                {
                    int w=sizes[s],h=sizes[s+1],weight=sizes[s+2],room=0;
                    if(w>span)continue;
                    while(room<h+2&&Fits(i,j+room,w,z,room))room++;
                    if(h>room)continue;
                    int top=j+h;
                    // A half cell left over in the row or under a forced joint can only take infill: it costs as much.
                    bool infill=Mathf.Min(w,h)<AshlarSub||span-w==1||room-h==1,cross=Crossing(i,j,w,h);
                    bool sliver=Mathf.Abs(left-top)==1||(w==span&&Mathf.Abs(right-top)==1);
                    int run=Mathf.Max(Mathf.Max(EdgeRun(true,j,i,i+w),EdgeRun(true,j+h,i,i+w)),
                        Mathf.Max(EdgeRun(false,i,j,j+h),EdgeRun(false,i+w,j,j+h)));
                    int penalty=!infill&&!sliver&&!cross&&run<=AshlarRun?0:
                        (infill?100:0)+(sliver?40:0)+(cross?20:0)+8*Mathf.Max(0,run-AshlarRun)+1;
                    if(top==left||(w==span&&top==right))weight*=AshlarLevel;
                    if(penalty<best){best=penalty;total=0;_pool.Clear();}
                    if(penalty==best){_pool.Add(s);_pool.Add(weight);total+=weight;}
                }
                int pick=(int)(PavingHash(i,j,salt+z)%(uint)total);
                for(int k=0;k<_pool.Count;k+=2)
                {
                    if(pick<_pool[k+1]){Place(i,j,sizes[_pool[k]],sizes[_pool[k]+1],z);return;}
                    pick-=_pool[k+1];
                }
            }

            private bool Neighbour(int i,int j,int z,int line)=>i>=0&&i<W&&_zone[i,j]==z&&_top[i,j]>=0&&_vk[line,j]==AshlarFree;

            private int Wall(int line,int j)
            {
                if(line<=0||line>=W)return 1<<20;
                int r=j;
                while(r<H&&_vk[line,r]!=AshlarFree)r++;
                return r<H?r:1<<20;
            }

            // Row k of a w-wide stone at column i, on grid row `row`: free, in the zone, crossing no forced joint.
            private bool Fits(int i,int row,int w,int z,int k)
            {
                if(row>=H)return false;
                for(int c=i;c<i+w;c++)
                    if(!Free(c,row,z)||(k>0&&_hk[c,row]==AshlarCovered)||(c>i&&_vk[c,row]==AshlarCovered))return false;
                return true;
            }

            // The longest visible run through the pieces of a stone's edge that would add a new joint.
            private int EdgeRun(bool horizontal,int line,int a,int b)
            {
                int best=0;
                for(int k=a;k<b;)
                {
                    if((horizontal?_hk[k,line]:_vk[line,k])==AshlarCovered){k++;continue;}
                    int start=k;bool fresh=false;
                    for(;k<b&&(horizontal?_hk[k,line]:_vk[line,k])!=AshlarCovered;k++)fresh|=(horizontal?_hc[k,line]:_vc[line,k])==0;
                    if(fresh)best=Mathf.Max(best,Run(horizontal,line,start,k));
                }
                return best;
            }

            // A visible straight joint through a..b: free and zone-edge segments, a zone edge counting at most
            // AshlarEdgeRun; a covered segment or none ends it.
            private int Run(bool horizontal,int line,int a,int b)
            {
                int n=horizontal?W:H,free=b-a,edge=0;
                for(;a>0;a--){int kind=Seen(horizontal,line,a-1);if(kind<0)break;if(kind==AshlarEdge)edge++;else free++;}
                for(;b<n;b++){int kind=Seen(horizontal,line,b);if(kind<0)break;if(kind==AshlarEdge)edge++;else free++;}
                return free+Mathf.Min(edge,AshlarEdgeRun);
            }

            private int Seen(bool horizontal,int line,int k)
            {
                int count=horizontal?_hc[k,line]:_vc[line,k],kind=horizontal?_hk[k,line]:_vk[line,k];
                return count==0||kind==AshlarCovered?-1:kind;
            }

            // Four joints meeting at a corner of the stone once it is laid.
            private bool Crossing(int i0,int j0,int w,int h)
            {
                for(int c=0;c<4;c++)
                {
                    int pi=i0+(c&1)*w,pj=j0+(c>>1)*h;
                    if(Joint(true,pi-1,pj,i0,j0,w,h)&&Joint(true,pi,pj,i0,j0,w,h)&&
                        Joint(false,pi,pj-1,i0,j0,w,h)&&Joint(false,pi,pj,i0,j0,w,h))return true;
                }
                return false;
            }

            private bool Joint(bool horizontal,int i,int j,int i0,int j0,int w,int h)=>horizontal
                ?i>=0&&i<W&&(_hc[i,j]>0||((j==j0||j==j0+h)&&i>=i0&&i<i0+w))
                :j>=0&&j<H&&(_vc[i,j]>0||((i==i0||i==i0+w)&&j>=j0&&j<j0+h));
        }

        // One laid stone, flush, in three layers: the joint bed over its whole footprint, a lit and a shaded bevel strip,
        // the face. Each is a shared-vertex grid or strip on the stone's light lines (AshlarLightStep), so a 3x2 flag
        // samples the forge light as densely as six 1x1 ones and a pool runs on across its joints.
        private static void AshlarFlag(Builder mesh,AshlarStone s)
        {
            float u0=s.I*.5f,v0=s.J*.5f,u1=(s.I+s.W)*.5f,v1=(s.J+s.H)*.5f,w=u1-u0,h=v1-v0,area=w*h;
            // The seal inset's flags and the west strip's setts keep their old variation, joint and bevel exactly.
            bool kept=s.Zone==1||s.Zone==3;
            int seedU=kept?(int)u0:s.I,seedV=kept?(int)v0:s.J;
            // Deterministic per-flag variation, strong enough to read one flag from the next at the portrait camera:
            // most flags sit near the base tone, about one in six is a darker replacement stone and one in ten a
            // paler, warmer one, the way the reference's courtyard is mottled - softer on the big flags (to 72% at 6
            // cells), whose dark tones would otherwise read as a stain, and on the half-unit infill (50%), where a dark
            // one ringed by its joint would read as a socket or a hole. Still flush colour only - no grit or floor
            // geometry that could be mistaken for a collision obstacle.
            float grain=StoneSample(seedU,seedV),hue=StoneSample(seedU+131,seedV-57);
            float tone=grain<.17f?Mathf.Lerp(.78f,.87f,grain/.17f):
                grain>.9f?Mathf.Lerp(1.08f,1.15f,(grain-.9f)/.1f):Mathf.Lerp(.93f,1.04f,(grain-.17f)/.73f);
            float stone=1+(tone-1)*(kept?1:area<1?.5f:Mathf.Clamp(1-.06f*(area-1),.72f,1));
            Color flagTint=hue<.22f?new Color(.97f,.99f,1.03f):hue>.82f?new Color(1.05f,1.0f,.93f):Color.white;
            // The gate apron's stone is a shade redder than the court's (hue ~281 against ~263 under the open light;
            // the concept's apron is ~287); its former blue cut (.93) turned it magenta-grey under the violet ambient.
            Color tint=s.Zone==1?new Color(1.08f,1.065f,1.015f):
                s.Zone==2?new Color(1.01f,.985f,.96f):
                s.Zone==3?new Color(.82f,.84f,.88f):Color.white;
            Color faceTint=flagTint*tint;
            // A darker joint and a lit/shaded bevel, so each flag reads as a laid stone: .028 / .06 on a 1x1, a little
            // wider on the big flags (.034 / .075 at 6 cells), never more than .14 of an infill stone's width.
            float joint=kept?.028f:.024f+.004f*Mathf.Sqrt(area);
            float bevel=kept?.06f:Mathf.Min(.05f+.01f*Mathf.Sqrt(area),.14f*Mathf.Min(w,h));
            // The joint bed's vertices are the 1-unit lattice the step test already lit; refined, it takes the finer one.
            float[] us=LightLines(u0,u1,1),vs=LightLines(v0,v1,1);
            var lattice=new Color[us.Length*vs.Length];
            for(int l=0;l<vs.Length;l++)for(int k=0;k<us.Length;k++)lattice[l*us.Length+k]=FloorLight(us[k],vs[l]);
            float step=AshlarLightStep(us,vs,lattice);
            if(step<1)PavingGrid(mesh,LightLines(u0,u1,step),LightLines(v0,v1,step),.5f,tint);
            else PavingGrid(mesh,us,vs,.5f,tint,lattice);
            // Lit from screen right: the v0 and u1 edges are one strip, mitred where they meet, the u0 and v1 edges the
            // shaded one. Outer (joint) and inner (bevel) rows take the same light lines: none falls inside the band.
            float[] ou=LightLines(u0+joint,u1-joint,step),ov=LightLines(v0+joint,v1-joint,step);
            float[] iu=LightLines(u0+bevel,u1-bevel,step),iv=LightLines(v0+bevel,v1-bevel,step);
            var outer=new List<Vector2>();var inner=new List<Vector2>();
            for(int k=0;k<ou.Length;k++){outer.Add(new Vector2(ou[k],v0+joint));inner.Add(new Vector2(iu[k],v0+bevel));}
            for(int k=1;k<ov.Length;k++){outer.Add(new Vector2(u1-joint,ov[k]));inner.Add(new Vector2(u1-bevel,iv[k]));}
            PavingStrip(mesh,outer,inner,stone*1.1f,faceTint);
            outer.Clear();inner.Clear();
            for(int k=0;k<ov.Length;k++){outer.Add(new Vector2(u0+joint,ov[k]));inner.Add(new Vector2(u0+bevel,iv[k]));}
            for(int k=1;k<ou.Length;k++){outer.Add(new Vector2(ou[k],v1-joint));inner.Add(new Vector2(iu[k],v1-bevel));}
            PavingStrip(mesh,outer,inner,stone*.74f,faceTint);
            PavingGrid(mesh,iu,iv,stone,faceTint);
            // Sparse flush corner repairs, not cracks or raised rubble. They use
            // continuous paving UVs and stay entirely within the individual flag.
            if(s.Zone!=1&&grain>.86f)
            {
                float size=Mathf.Min(.24f,Mathf.Min(w,h)*.22f);
                Vector3 corner=Project(u0+bevel,v0+bevel),b=Project(u0+bevel,v0+bevel+size);
                int first=mesh.C.Count;
                LitFloorQuad(mesh,corner,Project(u0+bevel+size,v0+bevel),b,b,stone*.84f);
                for(int i=first;i<mesh.C.Count;i++)mesh.C[i]*=tint;
            }
        }

        // The stone's light lattice: whole units, halved where the 1-unit interpolation of light (FloorLight on us x vs)
        // misses FloorLight by over .04 in any channel at a cell centre - the steep rims of the pit and boiler pools and
        // the apron's shoulder courses (some 80 of the 454 stones), ~3/255 on screen.
        private static float AshlarLightStep(float[] us,float[] vs,Color[] light)
        {
            for(int l=0;l+1<vs.Length;l++)
                for(int k=0;k+1<us.Length;k++)
                {
                    int a=l*us.Length+k;
                    Color miss=(light[a]+light[a+1]+light[a+us.Length]+light[a+us.Length+1])*.25f-
                        FloorLight((us[k]+us[k+1])*.5f,(vs[l]+vs[l+1])*.5f);
                    if(Mathf.Max(Mathf.Abs(miss.r),Mathf.Max(Mathf.Abs(miss.g),Mathf.Abs(miss.b)))>.04f)return .5f;
                }
            return 1;
        }

        // a, b and every multiple of step strictly between them.
        private static float[] LightLines(float a,float b,float step)
        {
            int k0=Mathf.FloorToInt((a+1e-4f)/step)+1,k1=Mathf.CeilToInt((b-1e-4f)/step)-1;
            var lines=new float[Mathf.Max(0,k1-k0+1)+2];
            lines[0]=a;lines[lines.Length-1]=b;
            for(int k=k0;k<=k1;k++)lines[k-k0+1]=k*step;
            return lines;
        }

        // A shared-vertex grid of flush quads over us x vs, wound like Builder.Quad's (u0,v0)-(u1,v0)-(u1,v1)-(u0,v1);
        // light, when given, is FloorLight at its points, row by row.
        private static void PavingGrid(Builder mesh,float[] us,float[] vs,float tone,Color tint,Color[] light=null)
        {
            int first=mesh.V.Count,n=us.Length;
            for(int l=0;l<vs.Length;l++)
                for(int k=0;k<n;k++)PavingVertex(mesh,us[k],vs[l],tone,tint,light!=null?light[l*n+k]:FloorLight(us[k],vs[l]));
            for(int l=0;l+1<vs.Length;l++)
                for(int k=0;k+1<n;k++)
                {
                    int a=first+l*n+k;
                    mesh.T.Add(a);mesh.T.Add(a+1);mesh.T.Add(a+n+1);mesh.T.Add(a);mesh.T.Add(a+n+1);mesh.T.Add(a+n);
                }
        }

        // A shared-vertex strip of flush quads between matching outer and inner points.
        private static void PavingStrip(Builder mesh,List<Vector2> outer,List<Vector2> inner,float tone,Color tint)
        {
            int first=mesh.V.Count;
            for(int k=0;k<outer.Count;k++)
            {
                PavingVertex(mesh,outer[k].x,outer[k].y,tone,tint,FloorLight(outer[k].x,outer[k].y));
                PavingVertex(mesh,inner[k].x,inner[k].y,tone,tint,FloorLight(inner[k].x,inner[k].y));
            }
            for(int k=0;k+1<outer.Count;k++)
            {
                int a=first+2*k;
                mesh.T.Add(a);mesh.T.Add(a+2);mesh.T.Add(a+3);mesh.T.Add(a);mesh.T.Add(a+3);mesh.T.Add(a+1);
            }
        }

        // One flush paving vertex: Builder.Quad's world-continuous floor UV, lit as LitFloorQuad lights its corners
        // (light: FloorLight at (u, v)).
        private static void PavingVertex(Builder mesh,float u,float v,float tone,Color tint,Color light)
        {
            Vector3 p=Project(u,v);
            mesh.V.Add(p);
            mesh.U.Add(new Vector2(1+(p.x+p.y*2)/8.55f,1+(-p.x+p.y*2)/8.55f));
            mesh.C.Add(new Color(light.r*tone*tint.r,light.g*tone*tint.g,light.b*tone*tint.b,1));
        }

        private static void BuildCourtStoneRing(Builder mesh)
        {
            // A flush annulus inside the existing 8x8 court inset. All corners
            // remain within +/-4, clear of the shaft. The engraved seal renders above it.
            const int segments=24;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                CourtRingFlag(mesh,a,b,3.26f,3.97f,.64f);
                float tone=(i%3==0?1.13f:1.07f);
                CourtRingFlag(mesh,a+.004f,b-.004f,3.285f,3.945f,tone);
                // A narrow outer dressed edge catches ambient light but never glows.
                CourtRingFlag(mesh,a+.004f,b-.004f,3.91f,3.945f,tone*1.045f);
            }
        }

        private static void CourtRingFlag(Builder mesh,float a,float b,float inner,float outer,float tone)
        {
            LitFloorQuad(mesh,Project(Mathf.Cos(a)*inner,Mathf.Sin(a)*inner),
                Project(Mathf.Cos(b)*inner,Mathf.Sin(b)*inner),
                Project(Mathf.Cos(b)*outer,Mathf.Sin(b)*outer),
                Project(Mathf.Cos(a)*outer,Mathf.Sin(a)*outer),tone);
        }

        private static void LitFloorQuad(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float stone)
        {
            int start=mesh.V.Count;
            mesh.Quad(a,b,c,d,true,Color.white);
            for(int i=start;i<start+4;i++)
            {
                Vector3 p=mesh.V[i];
                FoundryMap.ToDesign(p.x,p.y*2,out float u,out float v);
                Color color=FloorLight(u,v)*stone;color.a=1;
                mesh.C[i]=color;
            }
        }

        private static void BevelCap(Builder mesh,float u0,float v0,float u1,float v1,float rise)
        {
            float bevel=Mathf.Min(.12f,Mathf.Min(u1-u0,v1-v0)*.22f);
            Vector3 lift=Vector3.up*rise;
            var outer=new[]{Project(u0,v0),Project(u1,v0),Project(u1,v1),Project(u0,v1)};
            var inner=new[]{Project(u0+bevel,v0+bevel),Project(u1-bevel,v0+bevel),
                Project(u1-bevel,v1-bevel),Project(u0+bevel,v1-bevel)};
            Color cap=new Color(.88f,.91f,1);
            mesh.Quad(inner[0]+lift,inner[1]+lift,inner[2]+lift,inner[3]+lift,true,cap);
            for(int i=0;i<4;i++)
            {
                int next=(i+1)%4;
                Color rim=i==0||i==3?new Color(1.18f,1.16f,1.13f):new Color(.7f,.74f,.85f);
                mesh.Quad(outer[i]+lift,outer[next]+lift,inner[next]+lift,inner[i]+lift,true,rim);
            }
        }
    }
}
