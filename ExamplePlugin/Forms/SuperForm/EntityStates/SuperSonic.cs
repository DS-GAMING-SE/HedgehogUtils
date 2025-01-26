﻿using EntityStates;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Audio;
using RoR2.Skills;
using HedgehogUtils.Forms.EntityStates;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace HedgehogUtils.Forms.SuperForm.EntityStates
{
    public class SuperSonic : SonicFormBase
    {
        protected CharacterModel model;

        protected Transform chest;

        private TemporaryOverlayInstance temporaryOverlay;

        private GameObject superAura;
        private GameObject warning;
        private LoopSoundManager.SoundLoopPtr superLoop;

        private static float cameraDistance = -15;
        private CharacterCameraParamsData cameraParams = new CharacterCameraParamsData
        {
            maxPitch = 70f,
            minPitch = -70f,
            pivotVerticalOffset = 1.3f,
            idealLocalCameraPos = new Vector3(0f, 0f, cameraDistance),
            wallCushion = 0.1f
        };
        private CameraTargetParams.CameraParamsOverrideHandle camOverrideHandle;

        public static SkillDef melee;

        public static SkillDef sonicBoom;
        public static SkillDef parry;
        public static SkillDef idwAttack;
        public static SkillDef emptyParry;

        public static SkillDef boost;

        public static SkillDef grandSlam;

        // Character specific Super compat
        private Boost.BoostLogic boostLogic;
        private VoidSurvivorController viend;

        public override void OnEnter()
        {
            base.OnEnter();
            chest = base.FindModelChild("Chest");
            if (chest)
            {
                this.superAura = GameObject.Instantiate<GameObject>(Assets.superFormAura, base.FindModelChild("Chest"));
            }

            boostLogic = base.GetComponent<HedgehogUtils.Boost.BoostLogic>();
            if (boostLogic)
            {
                boostLogic.alwaysMaxBoost = true;
            }

            ApplyOverlay(ref temporaryOverlay, Assets.superFormOverlay);

            superLoop = LoopSoundManager.PlaySoundLoopLocal(base.gameObject, Assets.superLoopSoundDef);

            this.camOverrideHandle = base.cameraTargetParams.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest
            {
                cameraParamsData = this.cameraParams,
                priority = 0f
            }, 0f);

            if (base.isAuthority)
            {
                FireBlastAttack();

                if (base.skillLocator)
                {
                    SkillOverrides(true);
                }
                EffectManager.SimpleMuzzleFlash(Assets.superFormTransformationEffect, base.gameObject, "Chest", true);
            }
            if (NetworkServer.active)
            {
                RoR2.Util.CleanseBody(base.characterBody, true, false, true, true, true, false);
                viend = base.GetComponent<VoidSurvivorController>();
                Heal(1);
            }

            Flash(1);
        }

        public override void OnExit()
        {
            LoopSoundManager.StopSoundLoopLocal(superLoop);

            RemoveOverlay(ref temporaryOverlay);

            if (boostLogic)
            {
                boostLogic.alwaysMaxBoost = false;
            }

            if (this.superAura)
            {
                Destroy(this.superAura);
            }
            // Aura had despawning problem because all assets loaded are automatically given a component that makes them go away after 12 seconds
            if (this.warning)
            {
                Destroy(this.warning);
            }

            if (base.isAuthority && base.skillLocator)
            {
                SkillOverrides(false);
                if (base.skillLocator.secondary)
                {
                    base.skillLocator.secondary.UnsetSkillOverride(this, idwAttack, GenericSkill.SkillOverridePriority.Contextual);
                    base.skillLocator.secondary.UnsetSkillOverride(this, emptyParry, GenericSkill.SkillOverridePriority.Contextual);
                }
            }

            if (viend && NetworkServer.active)
            {
                viend.AddCorruption(-100);
            }

            base.cameraTargetParams.RemoveParamsOverride(this.camOverrideHandle, 0.5f);

            Flash(0.35f);

            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (viend && NetworkServer.active)
            {
                viend.AddCorruption(100);
            }
            if (this.superAura)
            {
                this.superAura.SetActive(this.characterModel.invisibilityCount <= 0);
            }
            if (base.fixedAge >= StaticValues.superSonicDuration - StaticValues.superSonicWarningDuration && !warning && chest)
            {
                this.warning = GameObject.Instantiate<GameObject>(Assets.superFormWarning, chest);
            }
        }

        public virtual void SkillOverrides(bool set)
        {
            /*if (!base.skillLocator) { return; }
            SkillHelper(base.skillLocator.primary, SonicTheHedgehogCharacter.primarySkillDef, melee, set);
            if (!SkillHelper(base.skillLocator.secondary, SonicTheHedgehogCharacter.sonicBoomSkillDef, sonicBoom, set))
            {
                SkillHelper(base.skillLocator.secondary, SonicTheHedgehogCharacter.parrySkillDef, parry, set);
            }
            SkillHelper(base.skillLocator.utility, SonicTheHedgehogCharacter.boostSkillDef, boost, set);
            SkillHelper(base.skillLocator.special, SonicTheHedgehogCharacter.grandSlamSkillDef, grandSlam, set);*/
        }

        public void ParryActivated()
        {
            base.skillLocator.secondary.SetSkillOverride(this, idwAttack, GenericSkill.SkillOverridePriority.Contextual);
        }

        public void IDWAttackActivated()
        {
            base.skillLocator.secondary.UnsetSkillOverride(this, idwAttack, GenericSkill.SkillOverridePriority.Contextual);
            base.skillLocator.secondary.SetSkillOverride(this, emptyParry, GenericSkill.SkillOverridePriority.Contextual);
        }

        private void FireBlastAttack()
        {
            if (base.isAuthority)
            {
                BlastAttack blastAttack = new BlastAttack();
                blastAttack.radius = 20;
                blastAttack.procCoefficient = 0;
                blastAttack.position = base.transform.position;
                blastAttack.attacker = base.gameObject;
                blastAttack.crit = false;
                blastAttack.baseDamage = 0;
                blastAttack.falloffModel = BlastAttack.FalloffModel.Linear;
                blastAttack.damageType = DamageType.Generic;
                blastAttack.baseForce = 7000;
                blastAttack.teamIndex = base.teamComponent.teamIndex;
                blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
                blastAttack.Fire();
            }
        }
    }
}