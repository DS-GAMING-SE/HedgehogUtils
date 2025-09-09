using RoR2;
using System;
using UnityEngine;
using R2API;
using UnityEngine.Networking;
using HedgehogUtils.Internal;
using R2API.Networking;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using LookingGlass.ItemStatsNameSpace;
using LookingGlass.LookingGlassLanguage;
using System.Linq;
using HedgehogUtils.Boost.EntityStates;
using UnityEngine.UI;

namespace HedgehogUtils.Boost
{
    public static class OnHooks
    {
        public static void Initialize()
        {
            On.RoR2.GenericSkill.CanApplyAmmoPack += CanApplyAmmoPackToBoost;
            On.RoR2.GenericSkill.ApplyAmmoPack += ApplyAmmoPackToBoost;
            On.RoR2.UI.HUD.Awake += CreateBoostMeterUI;
            // I realized it's probably a bad idea to make multiple hooks for hte same method in the same mod. Seems like it might be a performance issue. There's a Recalc hook in SuperForms.Stats
            // that's meant to be here but I put it there to not have duplicate hooks for the same method
        }

        private static bool CanApplyAmmoPackToBoost(On.RoR2.GenericSkill.orig_CanApplyAmmoPack orig, GenericSkill self)
        {
            if (typeof(EntityStates.Boost).IsAssignableFrom(self.activationState.stateType))
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
            if (typeof(EntityStates.Boost).IsAssignableFrom(self.activationState.stateType))
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