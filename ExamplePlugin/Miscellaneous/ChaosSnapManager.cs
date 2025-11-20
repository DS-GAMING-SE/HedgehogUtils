using R2API;
using Rewired;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HedgehogUtils.Miscellaneous
{
    public class ChaosSnapManager : MonoBehaviour
    {
        public static ChaosSnapManager instance;
        public static GameObject prefab;
        public static Material tempOverlayMaterial;

        public static void Initialize()
        {
            tempOverlayMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Huntress/matHuntressFlashBright.mat").WaitForCompletion();
            prefab = PrefabAPI.CreateEmptyPrefab("HedgehogUtilsChaosSnapManager");
            prefab.AddComponent<ChaosSnapManager>();
        }

        public void Start()
        {
            if (!instance)
            {
                instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        public void Teleport(CharacterBody target, Vector3 teleportFootPosition)
        {
            float duration;
            switch (target.hullClassification)
            {
                case HullClassification.Golem:
                    duration = 0.35f;
                    break;
                case HullClassification.BeetleQueen:
                    duration = 0.55f;
                    break;
                default:
                    duration = 0.25f;
                    break;
            }
            Teleport(target, teleportFootPosition, duration, duration);
        }
        public void Teleport(CharacterBody target, Vector3 teleportFootPosition, float startDuration, float endDuration)
        {
            if (startDuration > 0)
            {
                StartCoroutine(TeleportAfterDelay(target, teleportFootPosition, startDuration, endDuration));
            }
            else
            {
                TeleportVFX(target, target.corePosition, true, 0.2f, false);
                TeleportVFX(target, VFXPosition(target, teleportFootPosition), false, endDuration, true);
                TeleportHelper.TeleportBody(target, teleportFootPosition, true);
            }
        }
        IEnumerator TeleportAfterDelay(CharacterBody target, Vector3 teleportFootPosition, float startDuration, float endDuration)
        {
            TeleportVFX(target, target.corePosition, true, startDuration, true);
            yield return new WaitForSeconds(startDuration);
            if (target)
            {
                TeleportVFX(target, VFXPosition(target, teleportFootPosition), false, endDuration, true);
                TeleportHelper.TeleportBody(target, teleportFootPosition, true);
            }

        }
        public bool TeleportBodyToRandomNode(CharacterBody target, float minDistance, float maxDistance)
        {
            if (GetRandomTeleportNode(target, target.footPosition, out Vector3 vector, minDistance, maxDistance) && target.characterDirection)
            {
                instance.Teleport(target, vector);
                return true;
            }
            return false;
        }
        public static void TeleportVFX(CharacterBody target, Vector3 position, bool teleportIn, float duration, bool useTemporaryOverlay)
        {
            if (duration <= 0.05f) { return; }
            float scale;
            switch (target.hullClassification)
            {
                case HullClassification.Golem:
                    scale = 2f;
                    break;
                case HullClassification.BeetleQueen:
                    scale = 5f;
                    break;
                default:
                    scale = 1f;
                    break;
            }
            EffectData data = new EffectData
            {
                scale = scale,
                genericFloat = duration,
                networkSoundEventIndex = duration < 0.27f ? Assets.chaosSnapSoundEventDef.index : Assets.chaosSnapLargeSoundEventDef.index,
                origin = position,
                rootObject = target ? target.gameObject : null,
                genericBool = useTemporaryOverlay
            };
            EffectManager.SpawnEffect(teleportIn ? Assets.chaosSnapInEffect : Assets.chaosSnapOutEffect, data, true);
        }

        private static Vector3 VFXPosition(CharacterBody target, Vector3 footPosition)
        {
            return (target.corePosition - target.footPosition) + footPosition;
        }
        // Mostly copied from Unstable Transmitter. Couldn't just reuse their code because the vfx was built in to it
        public static bool GetRandomTeleportNode(CharacterBody target, Vector3 origin, out Vector3 destination, float minDistance, float maxDistance)
        {
            MapNodeGroup.GraphType nodeGraphType = MapNodeGroup.GraphType.Ground;
            if (!target.characterMotor || !target.characterMotor.isGrounded)
            {
                nodeGraphType = MapNodeGroup.GraphType.Air;
            }
            NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(nodeGraphType);
            NodeGraph.NodeIndex nodeIndex = NodeGraph.NodeIndex.invalid;
            HullMask hullMask = (HullMask)(1 << (int)target.hullClassification);
            List<NodeGraph.NodeIndex> list;

            list = nodeGraph.FindNodesInRange(origin, minDistance, maxDistance, HullMask.Human);
            if (list.Count > 0)
            {
                nodeIndex = list[UnityEngine.Random.Range(0, list.Count)];
            }
            if (nodeIndex == NodeGraph.NodeIndex.invalid)
            {
                list ??= new();
                nodeGraph.GetActiveNodesForHullMask(hullMask, list);
                if (list.Count > 0)
                {
                    nodeIndex = list[UnityEngine.Random.Range(0, Mathf.Max(1, list.Count))];
                }
            }
            if (list.Count <= 0)
            {
                destination = origin;
                return false;
            }

            nodeGraph.GetNodePosition(nodeIndex, out Vector3 vector3);
            destination = vector3;
            return true;
        }
    }
}
