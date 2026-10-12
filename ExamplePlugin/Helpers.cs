using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace HedgehogUtils
{
    public static class Helpers
    {
        public static string ScepterDescription(string desc)
        {
            return "\n<color=#d299ff>SCEPTER: " + desc + "</color>";
        }

        public static string SuperFormText(string desc)
        {
            return $"<color=#ffee00>{desc}</color>";
        }

        public const string wipIcon = "<sprite name=\"WIP\">";

        public static T[] Append<T>(ref T[] array, List<T> list)
        {
            var orig = array.Length;
            var added = list.Count;
            Array.Resize<T>(ref array, orig + added);
            list.CopyTo(array, orig);
            return array;
        }

        public static Func<T[], T[]> AppendDel<T>(List<T> list) => (r) => Append(ref r, list);

        public static void RemoveTempOrPermanentItem(this Inventory inventory, ItemIndex item, int count)
        {
            if (inventory)
            {
                int itemsToRemove = count;
                if (inventory.GetItemCountTemp(item) > 0)
                {
                    int removing = Math.Min(inventory.GetItemCountTemp(item), itemsToRemove);
                    inventory.RemoveItemTemp(item, removing);
                    itemsToRemove -= removing;
                }
                if (itemsToRemove > 0)
                {
                    inventory.RemoveItemPermanent(item, Math.Min(inventory.GetItemCountPermanent(item), itemsToRemove));
                }
            }
        }
        // I never realized CharacterMotor just has an isFlying var in it so all these Flying helper methods are basically useless
        public static bool Flying(GameObject gameObject, out ICharacterFlightParameterProvider flight)
        {
            if (gameObject) 
            { 
                flight = gameObject.GetComponent<ICharacterFlightParameterProvider>(); 
                return Flying(flight); 
            } 
            else 
            { 
                flight = null; 
                return false; 
            }
        }

        public static bool Flying(GameObject gameObject)
        {
            return Flying(gameObject, out _);
        }
        public static bool Flying(ICharacterFlightParameterProvider flight)
        {
            return flight != null && flight.isFlying;
        }

        public static void EndChrysalis(GameObject gameObject)
        {
            if (NetworkServer.active)
            {
                JetpackController chrysalis = JetpackController.FindJetpackController(gameObject);
                if (chrysalis)
                {
                    if (chrysalis.stopwatch >= chrysalis.duration)
                    {
                        UnityEngine.Object.Destroy(chrysalis.gameObject);
                    }
                }

            }
        }
        public static bool IsDoingSomething(CharacterMotor characterMotor, InputBankTest inputBank, bool allowGrounded, bool allowFlying, bool allowAirborne, bool allowMoving)
        {
            return (!characterMotor ||
                (!allowAirborne && !characterMotor.isGrounded && (!allowFlying || !characterMotor.isFlying)) ||
                (!allowGrounded && characterMotor.isGrounded) ||
                ((!inputBank) ||
                (!allowMoving && inputBank.moveVector.sqrMagnitude >= Mathf.Epsilon) ||
                inputBank.skill1.down ||
                inputBank.skill2.down ||
                inputBank.skill3.down ||
                inputBank.skill4.down ||
                inputBank.jump.down));
        }

        public static LocalUser FindLocalUser(CharacterBody characterBody)
        {
            if (characterBody)
            {
                foreach (LocalUser lu in LocalUserManager.readOnlyLocalUsersList)
                {
                    if (lu.cachedBody == characterBody)
                    {
                        return lu;
                    }
                }
            }
            return null;
        }

        public static T CopySkillDef<T>(SkillDef originDef) where T : SkillDef
        {
            T skillDef = ScriptableObject.CreateInstance<T>();
            skillDef.skillName = originDef.skillName;
            (skillDef as ScriptableObject).name = ((ScriptableObject)originDef).name;
            skillDef.skillNameToken = originDef.skillNameToken;
            skillDef.skillDescriptionToken = originDef.skillDescriptionToken;
            skillDef.icon = originDef.icon;

            skillDef.activationState = originDef.activationState;
            skillDef.activationStateMachineName = originDef.activationStateMachineName;
            skillDef.baseMaxStock = originDef.baseMaxStock;
            skillDef.baseRechargeInterval = originDef.baseRechargeInterval;
            skillDef.beginSkillCooldownOnSkillEnd = originDef.beginSkillCooldownOnSkillEnd;
            skillDef.canceledFromSprinting = originDef.canceledFromSprinting;
            skillDef.forceSprintDuringState = originDef.forceSprintDuringState;
            skillDef.fullRestockOnAssign = originDef.fullRestockOnAssign;
            skillDef.interruptPriority = originDef.interruptPriority;
            skillDef.resetCooldownTimerOnUse = originDef.resetCooldownTimerOnUse;
            skillDef.isCombatSkill = originDef.isCombatSkill;
            skillDef.mustKeyPress = originDef.mustKeyPress;
            skillDef.cancelSprintingOnActivation = originDef.cancelSprintingOnActivation;
            skillDef.rechargeStock = originDef.rechargeStock;
            skillDef.requiredStock = originDef.requiredStock;
            skillDef.stockToConsume = originDef.stockToConsume;

            skillDef.keywordTokens = originDef.keywordTokens;

            skillDef.suppressSkillActivation = originDef.suppressSkillActivation;
            skillDef.hideCooldown = originDef.hideCooldown;
            skillDef.hideStockCount = originDef.hideStockCount;
            skillDef.autoHandleLuminousShot = originDef.autoHandleLuminousShot;
            skillDef.triggeredByPressRelease = originDef.triggeredByPressRelease;
            skillDef.isCooldownBlockedUntilManuallyReset = originDef.isCooldownBlockedUntilManuallyReset;

            return skillDef;
        }

        public static T CopyBoostSkillDef<T>(Boost.SkillDefs.BoostSkillDef originDef) where T : SkillDef, Boost.SkillDefs.IBoostSkill
        {
            SerializableEntityStateType boostIdle = originDef.boostIdleState;
            SerializableEntityStateType brakeState = originDef.brakeState;
            T boostDef = CopySkillDef<T>(originDef);
            boostDef.boostIdleState = boostIdle;
            boostDef.brakeState = brakeState;
            boostDef.boostHUDColor = originDef.boostHUDColor;
            return boostDef;
        }
        public static T CopyBoostSkillDef<T>(Boost.SkillDefs.RequiresFormBoostSkillDef originDef) where T : SkillDef, Boost.SkillDefs.IBoostSkill
        {
            SerializableEntityStateType boostIdle = originDef.boostIdleState;
            SerializableEntityStateType brakeState = originDef.brakeState;
            T boostDef = CopySkillDef<T>(originDef);
            boostDef.boostIdleState = boostIdle;
            boostDef.brakeState = brakeState;
            boostDef.boostHUDColor = originDef.boostHUDColor;
            return boostDef;
        }
        public static Material MetalFresnel(this Material mat, Texture mask = null, float power = 1f, float boost = 1f)
        {
            return FresnelEmission(mat, AssetAsyncReferenceManager<Texture>.LoadAsset(new AssetReferenceT<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Drone_Tech.texDroneTechRamp_png)).WaitForCompletion(), mask, power, boost);
        }
        public static Material GoldFresnel(this Material mat, Texture mask = null, float power = 1f, float boost = 1f)
        {
            return FresnelEmission(mat, AssetAsyncReferenceManager<Texture>.LoadAsset(new AssetReferenceT<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampDroneFire_png)).WaitForCompletion(), mask, power, boost);
        }
        public static Material FresnelEmission(this Material mat, Texture ramp, Texture mask, float power = 1f, float boost = 1f)
        {
            mat.SetTexture("_FresnelRamp", ramp);
            if (mask) mat.SetTexture("_FresnelMask", mask);
            mat.EnableKeyword("FRESNEL_EMISSION");
            mat.SetFloat("_FresnelPower", power);
            mat.SetFloat("_FresnelBoost", boost);
            return mat;
        }
        public static Material SpecularIgnoreAlpha(this Material mat)
        {
            mat.EnableKeyword("FORCE_SPEC");
            return mat;
        }
        public static Material CreateGlassMaterial()
        {
            Material glass = new Material(Addressables.LoadAssetAsync<Shader>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Shaders.HGCloudRemap_shader).WaitForCompletion());
            glass.name = "matHedgehogUtilsGlass";
            glass.SetFloat("_InvFade", 0f);
            glass.SetInt("_Cull", 2);
            glass.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampDefault_png).WaitForCompletion());
            glass.EnableKeyword("FRESNEL");
            glass.SetFloat("_FresnelPower", 0.5f);
            glass.SetFloat("_SrcBlend", 1f);
            glass.SetFloat("_DstBlend", 10f);
            glass.SetFloat("_AlphaBoost", 1f);
            return glass;
        }
        public static Material CreateGlassMaterial(Color color)
        {
            Material glass = CreateGlassMaterial();
            glass.SetColor("_TintColor", color);
            return glass;
        }
        public static Material CreateGlassMaterial(Texture texture)
        {
            Material glass = CreateGlassMaterial();
            glass.SetTexture("_MainTex", texture);
            return glass;
        }

        public static bool TryGetSkinDef(string bodyName, string skinNameToken, out SkinDef skin)
        {
            skin = null;
            if (!BodyCatalog.availability.available)
            {
                Log.Error("Attempted to find skin before the BodyCatalog was available");
                return false;
            }
            BodyIndex index = BodyCatalog.FindBodyIndex(bodyName);
            if (index != BodyIndex.None)
            {
                skin = SkinCatalog.FindSkinsForBody(index).First(x => x.nameToken == skinNameToken);
                return skin;
            }
            return false;
        }
    }
}