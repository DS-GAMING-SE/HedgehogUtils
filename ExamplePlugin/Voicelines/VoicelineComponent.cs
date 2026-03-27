using R2API;
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
    public abstract class VoicelineComponent : MonoBehaviour, ILifeBehavior
    {
        public string soundBankFilePath;
        protected uint soundBankID;

        public CharacterBody characterBody;
        public GenericSkill voicelinesSkill;
        public string voicelinesSkillName = "Voicelines";
        public SkillDef voicelinesEnableSkillDef;

        public uint currentVoicelineID;
        public VoicelinePriority currentVoicelinePriority;
        public bool isVoicelinePlaying { get; private set; }

        public List<VoicelineComponent> nearbyVoices = new List<VoicelineComponent>();
        public virtual void SubscribeEvents()
        {

        }
        public virtual void UnsubscribeEvents()
        {

        }
        public virtual void OnDeathStart()
        {
            StopCurrentVoiceline();
            UnsubscribeEvents();
        }
        public void PlayVoiceline(string soundString, VoicelinePriority priority)
        {
            if (string.IsNullOrEmpty(soundString)) return;
            if (priority > currentVoicelinePriority)
            {
                AkSoundEngine.StopPlayingID(currentVoicelineID);
            }
            currentVoicelineID = AkSoundEngine.PostEvent(soundString, gameObject, (uint)AkCallbackType.AK_EndOfEvent, OnVoicelineEnd, null);
            currentVoicelinePriority = priority;
            isVoicelinePlaying = true;
        }
        public void PlayVoiceline(NetworkSoundEventIndex soundIndex, VoicelinePriority priority)
        {
            PlayVoiceline(NetworkSoundEventCatalog.GetEventNameFromNetworkIndex(soundIndex), priority);
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
        private void OnDisable()
        {
            InstanceTracker.Remove<VoicelineComponent>(this);
            if (!string.IsNullOrEmpty(soundBankFilePath)) SoundAPI.SoundBanks.Remove(soundBankID);
            UnsubscribeEvents();
        }
    }
}
