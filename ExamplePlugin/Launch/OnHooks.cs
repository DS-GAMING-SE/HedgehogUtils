using RoR2;
using R2API;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Launch
{
    public static class OnHooks
    {
        public static void Initialize()
        {
            On.RoR2.CharacterDeathBehavior.OnDeath += DontDieWhileLaunched;
            On.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool += DontDoNormalKnockbackWhenLaunched;
            On.RoR2.HealthComponent.TakeDamage += Launch;
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
            if (damageInfo.damageType.HasModdedDamageType(DamageTypes.launch) || damageInfo.damageType.HasModdedDamageType(DamageTypes.launchOnKill)) { return; }
            orig(self, damageInfo, alwaysApply, disableAirControlUntilCollision);
        }

        private static void Launch(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            if (damageInfo.attacker && damageInfo.attacker.TryGetComponent<CharacterBody>(out CharacterBody attackerBody))
            {
                if ((damageInfo.damageType.HasModdedDamageType(DamageTypes.launch)
                    || (damageInfo.damageType.HasModdedDamageType(DamageTypes.launchOnKill) && !self.alive))
                    && !damageInfo.rejected
                    && damageInfo.procCoefficient > 0.3f)
                {
                    Rigidbody rigidbody = self.gameObject.GetComponent<Rigidbody>();
                    if (rigidbody && rigidbody.mass <= damageInfo.force.magnitude)
                    {
                        // Launch is happening
                        Vector3 launchDirection = damageInfo.force.normalized;
                        if (self.body && self.body.characterMotor)
                        {
                            if (self.body.characterMotor.isGrounded)
                            {
                                if (Vector3.Dot(launchDirection, self.body.characterMotor.estimatedGroundNormal) < -0.6f) { return; }
                                launchDirection = LaunchManager.AngleAwayFromGround(launchDirection, self.body.characterMotor.estimatedGroundNormal);
                            }
                        }
                        launchDirection = LaunchManager.AngleTowardsEnemies(launchDirection, self.transform.position, self.gameObject, attackerBody.teamComponent.teamIndex);
                        launchDirection = launchDirection.normalized;
                        LaunchManager.Launch(self.body, attackerBody, launchDirection, LaunchManager.launchSpeed, damageInfo.damage, damageInfo.damage * 0.5f, damageInfo.crit, 1f, LaunchManager.baseDuration * damageInfo.procCoefficient);
                    }
                }
            }
        }
    }
}
