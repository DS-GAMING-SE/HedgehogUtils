using RoR2;
using System;
using UnityEngine;
using R2API;

namespace HedgehogUtils.Miscellaneous
{
    public static class MomentumStats
    {
        public static void Initialize()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStats;
        }

        public static void RecalculateStats(CharacterBody self, RecalculateStatsAPI.StatHookEventArgs stats)
        {
            MomentumPassive momentum = self.GetComponent<MomentumPassive>();
            if (momentum)
            {
                if (momentum.momentum >= 0)
                {
                    stats.moveSpeedMultAdd += (momentum.momentum * momentum.maxSpeedMultiplier);
                }
                else
                {
                    stats.moveSpeedReductionMultAdd += Mathf.Abs(momentum.momentum) * momentum.minSpeedMultiplier;
                }
            }
        }
    }
}