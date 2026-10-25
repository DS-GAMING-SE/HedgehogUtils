using EntityStates.MinePod;
using R2API;
using Rewired;
using RoR2;
using RoR2.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HedgehogUtils.Miscellaneous
{
    public class ChaosSnapManager : MonoBehaviour, ICameraStateProvider
    {
        public static ChaosSnapManager instance;
        public static GameObject prefab;

        private const float CAMERA_MOVEMENT_PERCENT_OF_DURATION = 0.7f;

        public static void Initialize()
        {
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

        public ChaosSnapInstanceHandle Teleport(CharacterBody target, Vector3 teleportFootPosition)
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
            return Teleport(target, teleportFootPosition, Vector3.zero, duration, duration);
        }
        public ChaosSnapInstanceHandle Teleport(CharacterBody target, Vector3 teleportFootPosition, float startDuration, float endDuration)
        {
            return Teleport(target, teleportFootPosition, Vector3.zero, startDuration, endDuration);
        }
        public ChaosSnapInstanceHandle Teleport(CharacterBody target, Vector3 teleportFootPosition, Vector3 targetForward, float startDuration, float endDuration)
        {
            ChaosSnapInstanceHandle handle = CreateInstance(target, teleportFootPosition, targetForward);
            if (startDuration > 0)
            {
                StartCoroutine(TeleportAfterDelay(handle, startDuration, endDuration));
            }
            else
            {
                StartCoroutine(InstantTeleport(handle, endDuration));
            }
            return handle;
        }
        private IEnumerator TeleportAfterDelay(ChaosSnapInstanceHandle handle, float startDuration, float endDuration)
        {
            TeleportVFX(handle.user, handle.user.corePosition, true, startDuration, true);
            
            yield return new WaitForSeconds(startDuration * (1 - CAMERA_MOVEMENT_PERCENT_OF_DURATION));
            handle.StartCameraMove((startDuration + endDuration) * CAMERA_MOVEMENT_PERCENT_OF_DURATION);
            yield return new WaitForSeconds(startDuration * CAMERA_MOVEMENT_PERCENT_OF_DURATION);
            if (handle.user)
            {
                TeleportVFX(handle.user, VFXPosition(handle.user, handle.targetPosition), false, endDuration, true);
                TeleportHelper.TeleportBody(handle.user, handle.targetPosition, true);
            }
            yield return new WaitForSeconds(endDuration * CAMERA_MOVEMENT_PERCENT_OF_DURATION);
            handle.EndCameraMove();
            if (!handle.user) 
            { 
                chaosSnapInstances.Remove(handle); 
                yield break; 
            }
            yield return new WaitForSeconds(endDuration * (1 - CAMERA_MOVEMENT_PERCENT_OF_DURATION));
            chaosSnapInstances.Remove(handle);
        }
        private IEnumerator InstantTeleport(ChaosSnapInstanceHandle handle, float endDuration)
        {
            TeleportVFX(handle.user, handle.user.corePosition, true, 0.2f, false);
            TeleportVFX(handle.user, VFXPosition(handle.user, handle.targetPosition), false, endDuration, true);
            TeleportHelper.TeleportBody(handle.user, handle.targetPosition, true);
            yield return new WaitForSeconds(endDuration * CAMERA_MOVEMENT_PERCENT_OF_DURATION);
            handle.EndCameraMove();
            if (!handle.user)
            {
                chaosSnapInstances.Remove(handle);
                yield break;
            }
            yield return new WaitForSeconds(endDuration * (1 - CAMERA_MOVEMENT_PERCENT_OF_DURATION));
            chaosSnapInstances.Remove(handle);
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
            TeleportVFX(target, position, teleportIn ? Assets.chaosSnapInEffect : Assets.chaosSnapOutEffect, duration, useTemporaryOverlay);
        }
        public static void TeleportVFX(CharacterBody target, Vector3 position, GameObject vfxPrefab, float duration, bool useTemporaryOverlay)
        {
            if (duration <= 0.05f) 
            { 
                if (vfxPrefab.TryGetComponent<ChaosSnapVFX>(out var vfx) && !string.IsNullOrEmpty(vfx.soundString))
                {
                    PlayChaosSnapSound(vfx.soundString, duration, target.gameObject);
                }
                return;
            }
            float scale = 1f;
            if (target)
            {
                switch (target.hullClassification)
                {
                    case HullClassification.Golem:
                        scale = 2f;
                        break;
                    case HullClassification.BeetleQueen:
                        scale = 5f;
                        break;
                    default:
                        break;
                }
            }
            EffectData data = new EffectData
            {
                scale = scale,
                genericFloat = duration,
                origin = position,
                rootObject = target ? target.gameObject : null,
                genericBool = useTemporaryOverlay && target
            };
            EffectManager.SpawnEffect(vfxPrefab, data, true);
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

        public static void PlayChaosSnapSound(string soundString, float duration, GameObject gameObject)
        {
            AkSoundEngine.SetRTPCValueByPlayingID("teleportDuration", duration, RoR2.Util.PlaySound(soundString, gameObject));
        }

        // Camera movement
        // Not sure if this set up is a good idea, since usually there's a separate ICameraStateProvider for each target, but I'm trying to do all targets with one provider
        // Worst case, it couldn't break, right? It'd just hold onto this override that won't actually change anything. Eventually it'll get overwritten by another override or..
        // .. when the singleton is deleted and respawned next stage. Teleports won't last more than a second and all overrides get cleared when all teleports end
        // Yep... solid... for sure

        public const float maxLerpTime = 0.35f; // Copied from amy multi-lock, currently unused. Not sure if I want to use it
        public class ChaosSnapInstanceHandle
        {
            public CharacterBody user;
            public Vector3 startPosition;
            public Vector3 targetPosition;
            public Quaternion targetRotation;
            public bool cameraMove;

            public void StartCameraMove(float lerpTime)
            {
                foreach (CameraRigController cameraRigController in CameraRigController.readOnlyInstancesList)
                {
                    if (cameraRigController.target == user.gameObject)
                    {
                        cameraRigController.SetOverrideCam(instance, lerpTime);//Mathf.Min(lerpTime, maxLerpTime));
                    }
                    else if (cameraRigController.IsOverrideCam(instance))
                    {
                        cameraRigController.SetOverrideCam(null, 0.05f);
                    }
                }
                cameraMove = true;
            }
            public void EndCameraMove()
            {
                ReadOnlyCollection<CameraRigController> readOnlyInstancesList = CameraRigController.readOnlyInstancesList;
                for (int i = 0; i < readOnlyInstancesList.Count; i++)
                {
                    CameraRigController cameraRigController = readOnlyInstancesList[i];
                    if (cameraRigController.IsOverrideCam(instance) && (instance.chaosSnapInstances.Count == 0 || (!user || cameraRigController.target == user.gameObject)))
                    {
                        cameraRigController.SetOverrideCam(null, 0.2f);
                    }
                }
                cameraMove = false;
            }
        }
        private readonly List<ChaosSnapInstanceHandle> chaosSnapInstances = new List<ChaosSnapInstanceHandle>();

        public static ChaosSnapInstanceHandle CreateInstance(CharacterBody user, Vector3 targetPosition, Vector3 targetForward)
        {
            targetForward.y = Mathf.Clamp(targetForward.y, -0.3f, 0f);
            ChaosSnapInstanceHandle provider = new ChaosSnapInstanceHandle
            {
                user = user,
                startPosition = user.transform.position,
                targetPosition = targetPosition,
                targetRotation = targetForward == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(targetForward)
            };
            instance.chaosSnapInstances.Add(provider);
            return provider;
        }

        public void GetCameraState(CameraRigController cameraRigController, ref CameraState cameraState)
        {
            if (TryGetInstance(cameraRigController, out var cameraInstance) && cameraInstance.cameraMove)
            {
                Vector3 userPosition = cameraInstance.user ? cameraInstance.user.transform.position : cameraInstance.startPosition;

                Vector3 cameraLocalPos = cameraRigController.targetParams.currentCameraParamsData.idealLocalCameraPos.value;
                cameraLocalPos.y += cameraRigController.targetParams.currentCameraParamsData.pivotVerticalOffset.value + 0.9f;
                cameraLocalPos += (cameraRigController.targetParams.cameraPivotTransform ? cameraRigController.targetParams.cameraPivotTransform.localPosition : Vector3.zero);
                Vector3 cameraPosition = cameraInstance.targetPosition;
                cameraPosition += (cameraInstance.targetRotation == Quaternion.identity ? cameraState.rotation : cameraInstance.targetRotation) * cameraLocalPos;

                Vector3 between = cameraPosition - userPosition;
                float distanceFromPivot = Raycast(userPosition, between.normalized, between.magnitude, 0.09f);
                Vector3 finalPosition = (between.normalized * distanceFromPivot) + userPosition;

                cameraState.position = finalPosition;

                if (cameraInstance.targetRotation != Quaternion.identity)
                {
                    cameraState.rotation = cameraInstance.targetRotation.normalized;
                }
            }
        }
        public float Raycast(Vector3 origin, Vector3 direction, float maxDistance, float wallCushion)
        {
            LayerIndex world = LayerIndex.world;
            int hitCount = HGPhysics.SphereCastAll(out RaycastHit[] array, origin, wallCushion, direction, maxDistance, world.mask, QueryTriggerInteraction.Ignore);
            float num = maxDistance;
            for (int i = 0; i < hitCount; i++)
            {
                float distance = array[i].distance;
                if (distance < num)
                {
                    Collider collider = array[i].collider;
                    if (collider && !collider.GetComponent<NonSolidToCamera>())
                    {
                        num = distance;
                    }
                }
            }
            HGPhysics.ReturnResults(array);
            return num;
        }

        public bool TryGetInstance(CameraRigController cameraRigController, out ChaosSnapInstanceHandle cameraInstance)
        {
            return TryGetInstance(cameraRigController.target, out cameraInstance);
        }
        public bool TryGetInstance(GameObject target, out ChaosSnapInstanceHandle cameraInstance)
        {
            cameraInstance = null;
            for (int i = 0; i < chaosSnapInstances.Count; i++)
            {
                if (chaosSnapInstances[i].user == target)
                {
                    cameraInstance = chaosSnapInstances[i];
                    return true;
                }
            }
            return false;
        }

        public bool IsHudAllowed(CameraRigController cameraRigController)
        {
            return true;
        }

        public bool IsUserControlAllowed(CameraRigController cameraRigController)
        {
            return true;
        }

        public bool IsUserLookAllowed(CameraRigController cameraRigController)
        {
            return true;
        }
    }
}
