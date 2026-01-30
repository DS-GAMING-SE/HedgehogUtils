using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Emotes
{
    public abstract class GenericMainWithIdleEmote : GenericCharacterMain
    {
        public float timeUntilEmote = 5f;
        public float emoteTimer { get; private set; }
        public override void OnEnter()
        {
            base.OnEnter();
            emoteTimer = timeUntilEmote;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (base.isAuthority)
            {
                if (base.characterBody && base.characterBody.inputBank.moveVector == Vector3.zero && base.characterMotor.isGrounded && !base.characterBody.inputBank.jump.down)
                {
                    emoteTimer -= Time.fixedDeltaTime;
                    if (emoteTimer < 0)
                    {
                        SetNextStateToIdleExtra();
                        emoteTimer = timeUntilEmote;
                        return;
                    }
                }
                else
                {
                    emoteTimer = timeUntilEmote;
                }
            }
        }
        public virtual void SetNextStateToIdleExtra()
        {
            
        }
    }
}
