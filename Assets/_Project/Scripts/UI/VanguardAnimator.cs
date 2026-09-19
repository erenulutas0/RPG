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
        private Vector3 _previous;
        private float _distance;
        private float _remaining;
        private float _duration;
        private int _loadout;
        private bool _moving;
        // A full four-pose cycle: shorter contacts reduce sliding without changing movement speed.
        private const float Stride = .8f;
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
            Show(0);
        }

        public void Reset()
        {
            _remaining=0; _distance=0; _moving=false; _previous=_root.position;
            ResetWeapons(); Show(0);
        }

        public void Attacked()
        {
            _loadout=_parts[2].gameObject.activeInHierarchy?1:_parts[4].gameObject.activeInHierarchy?2:0;
            _duration=Mathf.Min(.20f,(_attack.Weapon?.Interval ?? 1f)*.65f);
            _remaining=_duration;
            ResetWeapons();
            // Damage already happened in this event: enter the strike, never begin a delayed visual windup.
            Show(_loadout==0?6:0);
            ApplyAttack(0);
        }

        public void Tick(float dt)
        {
            Vector3 delta=_root.position-_previous; _previous=_root.position;
            if(dt<=0 || (_health!=null&&!_health.IsAlive)) return;
            float travel=new Vector2(delta.x,delta.y/ArenaFloor.DepthScale).magnitude;
            _moving=travel>=.0001f;
            if(travel>0) _distance=(_distance+travel)%Stride;
            if(_remaining>0)
            {
                _remaining=Mathf.Max(0,_remaining-dt);
                if(_remaining>0)
                {
                    float t=1-_remaining/_duration;
                    if(_loadout==0) Show(t<.42f?6:7);
                    ApplyAttack(t); return;
                }
                ResetWeapons();
            }
            if (_moving) Face(delta);
            // Anticipate only when the next actual sword attack is close and a valid target is in range.
            var weapon=_attack.Weapon;
            if(_parts[0].gameObject.activeInHierarchy && weapon!=null && weapon.CooldownRemaining>0
                && weapon.CooldownRemaining<=Mathf.Min(.09f,weapon.Interval*.25f)
                && _targeting!=null && _targeting.Acquire(weapon.Range)!=null)
            {
                if (!_moving) Face(_targeting.Acquire(weapon.Range).transform.position-_root.position);
                Show(5); _parts[0].transform.localRotation=Quaternion.Euler(0,0,FrontFacing?100:20); return;
            }
            ResetWeapons();
            if(travel<.0001f){_distance=0;Show(0);}else Show(1+Mathf.Min(3,(int)(_distance/Stride*4)));
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
