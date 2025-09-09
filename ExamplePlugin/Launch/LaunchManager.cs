using EntityStates;
using HedgehogUtils.Forms.SuperForm;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace HedgehogUtils.Launch
{   
    public static class LaunchManager
    {
        public static GameObject launchProjectilePrefab;
        
        public static string[] bodyBlacklist = { "BrotherBody", "BrotherGlassBody", "BrotherHurtBody", "FalseSonBossBody", "FalseSonBossBodyBrokenLunarShard", "FalseSonBossBodyLunarShard", "MagmaWormBody", "ElectricWormBody", "ShopkeeperBody", "MiniVoidRaidCrabBodyBase", "MiniVoidRaidCrabBodyPhase1", "MiniVoidRaidCrabBodyPhase2", "MiniVoidRaidCrabBodyPhase3", "ScorchlingBody", "GravekeeperTrackingFireball", "DeltaConstructBody" };

        public const float launchSpeed = 55f;

        public const float baseDuration = 0.85f;

        public static void Launch(CharacterBody target, CharacterBody attacker, Vector3 direction, float speed, float damage, float wallCollisionDamage, bool crit, float procCoefficient, float duration)
        {
            if (!NetworkServer.active || !TargetCanBeLaunched(target, out LaunchProjectileController currentLaunchController)) { return; }

            if (currentLaunchController)
            {
                currentLaunchController.Restart(attacker, direction, speed, damage, wallCollisionDamage, crit, procCoefficient, duration);
                return;
            }

            GameObject launchProjectile = UnityEngine.GameObject.Instantiate(launchProjectilePrefab, target.corePosition, Quaternion.LookRotation(direction));
            LaunchProjectileController launchController = launchProjectile.GetComponent<LaunchProjectileController>();
            launchController.Restart(attacker, direction, speed, damage, wallCollisionDamage, crit, procCoefficient, duration);
            VehicleSeat vehicle = launchProjectile.GetComponent<VehicleSeat>();
            vehicle.AssignPassenger(target.gameObject);
            target.AddBuff(Buffs.launchedBuff);
            NetworkServer.Spawn(launchProjectile);
        }

        public static void Launch(CharacterBody target, CharacterBody attacker, Vector3 direction, float damage, bool crit, float procCoefficient)
        {
            Launch(target, attacker, direction, LaunchManager.launchSpeed, damage, damage * 0.5f, crit, 1f, LaunchManager.baseDuration * procCoefficient);
        }

        public static Vector3 AngleAwayFromGround(Vector3 input, Vector3 groundNormal)
        {
            Vector3 adjusted = input;
            if (Vector3.Dot(input.normalized, groundNormal.normalized) <= 0.1f)
            {
                adjusted = Vector3.ProjectOnPlane(adjusted, groundNormal).normalized;
                adjusted = Vector3.Lerp(adjusted, groundNormal, 0.1f);
                adjusted *= input.magnitude;
            }
            return adjusted;
        }

        public static Vector3 AngleTowardsEnemies(Vector3 direction, Vector3 position, GameObject target, TeamIndex attackerTeam)
        {
            BullseyeSearch search = new BullseyeSearch();
            search.teamMaskFilter = TeamMask.GetEnemyTeams(attackerTeam);
            search.filterByLoS = true;
            search.searchOrigin = position;
            search.searchDirection = direction;
            search.sortMode = BullseyeSearch.SortMode.Angle;
            search.maxDistanceFilter = 0.9f * launchSpeed;
            search.minDistanceFilter = 0;
            search.maxAngleFilter = 45;
            search.RefreshCandidates();
            search.FilterOutGameObject(target);
            HurtBox hit = search.GetResults().FirstOrDefault();
            if (hit)
            {
                return ((hit.transform.position - (target.transform.position) + new Vector3(0, 0.5f, 0)).normalized) * direction.magnitude;
            }
            return direction;
        }

        public static bool TargetCanBeLaunched(CharacterBody target)
        {
            return TargetCanBeLaunched(target, out _);
        }

        public static bool TargetCanBeLaunched(CharacterBody target, out LaunchProjectileController currentLaunchController)
        {
            currentLaunchController = null;
            if (!(Config.LaunchBodyBlacklist().Value && bodyBlacklist.Contains(BodyCatalog.GetBodyName(target.bodyIndex)) || target.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreKnockback)) )
            {
                EntityStateMachine bodyState = EntityStateMachine.FindByCustomName(target.gameObject, "Body");
                if (bodyState && bodyState.CanInterruptState(InterruptPriority.Vehicle))
                {
                    if (target.HasBuff(Buffs.launchedBuff))
                    {
                        if (target.currentVehicle && target.currentVehicle.gameObject.TryGetComponent<LaunchProjectileController>(out LaunchProjectileController existingController))
                        {
                            currentLaunchController = existingController;
                            return existingController.age > 0.3f;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool AttackCanLaunch(HealthComponent target, CharacterBody attacker, DamageInfo damageInfo)
        {
            return AttackCanLaunch(target.body, attacker, damageInfo.rejected, damageInfo.procCoefficient, damageInfo.force);
        }

        public static bool AttackCanLaunch(CharacterBody target, CharacterBody attacker, bool targetInvincible, float procCoefficient, Vector3 force)
        {
            if (target && attacker)
            {
                if (!targetInvincible && procCoefficient > 0.3f)
                {
                    Rigidbody rigidbody = target.gameObject.GetComponent<Rigidbody>();
                    if (rigidbody && rigidbody.mass <= force.magnitude)
                    {
                        if (target.characterMotor && target.characterMotor.isGrounded)
                        {
                            return ((Vector3.Dot(force.normalized, target.characterMotor.estimatedGroundNormal) >= -0.6f));
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void Initialize()
        {
            launchProjectilePrefab = Assets.mainAssetBundle.LoadAsset<GameObject>("LaunchProjectile");

            launchProjectilePrefab.AddComponent<NetworkIdentity>();

            VehicleSeat vehicle = launchProjectilePrefab.AddComponent<VehicleSeat>();
            vehicle.disablePassengerMotor = false;
            vehicle.isEquipmentActivationAllowed = false;
            vehicle.shouldProximityHighlight = false;
            vehicle.seatPosition = vehicle.transform;
            vehicle.passengerState = new EntityStates.SerializableEntityStateType(typeof(Launched));
            vehicle.hidePassenger = false;
            vehicle.exitVelocityFraction = 0.3f;

            launchProjectilePrefab.AddComponent<LaunchProjectileController>();

            HitBoxGroup hitBoxGroup = launchProjectilePrefab.AddComponent<HitBoxGroup>();
            HitBox hitBox = launchProjectilePrefab.transform.Find("Hitbox").gameObject.AddComponent<HitBox>();
            hitBox.gameObject.layer = LayerIndex.entityPrecise.intVal;
            hitBoxGroup.hitBoxes = new HitBox[] { hitBox };

            launchProjectilePrefab.gameObject.layer = LayerIndex.projectileWorldOnly.intVal;

            launchProjectilePrefab.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Extrapolate;

            PrefabAPI.RegisterNetworkPrefab(launchProjectilePrefab);
        }
    }
}
