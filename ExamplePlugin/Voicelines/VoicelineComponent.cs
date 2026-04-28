using HG;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Audio;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Voicelines
{
    public abstract class SimpleVoicelineComponent : MonoBehaviour
    {
        public string soundBankFilePath;
        protected uint soundBankID;

        public uint currentVoicelineID;
        public VoicelinePriority currentVoicelinePriority;
        public bool isVoicelinePlaying { get; private set; }
        public void PlayVoiceline(string soundString, VoicelinePriority priority)
        {
            if (string.IsNullOrEmpty(soundString)) return;
            if (isVoicelinePlaying)
            {
                if (priority > currentVoicelinePriority)
                {
                    AkSoundEngine.StopPlayingID(currentVoicelineID);
                }
                else
                {
                    return;
                }
            }
            currentVoicelineID = AkSoundEngine.PostEvent(soundString, gameObject, (uint)AkCallbackType.AK_EndOfEvent, OnVoicelineEnd, null);
            currentVoicelinePriority = priority;
            isVoicelinePlaying = true;
        }
        public void PlayVoiceline(NetworkSoundEventIndex soundIndex, VoicelinePriority priority)
        {
            PlayVoiceline(NetworkSoundEventCatalog.GetEventNameFromNetworkIndex(soundIndex), priority);
        }
        public void PlayNetworkedVoiceline(NetworkSoundEventIndex soundIndex, VoicelinePriority priority)
        {
            new NetworkVoiceline(this, soundIndex, priority).Send(NetworkDestination.Clients);
        }
        public void StopCurrentVoiceline()
        {
            AkSoundEngine.StopPlayingID(currentVoicelineID);
            currentVoicelineID = 0;
            currentVoicelinePriority = 0;
            isVoicelinePlaying = false;
        }
        private void OnVoicelineEnd(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                currentVoicelineID = 0;
                currentVoicelinePriority = 0;
                isVoicelinePlaying = false;
            }
        }
        protected virtual void OnEnable()
        {
            if (!string.IsNullOrEmpty(soundBankFilePath)) soundBankID = SoundAPI.SoundBanks.Add(soundBankFilePath);
        }
        protected virtual void OnDisable()
        {
            if (!string.IsNullOrEmpty(soundBankFilePath)) SoundAPI.SoundBanks.Remove(soundBankID);
        }

        public static bool TryPlayVoiceline(GameObject gameObject, string soundString, VoicelinePriority priority)
        {
            if (gameObject.TryGetComponent<SimpleVoicelineComponent>(out var voiceline) && voiceline.enabled)
            {
                voiceline.PlayVoiceline(soundString, priority);
                return true;
            }
            return false;
        }
    }
    
    public abstract class VoicelineComponent : SimpleVoicelineComponent, ILifeBehavior
    {
        public CharacterBody characterBody;
        public GenericSkill voicelinesSkill;
        public string voicelinesSkillName = "Voicelines";
        public SkillDef voicelinesEnableSkillDef;

        public List<VoicelineComponent> nearbyVoices = new List<VoicelineComponent>();
        public virtual void SubscribeEvents()
        {
            /* Common events to use for voicelines
             * 
             * VoicelineManager events for stage entering and bosses stuff
             * CharacterBody.onJump for jumping
             * GlobalEventManager.OnClientDamageNotified for taking damage
             * GenericTransformationBase.OnGenericTransform for transforming. It's a static event, so make sure you're the one transforming. Event is global, so consider networking for random voiceline
             * If StageRanking mod, StageRankingPanel.OnStageRankingPanelEnd for reacting to your rank (With Util.HasEffectiveAuthority so lines aren't synced, no multiplayer overlapping lines)
            */
        }
        public virtual void UnsubscribeEvents()
        {

        }
        public virtual void OnDeathStart()
        {
            StopCurrentVoiceline();
            UnsubscribeEvents();
        }

        private void Awake()
        {
            characterBody = GetComponent<CharacterBody>();
        }
        public virtual bool ShouldEnableVoicelines()
        {
            if (characterBody && characterBody.skillLocator && characterBody.isPlayerControlled)
            {
                if (!voicelinesSkill) voicelinesSkill = characterBody.skillLocator.FindSkill(voicelinesSkillName);
                return (!voicelinesSkill || voicelinesSkill.skillDef == voicelinesEnableSkillDef);
            }
            return false;
        }
        private void EnableVoicelines()
        {
            InstanceTracker.Add<VoicelineComponent>(this);
            if (!string.IsNullOrEmpty(soundBankFilePath)) soundBankID = SoundAPI.SoundBanks.Add(soundBankFilePath);
            SubscribeEvents();
        }
        private void Start()
        {
            if (ShouldEnableVoicelines())
            {
                EnableVoicelines();
            }
            else
            {
                enabled = false;
            }
        }
        protected override void OnEnable()
        {

        }
        protected override void OnDisable()
        {
            InstanceTracker.Remove<VoicelineComponent>(this);
            base.OnDisable();
            UnsubscribeEvents();
        }
    }
}
