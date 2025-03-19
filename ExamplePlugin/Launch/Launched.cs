﻿using EntityStates;
using RoR2;
using RoR2.Audio;
using RoR2.Skills;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils.Launch
{
    public class Launched : EntityState
    {
        private SetStateOnHurt idleStateMachineProvider;
        
        public override void OnEnter()
        {
            base.OnEnter();
            if (base.characterBody)
            {
                base.characterBody.isSprinting = false;
            }
            if (base.characterDirection)
            {
                base.characterDirection.moveVector = base.characterDirection.forward;
            }
            if (base.rigidbodyMotor)
            {
                base.rigidbodyMotor.moveVector = Vector3.zero;
            }

            if (base.isAuthority)
            {
                idleStateMachineProvider = base.gameObject.GetComponent<SetStateOnHurt>();
                if (idleStateMachineProvider)
                {
                    EntityStateMachine[] array = idleStateMachineProvider.idleStateMachine;
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i] != this.outer)
                        {
                            array[i].SetNextState(new Idle());
                        }
                    }
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            //Log.Warning("Has Buff?: " + base.characterBody.HasBuff(Buffs.launchedBuff));
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Vehicle;
        }
    }
}