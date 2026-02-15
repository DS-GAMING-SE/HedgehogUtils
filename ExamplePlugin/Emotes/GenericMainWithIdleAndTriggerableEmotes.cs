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
                FindLocalUser();

                if (localUser != null && !localUser.isUIFocused)
                {
                    return stateMachine.SetInterruptState(new T(), InterruptPriority.Any);
                }
            }
            return false;
        }

        private void FindLocalUser()
        {
            if (localUser == null)
            {
                if (base.characterBody)
                {
                    foreach (LocalUser lu in LocalUserManager.readOnlyLocalUsersList)
                    {
                        if (lu.cachedBody == base.characterBody)
                        {
                            this.localUser = lu;
                            break;
                        }
                    }
                }
            }
        }
    }
}
