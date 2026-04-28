using HG;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Audio;
using RoR2.Skills;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements.StyleSheets;

namespace HedgehogUtils.Voicelines
{
    [RequireComponent(typeof(CharacterSelectSurvivorPreviewDisplayController))]
    public class VoicelineDisplayComponent : SimpleVoicelineComponent
    {
        public static VoicelineDisplayComponent AddDisplayPrefabVoicelineComponent(GameObject displayPrefab, GameObject bodyPrefab, SkillFamily voicelineSkillFamily, SkillDef voicelineEnableSkillDef, string soundBankFilePath, params NetworkSoundEventDef[] networkSoundEventDefs)
        {
            CharacterSelectSurvivorPreviewDisplayController csspdc = displayPrefab.EnsureComponent<CharacterSelectSurvivorPreviewDisplayController>();
            csspdc.bodyPrefab = bodyPrefab;
            VoicelineDisplayComponent voicelineComponent = displayPrefab.AddComponent<VoicelineDisplayComponent>();
            voicelineComponent.soundBankFilePath = soundBankFilePath;
            voicelineComponent.skillFamily = voicelineSkillFamily;
            voicelineComponent.skillDef = voicelineEnableSkillDef;
            voicelineComponent.networkSoundEventDefs = networkSoundEventDefs;
            return voicelineComponent;
        }

        private CharacterSelectSurvivorPreviewDisplayController csspdc;

        public NetworkSoundEventDef[] networkSoundEventDefs;
        public SkillFamily skillFamily;
        public SkillDef skillDef;

        public void Awake()
        {
            csspdc = GetComponent<CharacterSelectSurvivorPreviewDisplayController>();
            UnityEngine.Events.UnityEvent voicelineEvent = new UnityEngine.Events.UnityEvent();
            voicelineEvent.AddListener(PlayRandomVoiceline);
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
                Helpers.Append(ref csspdc.skillChangeResponses, [voicelineResponse]);
            }
        }

        public IEnumerator Start()
        {
            yield return null;
            BodyIndex index = BodyCatalog.FindBodyIndex(csspdc.bodyPrefab);
            if (index != BodyIndex.None)
            {
                if (CharacterSelectSurvivorPreviewDisplayController.HasSkillVariantEnabled(csspdc.currentLoadout, index, skillFamily, skillDef))
                {
                    PlayRandomVoiceline();
                }
            }
        }

        public void PlayRandomVoiceline()
        {
            if (!isVoicelinePlaying && NetworkServer.active) PlayNetworkedVoiceline(networkSoundEventDefs.GetRandom().index, VoicelinePriority.PriorityDialogue);
        }
    }
}
