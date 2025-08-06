using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.Security.Cryptography;
using TMPro;
using RoR2.Audio;
using static RoR2.OverlapAttack;
using System.Reflection;
using EntityStates;

namespace HedgehogUtils.Launch
{
    public class LaunchProjectileController : NetworkBehaviour
    {   
        private const float attacksPerSecond = 10f;
        private const float wallCollideDamageMultiplier = 0.5f;
        public bool crit;
        public float damage;

        public Vector3 movementVector;

        public float duration;
        public const float baseDuration = 0.85f;
        private const float fadeDurationPercent = 0.8f;
        private const float noImpactDuration = 0.2f;
        private const float hitStopDuration = 0.1f;

        protected OverlapAttack attack;
        private float attackTimer;
        public float age;
        protected bool collidedWithWall;

        public CharacterBody attacker;

        protected GameObject vfxObject;
        protected Renderer vfxRenderer;
        protected VehicleSeat vehicle;
        protected Rigidbody rigidbody;
        protected bool vehicleInit;
        protected SphereCollider collider;
        protected HitBoxGroup hitBoxGroup;

        protected float radius;

        protected CharacterModel characterModel;
        protected CharacterBody body;



        public void Awake()
        {
            vehicle = base.GetComponent<VehicleSeat>();
            rigidbody = base.GetComponent<Rigidbody>();
            collider = base.GetComponent<SphereCollider>();
            hitBoxGroup = base.GetComponent<HitBoxGroup>();

            vehicle.enterVehicleAllowedCheck.AddCallback(new CallbackCheck<Interactability, CharacterBody>.CallbackDelegate(NoInteractingWithLaunchVehicle));
            vehicle.exitVehicleAllowedCheck.AddCallback(new CallbackCheck<Interactability, CharacterBody>.CallbackDelegate(NoInteractingWithLaunchVehicle));
        }

        public void Start()
        {
            if (vehicle.passengerBodyObject)
            {
                VehicleReady();
            }
        }

        public void VehicleReady()
        {
            vehicleInit = true;
            body = vehicle.passengerInfo.body;
            radius = 2.5f;
            if (body)
            {
                radius = Mathf.Max(radius, body.bestFitRadius);

                VFXAura();

                collider.radius = radius;
            }
            Physics.IgnoreCollision(collider, vehicle.passengerInfo.collider, true);
            PrepareOverlapAttack();
        }

        protected virtual void VFXAura()
        {
            vfxObject = UnityEngine.Object.Instantiate(crit ? Assets.launchCritAuraEffect : Assets.launchAuraEffect, base.transform);
            vfxObject.transform.localScale *= radius;
            vfxRenderer = vfxObject.transform.Find("Aura").GetComponent<Renderer>();
        }

        public void FixedUpdate()
        {
            if (!vehicleInit)
            {
                if (vehicle.passengerBodyObject)
                {
                    VehicleReady();
                }
                else
                {
                    return;
                }
            }

            age += Time.fixedDeltaTime;

            Move();

            AttemptAttack();

            if (age > duration && NetworkServer.active)
            {
                Destroy(base.gameObject); // Gotta make sure there's time for client's buff to be removed before the launch ends so that their death after the launch won't be cancelled
            }
        }

        public void Move()
        {
            float finalSpeed = movementVector.magnitude;
            if (age > duration * fadeDurationPercent)
            {
                float lerp = (age - (duration * fadeDurationPercent)) - (duration * (1 - fadeDurationPercent));
                if (vfxRenderer)
                {
                    vfxRenderer.material.SetFloat("_AlphaBoost", Mathf.Lerp(0.2f, 0f, lerp));
                }
                finalSpeed = Mathf.Lerp(movementVector.magnitude, movementVector.magnitude / 2, lerp);
            }

            this.rigidbody.rotation = Quaternion.LookRotation(movementVector.normalized);
            this.rigidbody.velocity = age <= hitStopDuration ? Vector3.zero : movementVector.normalized * finalSpeed;
        }

        public void AttemptAttack()
        {
            if (Util.HasEffectiveAuthority(base.gameObject))
            {
                attackTimer += Time.fixedDeltaTime;
                if (attackTimer > (1 / attacksPerSecond))
                {
                    attackTimer %= (1 / attacksPerSecond);
                    attack.Fire();
                    /*if (attack.Fire())
                    {
                        Destroy(base.gameObject);
                    }*/
                }
            }
        }


        public void OnDestroy()
        {
            if (NetworkServer.active)
            {
                if (body)
                {
                    body.RemoveBuff(Buffs.launchedBuff);

                    if (body.healthComponent && !body.healthComponent.alive)
                    {
                        CharacterDeathBehavior death = body.gameObject.GetComponent<CharacterDeathBehavior>();
                        if (death)
                        {
                            death.OnDeath();
                        }
                    }
                }
            }
            if (attacker && Util.HasEffectiveAuthority(attacker.gameObject))
            {
                FinalImpactAttack();
            }
        }

        protected void FinalImpactAttack()
        {
            ResizeHitBox(3.5f);
            attack.Fire();
        }

        protected virtual void PrepareOverlapAttack()
        {
            attack = new OverlapAttack();
            ResizeHitBox(2f);
            attack.procCoefficient = 1;
            attack.attacker = attacker.gameObject;
            attack.isCrit = crit;
            attack.damage = damage;
            attack.damageType = DamageType.Stun1s;
            attack.teamIndex = attacker.teamComponent.teamIndex;
            attack.attackerFiltering = AttackerFiltering.NeverHitSelf;
            attack.pushAwayForce = 1500f;
            attack.hitEffectPrefab = crit ? Assets.launchCritHitEffect : Assets.launchHitEffect;
            attack.impactSound = NetworkSoundEventCatalog.FindNetworkSoundEventIndex("Play_beetle_guard_impact"); // I don't think this line actually works and I have no idea why
            attack.addIgnoredHitList(body.healthComponent);
        }

        private void ResizeHitBox(float mult)
        {
            Transform hitbox = hitBoxGroup.hitBoxes[0].transform;
            float size = radius * mult;
            hitbox.localScale = new Vector3(size, size, size);
            attack.hitBoxGroup = hitBoxGroup;
        }

        public void Restart(CharacterBody attacker, Vector3 direction, float damage, bool crit, float speed, float procCoefficient)
        {
            this.age = 0;
            this.attacker = attacker;
            this.movementVector = direction * speed;
            this.damage = damage;
            this.crit = crit;
            this.duration = baseDuration * procCoefficient;
            if (attacker.characterMotor)
            {
                Physics.IgnoreCollision(collider, attacker.characterMotor.capsuleCollider, true);
            }
        }

        public void OnCollisionStay(Collision collisionInfo)
        {
            if (collidedWithWall || !NetworkServer.active || !vehicleInit || (age < noImpactDuration)) { return; }
            if (collisionInfo.impulse.magnitude < 20f) {  return; }

            EffectManager.SimpleEffect
                (radius >= 5 ? Assets.launchWallCollisionLargeEffect : Assets.launchWallCollisionEffect,
                collisionInfo.contacts[0].point,
                Quaternion.FromToRotation(Vector3.up, Vector3.Lerp(collisionInfo.contacts[0].normal, collisionInfo.impulse.normalized, 0.5f)), 
                true);

            CollideWithWallDamage(collisionInfo.contacts[0].point);
            collidedWithWall = true;
            Destroy(base.gameObject);
        }

        protected void CollideWithWallDamage(Vector3 position)
        {
            HealthComponent healthComponent = body.healthComponent;
            if (healthComponent && healthComponent.alive && !body.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.attacker = attacker.gameObject;
                damageInfo.inflictor = attacker.gameObject;
                damageInfo.force = Vector3.zero;
                damageInfo.damage = damage * wallCollideDamageMultiplier;
                damageInfo.crit = crit;
                damageInfo.position = position;
                damageInfo.procCoefficient = 0;
                healthComponent.TakeDamage(damageInfo);
                GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
            }
        }

        private static void NoInteractingWithLaunchVehicle(CharacterBody callback, ref Interactability? resultOverride)
        {
            resultOverride = new Interactability?(Interactability.Disabled);
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(crit);
                writer.Write(damage);
                writer.Write(movementVector);
                writer.Write(attacker.netId);
            }
            bool flag = false;
            /*if ((base.syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(crit);
            }
            if ((base.syncVarDirtyBits & 2U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(damage);
            }
            if ((base.syncVarDirtyBits & 4U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(movementVector);
            }
            if ((base.syncVarDirtyBits & 8U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(attacker.netId);
            }*/
            return flag;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                this.crit = reader.ReadBoolean();
                this.damage = reader.ReadSingle();
                this.movementVector = reader.ReadVector3();
                this.attacker = reader.ReadNetworkIdentity().gameObject.GetComponent<CharacterBody>();
            }
            /*int num = (int)reader.ReadPackedUInt32();
            if ((num & 1U) != 0U)
            {
                this.crit = reader.ReadBoolean();
            }
            if ((num & 2U) != 0U)
            {
                this.damage = reader.ReadSingle();
            }
            if ((num & 4U) != 0U)
            {
                this.movementVector = reader.ReadVector3();
            }
            if ((num & 8U) != 0U)
            {
                this.attacker = reader.ReadNetworkIdentity().gameObject.GetComponent<CharacterBody>();
            }*/
        }
    }
}
