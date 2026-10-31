using EntityStates.JunkCube;
using HarmonyLib;
using HedgehogUtils.Boost;
using HedgehogUtils.Forms;
using HedgehogUtils.Forms.SuperForm;
using HedgehogUtils.Launch;
using HedgehogUtils.Miscellaneous;
using HedgehogUtils.Voicelines;
using LookingGlass.ItemStatsNameSpace;
using LookingGlass.LookingGlassLanguage;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils
{
    public static class OnHooks
    {
        public static void Initialize()
        {
            On.RoR2.HealthComponent.StartDeath += DontDieWhileLaunched;
            On.RoR2.HealthComponent.IsImmuneToPhysicsForces += DontDoNormalKnockbackWhenLaunched;
            On.RoR2.HealthComponent.TakeDamageProcess += TakeDamageHook;
            IL.RoR2.HealthComponent.TakeDamageProcess += TakeDamageILHook;

            On.RoR2.Util.GetBestBodyName += SuperNamePrefix;

            Stage.onServerStageBegin += StageBegin;

            if (HedgehogUtilsPlugin.lookingGlassLoaded)
            {
                RoR2Application.onLoad += LookingGlassSetup;
            }

            On.RoR2.GenericSkill.CanApplyAmmoPack += CanApplyAmmoPackToBoost;
            On.RoR2.GenericSkill.ApplyAmmoPack += ApplyAmmoPackToBoost;

            On.EntityStates.SolusHeart.Phase1.Mission1.OnEnter += SolusHeart1DefeatedVoiceline;
            On.EntityStates.SolusHeart.Phase2.Mission2.OnEnter += SolusHeart2DefeatedVoiceline;
            On.EntityStates.SolusHeart.Death.SolusHeartDeathSequence.Start.OnEnter += SolusHeart3DefeatedVoiceline;
            On.EntityStates.SolusHeart.SolusHeartTransformationBaseState.OnEnter += SolusHeartPhaseStartVoiceline;
            On.RoR2.SolusWing.SolusWingWeakPointsController.onWeakpointDestroyedServer += SolusWingWeakPointDefeatedVoiceline;
        }
        private static void DontDieWhileLaunched(On.RoR2.HealthComponent.orig_StartDeath orig, HealthComponent self)
        {
            if (self.body && self.body.HasBuff(Buffs.launchedBuff) || self.body.HasBuff(Buffs.suppressDeathBuff))
            {
                return;
            }
            orig(self);
        }

        private static bool DontDoNormalKnockbackWhenLaunched(On.RoR2.HealthComponent.orig_IsImmuneToPhysicsForces orig, HealthComponent self)
        {
            return orig(self) || self.body.HasBuff(Buffs.launchedBuff);
        }

        private static void TakeDamageHook(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
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
            if (NetworkServer.active && self && !damageInfo.rejected && 
                damageInfo.HasModdedDamageType(Miscellaneous.DamageTypes.chaosSnapRandom) && 
                ChaosSnapManager.instance && !ChaosSnapManager.instance.TryGetInstance(self.gameObject, out _))
            {
                ChaosSnapManager.instance.TeleportBodyToRandomNode(self.body, 30f, 50f);
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
                        LaunchManager.onTryLaunchServer?.Invoke(self, damageInfo);
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
        private static void TakeDamageILHook(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Instruction takeDamageForce = null;
            int compilerGeneratedClassIndex = -1;
            if (/*c.TryGotoNext(x => x.MatchCallOrCallvirt(AccessTools.Method(typeof(HealthComponent),
                nameof(HealthComponent.TakeDamageForce), [typeof(DamageInfo), typeof(bool), typeof(bool)]))*/
                c.TryGotoNext(x => x.MatchLdloca(out compilerGeneratedClassIndex),
                x => x.MatchLdarg(1), 
                x => x.MatchStfld(out var field) && field.Name.Equals("damageInfo"))
                &&
                c.TryGotoNext(x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.TakeDamageForce)))
                && 
                c.TryGotoPrev(x => x.MatchLdloc(compilerGeneratedClassIndex), 
                x => x.MatchLdfld(out var field) && field.Name.Equals("damageInfo"), // compiler generated classes........
                x => x.MatchLdfld(typeof(DamageInfo), nameof(DamageInfo.canRejectForce)),
                x => x.MatchBrtrue(out _)))
            {
                // turn if (!damageInfo.canRejectForce) { TakeDamageForce(); }
                // into if ((!alive && body.HasBuff(suppressDeathBuff)) || !damageInfo.canRejectForce) { TakeDamageForce(); }
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<HealthComponent>(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(HealthComponent), nameof(HealthComponent.alive)).Name);
                c.Emit(OpCodes.Brtrue, c.Next);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit<HealthComponent>(OpCodes.Ldfld, nameof(HealthComponent.body));
                c.Emit(OpCodes.Ldsfld, typeof(HedgehogUtils.Buffs).GetField("suppressDeathBuff"));
                c.Emit<BuffDef>(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(BuffDef), nameof(BuffDef.buffIndex)).Name);
                c.Emit<CharacterBody>(OpCodes.Callvirt, nameof(CharacterBody.HasBuff));

                c.Index += 4;
                takeDamageForce = c.Next;
                c.Index -= 4;
                c.Emit(OpCodes.Brtrue, takeDamageForce);
            }
            else
            {
                Log.Error($"{il.Method.Name} IL FAILED");
            }
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
                if (Forms.Forms.GetFormHandlerObject(SuperFormDef.superFormDef.formIndex))
                {
                    if (Forms.Forms.GetFormHandlerObject(SuperFormDef.superFormDef.formIndex).TryGetComponent(out SuperSonicHandler emeraldSpawner))
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
                        if (!RoR2.Language.IsTokenInvalid(component.activeForm.cachedName + "_PREFIX"))
                        {
                            string text = orig(bodyObject);
                            text = RoR2.Language.GetStringFormatted(component.activeForm.cachedName + "_PREFIX", new object[]
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

        private static void StageBegin(Stage stage)
        {
            if (!NetworkServer.active) return;

            NetworkServer.Spawn(GameObject.Instantiate<GameObject>(ChaosSnapManager.prefab));
            #region Form Handlers
            SceneDef scene = stage.sceneDef;

            foreach (FormDef form in FormCatalog.formsCatalog)
            {
                bool formAvailable = form.setIsEnabledFunc(form);

                if (!Forms.Forms.GetFormHandlerObject(form.formIndex) && formAvailable)
                {
                    Log.Message("Spawning new handler object for form " + form.ToString());
                    NetworkServer.Spawn(GameObject.Instantiate<GameObject>(Forms.Forms.GetFormHandlerPrefab(form.formIndex)));
                    form.enabled = true;
                }
                else
                {
                    Log.Message("Did NOT spawn handler object for form " + form.ToString());
                    form.enabled = false;
                    continue;
                }

                FormHandler formHandler = Forms.Forms.GetFormHandlerObject(form.formIndex).GetComponent<FormHandler>();

                formHandler.SetEvents(formAvailable);
            }
            #region Super Form Handler and Emeralds
            if (!Forms.Forms.GetFormHandlerObject(SuperFormDef.superFormDef.formIndex)) { return; }

            if (!Forms.Forms.GetFormHandlerObject(SuperFormDef.superFormDef.formIndex).TryGetComponent(out SuperSonicHandler emeraldSpawner)) { return; }

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
            if (typeof(Boost.SkillDefs.IBoostSkill).IsAssignableFrom(self.skillDef.GetType()))
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
            if (typeof(Boost.SkillDefs.IBoostSkill).IsAssignableFrom(self.skillDef.GetType()))
            {
                BoostLogic boost = self.characterBody.GetComponent<BoostLogic>();
                if (boost)
                {
                    boost.AddBoost(BoostLogic.boostRegenPerBandolier);
                }
            }
        }
        private static void SolusHeart1DefeatedVoiceline(On.EntityStates.SolusHeart.Phase1.Mission1.orig_OnEnter orig, EntityStates.SolusHeart.Phase1.Mission1 self)
        {
            orig(self);
            if (VoicelineManager.instance && NetworkServer.active)
            {
                VoicelineManager.instance.SendFinalBossDefeatedEvent(FinalBoss.SolusHeart1);
            }
        }
        private static void SolusHeart2DefeatedVoiceline(On.EntityStates.SolusHeart.Phase2.Mission2.orig_OnEnter orig, EntityStates.SolusHeart.Phase2.Mission2 self)
        {
            orig(self);
            if (VoicelineManager.instance && NetworkServer.active)
            {
                VoicelineManager.instance.SendFinalBossDefeatedEvent(FinalBoss.SolusHeart2);
            }
        }

        private static void SolusHeart3DefeatedVoiceline(On.EntityStates.SolusHeart.Death.SolusHeartDeathSequence.Start.orig_OnEnter orig, EntityStates.SolusHeart.Death.SolusHeartDeathSequence.Start self)
        {
            orig(self);
            if (VoicelineManager.instance && NetworkServer.active)
            {
                VoicelineManager.instance.SendFinalBossDefeatedEvent(FinalBoss.SolusHeart3);
            }
        }
        private static void SolusHeartPhaseStartVoiceline(On.EntityStates.SolusHeart.SolusHeartTransformationBaseState.orig_OnEnter orig, EntityStates.SolusHeart.SolusHeartTransformationBaseState self)
        {
            orig(self);
            if (VoicelineManager.instance && NetworkServer.active)
            {
                if (self is EntityStates.SolusHeart.Phase1.SolusHeart1)
                {
                    VoicelineManager.instance.SendFinalBossStartEvent(FinalBoss.SolusHeart2);
                }
                else if (self is EntityStates.SolusHeart.Phase2.SolusHeart2)
                {
                    VoicelineManager.instance.SendFinalBossStartEvent(FinalBoss.SolusHeart3);
                }
            }
        }
        private static void SolusWingWeakPointDefeatedVoiceline(On.RoR2.SolusWing.SolusWingWeakPointsController.orig_onWeakpointDestroyedServer orig, RoR2.SolusWing.SolusWingWeakPointsController self, GameObject weakpointObject, CharacterBody body)
        {
            orig(self, weakpointObject, body);
            if (VoicelineManager.instance && NetworkServer.active)
            {
                VoicelineManager.instance.SendFinalBossDefeatedEvent(FinalBoss.SolusWingWeakPoint);
            }
        }
    }
}
