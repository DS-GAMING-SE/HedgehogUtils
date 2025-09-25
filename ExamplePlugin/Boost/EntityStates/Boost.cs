using EntityStates;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UIElements.Experimental;

namespace HedgehogUtils.Boost.EntityStates
{
    public abstract class Boost : GenericCharacterMain
    {
        public const float minDuration = 0.4f;
        public const float minAirBoostDuration = 0.2f;
        public const float maxAirBoostDuration = 0.4f;

        protected virtual float boostStartMeterDrain
        {
            get { return 20f; }
        }
        protected virtual float boostMeterDrain
        {
            get { return 36f; }
        }

        public static float screenShake = 3.5f;
        public static float airBoostY = 8;

        protected Vector3 targetDirection;
        public BoostLogic boostLogic;

        private TemporaryOverlayInstance temporaryOverlay;

        protected ICharacterFlightParameterProvider flight;

        protected static float boostCameraDistance = -13;
        private CharacterCameraParamsData boostingCameraParams = new CharacterCameraParamsData
        {
            maxPitch = 70f,
            minPitch = -70f,
            pivotVerticalOffset = 1.1f,
            idealLocalCameraPos = new Vector3(0f, 0f, boostCameraDistance),
            wallCushion = 0.1f
        };
        private CameraTargetParams.CameraParamsOverrideHandle camOverrideHandle;

        protected GameObject aura;

        protected virtual BuffDef buff
        {
            get { return null; }
        }

        protected float maxRadiansTurnPerSecond = 3f;

        public bool airBoosting = false;

        protected bool firstFramePassed = false;

        protected bool initialBoostVelocity = false;

        public override void OnEnter()
        {
            base.OnEnter();
            base.characterBody.skillLocator.utility.onSkillChanged += OnSkillChanged;
            flight = base.characterBody.GetComponent<ICharacterFlightParameterProvider>();

            base.GetModelAnimator().SetBool("isBoosting", true);

            boostLogic = GetComponent<BoostLogic>();
            if (NetworkServer.active)
            {
                if (buff == null) { Log.Error($"Boost state of type \"{this.GetType()}\" does not have a buff defined. Override the property \"buff\" to assign your own buff"); }
                else { base.characterBody.AddBuff(buff); }
                base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.25f);
            }

            if (boostStartMeterDrain > 0)
            {
                boostLogic.InstantChangeBoostMeter(-(boostStartMeterDrain * characterBody.skillLocator.utility.cooldownScale));
            }
            boostLogic.boostBeingUsed = true;

            if (base.inputBank.moveVector != Vector3.zero)
            {
                base.characterDirection.forward = base.inputBank.moveVector;
            }

            targetDirection = base.characterDirection.forward;

            if (airBoosting)
            {
                PlayAirBoostAnimation();
                base.skillLocator.utility.DeductStock(1);
            }
            else
            {
                PlayBoostAnimation();
            }

            this.camOverrideHandle = base.cameraTargetParams.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest
            {
                cameraParamsData = boostingCameraParams,
                priority = 1f
            }, minDuration / 2f);

            CreateBoostVFX();
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (boostMeterDrain > 0)
            {
                if (NetworkServer.active)
                {
                    boostLogic.RemoveBoost(boostMeterDrain * Time.fixedDeltaTime);
                }
                boostLogic.boostMeterDrain = boostMeterDrain;
                boostLogic.boostDraining = true;
            }

            base.characterBody.isSprinting = true;

            if (firstFramePassed && !initialBoostVelocity) // I wait 1 frame before giving the initial velocity of the boost because it takes 1 frame for the boost speed buff to be applied
            {
                base.characterMotor.velocity = targetDirection * base.characterBody.moveSpeed;
                initialBoostVelocity = true;
            }

            if (airBoosting)
            {
                if (!base.isGrounded)
                {
                    if (!Helpers.Flying(flight))
                    {
                        base.characterMotor.velocity.y = Mathf.Max(airBoostY, base.characterMotor.velocity.y);
                    }
                }
                else
                {
                    airBoosting = false;
                }
                if (base.fixedAge > maxAirBoostDuration)
                {
                    airBoosting = false;
                }
                if (base.isAuthority && (base.fixedAge > minAirBoostDuration && !base.inputBank.skill3.down))
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }
            else if (base.isAuthority && base.fixedAge > minDuration && !base.inputBank.skill3.down)
            {
                SetNextState();
                return;
            }

            if (base.isAuthority && inputBank.moveVector == Vector3.zero && !airBoosting && base.fixedAge > minDuration)
            {
                SetNextState();
                return;
            }

            if (base.isAuthority && Vector3.Dot(inputBank.moveVector, targetDirection) < -0.05f && base.fixedAge > minDuration)
            {
                SetNextState();
                return;
            }

            if (base.isAuthority && (boostLogic.boostMeter <= 0 || !boostLogic.boostAvailable) && base.fixedAge > minDuration)
            {
                SetNextState();
                return;
            }
            if (!firstFramePassed)
            {
                firstFramePassed = true;
            }
        }

        public override void HandleMovements()
        {
            if (base.isAuthority)
            {
                MoveDirection();
                this.ProcessJump();
            }
        }

        protected virtual void MoveDirection()
        {
            if (this.hasCharacterMotor)
            {
                if (this.moveVector != Vector3.zero)
                {
                    targetDirection = Vector3.RotateTowards(targetDirection, this.moveVector, maxRadiansTurnPerSecond * Time.fixedDeltaTime, 0.1f);
                }
                base.characterMotor.moveDirection = targetDirection;
                if (base.hasCharacterDirection)
                {
                    this.characterDirection.moveVector = base.characterMotor.moveDirection;
                }
            }
        }

        public override void UpdateAnimationParameters()
        {
            base.UpdateAnimationParameters();
            if (this.characterAnimParamAvailability.isMoving)
            {
                this.modelAnimator.SetBool(AnimationParameters.isMoving, true);
            }
        }


        protected virtual void CreateBoostVFX()
        {
            Util.PlaySound(GetSoundString(), base.gameObject);
            if (base.isAuthority)
            {
                base.AddRecoil(-1f * screenShake, 1f * screenShake, -0.5f * screenShake, 0.5f * screenShake);
                EffectManager.SimpleMuzzleFlash(GetFlashPrefab(), base.gameObject, "MainHurtbox", true);
            }

            CreateTemporaryOverlay();

            if (GetAuraPrefab())
            {
                aura = GameObject.Instantiate<GameObject>(GetAuraPrefab(), base.FindModelChild("MainHurtbox"));
            }
        }

        protected virtual void RemoveBoostVFX()
        {
            RemoveTemporaryOverlay();

            if (aura)
            {
                Destroy(aura);
            }
        }

        private void CreateTemporaryOverlay()
        {
            if (!GetOverlayMaterial()) { return; }
            if (temporaryOverlay != null && temporaryOverlay.ValidateOverlay()) { return; }
            Transform modelTransform = base.GetModelTransform();
            if (!modelTransform) { return; }
            CharacterModel model = modelTransform.GetComponent<CharacterModel>();
            if (model)
            {
                temporaryOverlay = TemporaryOverlayManager.AddOverlay(model.gameObject);
                temporaryOverlay.originalMaterial = GetOverlayMaterial();
                temporaryOverlay.animateShaderAlpha = true; // Why does animateShaderAlpha make the entire TemporaryOverlayManager (and by extension the ENTIRE GAME) break when I try to destroy an overlay
                temporaryOverlay.duration = 0.2f;
                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 2f, 3f, 0.4f);
                temporaryOverlay.destroyComponentOnEnd = false;
                temporaryOverlay.destroyObjectOnEnd = false;
                temporaryOverlay.inspectorCharacterModel = model;
                temporaryOverlay.Start();
            }
        }

        private void RemoveTemporaryOverlay()
        {
            if (temporaryOverlay != null)
            {
                temporaryOverlay.animateShaderAlpha = false; // this is necessary trust me
                temporaryOverlay.Destroy();
            }
        }

        public virtual void SetNextState()
        {
            if (!base.isAuthority) { return; }

            if (base.isGrounded || Helpers.Flying(flight))
            {
                if (inputBank.moveVector == Vector3.zero)
                {
                    SetBrakeState(characterDirection.forward);
                    return;
                }
                else if (Vector3.Dot(inputBank.moveVector, characterDirection.forward) < 0f)
                {
                    SetBrakeState(inputBank.moveVector.normalized);
                    return;
                }
            }
            this.outer.SetNextStateToMain();
        }

        protected virtual void SetBrakeState(Vector3 endDirection)
        {
            if (typeof(SkillDefs.IBoostSkill).IsAssignableFrom(base.skillLocator.utility.skillDef.GetType()))
            {
                Brake brake = EntityStateCatalog.InstantiateState(((SkillDefs.IBoostSkill)base.skillLocator.utility.skillDef).brakeState.stateType) as Brake;
                brake.endDirection = endDirection;
                outer.SetNextState(brake);
            }
            else
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.GetModelAnimator().SetBool("isBoosting", false);
            boostLogic.boostDraining = false;
            boostLogic.boostBeingUsed = false;
            base.cameraTargetParams.RemoveParamsOverride(this.camOverrideHandle, 1f);
            if (NetworkServer.active)
            {
                base.characterBody.RemoveBuff(buff);
            }
            RemoveBoostVFX();

            base.characterBody.skillLocator.utility.onSkillChanged -= OnSkillChanged;
            base.OnExit();
        }

        public virtual string GetSoundString()
        {
            return "Play_hedgehogutils_boost";
        }

        public virtual GameObject GetFlashPrefab()
        {
            return Assets.powerBoostFlashEffect;
        }
        public virtual GameObject GetAuraPrefab()
        {
            return Assets.powerBoostAuraEffect;
        }
        public virtual Material GetOverlayMaterial()
        {
            return LegacyResourcesAPI.Load<Material>("Materials/matOnHelfire");
        }

        public virtual void PlayBoostAnimation()
        {
            base.PlayCrossfade("Body", "Boost", 0.1f);
        }

        public virtual void PlayAirBoostAnimation()
        {
            base.PlayCrossfade("Body", "AirBoost", "Roll.playbackRate", minAirBoostDuration, minAirBoostDuration);
        }

        public virtual void OnSkillChanged(GenericSkill skill)
        {
            if (typeof(Boost).IsAssignableFrom(skill.activationState.stateType))
            {
                outer.SetNextState(EntityStateCatalog.InstantiateState(skill.activationState.stateType));
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(airBoosting);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            airBoosting = reader.ReadBoolean();
        }
    }
}