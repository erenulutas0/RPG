using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    // Hanging masonry below the platform, after the approved concept: the court does not end in a level skirt but in
    // clusters of stepped, inverted masses that hang on into the cavern's haze, so the platform reads as a floating
    // fortress rather than a slab. Visual only; nothing here is walkable or an actor occluder. The keels open the
    // below-deck masonry batch (-16): the curtain, arches, buttresses, bastions, bridge bents and shelf foundations,
    // all in front of them, paint over them.
    public sealed partial class FoundryRegionView
    {
        // Under each camera-facing foundation span, keels hang from just inside the curtain skirt's foot (KeelTop, the
        // skirt ends 7.25 down) in two or three steps 1.1-2.6 deep, each narrowed from the one above and mostly set back
        // under it, down to ~15 below the deck.
        private const float KeelTop=7.1f;
        // Toward the cavern haze: the vertex tone a keel's face reaches at depth KeelHazeDepth (the haze over the painted
        // void below the deck is ~(.21, .17, .28) on the masonry's ~.45 albedo).
        private static readonly Color KeelHaze=new Color(.46f,.38f,.62f);
        private const float KeelHazeDepth=16f;

        private void BuildHangingKeels(ArenaView arena)
        {
            var stone=new Builder();
            BuildHangingKeelGeometry(stone);
            Create("Hanging keel masonry",stone,arena.PlatformMaterials.FoundryWall,-16);
        }

        // layout, when given, receives every vertical face's deck points and its upper and lower depth below the deck.
        private void BuildHangingKeelGeometry(Builder stone,List<(Vector2 p,Vector2 q,float upper,float lower)> layout=null)
        {
            var faces=new List<(float depth,System.Action<Builder> emit)>();
            foreach(var span in FoundationSpans())
            {
                // Not under the gate causeway, where the boilers' foundation drums and the gate's foundations stand
                // behind the edge and, drawn after the keels, would paint over them.
                if(span.a.x>Cryptforge.Art.FoundryMap.East-.01f&&span.b.x>Cryptforge.Art.FoundryMap.East-.01f)continue;
                Vector2 along=span.b-span.a;
                float length=along.magnitude;
                if(length<1.6f)continue;
                Vector2 direction=along/length;
                // Outward is the camera-facing side of the span (toward -u, -v).
                Vector2 outward=new Vector2(-direction.y,direction.x);
                if(outward.x+outward.y>0)outward=-outward;
                // A chamfer seen edge-on has no face to hang from.
                if(outward.x+outward.y>-.1f)continue;
                // The skirt's foot: the span contracted toward the same centre the curtain uses.
                Vector2 centre=new Vector2(-8,2);
                Vector2 footA=Vector2.Lerp(span.a,centre,.035f),footB=Vector2.Lerp(span.b,centre,.035f);
                int seed=Mathf.RoundToInt(span.a.x*7+span.a.y*13+span.b.x*3);
                float t=.3f+KeelHash(seed,0)*.6f;
                for(int keel=1;t<length-.9f;keel++)
                {
                    float width=Mathf.Min(1.4f+KeelHash(seed,keel*5)*1.8f,length-.3f-t);
                    if(width>=.9f)AddKeel(faces,layout,footA,footB,length,outward,t,t+width,seed+keel*17);
                    // Clusters: about half the keels butt their neighbour, the rest leave a gap of haze.
                    t+=width+(KeelHash(seed,keel*5+1)<.45f?0:.25f+KeelHash(seed,keel*5+2)*.9f);
                }
            }
            // Far to near, so a nearer keel or step paints over the end and front of one behind it.
            faces.Sort((x,y)=>y.depth.CompareTo(x.depth));
            foreach(var face in faces)face.emit(stone);
        }

        private static void AddKeel(List<(float depth,System.Action<Builder> emit)> faces,
            List<(Vector2 p,Vector2 q,float upper,float lower)> layout,Vector2 footA,Vector2 footB,float length,Vector2 outward,
            float t0,float t1,int seed)
        {
            Vector2 direction=(footB-footA)/length,inward=-outward;
            // Only the drop toward -u, -v is in view: a face toward -v is lit, one toward -u shaded, as the bastions' blocks.
            Color front=KeelFacing(outward);
            int steps=2+Mathf.FloorToInt(KeelHash(seed,1)*1.999f);
            float top=KeelTop,inset=0,shrink=0;
            Vector2 aboveP=Vector2.zero,aboveQ=Vector2.zero;
            for(int step=0;step<steps;step++)
            {
                float height=1.1f+KeelHash(seed,step*3+2)*1.5f;
                float a=t0+shrink,b=t1-shrink;
                if(b-a<.55f)break;
                Vector2 p=footA+direction*a+inward*inset,q=footA+direction*b+inward*inset;
                float upper=top,lower=top+height;
                Color tone=Tinted(front,new Color(1-.04f*step,1-.04f*step,1-.04f*step));
                faces.Add((p.x+p.y+q.x+q.y,mesh=>KeelFace(mesh,p,q,upper,lower,tone)));
                layout?.Add((p,q,upper,lower));
                // A step that stands proud of the one above shows its top as a lit ledge, from the upper step's front out
                // to its own over its length.
                if(step>0&&Vector2.Dot(p-aboveP,outward)>.01f)
                {
                    Vector2 backP=aboveP+direction*Vector2.Dot(p-aboveP,direction),backQ=aboveP+direction*Vector2.Dot(q-aboveP,direction);
                    faces.Add((p.x+p.y+q.x+q.y+.01f,mesh=>KeelLedge(mesh,backP,backQ,q,p,upper)));
                }
                // The end that turns to the camera, back into the mass under the deck.
                foreach(bool start in new[]{true,false})
                {
                    Vector2 end=start?p:q,away=start?-direction:direction;
                    if(away.x+away.y>=-.01f)continue;
                    Vector2 back=end+inward*1.1f;
                    Color side=Tinted(KeelFacing(away),new Color(.94f-.04f*step,.94f-.04f*step,.94f-.04f*step));
                    faces.Add(((end.x+end.y)*2+inward.x+inward.y,mesh=>KeelFace(mesh,start?back:end,start?end:back,upper,lower,side)));
                    layout?.Add((end,back,upper,lower));
                }
                top=lower;aboveP=p;aboveQ=q;
                // Mostly each step is set back under the one above; about one in three stands proud of it instead.
                inset+=KeelHash(seed,step*3+3)<.35f?-(.18f+KeelHash(seed,step*3+5)*.27f):.25f+KeelHash(seed,step*3+5)*.35f;
                shrink+=.15f+KeelHash(seed,step*3+4)*.3f;
            }
        }

        private static Color KeelFacing(Vector2 normal)=>Tinted(Mathf.Abs(normal.y)>Mathf.Abs(normal.x)?LitFace:ShadeFace,
            new Color(1.02f,1.02f,1.02f));

        // A keel's lit ledge: the flat top of a proud step, depth below the deck, in the bastions' cap stone.
        private static void KeelLedge(Builder mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,float depth)
        {
            Vector3 down=Vector3.down*depth;
            int first=mesh.V.Count;
            mesh.Quad(Project(a.x,a.y)+down,Project(b.x,b.y)+down,Project(c.x,c.y)+down,Project(d.x,d.y)+down,true,
                Tinted(CapTone,new Color(.82f,.82f,.82f)));
            for(int i=first;i<mesh.V.Count;i++)mesh.C[i]=KeelDepthTone(mesh.C[i],depth);
        }

        // A vertical face from deck points p to q, upper to lower below the deck, in the bastions' weathered masonry (its
        // courses running on from face to face), its tone falling into shadow and then the cavern haze with depth.
        private static void KeelFace(Builder mesh,Vector2 p,Vector2 q,float upper,float lower,Color tone)
        {
            int first=mesh.V.Count;
            WeatheredBoxFace(mesh,p,q,-lower,-upper,tone);
            Vector3 a=Project(p.x,p.y),b=Project(q.x,q.y);
            for(int i=first;i<mesh.V.Count;i++)
            {
                Vector3 v=mesh.V[i];
                float s=Mathf.Abs(b.x-a.x)>.001f?Mathf.Clamp01((v.x-a.x)/(b.x-a.x)):0;
                mesh.C[i]=KeelDepthTone(mesh.C[i],Mathf.Lerp(a.y,b.y,s)-v.y);
            }
        }

        // Into shadow below the deck, then into the cavern haze.
        private static Color KeelDepthTone(Color c,float depth)
        {
            float shade=Mathf.Lerp(1,.74f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(3f,11f,depth)));
            c=new Color(c.r*shade*.97f,c.g*shade,c.b*shade*1.03f,1);
            return Color.Lerp(c,KeelHaze,.55f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(9f,KeelHazeDepth,depth)));
        }

        private static float KeelHash(int seed,int salt)
        {
            unchecked
            {
                uint h=(uint)seed*2654435761u^(uint)salt*2246822519u^0x9E3779B9u;
                h^=h>>15;h*=2246822519u;h^=h>>13;h*=3266489917u;h^=h>>16;
                return (h&16777215u)/16777216f;
            }
        }
    }
}
