using RoR2;
using System;
using UnityEngine;
using R2API;

namespace HedgehogUtils.Forms.SuperForm
{
    public static class Stats
    {
        public static void Initialize()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStats;
            On.RoR2.CharacterBody.RecalculateStats += RecalcStatAPIDoesntHaveAcceleration;
        }

        public static void RecalculateStats(CharacterBody self, RecalculateStatsAPI.StatHookEventArgs stats)
        {
            #region Super Form
            if (self.HasBuff(Buffs.superFormBuff))
            {
                stats.baseMoveSpeedAdd += StaticValues.superSonicMovementSpeed * self.baseMoveSpeed;
                stats.attackSpeedMultAdd += StaticValues.superSonicAttackSpeed;
                stats.damageMultAdd += StaticValues.superSonicBaseDamage;
                stats.jumpPowerMultAdd += StaticValues.superSonicJumpHeight;
                stats.armorAdd += StaticValues.superSonicArmor;
            }
            #endregion
        }

        private static void RecalcStatAPIDoesntHaveAcceleration(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            if (self.HasBuff(Buffs.superFormBuff))
            {
                self.acceleration *= 5;
            }
        }
    }
}