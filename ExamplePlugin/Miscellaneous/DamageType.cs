using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace HedgehogUtils.Miscellaneous
{
    public static class DamageTypes
    {
        public static DamageAPI.ModdedDamageType chaosSnapRandom;
        public static void Initialize()
        {
            chaosSnapRandom = DamageAPI.ReserveDamageType();
        }
    }
}