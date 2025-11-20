using EntityStates.JunkCube;
using HedgehogUtils.Boost;
using HedgehogUtils.Forms;
using HedgehogUtils.Forms.SuperForm;
using HedgehogUtils.Launch;
using HedgehogUtils.Miscellaneous;
using LookingGlass.ItemStatsNameSpace;
using LookingGlass.LookingGlassLanguage;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using HarmonyLib;

namespace HedgehogUtils
{
    public static class OnHooks
    {
        public static void Initialize()
        {
            On.RoR2.CharacterDeathBehavior.OnDeath += DontDieWhileLaunched;
            On.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool += DontDoNormalKnockbackWhenLaunched;
            On.RoR2.HealthComponent.TakeDamage += TakeDamageHook;

            On.RoR2.Util.GetBestBodyName += SuperNamePrefix;

            On.RoR2.SceneDirector.Start += SceneDirectorOnStart;

            if (HedgehogUtilsPlugin.lookingGlassLoaded)
            {
                RoR2Application.onLoad += LookingGlassSetup;
            }

            On.RoR2.GenericPickupController.Start += EmeraldDropSound;

            On.RoR2.GenericSkill.CanApplyAmmoPack += CanApplyAmmoPackToBoost;
            On.RoR2.GenericSkill.ApplyAmmoPack += ApplyAmmoPackToBoost;
            On.RoR2.UI.HUD.Awake += CreateBoostMeterUI;
        }
        private static void DontDieWhileLaunched(On.RoR2.CharacterDeathBehavior.orig_OnDeath orig, CharacterDeathBehavior self)
        {
            if (self.gameObject.TryGetComponent<CharacterBody>(out CharacterBody body))
            {
                if (body.HasBuff(Buffs.launchedBuff))
                {
                    return;
                }
            }
            orig(self);
        }

        private static void DontDoNormalKnockbackWhenLaunched(On.RoR2.HealthComponent.orig_TakeDamageForce_DamageInfo_bool_bool orig, HealthComponent self, DamageInfo damageInfo, bool alwaysApply, bool disableAirControlUntilCollision)
        {
            if (damageInfo.damageType.HasModdedDamageType(Launch.DamageTypes.launch) || damageInfo.damageType.HasModdedDamageType(Launch.DamageTypes.launchOnKill)) { return; }
            orig(self, damageInfo, alwaysApply, disableAirControlUntilCollision);
        }

        private static void TakeDamageHook(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            #region Form Invincibility
            if (NetworkServer.active && self)
            {
                if (self.gameObject.TryGetComponent(out FormComponent formComponent))
                {
                    if (formComponent.activeForm)
                    {
                        if (formComponent.activeForm.invincible)
                        {
                            damageInfo.rejected = true;
                            EffectManager.SpawnEffect(HealthComponent.AssetReferences.damageRejectedPrefab, new EffectData
                            {
                                origin = damageInfo.position
                            }, true);
                        }
                    }
                }
            }
            #endregion


            orig(self, damageInfo);

            #region Chaos Snap On Hit
            if (NetworkServer.active && self && !damageInfo.rejected && !self.body.HasBuff(Buffs.chaosSnapRandomIdentifierBuff) && 
                damageInfo.HasModdedDamageType(Miscellaneous.DamageTypes.chaosSnapRandom) && ChaosSnapManager.instance)
            {
                ChaosSnapManager.instance.TeleportBodyToRandomNode(self.body, 30f, 50f);
                self.body.AddTimedBuff(Buffs.chaosSnapRandomIdentifierBuff, 1f);
            }
            #endregion

            #region Launch
            if (NetworkServer.active && (damageInfo.damageType.HasModdedDamageType(Launch.DamageTypes.launch)
                    || (damageInfo.damageType.HasModdedDamageType(Launch.DamageTypes.launchOnKill) && !self.alive)))
            {
                if (!damageInfo.damageType.HasModdedDamageType(Miscellaneous.DamageTypes.chaosSnapRandom) && damageInfo.attacker && 
                    damageInfo.attacker.TryGetComponent<CharacterBody>(out CharacterBody attackerBody))
                {
                    if (LaunchManager.AttackCanLaunch(self, attackerBody, damageInfo))
                    {
                        Vector3 launchDirection = damageInfo.force.normalized;
                        if (!damageInfo.damageType.HasModdedDamageType(Launch.DamageTypes.removeLaunchAutoAim))
                        {
                            if (self.body && self.body.characterMotor)
                            {
                                if (self.body.characterMotor.isGrounded)
                                {
                                    launchDirection = LaunchManager.AngleAwayFromGround(launchDirection, self.body.characterMotor.estimatedGroundNormal);
                                }
                            }
                            launchDirection = LaunchManager.AngleTowardsEnemies(launchDirection, self.transform.position, self.gameObject, attackerBody.teamComponent.teamIndex);
                            launchDirection = launchDirection.normalized;
                        }
                        LaunchManager.Launch(self.body, attackerBody, launchDirection, damageInfo.damage, damageInfo.crit, damageInfo.procCoefficient);
                    }
                }
            }
            #endregion
        }

        public static void LookingGlassSetup()
        {
            if (RoR2.Language.languagesByName.TryGetValue("en", out RoR2.Language language))
            {
                RegisterLookingGlassBuff(language, Buffs.superFormBuff, "Super Form", $"Immune to all attacks. Gain <style=cIsDamage>+{100f * StaticValues.superSonicBaseDamage}% damage</style>, <style=cIsUtility>+{100f * StaticValues.superSonicAttackSpeed}% attack speed</style>, and <style=cIsUtility>+{100f * StaticValues.superSonicMovementSpeed}% base movement speed</style>.");
            }

            #region Emerald Looking Glass
            ItemStatsDef emeraldStats = new ItemStatsDef();
            emeraldStats.descriptions.Add("Unique Emeralds: ");
            emeraldStats.valueTypes.Add(ItemStatsDef.ValueType.Stack);
            emeraldStats.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Number);
            emeraldStats.calculateValuesNew = (luck, stackCount, procChance) =>
            {
                var list = new List<float>();
                if (Forms.Forms.formToHandlerObject.TryGetValue(SuperFormDef.superFormDef, out GameObject handler))
                {
                    if (handler.TryGetComponent(out SuperSonicHandler emeraldSpawner))
                    {
                        list.Add(7 - ((SyncedItemTracker)emeraldSpawner.itemTracker).missingItems.Count);
                    }
                }
                else
                {
                    list.Add(-1);
                }
                return list;
            };
            ItemDefinitions.allItemDefinitions.Add((int)Items.redEmerald.itemIndex, emeraldStats);
            ItemDefinitions.allItemDefinitions.Add((int)Items.yellowEmerald.itemIndex, emeraldStats);
            ItemDefinitions.allItemDefinitions.Add((int)Items.greenEmerald.itemIndex, emeraldStats);
            ItemDefinitions.allItemDefinitions.Add((int)Items.blueEmerald.itemIndex, emeraldStats);
            ItemDefinitions.allItemDefinitions.Add((int)Items.purpleEmerald.itemIndex, emeraldStats);
            ItemDefinitions.allItemDefinitions.Add((int)Items.cyanEmerald.itemIndex, emeraldStats);
            ItemDefinitions.allItemDefinitions.Add((int)Items.grayEmerald.itemIndex, emeraldStats);
            #endregion
        }

        private static void RegisterLookingGlassBuff(RoR2.Language language, BuffDef buff, string name, string description) // There's a method just like this in lookingglass but I can't access it due to protection level. I might be missing something 
        {
            LookingGlassLanguageAPI.SetupToken(language, $"NAME_{buff.name}", name);
            LookingGlassLanguageAPI.SetupToken(language, $"DESCRIPTION_{buff.name}", description);
        }

        private static string SuperNamePrefix(On.RoR2.Util.orig_GetBestBodyName orig, GameObject bodyObject)
        {
            if (bodyObject)
            {
                if (bodyObject.TryGetComponent(out FormComponent component))
                {
                    if (component.activeForm)
                    {
                        if (!RoR2.Language.IsTokenInvalid(component.activeForm.name + "_PREFIX"))
                        {
                            string text = orig(bodyObject);
                            text = RoR2.Language.GetStringFormatted(component.activeForm.name + "_PREFIX", new object[]
                            {
                            text
                            });
                            return text;
                        }
                    }
                }
            }
            return orig(bodyObject);
        }

        private static void EmeraldDropSound(On.RoR2.GenericPickupController.orig_Start orig, GenericPickupController self)
        {
            orig(self);
            if (self && self.pickupDisplay)
            {
                PickupDef pickupDef = self.pickup.pickupIndex.pickupDef;
                if (pickupDef != null)
                {
                    ItemIndex itemIndex = pickupDef.itemIndex;
                    if (itemIndex != ItemIndex.None)
                    {
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                        if (itemDef && itemDef._itemTierDef == Items.emeraldTier)
                        {
                            Util.PlaySound("Play_hedgehogutils_emerald_spawn", self.gameObject);
                        }
                    }
                }
            }
        }

        private static void SceneDirectorOnStart(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            orig(self);
            if (!NetworkServer.active) return;

            NetworkServer.Spawn(GameObject.Instantiate<GameObject>(ChaosSnapManager.prefab));
            #region Form Handlers
            SceneDef scene = SceneCatalog.GetSceneDefForCurrentScene();
            /*if (sceneName == "intro")
            {
                return;
            }

            if (sceneName == "title")
            {
                // TODO:: create prefab of super sonic floating in the air silly style.
                Vector3 vector = new Vector3(38, 23, 36);
            }*/

            foreach (FormDef form in FormCatalog.formsCatalog)
            {
                bool formAvailable = form.enabled(form);

                if (!Forms.Forms.formToHandlerObject.ContainsKey(form) && formAvailable)
                {
                    Log.Message("Spawning new handler object for form " + form.ToString());
                    NetworkServer.Spawn(GameObject.Instantiate<GameObject>(Forms.Forms.formToHandlerPrefab.GetValueSafe(form)));
                }
                else
                {
                    Log.Message("Did NOT spawn handler object for form " + form.ToString());
                    continue;
                }

                FormHandler formHandler = Forms.Forms.formToHandlerObject.GetValueSafe(form).GetComponent(typeof(FormHandler)) as FormHandler;

                formHandler.SetEvents(formAvailable);
            }
            #region Super Form Handler and Emeralds
            if (!Forms.Forms.formToHandlerObject.TryGetValue(SuperFormDef.superFormDef, out GameObject handler)) { return; }

            if (!handler.TryGetComponent(out SuperSonicHandler emeraldSpawner)) { return; }

            emeraldSpawner.FilterOwnedEmeralds();

            if (SuperSonicHandler.available.Count > 0 && scene && scene.sceneType == SceneType.Stage && !scene.cachedName.Contains("moon") && !scene.cachedName.Contains("voidraid") && !scene.cachedName.Contains("voidstage"))
            {
                int maxEmeralds = Run.instance is InfiniteTowerRun ? Config.EmeraldsPerSimulacrumStage().Value : Config.EmeraldsPerStage().Value;
                SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();

                spawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
                spawnCard.sendOverNetwork = true;

                spawnCard.prefab = ChaosEmeraldInteractable.prefabBase;

                for (int i = 0; i < maxEmeralds && i < SuperSonicHandler.available.Count; i++)
                {
                    DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, ChaosEmeraldInteractable.placementRule, Run.instance.stageRng));
                }
            }
            #endregion
            #endregion
        }

        private static bool CanApplyAmmoPackToBoost(On.RoR2.GenericSkill.orig_CanApplyAmmoPack orig, GenericSkill self)
        {
            if (typeof(Boost.EntityStates.Boost).IsAssignableFrom(self.activationState.stateType))
            {
                BoostLogic boost = self.characterBody.GetComponent<BoostLogic>();
                if (boost)
                {
                    return boost.boostMeter < boost.maxBoostMeter;
                }
            }
            return orig(self);
        }
        private static void ApplyAmmoPackToBoost(On.RoR2.GenericSkill.orig_ApplyAmmoPack orig, GenericSkill self)
        {
            orig(self);
            if (typeof(Boost.EntityStates.Boost).IsAssignableFrom(self.activationState.stateType))
            {
                BoostLogic boost = self.characterBody.GetComponent<BoostLogic>();
                if (boost)
                {
                    boost.AddBoost(BoostLogic.boostRegenPerBandolier);
                }
            }
        }

        public static void CreateBoostMeterUI(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
        {
            orig.Invoke(self);
            BoostHUD boostHud = self.gameObject.AddComponent<BoostHUD>();
            GameObject boostUI = GameObject.Instantiate(Assets.boostHUD, self.transform.Find("MainContainer/MainUIArea/CrosshairCanvas"));
            boostHud.boostMeter = boostUI;
        }
    }
}
