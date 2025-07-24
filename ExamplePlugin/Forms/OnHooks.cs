using RoR2;
using System;
using UnityEngine;
using R2API;
using UnityEngine.Networking;
using HedgehogUtils.Internal;
using HedgehogUtils.Forms.SuperForm;
using R2API.Networking;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using LookingGlass.ItemStatsNameSpace;
using LookingGlass.LookingGlassLanguage;
using System.Linq;

namespace HedgehogUtils.Forms
{
    public static class OnHooks
    {
        public static void Initialize()
        {
            On.RoR2.HealthComponent.TakeDamage += TakeDamage;

            On.RoR2.Util.GetBestBodyName += SuperNamePrefix;

            On.RoR2.SceneDirector.Start += SceneDirectorOnStart;

            if (HedgehogUtilsPlugin.lookingGlassLoaded)
            {
                RoR2Application.onLoad += LookingGlassSetup;
            }

            On.RoR2.GenericPickupController.Start += EmeraldDropSound;
        }

        private static void TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damage)
        {
            if (!NetworkServer.active) { orig(self, damage); }
            if (self)
            {
                if (self.gameObject.TryGetComponent(out FormComponent formComponent))
                {
                    if (formComponent.activeForm)
                    {
                        if (formComponent.activeForm.invincible)
                        {
                            damage.rejected = true;
                            EffectManager.SpawnEffect(HealthComponent.AssetReferences.damageRejectedPrefab, new EffectData
                            {
                                origin = damage.position
                            }, true);
                        }
                    }
                }
            }
            orig(self, damage);
        }
        public static void LookingGlassSetup()
        {
            if (RoR2.Language.languagesByName.TryGetValue("en", out RoR2.Language language))
            {
                RegisterLookingGlassBuff(language, Buffs.superFormBuff, "Super Form", $"Immune to all attacks. Gain <style=cIsDamage>+{100f * StaticValues.superSonicBaseDamage}% damage</style>, <style=cIsUtility>+{100f * StaticValues.superSonicAttackSpeed}% attack speed</style>, and <style=cIsUtility>+{100f * StaticValues.superSonicMovementSpeed}% base movement speed</style>.");
            }

            ItemStatsDef emeraldStats = new ItemStatsDef();
            emeraldStats.descriptions.Add("Unique Emeralds: ");
            emeraldStats.valueTypes.Add(ItemStatsDef.ValueType.Stack);
            emeraldStats.measurementUnits.Add(ItemStatsDef.MeasurementUnits.Number);
            emeraldStats.calculateValuesNew = (luck, stackCount, procChance) =>
            {
                var list = new List<float>();
                if (Forms.formToHandlerObject.TryGetValue(SuperFormDef.superFormDef, out GameObject handler))
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
                PickupDef pickupDef = PickupCatalog.GetPickupDef(self.pickupIndex);
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
                bool someoneCanUseForm = false;
                foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
                {
                    if (form.allowedBodyList.BodyIsAllowed(BodyCatalog.FindBodyIndex(player.master.bodyPrefab)))
                    {
                        someoneCanUseForm = true;
                        break;
                    }
                }

                bool formAvailable = someoneCanUseForm && RunArtifactManager.instance.IsArtifactEnabled(Artifact.chaosEmeraldArtifactDef);
                // Complicated bool mess here is mostly just to make sure Chaos Emeralds should spawn and, by extension, Super Sonic should be available.
                // Checks metamorphosis, but metamorphosis is okay if emeralds can spawn without Sonic, etc

                if (!Forms.formToHandlerObject.ContainsKey(form) && formAvailable)
                {
                    Log.Message("Spawning new handler object for form " + form.ToString());
                    NetworkServer.Spawn(GameObject.Instantiate<GameObject>(Forms.formToHandlerPrefab.GetValueSafe(form)));
                }
                else
                {
                    Log.Message("Did NOT spawn handler object for form " + form.ToString());
                    continue;
                }

                FormHandler formHandler = Forms.formToHandlerObject.GetValueSafe(form).GetComponent(typeof(FormHandler)) as FormHandler;

                formHandler.SetEvents(formAvailable);
            }
            if (!Forms.formToHandlerObject.TryGetValue(SuperFormDef.superFormDef, out GameObject handler)) { return; }

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
        }
    }
}