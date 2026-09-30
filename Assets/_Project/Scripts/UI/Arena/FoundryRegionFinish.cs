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
        // lanterns, braziers and beacons are weaker point sources. The floor is sampled only at flag corners (court
        // flags 2x1, apron 1x2, seal 2x2, bridge every 1 s / 2 d), so no falloff radius is below 3.4: neighbouring rows
        // of the staggered bond then interpolate within ~11/255 in R-B of each other (the old apron pools showed ~14),
        // a soft gradient rather than a lit disc or a pool that steps flag by flag.
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

        private void BuildPavingCell(Builder mesh,Vector2Int cell)
        {
            int zone=PavingZone(cell),x=cell.x,y=cell.y;
            // The seal's 8x8 inset consists of sixteen uninterrupted 2x2 flags.
            if(zone==1)
            {
                if((x&1)==0&&(y&1)==0)Paving(mesh,x,y,x+2,y+2,zone);
                return;
            }
            if(zone==3){Paving(mesh,x,y,x+1,y+1,zone);return;}
            bool crosswise=zone==2;
            Vector2Int step=crosswise?Vector2Int.up:Vector2Int.right;
            int along=crosswise?y:x,across=crosswise?x:y;
            bool starts=((along+(across&1))&1)==0;
            bool next=_cells.Contains(cell+step)&&PavingZone(cell+step)==zone;
            bool previous=_cells.Contains(cell-step)&&PavingZone(cell-step)==zone;
            // Clip the pairing at each material boundary, including a half flag at
            // staggered row ends. Every existing cell stays covered exactly once.
            if(starts||!previous)
            {
                int length=starts&&next?2:1;
                Paving(mesh,x,y,x+(crosswise?1:length),y+(crosswise?length:1),zone);
            }
        }

        private static void Paving(Builder mesh,float u0,float v0,float u1,float v1,int zone=0)
        {
            int first=mesh.C.Count;
            // Deterministic per-flag variation, strong enough to read one flag from the next at the portrait camera:
            // most flags sit near the base tone, about one in six is a darker replacement stone and one in ten a
            // paler, warmer one, the way the reference's courtyard is mottled. Still flush colour only - no grit or
            // floor geometry that could be mistaken for a collision obstacle.
            float grain=StoneSample((int)u0,(int)v0);
            float stone=grain<.17f?Mathf.Lerp(.78f,.87f,grain/.17f):
                grain>.9f?Mathf.Lerp(1.08f,1.15f,(grain-.9f)/.1f):Mathf.Lerp(.93f,1.04f,(grain-.17f)/.73f);
            float hue=StoneSample((int)u0+131,(int)v0-57);
            Color flagTint=hue<.22f?new Color(.97f,.99f,1.03f):hue>.82f?new Color(1.05f,1.0f,.93f):Color.white;
            // A darker, slightly wider joint and a stronger lit/shaded bevel, so each flag reads as a laid stone.
            const float joint=.028f,bevel=.06f;
            LitFloorQuad(mesh,Project(u0,v0),Project(u1,v0),Project(u1,v1),Project(u0,v1),.5f);
            var outer=new[]{Project(u0+joint,v0+joint),Project(u1-joint,v0+joint),
                Project(u1-joint,v1-joint),Project(u0+joint,v1-joint)};
            var inner=new[]{Project(u0+bevel,v0+bevel),Project(u1-bevel,v0+bevel),
                Project(u1-bevel,v1-bevel),Project(u0+bevel,v1-bevel)};
            int flag=mesh.C.Count;
            LitFloorQuad(mesh,inner[0],inner[1],inner[2],inner[3],stone);
            for(int edge=0;edge<4;edge++)
                LitFloorQuad(mesh,outer[edge],outer[(edge+1)%4],inner[(edge+1)%4],inner[edge],
                    stone*(edge<2?1.1f:.74f));
            for(int i=flag;i<mesh.C.Count;i++)mesh.C[i]*=flagTint;
            // Sparse flush corner repairs, not cracks or raised rubble. They use
            // continuous paving UVs and stay entirely within the individual flag.
            if(zone!=1&&grain>.86f)
            {
                float size=Mathf.Min(.24f,Mathf.Min(u1-u0,v1-v0)*.22f);
                Vector3 corner=Project(u0+bevel,v0+bevel);
                Vector3 a=Project(u0+bevel+size,v0+bevel);
                Vector3 b=Project(u0+bevel,v0+bevel+size);
                LitFloorQuad(mesh,corner,a,b,b,stone*.84f);
            }
            // The gate apron's stone is a shade redder than the court's (hue ~281 against ~263 under the open light;
            // the concept's apron is ~287); its former blue cut (.93) turned it magenta-grey under the violet ambient.
            Color tint=zone==1?new Color(1.08f,1.065f,1.015f):
                zone==2?new Color(1.01f,.985f,.96f):
                zone==3?new Color(.82f,.84f,.88f):Color.white;
            for(int i=first;i<mesh.C.Count;i++)mesh.C[i]*=tint;
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
