using EntityStates;
using HedgehogUtils.Forms.EntityStates;
using RoR2;
using RoR2.Audio;
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace HedgehogUtils.Forms.SuperForm.EntityStates
{
    public class SuperSonicTransformation : GenericTransformationBase
    {
        protected override float duration => 2.4f;
        protected override float transformationDurationPercent => 0.5f;
        private TemporaryOverlayInstance growingGlowOverlay;

        private static float cameraDistance = -7;
        private CharacterCameraParamsData cameraParams = new CharacterCameraParamsData
        {
            maxPitch = 70f,
            minPitch = -70f,
            pivotVerticalOffset = 0.5f,
            idealLocalCameraPos = new Vector3(0f, 0f, cameraDistance),
            wallCushion = 0.1f
        };
        private CameraTargetParams.CameraParamsOverrideHandle camOverrideHandle;


        public override void OnEnter()
        {
            base.OnEnter();
            if (!fromTeamSuper)
            {
                Util.PlaySound("Play_hedgehogutils_emerald", base.gameObject);
                if (base.isAuthority)
                {
                    EffectManager.SimpleEffect(Assets.transformationEmeraldSwirl, base.gameObject.transform.position, base.gameObject.transform.rotation, true);
                }
            }
            GrowingGlow(duration * transformationDurationPercent);

            this.camOverrideHandle = base.cameraTargetParams.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest
            {
                cameraParamsData = this.cameraParams,
                priority = 1f
            }, duration / 2f);

        }

        public override void Transform()
        {
            base.Transform();
            Util.PlaySound("Play_hedgehogutils_super_transform", base.gameObject);
            base.cameraTargetParams.RemoveParamsOverride(this.camOverrideHandle, 0.2f);
        }

        protected void GrowingGlow(float duration)
        {
            if (characterModel)
            {
                growingGlowOverlay = TemporaryOverlayManager.AddOverlay(characterModel.gameObject); // Flash
                growingGlowOverlay.duration = duration;
                growingGlowOverlay.animateShaderAlpha = true;
                growingGlowOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0.4f);
                growingGlowOverlay.originalMaterial = Assets.superFormGlowingMaterial;
                growingGlowOverlay.destroyComponentOnEnd = true;
                growingGlowOverlay.inspectorCharacterModel = characterModel;
            }
        }
    }
}