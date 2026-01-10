using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Pool;

namespace HedgehogUtils.Voicelines
{
    public class VoicelineManager : MonoBehaviour
    {
        public static VoicelineManager instance;
        public static GameObject prefab;

        [Tooltip("Normally, if multiple voiceline characters are responding to the same event, their responses will happen one after another. If they are this far away or greater, they won't be.")]
        public const float nearbyMaxDistance = 60f;

        public static BodyIndex mithrix1BodyIndex;
        public static BodyIndex mithrix4BodyIndex;
        public static BodyIndex voidlingBodyIndex;
        public static BodyIndex falseSonBodyIndex;
        public static BodyIndex solusWingBodyIndex;
        public static BodyIndex solusHeartBodyIndex;
        public static BodyIndex[] lunarScavengerIndices;
        public static BodyIndex arraignEnemiesReturnsBodyIndex;

        public static void Initialize()
        {
            prefab = PrefabAPI.CreateEmptyPrefab("HedgehogUtilsVoicelineManager");
            prefab.AddComponent<VoicelineManager>();
            Stage.onStageStartGlobal += SpawnVoicelineManager;
        }
        private static void SpawnVoicelineManager(Stage stage)
        {
            if (NetworkServer.active)
            {
                GameObject.Instantiate(prefab);
            }
        }
        public void OnEnable()
        {
            SingletonHelper.Assign<VoicelineManager>(ref instance, this);
            BossGroup.onBossGroupStartServer += BossStartVoicelines;
            BossGroup.onBossGroupDefeatedServer += BossDefeatedVoicelines;

            if (Stage.instance && !Stage.instance.usePod)
            {
                StartCoroutine(StaggerVoicelines(voice =>
                {
                    Log.Message("HedgehogUtils VoicelineManager Enabled", Config.Logs.All);
                    voice.StageStart(Stage.instance);
                }, 3f));
            }
        }
        // Boss defined out of scope of lambda?
        private void BossStartVoicelines(BossGroup boss)
        {
            StartCoroutine(StaggerVoicelines(voice =>
            {
                FinalBoss finalBoss = GetFinalBoss(boss);
                if (finalBoss == FinalBoss.None)
                {
                    voice.BossStart();
                }
                else
                {
                    voice.FinalBossStart(finalBoss);
                }
            }, 1f));
        }
        private void BossDefeatedVoicelines(BossGroup boss)
        {
            StartCoroutine(StaggerVoicelines(voice =>
            {
                FinalBoss finalBoss = GetFinalBoss(boss);
                if (finalBoss == FinalBoss.None)
                {
                    voice.BossDefeated();
                }
                else
                {
                    voice.FinalBossDefeated(finalBoss);
                }
            }, 0.5f));
        }
        public IEnumerator RefreshNearby()
        {
            List<VoicelineComponent> voices = InstanceTracker.GetInstancesList<VoicelineComponent>();
            for (int i = 0; i < voices.Count; i++)
            {
                voices[i].nearbyVoices.Clear();
                for (int j = 0; j < voices.Count; j++)
                {
                    if (i != j && Vector3.Distance(voices[i].transform.position, voices[j].transform.position) <= nearbyMaxDistance)
                    {
                        voices[i].nearbyVoices.Add(voices[j]);
                    }
                }
                yield return null;
            }
        }
        public IEnumerator StaggerVoicelines(Action<VoicelineComponent> playVoiceline, float startDelay)
        {
            RefreshNearby();

            if (startDelay > 0) yield return new WaitForSeconds(startDelay);
            List<VoicelineComponent> voices = InstanceTracker.GetInstancesList<VoicelineComponent>();
            if (voices.Count > 0)
            {
                List<VoicelineComponent> skippedVoices = new List<VoicelineComponent>();
                while (voices.Count > 0)
                {
                    skippedVoices.Clear();
                    for (int i = 0; i < voices.Count; i++)
                    {
                        if (voices[i])
                        {
                            if (skippedVoices.Contains(voices[i])) { continue; }
                            if (voices[i].nearbyVoices.Count > 0) skippedVoices.Concat(voices[i].nearbyVoices);
                            playVoiceline(voices[i]);
                        }
                        voices.RemoveAt(i);
                        i--;
                    }
                    if (voices.Count > 0) yield return new WaitForSeconds(1.8f);
                }
            }
        }

        public void OnDisable()
        {
            BossGroup.onBossGroupStartServer -= BossStartVoicelines;
            BossGroup.onBossGroupDefeatedServer -= BossDefeatedVoicelines;
            SingletonHelper.Unassign<VoicelineManager>(ref instance, this);
        }

        [SystemInitializer(typeof(BodyCatalog))]
        public static void SaveFinalBossBodyIndices()
        {
            mithrix1BodyIndex = BodyCatalog.FindBodyIndex("BrotherBody");
            mithrix4BodyIndex = BodyCatalog.FindBodyIndex("BrotherHurtBody");
            voidlingBodyIndex = BodyCatalog.FindBodyIndex("VoidRaidCrabBody");
            falseSonBodyIndex = BodyCatalog.FindBodyIndex("FalseSonBossBody");
            solusWingBodyIndex = BodyCatalog.FindBodyIndex("SolusWingBody");
            solusHeartBodyIndex = BodyCatalog.FindBodyIndex("SolusHeartBody");
            lunarScavengerIndices = new BodyIndex[]{ 
                BodyCatalog.FindBodyIndex("ScavLunar1Body"),
                BodyCatalog.FindBodyIndex("ScavLunar2Body"),
                BodyCatalog.FindBodyIndex("ScavLunar3Body"),
                BodyCatalog.FindBodyIndex("ScavLunar4Body"),
            BodyCatalog.FindBodyIndex("ArraignP1Body"),
            BodyCatalog.FindBodyIndex("ProvidenceP1Body")};
            BodyCatalog.FindBodyPrefab("SonicTheHedgehog").AddComponent<VoicelineComponent>();
        }
        public static FinalBoss GetFinalBoss(BossGroup boss)
        {
            if (boss.bossMemories.Length > 0)
            {
                BossGroup.BossMemory bossMemory = boss.bossMemories.First();
                if (bossMemory.cachedBody)
                {
                    return GetFinalBoss(bossMemory.cachedBody.bodyIndex);
                }
            }
            return FinalBoss.None;
        }

        public static FinalBoss GetFinalBoss(BodyIndex index)
        {
            if (index == BodyIndex.None) return FinalBoss.None;
            if (index == mithrix1BodyIndex)
            {
                if (PhaseCounter.instance && PhaseCounter.instance.phase == 3)
                {
                    return FinalBoss.Mithrix3;
                }
                else
                {
                    return FinalBoss.Mithrix1;
                }
            }
            if (index == mithrix4BodyIndex) { return FinalBoss.Mithrix4; }
            if (index == voidlingBodyIndex) { return FinalBoss.Voidling; }
            if (index == falseSonBodyIndex) 
            {  
                if (MeridianEventTriggerInteraction.instance)
                {
                    switch (MeridianEventTriggerInteraction.instance.meridianEventState)
                    {
                        case MeridianEventState.Phase2:
                            return FinalBoss.FalseSon2;
                        case MeridianEventState.Phase3:
                            return FinalBoss.FalseSon3;
                        default:
                            return FinalBoss.FalseSon1;
                    }
                }
            }
            if (index == solusWingBodyIndex) { return FinalBoss.SolusWing; }
            if (index == solusHeartBodyIndex) { return FinalBoss.SolusHeart; }
            if (lunarScavengerIndices.Contains(index)) { return FinalBoss.LunarScavenger; }
            if (index == arraignEnemiesReturnsBodyIndex) { return FinalBoss.Arraign; }
            return FinalBoss.None;
        }
    }
    public enum FinalBoss : byte
    {
        None,
        Mithrix1,
        Mithrix3,
        Mithrix4,
        Voidling,
        FalseSon1,
        FalseSon2,
        FalseSon3,
        SolusWing,
        SolusHeart,
        LunarScavenger,
        Arraign
    }
    public struct NetworkedVoiceline
    {
        public NetworkInstanceId netId;
        public NetworkSoundEventIndex soundIndex;
        public VoicelinePriority priority;
    }
    public enum VoicelinePriority : byte
    {
        Any,
        Skill,
        PrioritySkill,
        Dialogue,
        PriorityDialogue
    }
}
