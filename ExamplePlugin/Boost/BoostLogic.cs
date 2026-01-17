using R2API;
using RoR2;
using RoR2.HudOverlay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils.Boost
{
    public class BoostLogic : NetworkBehaviour
    {
        // I HATE NETWORKING I HATE NETWORKING I HATE NETWORKING I HATE NETWORKING I HATE NETWORKING I HATE NETWORKING I HATE NETWORKING I HATE NETWORKING I HATE NETWORKING I HATE NETWORKING 
        // I've been putting this SyncVar stuff all around because that's what I've seen other people do but apparently it doesn't do anything without some extra thing I don't have
        public CharacterBody body;
        public SkillDefs.IBoostSkill boostSkillDef;

        [SyncVar(hook = nameof(OnBoostMeterChanged))]
        public float boostMeter;
        
        [SyncVar]
        public float maxBoostMeter;

        public float boostRegen;

        public bool boostAvailable = true;

        public float predictedMeter;

        public bool alwaysMaxBoost;

        public float boostMeterDrain;
        public bool boostDraining = false;
        public bool boostBeingUsed;

        public bool boostExists;

        private const float baseMaxBoostMeter = 100f;
        private const float boostMeterPerFlatReduction = 100/3f;
        public float baseBoostRegen = 21f;
        public const float boostRegenPerBandolier = 25f;

        private OverlayController overlayController;

        #region Boost Meter Functionality
        public void Awake()
        {
            body = GetComponent<CharacterBody>();
        }
        public void Start()
        {
            body.characterMotor.onHitGroundAuthority += ResetAirBoost;
            body.skillLocator.utility.onSkillChanged += OnSkillChanged;
            BoostExists();
            CalculateBoostVariables(body);
            body.onRecalculateStats += CalculateBoostVariables;
            this.NetworkboostMeter = maxBoostMeter;

            CreateBoostHUD();
        }
        
        public void FixedUpdate()
        {
            if (boostExists)
            {
                PredictMeter();
                if (NetworkServer.active)
                {
                    if (this.boostRegen >= boostMeterDrain || alwaysMaxBoost)
                    {
                        this.AddBoost(maxBoostMeter);
                    }
                    else
                    {
                        this.AddBoost(boostRegen * Time.fixedDeltaTime);
                    }
                }
            }
        }

        public virtual void CalculateBoostVariables(CharacterBody body)
        {
            if (body && boostExists)
            {
                this.boostRegen = baseBoostRegen / body.skillLocator.utility.cooldownScale;
                this.maxBoostMeter = Mathf.Round((baseMaxBoostMeter + (boostMeterPerFlatReduction * Mathf.Max(body.skillLocator.utility.flatCooldownReduction, -2))) / 10) * 10;
                this.NetworkmaxBoostMeter = maxBoostMeter;
                if ((body.characterMotor.isGrounded || body.characterMotor.isFlying) && body.skillLocator.utility.stock != body.skillLocator.utility.maxStock && boostAvailable)
                {
                    body.skillLocator.utility.stock = body.skillLocator.utility.maxStock;
                }
            }
        }

        public void OnSkillChanged(GenericSkill skill)
        {
            BoostExists();
        }

        private void BoostExists()
        {
            bool prevBoostExists = boostExists;
            if (typeof(SkillDefs.IBoostSkill).IsAssignableFrom(body.skillLocator.utility.skillDef.GetType()))
            {
                boostSkillDef = body.skillLocator.utility.skillDef as SkillDefs.IBoostSkill;
                boostExists = true;
            }
            else
            {
                boostExists = false;
            }
            if (!prevBoostExists)
            {
                this.NetworkboostAvailable = true;
            }
        }

        protected virtual void CreateBoostHUD()
        {
            overlayController = HudOverlayManager.AddOverlay(base.gameObject, new OverlayCreationParams
            {
                prefab = Assets.boostHUD,
                childLocatorEntry = "CrosshairExtras"
            });
            overlayController.onInstanceAdded += PrepareBoostHUD;
        }
        private void PrepareBoostHUD(OverlayController overlayController, GameObject instance)
        {
            if (instance.TryGetComponent<BoostHUD>(out var boostHud))
            {
                boostHud.PrepareBoostMeter();
                boostHud.boostLogic = this;
                BoostHUDCreated(instance);
            }
        }
        protected virtual void BoostHUDCreated(GameObject hud)
        {

        }

        public void OnDestroy()
        {
            overlayController.onInstanceAdded -= PrepareBoostHUD;
            HudOverlayManager.RemoveOverlay(overlayController);
            body.characterMotor.onHitGroundAuthority -= ResetAirBoost;
            body.skillLocator.utility.onSkillChanged -= OnSkillChanged;
            body.onRecalculateStats -= CalculateBoostVariables;
        }

        [Server]
        public void AddBoost(float amount)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("AddBoost run on client", Config.Logs.All);
                return;
            }
            this.NetworkboostMeter = Mathf.Clamp(this.boostMeter + amount, 0, this.maxBoostMeter);

            if (!boostAvailable && boostMeter>=Math.Min(baseMaxBoostMeter,this.maxBoostMeter))
            {
                this.NetworkboostAvailable = true;
                //body.skillLocator.utility.stock = body.skillLocator.utility.maxStock;
            }
        }

        [Server]
        public void RemoveBoost(float amount)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("RemoveBoost run on client", Config.Logs.All);
                return;
            }
            this.NetworkboostMeter = Mathf.Clamp(this.boostMeter - amount, 0, this.maxBoostMeter);

            if (boostMeter<=0)
            {
                this.NetworkboostAvailable = false;
                //body.skillLocator.utility.stock = 0;
            }
        }

        public void InstantChangeBoostMeter(float amount)
        {
            if (NetworkServer.active)
            {
                if (amount > 0)
                {
                    AddBoost(amount);
                }
                else
                {
                    RemoveBoost(Mathf.Abs(amount));
                }
            }
            else
            {
                predictedMeter = Mathf.Clamp(predictedMeter + amount, 0, maxBoostMeter);
            }
        }

        public void ResetAirBoost(ref CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            if (boostExists && boostAvailable)
            {
                body.skillLocator.utility.stock = body.skillLocator.utility.maxStock;
            }
        }

        private void OnBoostMeterChanged(float newBoostMeter)
        {
            predictedMeter = newBoostMeter;
            this.NetworkboostMeter = newBoostMeter;
        }

        private void PredictMeter()
        {
            if (this.boostRegen >= boostMeterDrain || alwaysMaxBoost)
            {
                predictedMeter = maxBoostMeter;
            }
            else
            {
                predictedMeter = Mathf.Clamp(this.predictedMeter + (boostDraining ? (boostRegen - boostMeterDrain) * Time.fixedDeltaTime : boostRegen * Time.fixedDeltaTime), 0, this.maxBoostMeter);
            }
        }
        #endregion

        #region Helpers
        // This makes half of the speed increase from boost apply to baseMoveSpeed so it scales better with having movement speed items
        // Overall speed boost (assuming sprinting and no other buffs) is the same as the input percent
        // figure out this stupid math soons please
        public static void BoostStats(CharacterBody self, RecalculateStatsAPI.StatHookEventArgs stats, float moveSpeedPercentBuff)
        {
            if (self)
            {
                stats.baseMoveSpeedAdd += ((moveSpeedPercentBuff * self.baseMoveSpeed) / 2) + 0.25f;
                stats.moveSpeedMultAdd += moveSpeedPercentBuff / 3f;
            }
        }
        #endregion

        #region Networking
        // I have no clue how to network I am just copying viend and red mist
        // now doing the one thing more dangerous than copying, trying to be original
        public float NetworkboostMeter
        {
            get
            {
                return this.boostMeter;
            }
            [param: In]
            set
            {
                if (NetworkServer.localClientActive && !base.syncVarHookGuard)
                {
                    base.syncVarHookGuard = true;
                    this.OnBoostMeterChanged(value);
                    //NetworkboostMeter = value;
                    base.syncVarHookGuard = false;
                }
                base.SetSyncVar<float>(value, ref this.boostMeter, 1U);
            }
        }

        public float NetworkmaxBoostMeter
        {
            get
            {
                return this.maxBoostMeter;
            }
            [param: In]
            set
            {
                if (NetworkServer.localClientActive && !base.syncVarHookGuard)
                {
                    base.syncVarHookGuard = true;
                    NetworkmaxBoostMeter = value;
                    base.syncVarHookGuard = false;
                }
                base.SetSyncVar<float>(value, ref this.maxBoostMeter, 2U);
            }
        }

        public bool NetworkboostAvailable
        {
            get
            {
                return this.boostAvailable;
            }
            [param: In]
            set
            {
                if (NetworkServer.localClientActive && !base.syncVarHookGuard)
                {
                    base.syncVarHookGuard = true;
                    this.NetworkboostAvailable = value;
                    //NetworkboostAvailable = value;
                    base.syncVarHookGuard = false;
                }
                base.SetSyncVar<bool>(value, ref this.boostAvailable, 4U);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool forceAll)
        {
            if (forceAll)
            {
                writer.Write(this.boostMeter);
                writer.Write(this.maxBoostMeter);
                writer.Write(this.boostAvailable);
                return true;
            }
            bool flag = false;
            if ((base.syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(this.boostMeter);
            }
            if ((base.syncVarDirtyBits & 2U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(this.maxBoostMeter);
            }
            if ((base.syncVarDirtyBits & 4U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(this.boostAvailable);
            }
            if (!flag)
            {
                writer.WritePackedUInt32(base.syncVarDirtyBits);
            }
            return flag;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                this.boostMeter = reader.ReadSingle();
                //this.OnBoostMeterChanged(reader.ReadSingle());
                this.maxBoostMeter = reader.ReadSingle();
                this.boostAvailable = reader.ReadBoolean();
                //this.OnBoostAvailableChanged(reader.ReadBoolean());
                return;
            }
            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1U) != 0U)
            {
                //this.boostMeter = reader.ReadSingle();
                this.OnBoostMeterChanged(reader.ReadSingle());
            }
            if ((num & 2U) != 0U)
            {
                this.maxBoostMeter = reader.ReadSingle();
            }
            if ((num & 4U) != 0U)
            {
                //this.boostAvailable = reader.ReadBoolean();
                this.boostAvailable = reader.ReadBoolean();
            }
        }
        #endregion
    }
}