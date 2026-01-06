using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Boost.EntityStates
{
    public abstract class BoostIdle : BaseSkillState
    {
        public Animator modelAnimator;
        public AimAnimator aimAnimator;
        public override void OnEnter()
        {
            base.OnEnter();
            PlayBoostIdleEnterAnimation();
            modelAnimator = GetModelAnimator();
            aimAnimator = GetAimAnimator();
            if (aimAnimator)
            {
                aimAnimator.enabled = true;
            }
            if (modelAnimator)
            {
                modelAnimator.SetBool("isBoosting", true);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority)
            {
                if (base.inputBank.moveVector != Vector3.zero)
                {
                    EnterBoost();
                    return;
                }
                if (!base.inputBank.skill3.down || (!base.characterMotor.isGrounded && !characterMotor.isFlying))
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }

        }

        public override void OnExit()
        {
            if (modelAnimator)
            {
                modelAnimator.SetBool("isBoosting", false);
            }
            if (aimAnimator)
            {
                aimAnimator.enabled = false;
            }
            if (GetModelTransform())
            base.OnExit();
        }

        public virtual void EnterBoost()
        {
            if (typeof(SkillDefs.IBoostSkill).IsAssignableFrom(base.skillLocator.utility.skillDef.GetType()))
            {
                outer.SetNextState(EntityStateCatalog.InstantiateState(base.skillLocator.utility.skillDef.activationState.stateType));
            }
            else
            {
                outer.SetNextStateToMain();
            }
        }

        public virtual void PlayBoostIdleEnterAnimation()
        {
            base.PlayCrossfade("Body", "BoostIdleEnter", 0.3f);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
