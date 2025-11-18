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
        protected ICharacterFlightParameterProvider flight;
        public override void OnEnter()
        {
            base.OnEnter();
            flight = base.characterBody.GetComponent<ICharacterFlightParameterProvider>();
            PlayBoostIdleEnterAnimation();
            base.GetModelAnimator().SetBool("isBoosting", true);
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
                if (!base.inputBank.skill3.down || (!base.characterMotor.isGrounded && !Helpers.Flying(flight)))
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }

        }

        public override void OnExit()
        {
            base.GetModelAnimator().SetBool("isBoosting", false);
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
