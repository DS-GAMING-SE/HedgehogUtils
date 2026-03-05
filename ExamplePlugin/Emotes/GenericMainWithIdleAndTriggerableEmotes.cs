using BepInEx.Configuration;
using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Emotes
{
    public abstract class GenericMainWithIdleAndTriggerableEmotes : GenericMainWithIdleEmote
    {
        public LocalUser localUser;

        // Thank you Paladin
        // Switch to rebindables
        protected bool TryEmote<T>(ConfigEntry<KeyCode> keybind, EntityStateMachine stateMachine) where T : EntityState, new()
        {
            if (base.isAuthority && Input.GetKeyDown(keybind.Value))
            {
                if (localUser == null) localUser = Helpers.FindLocalUser(base.characterBody);

                if (localUser != null && !localUser.isUIFocused)
                {
                    return stateMachine.SetInterruptState(new T(), InterruptPriority.Any);
                }
            }
            return false;
        }

        
    }
}
