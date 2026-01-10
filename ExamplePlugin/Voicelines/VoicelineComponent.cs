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
    public class VoicelineComponent : MonoBehaviour
    {
        public string soundBankFilePath;
        protected uint soundBankID;

        public CharacterBody characterBody;
        public GenericSkill voicelinesSkill;
        public string voicelinesSkillName = "Voicelines";
        public SkillDef voicelinesEnableSkillDef;

        public uint currentVoicelineID;
        public VoicelinePriority currentVoicelinePriority;

        public List<VoicelineComponent> nearbyVoices = new List<VoicelineComponent>();

        public virtual void StageStart(Stage stage)
        {
            Chat.AddMessage("Stage start");
        }
        public virtual void BossDefeated()
        {
            Chat.AddMessage("Boss Defeated");
        }
        public virtual void BossStart()
        {
            Chat.AddMessage("Boss Start");
        }
        public virtual void FinalBossStart(FinalBoss boss)
        {
            Chat.AddMessage($"{boss.ToString()} Start");
        }
        public virtual void FinalBossDefeated(FinalBoss boss)
        {
            Chat.AddMessage($"{boss.ToString()} Defeated");
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
        }
        public void PlayVoiceline(NetworkSoundEventIndex soundIndex, VoicelinePriority priority)
        {
            PlayVoiceline(NetworkSoundEventCatalog.GetEventNameFromNetworkIndex(soundIndex), priority);
        }
        private void OnVoicelineEnd(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                currentVoicelineID = 0;
                currentVoicelinePriority = 0;
            }
        }

        public void Awake()
        {
            characterBody = GetComponent<CharacterBody>();
        }
        public void Start()
        {
            if (!TryEnableVoicelines())
            {
                enabled = false;
            }
        }
        public virtual bool TryEnableVoicelines()
        {
            if (characterBody && characterBody.skillLocator && characterBody.isPlayerControlled)
            {
                if (!voicelinesSkill) voicelinesSkill = characterBody.skillLocator.FindSkill(voicelinesSkillName);
                if (!voicelinesSkill || voicelinesSkill.skillDef == voicelinesEnableSkillDef)
                {
                    EnableVoicelines();
                    return true;
                }
            }
            return false;
        }
        public void EnableVoicelines()
        {
            InstanceTracker.Add<VoicelineComponent>(this);
            if (!string.IsNullOrEmpty(soundBankFilePath)) soundBankID = SoundAPI.SoundBanks.Add(soundBankFilePath);
        }
        public void OnEnable()
        {
            TryEnableVoicelines();
        }
        public void OnDisable()
        {
            InstanceTracker.Remove<VoicelineComponent>(this);
            if (!string.IsNullOrEmpty(soundBankFilePath)) SoundAPI.SoundBanks.Remove(soundBankID);
        }
    }
}
