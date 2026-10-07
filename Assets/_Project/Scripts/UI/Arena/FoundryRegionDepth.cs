using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    // Depth ordering between the batched structures and the actors, without extra renderers.
    // Every above-floor vertex belongs to a structure footprint (u0,v0)-(u1,v1). Its depth is the screen height of
    // the footprint's left-right diagonal at the vertex's column: for an actor standing outside the footprint, any
    // separator inside it orders the pair the same way, and the diagonal is linear in x, so one value per vertex
    // interpolates exactly. The value goes into vertex z (FoundryDepth), the cutaway shader writes it in a depth-only
    // pass, and actors (whose sprite z follows their feet) are depth-tested against it by the default sprite shader.
    // Vertices find their structure, in order: an explicit footprint (Block), the enclosing DepthStructure scope, or the
    // landmark whose volume holds their quad. Anything else writes no depth and draws under actors, as before.
    public sealed partial class FoundryRegionView
    {
        private sealed class DepthFootprint
        {
            internal Vector3 Left,Front,Right,Back;
            internal float Height;
            internal bool Fade;
            // Screen height of the left-right diagonal; clamped beyond the footprint for small overhangs.
            internal float DepthAt(float x) => Mathf.Lerp(Left.y,Right.y,Mathf.InverseLerp(Left.x,Right.x,x));
            // The projected volume: the footprint's column span, from its front edge to its rear edge plus height.
            internal bool Holds(Vector3 p)
            {
                const float margin=.2f;
                if(p.x<Left.x-margin||p.x>Right.x+margin)return false;
                float x=Mathf.Clamp(p.x,Left.x,Right.x);
                float front=x<=Front.x?Mathf.Lerp(Left.y,Front.y,Mathf.InverseLerp(Left.x,Front.x,x))
                    :Mathf.Lerp(Front.y,Right.y,Mathf.InverseLerp(Front.x,Right.x,x));
                float back=x<=Back.x?Mathf.Lerp(Left.y,Back.y,Mathf.InverseLerp(Left.x,Back.x,x))
                    :Mathf.Lerp(Back.y,Right.y,Mathf.InverseLerp(Back.x,Right.x,x));
                return p.y>=front-margin&&p.y<=back+Height+margin;
            }
        }
        // Restores the previous scope on Dispose, so scopes nest.
        private readonly struct DepthScope:System.IDisposable
        {
            private readonly FoundryRegionView _view;
            private readonly int _previous;
            internal DepthScope(FoundryRegionView view,int footprint){_view=view;_previous=view._depthScope;view._depthScope=footprint;}
            public void Dispose(){if(_view!=null)_view._depthScope=_previous;}
        }
        private readonly List<DepthFootprint> _depthFootprints=new List<DepthFootprint>();
        private readonly List<int> _depthLandmarks=new List<int>();
        private int _depthScope=-1;
        private bool DepthScopeFades=>_depthScope>=0&&_depthFootprints[_depthScope].Fade;

        // Everything created inside `using(DepthStructure(...))` takes this footprint's depth; its volume also claims
        // unscoped detail quads emitted elsewhere. Structures of height 1 or more fade in front of the hero.
        private DepthScope DepthStructure(float u0,float v0,float u1,float v1,float height)=>
            new DepthScope(this,RegisterDepthFootprint(u0,v0,u1,v1,height,height>=1f,true));
        // A structure whose parts are built elsewhere, without a scope: its volume claims their quads at Emit.
        private int DepthLandmark(float u0,float v0,float u1,float v1,float height)=>RegisterDepthFootprint(u0,v0,u1,v1,height,height>=1f,true);
        private int RegisterDepthFootprint(float u0,float v0,float u1,float v1,float height,bool fade,bool landmark)
        {
            float a0=Mathf.Min(u0,u1),a1=Mathf.Max(u0,u1),b0=Mathf.Min(v0,v1),b1=Mathf.Max(v0,v1);
            _depthFootprints.Add(new DepthFootprint{Left=Project(a0,b1),Front=Project(a0,b0),Right=Project(a1,b0),Back=Project(a1,b1),
                Height=Mathf.Max(0,height),Fade=fade});
            int index=_depthFootprints.Count-1;
            if(landmark)_depthLandmarks.Add(index);
            return index;
        }
        // Occluders whose parts are built in other partial files without a scope (dressing, site finish, workshop).
        private void RegisterDepthLandmarks()
        {
            DepthLandmark(-21,11,-18,15,8.6f); // Crane footing, mast, collar and the jib root.
            foreach(float v in new[]{15.8f,-5.8f})DepthLandmark(21.3f,v-1,23.3f,v+1,3.7f); // Pressure reservoirs.
            foreach(float v in new[]{-4.5f,14.5f})DepthLandmark(19.7f,v-.4f,25.1f,v+.4f,2.4f); // Service mains.
            foreach(var p in new[]{new Vector2(-18.5f,-6),new Vector2(-18.5f,12),new Vector2(-9,-14.5f),
                new Vector2(2,-14.5f),new Vector2(10,-14.5f),new Vector2(-14,18.5f),new Vector2(0,18.5f),new Vector2(10,18.5f)})
                DepthLandmark(p.x-.4f,p.y-.4f,p.x+.4f,p.y+.4f,2.85f); // Outer posts and court lanterns.
            foreach(var p in BridgeFootPosts)
                DepthLandmark(p.x-.32f,p.y-.32f,p.x+.32f,p.y+.32f,3.2f); // Bridge piers with their beacons.
            foreach(var p in BridgeHeadPosts)DepthLandmark(p.x-.7f,p.y-.7f,p.x+.7f,p.y+.7f,4.3f);
            DepthLandmark(-23f,5,-18.45f,8.4f,1.6f); // Salvage shelf with its anvil, crate and stock.
            DepthLandmark(2,18.5f,7,22.4f,1.6f); // Pipe shelf with its basin, rack and stored pipes.
        }
        // The frontmost landmark whose volume holds the point; -1 when none does.
        private int ResolveDepthOwner(Vector3 point)
        {
            int best=-1;float nearest=float.MaxValue;
            foreach(int index in _depthLandmarks)
            {
                var footprint=_depthFootprints[index];
                if(!footprint.Holds(point))continue;
                float depth=footprint.DepthAt(point.x);
                if(depth<nearest){nearest=depth;best=index;}
            }
            return best;
        }
        // Writes structure depth into vertex z and returns the per-vertex flags (x: writes depth, y: may fade).
        // Owners shorter than the vertex list (geometry appended to a batch directly) resolve like unscoped quads.
        private List<Vector2> ApplyStructureDepth(Builder b,List<int> owners)
        {
            int count=b.V.Count;
            var flags=new List<Vector2>(count);
            var owner=new int[count];
            var parent=new int[count];
            for(int i=0;i<count;i++){owner[i]=owners!=null&&i<owners.Count?owners[i]:-1;parent[i]=i;}
            // Unscoped vertices resolve per connected piece (a quad, usually), so a face never straddles two structures.
            for(int t=0;t+2<b.T.Count;t+=3){DepthJoin(parent,b.T[t],b.T[t+1]);DepthJoin(parent,b.T[t],b.T[t+2]);}
            var sum=new Vector3[count];var members=new int[count];var resolved=new int[count];
            for(int i=0;i<count;i++)
            {
                resolved[i]=-2;
                if(owner[i]>=0)continue;
                int root=DepthRoot(parent,i);sum[root]+=b.V[i];members[root]++;
            }
            for(int i=0;i<count;i++)
            {
                if(owner[i]<0)
                {
                    int root=DepthRoot(parent,i);
                    if(resolved[root]==-2)resolved[root]=ResolveDepthOwner(sum[root]/members[root]);
                    owner[i]=resolved[root];
                }
                Vector3 p=b.V[i];
                if(owner[i]>=0&&owner[i]<_depthFootprints.Count)
                {
                    var footprint=_depthFootprints[owner[i]];
                    b.V[i]=new Vector3(p.x,p.y,FoundryDepth.Z(footprint.DepthAt(p.x)));
                    flags.Add(new Vector2(1,footprint.Fade?1:0));
                }
                else
                {
                    b.V[i]=new Vector3(p.x,p.y,FoundryDepth.Base);
                    flags.Add(Vector2.zero);
                }
            }
            return flags;
        }
        private static int DepthRoot(int[] parent,int i)
        {
            int root=i;while(parent[root]!=root)root=parent[root];
            while(parent[i]!=root){int next=parent[i];parent[i]=root;i=next;}
            return root;
        }
        private static void DepthJoin(int[] parent,int a,int b)
        {
            if(a<0||b<0||a>=parent.Length||b>=parent.Length)return;
            int x=DepthRoot(parent,a),y=DepthRoot(parent,b);
            if(x!=y)parent[y]=x;
        }
    }
}
