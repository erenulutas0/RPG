using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only: observe displacement and cooldown; never tick a weapon, move a hero or apply damage.
    internal sealed class VanguardAnimator
    {
        private readonly VanguardArtSet _art;
        private readonly SpriteRenderer _body;
        private readonly SpriteRenderer[] _parts;
        private readonly Transform _root;
        private readonly AttackController _attack;
        private readonly Targeting _targeting;
        private readonly Health _health;
        private readonly Sprite _orb;
        private readonly Sprite _litOrb;
        private readonly SpriteRenderer _grip;
        private readonly Transform _pose;
        private Vector3 _previous;
        private float _distance;
        private float _remaining;
        private float _duration;
        private int _loadout;
        private bool _moving;
        // The weight's state: how much of the stride's motion shows, the stride phase it follows, the unswayed lean in
        // degrees, the stop's settle time left and the stride weight it settles from, and the breath clock.
        private float _gait;
        private float _phase;
        private float _lean;
        private float _settle;
        private float _settleWeight;
        private float _breath;
        private bool _striding;
        // A full four-pose cycle: shorter contacts reduce sliding without changing movement speed.
        private const float Stride = .8f;

        // The weight the four painted poses lack (docs/45 section 4). It moves a presentation transform between the actor
        // and its body, never the body: CombatantView owns the body's local position, the hand anchors and the grip stay
        // body-relative so every weapon rides along, and the contact shadow, a child of the actor, stays on the floor.
        // The transform's origin is the boots, so the lean and the squash pivot where the figure meets the floor.
        // Heights are world units or shares of the 176-texel (1.375 unit) figure, which stands about 80 px tall on a phone;
        // each is sized to read as weight there without turning the walk into a bounce.
        internal const string PoseName = "Hero Pose";
        // The passing poses ride this high above the contacts (4 texels, about 2 px on the phone): a step that reads as
        // weight, not a hop; at the contacts the boots are exactly on the shadow.
        internal const float BobHeight = .03f;
        // Each contact shortens the figure this share and each passing stretches it as much; the width moves half as
        // far the other way so the shield and the sword barely shift. About 1 px on the phone.
        internal const float StepSquash = .015f;
        // The lean into the travel direction, about the boots: the helmet leads by about 9 texels (4 px). It follows the
        // painted facing, so the figure never leans back against the way its sprite looks.
        internal const float LeanDegrees = 3f;
        // The lean deepens by this share on each contact and eases as much on each passing: the torso dips forward as the
        // weight lands and rises upright as it passes over the boot.
        internal const float LeanSway = .2f;
        // Degrees a second: a full lean builds or clears in about 0.12 s, so a turn rolls the figure over, never snaps it.
        internal const float LeanRate = 25f;
        // The stride's bob and squash grow in over this first part of a walk, so the first step does not pop.
        internal const float GaitRise = .1f;
        // On stopping, the stride's motion fades over this time while the figure dips this share and recovers: the weight
        // lands instead of the idle pose freezing mid-bob.
        internal const float SettleDuration = .18f;
        internal const float SettleSquash = .025f;
        // Standing, the figure rises this share (2 texels, about 1 px) and falls over this period: slow and small enough
        // to read as breath, not bounce. The width stays, so only the height breathes.
        internal const float BreathStretch = .012f;
        internal const float BreathPeriod = 2.6f;
        public int FrameIndex { get; private set; } = -1;
        public bool IsAttacking => _remaining > 0;
        public bool FrontFacing { get; private set; }
        public bool Mirrored { get; private set; }

        public VanguardAnimator(VanguardArtSet art, SpriteRenderer body, SpriteRenderer[] parts,
            Transform root, AttackController attack, Sprite litOrb)
        {
            _art=art; _body=body; _parts=parts; _root=root; _attack=attack;
            _targeting=root.GetComponent<Targeting>(); _health=root.GetComponent<Health>();
            _previous=root.position; _orb=parts[3].sprite; _litOrb=litOrb;
            _parts[0].sprite=art.Sword; _parts[1].sprite=art.Shield;
            if (art.Dagger != null) _parts[4].sprite=_parts[5].sprite=art.Dagger;
            if (art.Staff != null)
            {
                _parts[2].sprite=art.Staff;
                // The imported staff includes its crystal; retain the fallback renderer but never draw it twice.
                _parts[3].enabled=false;
            }
            var grip = new GameObject("Sword Grip");
            grip.transform.SetParent(body.transform, false);
            _grip = grip.AddComponent<SpriteRenderer>();
            _grip.sharedMaterial = body.sharedMaterial;
            _grip.sortingLayerID = body.sortingLayerID;
            _pose = PoseOf(body.transform);
            Show(0);
        }

        // Inserts the weight's transform above the body at the body's old place, so nothing moves on screen and the body's
        // local position, which CombatantView reads as its rest, is unchanged. A clone that already has one reuses it.
        private static Transform PoseOf(Transform body)
        {
            Transform parent = body.parent;
            if (parent != null && parent.name == PoseName) return parent;
            var pose = new GameObject(PoseName).transform;
            pose.SetParent(parent, false);
            pose.SetSiblingIndex(body.GetSiblingIndex());
            body.SetParent(pose, false);
            return pose;
        }

        public void Reset()
        {
            _remaining=0; _distance=0; _moving=false; _previous=_root.position;
            _gait=_lean=_settle=_settleWeight=_breath=0; _striding=false;
            ResetWeapons(); Show(0); ApplyWeight();
        }

        public void Attacked()
        {
            _loadout=_parts[2].gameObject.activeInHierarchy?1:_parts[4].gameObject.activeInHierarchy?2:0;
            _duration=Mathf.Min(.20f,(_attack.Weapon?.Interval ?? 1f)*.65f);
            _remaining=_duration;
            ResetWeapons();
            // Damage already happened in this event: enter the strike, never begin a delayed visual windup. A walking hero
            // keeps walking - only the weapon swings - so an attack never freezes the legs mid-stride (the owner,
            // 2026-10-26: with Tempo a Staff attacked every half second and the hero glided in its idle pose).
            if(_moving) ShowWalk(); else Show(_loadout==0?6:0);
            ApplyAttack(0);
        }

        public void Tick(float dt)
        {
            Vector3 delta=_root.position-_previous; _previous=_root.position;
            if(dt<=0 || (_health!=null&&!_health.IsAlive)) return;
            float travel=new Vector2(delta.x,delta.y/ArenaFloor.DepthScale).magnitude;
            _moving=travel>=.0001f;
            if(travel>0) _distance=(_distance+travel)%Stride;
            Animate(dt,delta,travel);
            // After the pose, whichever branch chose it: the weight reads this tick's stride phase and facing.
            Carry(dt,delta,travel);
        }

        // This tick's painted pose: an attack's, the sword's windup, the walk cycle's or the stance.
        private void Animate(float dt, Vector3 delta, float travel)
        {
            if(_remaining>0)
            {
                _remaining=Mathf.Max(0,_remaining-dt);
                if(_remaining>0)
                {
                    float t=1-_remaining/_duration;
                    if(_moving) ShowWalk();
                    else if(_loadout==0) Show(t<.42f?6:7);
                    ApplyAttack(t); return;
                }
                ResetWeapons();
            }
            if (_moving) Face(delta);
            // Anticipate only when the next actual sword attack is close and a valid target is in range.
            var weapon=_attack.Weapon;
            if(!_moving && _parts[0].gameObject.activeInHierarchy && weapon!=null && weapon.CooldownRemaining>0
                && weapon.CooldownRemaining<=Mathf.Min(.09f,weapon.Interval*.25f)
                && _targeting!=null && _targeting.Acquire(weapon.Range)!=null)
            {
                if (!_moving) Face(_targeting.Acquire(weapon.Range).transform.position-_root.position);
                Show(5); _parts[0].transform.localRotation=Quaternion.Euler(0,0,FrontFacing?100:20); return;
            }
            ResetWeapons();
            if(travel<.0001f){_distance=0;Show(0);}else ShowWalk();
        }

        // The walk cycle's pose for the distance travelled: four poses a Stride.
        private void ShowWalk() => Show(1+Mathf.Min(3,(int)(_distance/Stride*4)));

        // Advances the weight by one live, unpaused tick. Walking, it follows the distance-driven stride phase that picks the
        // painted pose, so it can never drift from the legs; stopping, it settles to the stance in SettleDuration.
        private void Carry(float dt, Vector3 delta, float travel)
        {
            float lean=0;
            if(_moving)
            {
                _settle=0;
                _gait=Mathf.Min(1,_gait+dt/GaitRise);
                _phase=_distance/Stride;
                // Only the screen-horizontal share of the floor direction leans: walking up or down the screen stays upright.
                lean=(Mirrored?LeanDegrees:-LeanDegrees)*Mathf.Abs(delta.x)/travel;
            }
            else
            {
                if(_striding){_settle=SettleDuration;_settleWeight=_gait;}
                _settle=Mathf.Max(0,_settle-dt);
                _gait=_settleWeight*_settle/SettleDuration;
            }
            _striding=_moving;
            _lean=Mathf.MoveTowards(_lean,lean,LeanRate*dt);
            // The breath restarts from its low point once the stride fully hides it, so it always grows in from rest.
            _breath=_gait>=1?0:(_breath+dt)%BreathPeriod;
            ApplyWeight();
        }

        private void ApplyWeight()
        {
            // +1 at a contact pose's centre and -1 at a passing pose's (two steps a Stride, frames 1-4), scaled by the gait.
            float step=_gait>0?_gait*Mathf.Cos(4*Mathf.PI*(_phase-.125f)):0;
            float land=_settle>0?SettleSquash*_settleWeight*Mathf.Sin(Mathf.PI*(1-_settle/SettleDuration)):0;
            float breath=BreathStretch*(1-_gait)*.5f*(1-Mathf.Cos(2*Mathf.PI*_breath/BreathPeriod));
            float squash=StepSquash*step+land;
            _pose.localPosition=new Vector3(0,BobHeight*.5f*(_gait-step),0);
            _pose.localRotation=Quaternion.Euler(0,0,_lean*(1+LeanSway*step));
            _pose.localScale=new Vector3(1+squash*.5f,1-squash+breath,1);
        }

        private void Show(int index)
        {
            FrameIndex=index;
            var frame=_art.GetFrame(index,FrontFacing); _body.sprite=frame.Body;
            // Mirror the presentation subtree only; movement, targeting and the floor shadow remain untouched.
            _body.transform.localScale=new Vector3(Mirrored?-1:1,1,1);
            _parts[0].sortingOrder=_body.sortingOrder+(FrontFacing?2:-1);
            _parts[1].sortingOrder=_body.sortingOrder+(FrontFacing?3:-1);
            if (_art.Staff != null) _parts[2].sortingOrder=_body.sortingOrder+(FrontFacing?2:-1);
            // Reuse the authored knuckle pixels above the hilt. Rear equipment is already behind the body.
            _grip.sprite=frame.Grip;
            _grip.transform.localPosition=frame.GripOffset;
            _grip.sortingOrder=_body.sortingOrder+4;
            _grip.enabled=FrontFacing && frame.Grip!=null && (_parts[0].gameObject.activeInHierarchy
                || (_art.Staff!=null && _parts[2].gameObject.activeInHierarchy));
            _parts[0].transform.localPosition=frame.RightHand;
            _parts[1].transform.localPosition=frame.LeftHand;
            _parts[2].transform.localPosition=frame.RightHand;
            _parts[3].transform.localPosition=frame.RightHand+Vector2.up*(HeroArt.StaffCapRow-HeroArt.StaffGripRow+4)/32f;
            _parts[4].transform.localPosition=frame.LeftHand;
            _parts[5].transform.localPosition=frame.RightHand;
            if (_art.Dagger != null)
            {
                _parts[4].sortingOrder=_parts[5].sortingOrder=_body.sortingOrder-1;
                ApplyDaggerAngle(0);
            }
        }

        private void ApplyAttack(float t)
        {
            var frame=_art.GetFrame(FrameIndex,FrontFacing); float pulse=Mathf.Sin(t*Mathf.PI);
            if(_loadout==0) _parts[0].transform.localRotation=Quaternion.Euler(0,0,FrontFacing?Mathf.Lerp(-100,-180,t):Mathf.Lerp(-50,0,t));
            else if(_loadout==1)
            {
                var lean=Quaternion.Euler(0,0,-14*pulse);
                _parts[2].transform.localRotation=lean; _parts[3].transform.localRotation=lean;
                _parts[3].transform.localPosition=(Vector3)frame.RightHand+lean*(Vector3.up*(HeroArt.StaffCapRow-HeroArt.StaffGripRow+4)/32f);
                _parts[3].sprite=_litOrb;
            }
            else
            {
                if (_art.Dagger != null)
                {
                    // Neutral body hands do not translate: rotate the painted blades about their registered grips.
                    _parts[4].transform.localPosition=frame.LeftHand;
                    _parts[5].transform.localPosition=frame.RightHand;
                    ApplyDaggerAngle(pulse);
                }
                else
                {
                    _parts[4].transform.localPosition=frame.LeftHand+Vector2.up*(.12f*pulse);
                    _parts[5].transform.localPosition=frame.RightHand+Vector2.up*(.12f*pulse);
                }
            }
        }

        private void ApplyDaggerAngle(float pulse)
        {
            float angle=35+12*pulse;
            _parts[4].transform.localRotation=Quaternion.Euler(0,0,(FrontFacing?-1:1)*angle);
            _parts[5].transform.localRotation=Quaternion.Euler(0,0,(FrontFacing?1:-1)*angle);
        }

        private void ResetWeapons()
        {
            for(int i=0;i<_parts.Length;i++) _parts[i].transform.localRotation=Quaternion.identity;
            if(FrontFacing) _parts[0].transform.localRotation=Quaternion.Euler(0,0,-180);
            _parts[3].sprite=_orb;
            if (_art.Dagger != null) ApplyDaggerAngle(0);
        }

        public void FaceAttack(Vector3 target)
        {
            // Struck supplies the actual hit target, including a target killed by that hit.
            // Moving heroes keep their travel facing so a nearby enemy cannot reverse the gait every swing.
            if (!_moving) Face(target-_root.position);
            Show(FrameIndex); ApplyAttack(0);
        }

        private void Face(Vector3 delta)
        {
            if (!_art.HasFrontFrames) return;
            Vector2 direction=new Vector2(delta.x,delta.y/ArenaFloor.DepthScale);
            if(direction.sqrMagnitude<.00000001f) return;
            direction.Normalize();
            // Retain each axis inside a small dead band, including exact cardinal motion.
            if(Mathf.Abs(direction.x)>.15f) Mirrored=direction.x<0;
            if(Mathf.Abs(direction.y)>.15f) FrontFacing=direction.y<0;
        }
    }
}
