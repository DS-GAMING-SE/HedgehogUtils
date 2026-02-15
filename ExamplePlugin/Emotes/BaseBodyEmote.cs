using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Emotes
{
    public abstract class BaseBodyEmote : BaseState
    {
        public abstract float animationDuration { get; }
        public override void OnEnter()
        {
            base.OnEnter();
            PlayEmoteAnimation();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority && fixedAge > animationDuration || ShouldInterrupt())
            {
                this.outer.SetNextStateToMain();
            }
        }
        public abstract void PlayEmoteAnimation();

        public virtual bool ShouldInterrupt()
        {
            return Helpers.IsDoingSomething(characterMotor, inputBank, true, true, false, false);
        }
    }
}
