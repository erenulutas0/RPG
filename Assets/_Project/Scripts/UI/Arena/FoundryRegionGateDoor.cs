using UnityEngine;

namespace Cryptforge.UI
{
    internal readonly struct FoundryDoorVertex
    {
        internal readonly int Index;
        // The jamb the vertex's leaf hangs on (design v), its signed distance from that hinge along the wall (toward the
        // seam) and its height above the landing.
        internal readonly float Hinge, Reach, Height;
        internal FoundryDoorVertex(int index,float hinge,float reach,float height){Index=index;Hinge=hinge;Reach=reach;Height=height;}
    }

    public sealed partial class FoundryRegionView
    {
        // The painted door's two leaves (GateFacade texture x .3385-.5 and .5-.6641, y .0303-.8828), re-laid over the
        // painting exactly, so nothing changes until the floor's boss falls. They then swing outward onto the landing on
        // their jamb hinges (FoundryGateDoorMotion), framing the gate exit, over a doorway lit from the forge hall beyond.
        // Outward is also the one way the leaves can draw over the facade without passing through the wall. Same
        // material, rank and depth structure as the facade, so the inner piers and every actor keep their order.
        internal const float GateDoorLeftU=.3385f,GateDoorSeamU=.5f,GateDoorRightU=.6641f,GateDoorBottomV=.0303f,GateDoorTopV=.8828f;
        private static float GateDoorWallV(float textureX)=>11f-12f*textureX;
        // The facade's wall plane (design u), the leaves' shut position and the hinge line they swing from.
        internal static float GateDoorFaceU=>GateU(31.99f);
        // SceneryOrder maps this to the facade's (-12) sorting order, but as a separate batch key.
        private const float GateDoorOrder=-11.75f;

        private void BuildGateDoorway(Material facade,float height)
        {
            float face=GateDoorFaceU;
            float left=GateDoorWallV(GateDoorLeftU),seam=GateDoorWallV(GateDoorSeamU),right=GateDoorWallV(GateDoorRightU);
            float bottom=GateDoorBottomV*height,top=GateDoorTopV*height;
            var door=new Builder();
            // The doorway: one flat patch of the door's brass bar, tinted from a low forge glow to darkness above.
            var dark=new Vector2(.4844f,.789f);
            Vector3 a=Project(face,left)+Vector3.up*bottom,b=Project(face,right)+Vector3.up*bottom;
            Vector3 c=Project(face,right)+Vector3.up*top,d=Project(face,left)+Vector3.up*top;
            int first=door.V.Count;
            door.V.AddRange(new[]{a,b,c,d});door.U.AddRange(new[]{dark,dark,dark,dark});
            Color glow=new Color(1.15f,.66f,.42f,1),shadow=new Color(.05f,.04f,.05f,1);
            door.C.AddRange(new[]{glow,glow,shadow,shadow});
            door.T.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            // Two leaves, each on its own jamb: left (wall v left..seam) and right (seam..right).
            DoorLeaf(door,face,left,seam,GateDoorLeftU,GateDoorSeamU,bottom,top);
            DoorLeaf(door,face,right,seam,GateDoorRightU,GateDoorSeamU,bottom,top);
            // Its own batch, so no other motion (the facade batch's flames) ever rewrites the leaves' vertices; it shares
            // the facade's sorting order and sorts just after it (FoundryRegionView.Emit).
            Create("Gate door leaves",door,facade,GateDoorOrder);
        }

        private static void DoorLeaf(Builder door,float face,float hinge,float seam,float hingeX,float seamX,float bottom,float top)
        {
            int first=door.V.Count;
            float reach=seam-hinge;
            foreach(var (along,height,x,y) in new[]{(0f,bottom,hingeX,GateDoorBottomV),(reach,bottom,seamX,GateDoorBottomV),
                (reach,top,seamX,GateDoorTopV),(0f,top,hingeX,GateDoorTopV)})
            {
                door.Door.Add(new FoundryDoorVertex(door.V.Count,hinge,along,height));
                door.V.Add(Project(face,hinge+along)+Vector3.up*height);
                door.U.Add(new Vector2(x,y));door.C.Add(Color.white);
            }
            door.T.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
        }
    }
}
