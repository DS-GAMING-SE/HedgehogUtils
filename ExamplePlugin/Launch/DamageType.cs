using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace HedgehogUtils.Launch
{
    public static class DamageTypes
    {
        public static DamageAPI.ModdedDamageType launch;
        public static DamageAPI.ModdedDamageType launchOnKill;
        public static DamageAPI.ModdedDamageType removeLaunchAutoAim;
        public static void Initialize()
        {
            launch = DamageAPI.ReserveDamageType();
            launchOnKill = DamageAPI.ReserveDamageType();
            removeLaunchAutoAim = DamageAPI.ReserveDamageType();
        }
    }
}