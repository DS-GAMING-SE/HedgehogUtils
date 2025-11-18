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
        public static EffectManagerHelper effectManagerHelper;
        private static EffectManagerHelper _emh_teleportInEffectInstance;
        private static EffectManagerHelper _emh_teleportOutEffectInstance;

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
            Teleport(target, null, teleportFootPosition, duration, duration, duration);
        }
        public void Teleport(CharacterBody target, CharacterModel characterModel, Vector3 teleportFootPosition, float startVFXDuration, float teleportDelay, float endVFXDuration)
        {
            if (!target) { return; }
            CharacterModel model = characterModel;
            if (!model && target.modelLocator && target.modelLocator.modelTransform)
            {
                model = target.modelLocator.modelTransform.GetComponent<CharacterModel>();
            }
            if (teleportDelay > 0)
            {
                StartCoroutine(TeleportAfterDelay(target, model, teleportFootPosition, startVFXDuration, teleportDelay, endVFXDuration));
            }
            else
            {
                TeleportVFX(target, target.corePosition, true, startVFXDuration);
                TeleportVFX(target, VFXPosition(target, teleportFootPosition), false, endVFXDuration);
                TeleportHelper.TeleportBody(target, teleportFootPosition, true);
                Flash(model, endVFXDuration);
            }
        }
        IEnumerator TeleportAfterDelay(CharacterBody target, CharacterModel characterModel, Vector3 teleportFootPosition, float startVFXDuration, float teleportDelay, float endVFXDuration)
        {
            ReverseFlash(characterModel, teleportDelay);
            TeleportVFX(target, target.corePosition, true, startVFXDuration);
            yield return new WaitForSeconds(teleportDelay);
            if (target)
            {
                TeleportVFX(target, VFXPosition(target, teleportFootPosition), false, endVFXDuration);
                TeleportHelper.TeleportBody(target, teleportFootPosition, true);
                Flash(characterModel, endVFXDuration);
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
        public static void TeleportVFX(CharacterBody target, Vector3 position, bool teleportIn, float duration)
        {
            GameObject vfx = CreatePooledVFX(teleportIn? Assets.chaosSnapInEffect : Assets.chaosSnapOutEffect, teleportIn, position);
            switch (target.hullClassification)
            {
                case HullClassification.Golem:
                    vfx.transform.localScale = new Vector3(2f, 2f, 2f);
                    break;
                case HullClassification.BeetleQueen:
                    vfx.transform.localScale = new Vector3(5f, 5f, 5f);
                    break;
                default:
                    vfx.transform.localScale = new Vector3(1f, 1f, 1f);
                    break;
            }
            ScaleTeleportVFX(vfx, duration);
            Util.PlaySound(duration < 0.27f ? "Play_hedgehogutils_teleport" : "Play_hedgehogutils_teleport_large", vfx);
        }
        public static void ScaleTeleportVFX(GameObject vfx, float duration)
        {
            if (vfx.TryGetComponent<ScaleParticleSystemDuration>(out ScaleParticleSystemDuration particle))
            {
                ScaleTeleportVFX(particle, duration);
            }
        }
        public static void ScaleTeleportVFX(ScaleParticleSystemDuration vfx, float duration)
        {
            vfx.newDuration = duration;
            vfx.UpdateDuration();
            Transform light = vfx.transform.Find("Point Light");
            light.GetComponent<LightIntensityCurve>().timeMax = duration;
        }
        private static GameObject CreatePooledVFX(GameObject prefab, bool teleportIn, Vector3 position)
        {
            if (prefab)
            {
                if (teleportIn)
                {
                    _emh_teleportInEffectInstance = EffectManager.GetAndActivatePooledEffect(prefab, position, Quaternion.identity);
                    return _emh_teleportInEffectInstance.gameObject;
                }
                else
                {
                    _emh_teleportOutEffectInstance = EffectManager.GetAndActivatePooledEffect(prefab, position, Quaternion.identity);
                    return _emh_teleportOutEffectInstance.gameObject;
                }
            }
            return null;
        }
        private static GameObject CreatePooledVFX(GameObject prefab, ref EffectManagerHelper emh, Vector3 position)
        {
            if (prefab)
            {
                if (emh)
                {
                    emh = EffectManager.GetAndActivatePooledEffect(prefab, position, Quaternion.identity);
                    return emh.gameObject;
                }
                CreatePooledVFX(prefab, false, position);
            }
            return null;
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
        public static void Flash(CharacterModel characterModel, float duration)
        {
            if (characterModel)
            {
                TemporaryOverlayInstance flashOverlay = TemporaryOverlayManager.AddOverlay(characterModel.gameObject); // Flash
                flashOverlay.duration = duration;
                flashOverlay.animateShaderAlpha = true;
                flashOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                flashOverlay.originalMaterial = tempOverlayMaterial;
                flashOverlay.destroyComponentOnEnd = true;
                flashOverlay.inspectorCharacterModel = characterModel;
            }
        }
        public static void ReverseFlash(CharacterModel characterModel, float duration)
        {
            if (characterModel)
            {
                TemporaryOverlayInstance flashOverlay = TemporaryOverlayManager.AddOverlay(characterModel.gameObject); // Flash
                flashOverlay.duration = duration;
                flashOverlay.animateShaderAlpha = true;
                flashOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                flashOverlay.originalMaterial = tempOverlayMaterial;
                flashOverlay.destroyComponentOnEnd = true;
                flashOverlay.inspectorCharacterModel = characterModel;
            }
        }
    }
}
