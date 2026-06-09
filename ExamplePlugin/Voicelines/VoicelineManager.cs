using Newtonsoft.Json.Utilities;
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

        private bool[] finalBossStartSaid = new bool[17];
        private bool[] finalBossDefeatSaid = new bool[17];

        public delegate void StageEventHandler(Stage stage, List<NetworkedVoiceline> networkedVoicelines);
        public delegate void BossEventHandler(BodyIndex boss, List<NetworkedVoiceline> networkedVoicelines);
        public delegate void FinalBossEventHandler(FinalBoss finalBoss, List<NetworkedVoiceline> networkedVoicelines);

        public static event StageEventHandler OnStageStart;
        public static event BossEventHandler OnBossStart;
        public static event BossEventHandler OnBossDefeated;
        public static event FinalBossEventHandler OnFinalBossStart;
        public static event FinalBossEventHandler OnFinalBossDefeated;

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
        public static BodyIndex solusWingWeakPointBodyIndex;
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
            EntityStates.SolusHeart.PhasePreFightCutscene.MissionPreFightCutscene.onCutsceneFinished += SolusHeartStartVoicelines;
            EntityStates.ScavMonster.ExitSit.OnExitSit += LunarScavengerStartVoicelines;
        }
        public void Start()
        {
            StartCoroutine(StageStartVoicelines());
        }
        private IEnumerator StageStartVoicelines()
        {
            yield return new WaitForSeconds(3.5f);
            if (Stage.instance && !Stage.instance.usePod)
            {
                List<NetworkedVoiceline> voicelinesToSend = new List<NetworkedVoiceline>();
                if (OnStageStart != null)
                {
                    foreach (StageEventHandler @event in OnStageStart.GetInvocationList().Cast<StageEventHandler>())
                    {
                        try
                        {
                            @event(Stage.instance, voicelinesToSend);
                        }
                        catch (Exception e)
                        {
                            Log.Error(
                                $"Exception thrown by : {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                        }
                    }
                }
                StartCoroutine(StaggerVoicelines(voicelinesToSend));
            }
        }
        private void BossStartVoicelines(BossGroup boss)
        {
            BodyIndex bodyIndex = GetBossBodyIndex(boss);
            if (bodyIndex == BodyIndex.None) return;
            FinalBoss finalBoss = GetFinalBoss(bodyIndex);

            if (finalBoss == FinalBoss.SolusHeart1 ||
                finalBoss == FinalBoss.SolusHeart2 ||
                finalBoss == FinalBoss.SolusHeart3 ||
                finalBoss == FinalBoss.LunarScavenger) return;

            if (finalBoss == FinalBoss.None)
            {
                SendBossStartEvent(bodyIndex);
            }
            else
            {
                SendFinalBossStartEvent(finalBoss);
            }
        }
        private void SolusHeartStartVoicelines(SolusWebMissionController solus)
        {
            SendFinalBossStartEvent(FinalBoss.SolusHeart1);
        }
        private void LunarScavengerStartVoicelines(CharacterBody body)
        {
            // Base game uses this to start warbonds on twisted scav except they forget to check if it's specifically twisted scav, so it activates..
            // .. war bonds everytime a scav stands up even if it isn't the boss. Silly base game bugs
            if (lunarScavengerIndices.Contains(body.bodyIndex))
            {
                SendFinalBossStartEvent(FinalBoss.LunarScavenger);
            }
        }
        public void SendBossStartEvent(BodyIndex bodyIndex)
        {
            List<NetworkedVoiceline> voicelinesToSend = new List<NetworkedVoiceline>();
            if (OnBossStart != null)
            {
                foreach (BossEventHandler @event in OnBossStart.GetInvocationList().Cast<BossEventHandler>())
                {
                    try
                    {
                        @event(bodyIndex, voicelinesToSend);
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            $"Exception thrown by : {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                    }
                }
            }
            StartCoroutine(StaggerVoicelines(voicelinesToSend, 2.5f));
        }
        public void SendFinalBossStartEvent(FinalBoss finalBoss)
        {
            if (finalBossStartSaid[(byte)finalBoss-1]) return;
            finalBossStartSaid[(byte)finalBoss-1] = true;
            List<NetworkedVoiceline> voicelinesToSend = new List<NetworkedVoiceline>();
            if (OnFinalBossStart != null)
            {
                foreach (FinalBossEventHandler @event in OnFinalBossStart.GetInvocationList().Cast<FinalBossEventHandler>())
                {
                    try
                    {
                        @event(finalBoss, voicelinesToSend);
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            $"Exception thrown by : {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                    }
                }
            }
            float startDelay = 2.5f;
            switch (finalBoss)
            {
                case FinalBoss.Mithrix1:
                    startDelay = 3.5f;
                    break;
                case FinalBoss.Voidling1:
                    startDelay = 6f;
                    break;
                case FinalBoss.Voidling2:
                    startDelay = 4f;
                    break;
                case FinalBoss.Voidling3:
                    startDelay = 4f;
                    break;
                case FinalBoss.FalseSon1:
                    startDelay = 3.5f;
                    break;
                case FinalBoss.SolusHeart1:
                    startDelay = 1.8f;
                    break;
                case FinalBoss.Arraign1:
                    startDelay = 3.5f;
                    break;
            }
            StartCoroutine(StaggerVoicelines(voicelinesToSend, startDelay));
        }
        private void BossDefeatedVoicelines(BossGroup boss)
        {
            BodyIndex bodyIndex = GetBossMemoryBodyIndex(boss);
            if (bodyIndex == BodyIndex.None) return;
            FinalBoss finalBoss = GetFinalBoss(bodyIndex);

            if (finalBoss == FinalBoss.SolusHeart1 ||
                finalBoss == FinalBoss.SolusHeart2 ||
                finalBoss == FinalBoss.SolusHeart3) return;

            if (finalBoss == FinalBoss.None)
            {
                SendBossDefeatedEvent(bodyIndex);
            }
            else
            {
                SendFinalBossDefeatedEvent(finalBoss);
            }
        }
        public void SendBossDefeatedEvent(BodyIndex bodyIndex)
        {
            List<NetworkedVoiceline> voicelinesToSend = new List<NetworkedVoiceline>();
            if (OnBossDefeated != null)
            {
                foreach (BossEventHandler @event in OnBossDefeated.GetInvocationList().Cast<BossEventHandler>())
                {
                    try
                    {
                        @event(bodyIndex, voicelinesToSend);
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            $"Exception thrown by : {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                    }
                }
            }
            StartCoroutine(StaggerVoicelines(voicelinesToSend, 1f));
        }
        public void SendFinalBossDefeatedEvent(FinalBoss finalBoss)
        {
            if (finalBossDefeatSaid[(byte)finalBoss-1]) return;
            finalBossDefeatSaid[(byte)finalBoss-1] = true;
            List<NetworkedVoiceline> voicelinesToSend = new List<NetworkedVoiceline>();
            if (OnFinalBossDefeated != null)
            {
                foreach (FinalBossEventHandler @event in OnFinalBossDefeated.GetInvocationList().Cast<FinalBossEventHandler>())
                {
                    try
                    {
                        @event(finalBoss, voicelinesToSend);
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            $"Exception thrown by : {@event.Method.DeclaringType.Name}.{@event.Method.Name}:\n{e}");
                    }
                }
            }
            StartCoroutine(StaggerVoicelines(voicelinesToSend, 1.6f));
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

            if (voicelines.Count > 0)
            {
                yield return RefreshNearby();
                yield return StaggerVoicelineChunk(voicelines, null, false);
            }
        }
        private IEnumerator StaggerVoicelineChunk(List<NetworkedVoiceline> voicelines, VoicelineComponent waitOn, bool startDelay = true)
        {
            yield return new WaitUntil(() => !waitOn || !waitOn.isVoicelinePlaying);
            if (startDelay) yield return new WaitForSeconds(0.7f);
            
            List<NetworkedVoiceline> skippedVoices = new List<NetworkedVoiceline>();
            for (int i = 0; i < voicelines.Count; i++)
            {
                VoicelineComponent voicelineComponent = null;
                if (voicelines[i].IsValid())
                {
                    if (skippedVoices.Contains(voicelines[i])) { continue; }
                    voicelineComponent = voicelines[i].voicelineComponent as VoicelineComponent;
                    if (voicelineComponent && voicelineComponent.nearbyVoices.Count > 0)
                    {
                        for (int j = 0; j < voicelines.Count; j++)
                        {
                            if (voicelineComponent.nearbyVoices.Contains(voicelines[j].voicelineComponent))
                            {
                                skippedVoices.Add(voicelines[j]);
                            }
                        }
                    }

                    Log.Message("HedgehogUtils Staggered Voiceline sent", Config.Logs.All);
                    new NetworkVoiceline(voicelines[i]).Send(NetworkDestination.Clients);
                }
                if (skippedVoices.Count > 0) StartCoroutine(StaggerVoicelineChunk(skippedVoices, voicelineComponent));
                voicelines.RemoveAt(i);
                i--;
            }
        }

        public void OnDisable()
        {
            BossGroup.onBossGroupStartServer -= BossStartVoicelines;
            BossGroup.onBossGroupDefeatedServer -= BossDefeatedVoicelines;
            EntityStates.SolusHeart.PhasePreFightCutscene.MissionPreFightCutscene.onCutsceneFinished -= SolusHeartStartVoicelines;
            EntityStates.ScavMonster.ExitSit.OnExitSit -= LunarScavengerStartVoicelines;
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
            solusWingWeakPointBodyIndex = BodyCatalog.FindBodyIndex("ExhaustPortWeakpointBody");
            solusHeartBodyIndex = BodyCatalog.FindBodyIndex("SolusHeartBody");
            lunarScavengerIndices = [
                BodyCatalog.FindBodyIndex("ScavLunar1Body"),
                BodyCatalog.FindBodyIndex("ScavLunar2Body"),
                BodyCatalog.FindBodyIndex("ScavLunar3Body"),
                BodyCatalog.FindBodyIndex("ScavLunar4Body")];
            arraign1EnemiesReturnsBodyIndex = BodyCatalog.FindBodyIndex("ArraignP1Body");
            arraign2EnemiesReturnsBodyIndex = BodyCatalog.FindBodyIndex("ArraignP2Body");
            //BodyCatalog.FindBodyIndex("ProvidenceP1Body")};
        }
        public static BodyIndex GetBossBodyIndex(BossGroup boss)
        {
            if (boss.combatSquad.readOnlyMembersList.Count > 0)
            {
                CharacterMaster master = boss.combatSquad.readOnlyMembersList.FirstOrDefault();
                if (master)
                {
                    CharacterBody body = master.GetBody();
                    if (body)
                    {
                        return body.bodyIndex;
                    }
                    else
                    {
                        return master.backupBodyIndex;
                    }
                }
            }
            return BodyIndex.None;
        }
        public static BodyIndex GetBossMemoryBodyIndex(BossGroup boss)
        {
            if (boss.bossMemoryCount > 0)
            {
                if (boss.bossMemories[0].cachedBody)
                {
                    return boss.bossMemories[0].cachedBody.bodyIndex;
                }
            }
            return BodyIndex.None;
        }

        public static FinalBoss GetFinalBoss(BossGroup boss)
        {
            return GetFinalBoss(GetBossBodyIndex(boss));
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
            if (index == solusWingWeakPointBodyIndex) { return FinalBoss.SolusWingWeakPoint; }
            if (index == solusHeartBodyIndex) { return FinalBoss.SolusHeart1; }

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
        SolusWingWeakPoint,
        SolusHeart1,
        SolusHeart2,
        SolusHeart3,
        LunarScavenger,
        Arraign1,
        Arraign2
            // if you add more, remember to update said array size
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
