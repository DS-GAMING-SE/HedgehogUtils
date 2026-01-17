using EntityStates;
using RoR2;
using RoR2.Audio;
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils.Forms.EntityStates
{
    public abstract class GenericTransformationBase : TransformationBase
    {
        protected abstract float duration
        {
            get;
        }
        protected abstract float transformationDurationPercent
        {
            get;
        }
        protected string animationLayerName { get { return "FullBody, Override"; } }
        protected string animationName { get { return "HedgehogUtilsTransform"; } }
        protected string animationPlaybackRateParam { get { return "Roll.playbackRate"; } }

        protected bool effectFired = false;
        private int animationLayerIndex;
        private Animator animator;
        public override void OnEnter()
        {
            base.OnEnter();
            if (base.isAuthority)
            {
                this.formComponent.superSonicState.SetNextStateToMain(); // detransform
            }
            if (duration > 0)
            {
                animator = GetModelAnimator();
                if (animator)
                {
                    animationLayerIndex = animator.GetLayerIndex(animationLayerName); // Defaults to first animation layer if animator doesn't have animationLayerName
                    if (animationLayerIndex == -1) animationLayerIndex = 0;
                    PlayAnimationOnAnimator(animator, animator.GetLayerName(animationLayerIndex), animationName, animationPlaybackRateParam, this.duration);
                }
                //base.PlayAnimation("Body", "HedgehogUtilsTransform", "Roll.playbackRate", this.duration);
                if (NetworkServer.active)
                {
                    base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, duration, 1);
                }
            }

        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= this.duration * transformationDurationPercent && !effectFired && this.formComponent)
            {
                Transform();
            }
           
            
            if (fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }

            if (base.characterMotor)
            {
                base.characterMotor.velocity = Vector3.zero;
            }
        }

        public override void Transform()
        {
            effectFired = true;
            base.Transform();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Vehicle;
        }
    }
}