using EntityStates;
using RoR2;
using RoR2.Audio;
using RoR2.Skills;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils.Miscellaneous
{
    public class Death : GenericCharacterDeath
    {
        private float destroyTime = 1f;
        protected virtual float fadeDuration { get { return 0.35f; } }
        private bool attemptedDestroy;
        protected virtual string animationLayerName { get { return "FullBody, Override"; } }
        private Animator animator;
        private CharacterModel characterModel;
        private int animationLayerIndex;
        private bool destroyTimeSet;
        public override bool shouldAutoDestroy => false;
        public override void OnEnter()
        {
            base.OnEnter();
            if (cameraTargetParams)
            {
                cameraTargetParams.cameraPivotTransform = base.gameObject.transform;
            }
            if (cachedModelTransform)
            {
                characterModel = cachedModelTransform.GetComponent<CharacterModel>();
            }
        }
        public override void PlayDeathAnimation(float crossfadeDuration = 0.1F)
        {
            animator = GetModelAnimator();
            if (animator)
            {
                animationLayerIndex = animator.GetLayerIndex(animationLayerName);
                if (animationLayerIndex == -1) animationLayerIndex = 0;
                animator.Play("Death", animationLayerIndex);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!destroyTimeSet && animator)
            {
                if (animator.GetCurrentAnimatorStateInfo(animationLayerIndex).IsName("Death"))
                {
                    destroyTimeSet = true;
                    destroyTime = animator.GetCurrentAnimatorStateInfo(animationLayerIndex).length;
                }
            }
            if (base.characterMotor)
            {
                base.characterMotor.velocity = Vector3.zero;
            }
            if (characterModel)
            {
                characterModel.corpseFade = 1 - Mathf.Clamp01((base.fixedAge - (destroyTime - fadeDuration)) * (1 / fadeDuration));
            }

            if (base.fixedAge >= destroyTime)
            {
                AttemptDestroy();
            }
        }

        private void AttemptDestroy()
        {
            if (attemptedDestroy) { return; }
            attemptedDestroy = true;
            DestroyModel();
            if (NetworkServer.active)
            {
                DestroyBodyAsapServer();
            }
        }
        public override void OnExit()
        {
            if (!this.outer.destroying)
            {
                AttemptDestroy();
            }
            base.OnExit();
        }
    }
}