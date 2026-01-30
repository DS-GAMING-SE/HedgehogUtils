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
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public virtual void OnInterrupt()
        {
            this.outer.SetNextStateToMain();
        }
    }
}
