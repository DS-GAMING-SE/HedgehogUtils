using HG;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;

namespace HedgehogUtils.Voicelines
{
    public class VoicelineDisplayComponent : MonoBehaviour
    {
        public static VoicelineDisplayComponent AddDisplayPrefabVoicelineComponent(GameObject displayPrefab, GameObject bodyPrefab, SkillFamily voicelineSkillFamily, SkillDef voicelineEnableSkillDef, string soundBankFilePath, params string[] soundStrings)
        {
            CharacterSelectSurvivorPreviewDisplayController csspdc = displayPrefab.EnsureComponent<CharacterSelectSurvivorPreviewDisplayController>();
            csspdc.bodyPrefab = bodyPrefab;
            VoicelineDisplayComponent voicelineComponent = displayPrefab.AddComponent<VoicelineDisplayComponent>();
            voicelineComponent.soundBankFilePath = soundBankFilePath;
            voicelineComponent.skillFamily = voicelineSkillFamily;
            voicelineComponent.skillDef = voicelineEnableSkillDef;
            voicelineComponent.soundStrings = soundStrings;
            return voicelineComponent;
        }
        private CharacterSelectSurvivorPreviewDisplayController csspdc;

        private uint currentVoicelineID;
        public string[] soundStrings;
        public SkillFamily skillFamily;
        public SkillDef skillDef;

        public string soundBankFilePath;
        protected uint soundBankID;
        public void PlayVoiceline(string soundString)
        {
            if (currentVoicelineID != 0) return;
            currentVoicelineID = AkSoundEngine.PostEvent(soundString, gameObject, (uint)AkCallbackType.AK_EndOfEvent, OnVoicelineEnd, null);
        }
        private void OnVoicelineEnd(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                currentVoicelineID = 0;
            }
        }
        public void Awake()
        {
            csspdc = GetComponent<CharacterSelectSurvivorPreviewDisplayController>();
            if (csspdc)
            {
                UnityEngine.Events.UnityEvent voicelineEvent = new UnityEngine.Events.UnityEvent();
                voicelineEvent.AddListener(() => PlayVoiceline(soundStrings.GetRandom()));
                CharacterSelectSurvivorPreviewDisplayController.SkillChangeResponse voicelineResponse = new CharacterSelectSurvivorPreviewDisplayController.SkillChangeResponse
                {
                    triggerSkillFamily = skillFamily,
                    triggerSkill = skillDef,
                    response = voicelineEvent
                };
                if (csspdc.skillChangeResponses == null)
                {
                    csspdc.skillChangeResponses = new[] { voicelineResponse };
                }
                else
                {
                    Helpers.Append(ref csspdc.skillChangeResponses,[voicelineResponse]);
                }
            }
        }
        public void OnEnable()
        {
            if (!string.IsNullOrEmpty(soundBankFilePath)) soundBankID = SoundAPI.SoundBanks.Add(soundBankFilePath);
        }
        public void OnDisable()
        {
            if (!string.IsNullOrEmpty(soundBankFilePath)) SoundAPI.SoundBanks.Remove(soundBankID);
        }
    }
}
