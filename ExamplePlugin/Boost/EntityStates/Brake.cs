using EntityStates;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking;
using RoR2;

namespace HedgehogUtils.Boost.EntityStates
{
    public class Brake : BaseSkillState
    {
        public Vector3 startDirection;
        public Vector3 endDirection;

        protected bool reverse;
        protected bool finalAnimationPlayed;

        public const float minDuration = 0.15f;
        public const float baseDuration = 0.4f;
        public const float expectedMovementSpeed = 14f;
        public float duration;

        public Vector3 startVelocity;

        public override void OnEnter()
        {
            base.OnEnter();
            base.characterBody.isSprinting = true;

            startDirection = characterDirection.forward;
            UpdateReverse();

            if (Vector3.Dot(base.characterDirection.forward, endDirection) < 0.4f)
            {
                base.characterDirection.forward = endDirection;
                base.characterDirection.moveVector = endDirection;
            }

            startVelocity = base.characterMotor.velocity;

            if (base.characterBody.moveSpeed == 0 && base.isAuthority) { outer.SetNextStateToMain(); return; }
            duration = ((baseDuration - minDuration) * (expectedMovementSpeed / base.characterBody.moveSpeed)) + minDuration;

            PlayAnimation(duration, reverse);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.isGrounded)
            {
                Vector3 startVelocityFlattened = new Vector3(startVelocity.x, base.characterMotor.velocity.y, startVelocity.z);
                base.characterMotor.velocity = Vector3.Lerp(startVelocityFlattened, new Vector3(0, base.characterMotor.velocity.y, 0), fixedAge / duration);
            }
            else
            {
                base.characterMotor.velocity = Vector3.Lerp(startVelocity, Vector3.zero, fixedAge / duration);
            }

            if (base.isAuthority && fixedAge < minDuration && !reverse && base.inputBank.moveVector != Vector3.zero)
            {
                if (Vector3.Dot(base.characterDirection.forward, base.inputBank.moveVector) < -0.3f)
                {
                    if (typeof(SkillDefs.IBoostSkill).IsAssignableFrom(base.skillLocator.utility.skillDef.GetType()))
                    {
                        Brake reverse = EntityStateCatalog.InstantiateState(((SkillDefs.IBoostSkill)base.skillLocator.utility.skillDef).brakeState.stateType) as Brake;
                        reverse.endDirection = base.inputBank.moveVector;
                        outer.SetNextState(reverse);
                    }
                    else
                    {
                        outer.SetNextStateToMain();
                    }
                    return;
                }
            }

            if (base.isAuthority && fixedAge > duration)
            {
                EndState();
            }
        }

        protected void UpdateReverse()
        {
            Vector2 startFlattened = new Vector2(startDirection.x, startDirection.z);
            Vector2 endFlattened = new Vector2(endDirection.x, endDirection.z);
            reverse = Vector2.Dot(startFlattened, endFlattened) < -0.3f;
        }

        public virtual void PlayAnimation(float duration, bool reverse)
        {
            if (reverse)
            {
                base.PlayCrossfade("Body", "BrakeReverse", "Roll.playbackRate", duration * 0.8f, duration / 4);
            }
            else
            {
                base.PlayCrossfade("Body", "Brake", "Roll.playbackRate", duration * 0.8f, duration / 4);
            }
        }

        public virtual void EndState()
        {
            if (!base.isAuthority) { return; }

            if (base.skillLocator.utility.IsReady() && base.inputBank.skill3.down)
            {
                base.skillLocator.utility.OnExecute();
            }
            else
            {
                outer.SetNextStateToMain();
            }
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(endDirection);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            endDirection = reader.ReadVector3();
        }
    }
}
