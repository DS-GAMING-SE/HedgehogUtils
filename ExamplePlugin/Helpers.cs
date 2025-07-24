using RoR2;
using System;
using System.Collections.Generic;
using RoR2.Skills;
using EntityStates;
using UnityEngine;

namespace HedgehogUtils
{
    public static class Helpers
    {
        public static string ScepterDescription(string desc)
        {
            return "\n<color=#d299ff>SCEPTER: " + desc + "</color>";
        }

        public static T[] Append<T>(ref T[] array, List<T> list)
        {
            var orig = array.Length;
            var added = list.Count;
            Array.Resize<T>(ref array, orig + added);
            list.CopyTo(array, orig);
            return array;
        }

        public static Func<T[], T[]> AppendDel<T>(List<T> list) => (r) => Append(ref r, list);

        public static bool Flying(ICharacterFlightParameterProvider flight)
        {
            return flight != null && flight.isFlying;
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
    }
}