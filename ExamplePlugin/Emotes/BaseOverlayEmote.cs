using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Emotes
{
    public abstract class BaseOverlayEmote : BaseState
    {
        public abstract float animationDuration { get; }
        public abstract bool allowAirborne { get; }
        public abstract bool allowGrounded { get; }
        public abstract bool allowMoving { get; }
        private EntityStateMachine bodyState;
        public override void OnEnter()
        {
            base.OnEnter();
            bodyState = EntityStateMachine.FindByCustomName(base.gameObject, "Body");
            PlayEmoteAnimation();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority && fixedAge >= animationDuration || ShouldInterrupt() || bodyState.state.GetType() != bodyState.mainStateType.stateType)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public abstract void PlayEmoteAnimation();

        public virtual bool ShouldInterrupt()
        {
            return (!characterMotor || 
                (!allowAirborne && !characterMotor.isGrounded) ||
                (!allowGrounded && characterMotor.isGrounded) && 
                ((!inputBank) ||
                (!allowMoving && inputBank.moveVector != Vector3.zeroVector) ||
                inputBank.skill1.down ||
                inputBank.skill2.down ||
                inputBank.skill3.down ||
                inputBank.skill4.down ||
                inputBank.jump.down));
        }
    }
}
