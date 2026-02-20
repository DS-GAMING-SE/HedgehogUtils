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
        public float timeBetweenEmotes = 5f;
        public float emoteTimer { get; private set; }
        public override void OnEnter()
        {
            base.OnEnter();
            emoteTimer = timeBetweenEmotes;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (base.isAuthority)
            {
                if (!ShouldInterrupt())
                {
                    emoteTimer -= Time.fixedDeltaTime;
                    if (emoteTimer < 0)
                    {
                        SetNextStateToIdleExtra();
                        emoteTimer = timeBetweenEmotes;
                        return;
                    }
                }
                else
                {
                    emoteTimer = timeBetweenEmotes;
                }
            }
        }
        public abstract void SetNextStateToIdleExtra();

        public virtual bool ShouldInterrupt()
        {
            return Helpers.IsDoingSomething(characterMotor, inputBank, true, true, false, false);
        }
    }
}
