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
        public static BodyIndex voidlingBigBodyIndex;
        public static BodyIndex voidling1BodyIndex;
        public static BodyIndex voidling2BodyIndex;
        public static BodyIndex voidling3BodyIndex;
        public static BodyIndex falseSon1BodyIndex;
        public static BodyIndex falseSon2BodyIndex;
        public static BodyIndex falseSon3BodyIndex;
        public static BodyIndex solusWingBodyIndex;
        public static BodyIndex solusHeartBodyIndex;
        public static BodyIndex[] lunarScavengerIndices;
        public static BodyIndex arraign1EnemiesReturnsBodyIndex;
        public static BodyIndex arraign2EnemiesReturnsBodyIndex;

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

            StartCoroutine(StageStartVoicelines());
        }
        private IEnumerator StageStartVoicelines()
        {
            yield return new WaitForSeconds(3.5f);
            if (Stage.instance && !Stage.instance.usePod)
            {
                List<NetworkedVoiceline> voicelinesToSend = new List<NetworkedVoiceline>();
                List<VoicelineComponent> voices = InstanceTracker.GetInstancesList<VoicelineComponent>();
                foreach (var voice in voices)
                {
                    NetworkedVoiceline voiceline = voice.StageStart(Stage.instance);
                    if (voiceline.IsValid()) voicelinesToSend.Add(voiceline);
                }
                StartCoroutine(StaggerVoicelines(voicelinesToSend));
            }
        }
        private void BossStartVoicelines(BossGroup boss)
        {
            List<VoicelineComponent> voices = InstanceTracker.GetInstancesList<VoicelineComponent>();
            BodyIndex bodyIndex = GetBossBodyIndex(boss);
            FinalBoss finalBoss = GetFinalBoss(bodyIndex);
            List<NetworkedVoiceline> voicelinesToSend = new List<NetworkedVoiceline>();
            foreach (var voice in voices)
            {
                NetworkedVoiceline voiceline = finalBoss == FinalBoss.None ? voice.BossStart(bodyIndex) : voice.FinalBossStart(finalBoss);
                if (voiceline.IsValid()) voicelinesToSend.Add(voiceline);
            }
            StartCoroutine(StaggerVoicelines(voicelinesToSend, 2.5f));
        }
        private void BossDefeatedVoicelines(BossGroup boss)
        {
            List<VoicelineComponent> voices = InstanceTracker.GetInstancesList<VoicelineComponent>();
            BodyIndex bodyIndex = GetBossBodyIndex(boss);
            FinalBoss finalBoss = GetFinalBoss(bodyIndex);
            List<NetworkedVoiceline> voicelinesToSend = new List<NetworkedVoiceline>();
            foreach (var voice in voices)
            {
                NetworkedVoiceline voiceline = finalBoss == FinalBoss.None ? voice.BossDefeated(bodyIndex) : voice.FinalBossDefeated(finalBoss);
                if (voiceline.IsValid()) voicelinesToSend.Add(voiceline);
            }
            StartCoroutine(StaggerVoicelines(voicelinesToSend, 1f));
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
        public IEnumerator StaggerVoicelines(List<NetworkedVoiceline> voicelines, float startDelay = 0f)
        {
            if (startDelay > 0) yield return new WaitForSeconds(startDelay);

            RefreshNearby();
            if (voicelines.Count > 0)
            {
                List<VoicelineComponent> skippedVoices = new List<VoicelineComponent>();
                while (voicelines.Count > 0)
                {
                    skippedVoices.Clear();
                    for (int i = 0; i < voicelines.Count; i++)
                    {
                        if (voicelines[i].IsValid())
                        {
                            if (skippedVoices.Contains(voicelines[i].voicelineComponent)) { continue; }
                            if (voicelines[i].voicelineComponent.nearbyVoices.Count > 0) skippedVoices.Concat(voicelines[i].voicelineComponent.nearbyVoices);

                            Log.Message("HedgehogUtils Staggered Voiceline sent", Config.Logs.All);
                            new NetworkVoiceline(voicelines[i]).Send(NetworkDestination.Clients);
                        }
                        voicelines.RemoveAt(i);
                        i--;
                    }
                    if (voicelines.Count > 0) yield return new WaitForSeconds(1.8f);
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
            voidlingBigBodyIndex = BodyCatalog.FindBodyIndex("VoidRaidCrabBody");
            voidling1BodyIndex = BodyCatalog.FindBodyIndex("MiniVoidRaidCrabBodyPhase1");
            voidling2BodyIndex = BodyCatalog.FindBodyIndex("MiniVoidRaidCrabBodyPhase2");
            voidling3BodyIndex = BodyCatalog.FindBodyIndex("MiniVoidRaidCrabBodyPhase3");
            falseSon1BodyIndex = BodyCatalog.FindBodyIndex("FalseSonBossBody");
            falseSon2BodyIndex = BodyCatalog.FindBodyIndex("FalseSonBossBodyLunarShard");
            falseSon3BodyIndex = BodyCatalog.FindBodyIndex("FalseSonBossBodyBrokenLunarShard");
            solusWingBodyIndex = BodyCatalog.FindBodyIndex("SolusWingBody");
            solusHeartBodyIndex = BodyCatalog.FindBodyIndex("SolusHeartBody");
            lunarScavengerIndices = [
                BodyCatalog.FindBodyIndex("ScavLunar1Body"),
                BodyCatalog.FindBodyIndex("ScavLunar2Body"),
                BodyCatalog.FindBodyIndex("ScavLunar3Body"),
                BodyCatalog.FindBodyIndex("ScavLunar4Body")];
            arraign1EnemiesReturnsBodyIndex = BodyCatalog.FindBodyIndex("ArraignP1Body");
            arraign2EnemiesReturnsBodyIndex = BodyCatalog.FindBodyIndex("ArraignP2Body");
            BodyCatalog.GetBodyPrefab(BodyCatalog.FindBodyIndex("SonicTheHedgehog")).AddComponent<VoicelineComponent>();
            //BodyCatalog.FindBodyIndex("ProvidenceP1Body")};
        }
        public static BodyIndex GetBossBodyIndex(BossGroup boss)
        {
            if (boss.bossMemories.Length > 0)
            {
                BossGroup.BossMemory bossMemory = boss.bossMemories.First();
                if (bossMemory.cachedBody)
                {
                    return bossMemory.cachedBody.bodyIndex;
                }
            }
            return BodyIndex.None;
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

            if (index == voidlingBigBodyIndex) // I'd assume Fathomless uses this or something? Might as well keep it in
            { 
                if (VoidRaidGauntletController.instance)
                {
                    switch (VoidRaidGauntletController.instance.gauntletIndex)
                    {
                        case 1:
                            return FinalBoss.Voidling2;
                        case 2:
                            return FinalBoss.Voidling3;
                        default:
                            return FinalBoss.Voidling1;
                    }
                }
                return FinalBoss.Voidling1; 
            }

            if (index == voidling1BodyIndex) { return FinalBoss.Voidling1; }
            if (index == voidling2BodyIndex) { return FinalBoss.Voidling2; }
            if (index == voidling3BodyIndex) { return FinalBoss.Voidling3; }

            if (index == falseSon1BodyIndex) {  return FinalBoss.FalseSon1; }
            if (index == falseSon2BodyIndex) { return FinalBoss.FalseSon2; }
            if (index == falseSon3BodyIndex) { return FinalBoss.FalseSon3; }

            if (index == solusWingBodyIndex) { return FinalBoss.SolusWing; }
            if (index == solusHeartBodyIndex) { return FinalBoss.SolusHeart; }

            if (lunarScavengerIndices.Contains(index)) { return FinalBoss.LunarScavenger; }
            if (index == arraign1EnemiesReturnsBodyIndex) { return FinalBoss.Arraign1; }
            if (index == arraign2EnemiesReturnsBodyIndex) { return FinalBoss.Arraign2; }
            return FinalBoss.None;
        }
    }
    public enum FinalBoss : byte
    {
        None,
        Mithrix1,
        Mithrix3,
        Mithrix4,
        Voidling1,
        Voidling2,
        Voidling3,
        FalseSon1,
        FalseSon2,
        FalseSon3,
        SolusWing,
        SolusHeart,
        LunarScavenger,
        Arraign1,
        Arraign2
    }
    public struct NetworkedVoiceline
    {
        public VoicelineComponent voicelineComponent;
        public NetworkSoundEventIndex soundIndex;
        public VoicelinePriority priority;

        public NetworkedVoiceline(VoicelineComponent voicelineComponent, NetworkSoundEventIndex networkSoundEventIndex, VoicelinePriority voicelinePriority)
        {
            this.voicelineComponent = voicelineComponent;
            this.soundIndex = networkSoundEventIndex;
            this.priority = voicelinePriority;
        }

        public bool IsValid()
        {
            return soundIndex != NetworkSoundEventIndex.Invalid && voicelineComponent;
        }
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
