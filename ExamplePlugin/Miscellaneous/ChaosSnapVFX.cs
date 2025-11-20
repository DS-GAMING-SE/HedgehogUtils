using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace HedgehogUtils.Miscellaneous
{
    [RequireComponent(typeof(EffectComponent), typeof(EffectManagerHelper))]
    public class ChaosSnapVFX : MonoBehaviour
    {
        public EffectComponent effectComponent;
        public EffectManagerHelper efh;
        public ScaleParticleSystemDuration scale;
        public LightIntensityCurve lightIntensityCurve;
        public bool reverse;

        private void Awake()
        {
            this.effectComponent = base.GetComponent<EffectComponent>();
            this.efh = base.GetComponent<EffectManagerHelper>();
            this.scale = base.GetComponent<ScaleParticleSystemDuration>();
            Transform light = transform.Find("Point Light");
            if (light)
            {
                lightIntensityCurve = light.GetComponent<LightIntensityCurve>();
            }
            if (this.efh != null)
            {
                efh.OnEffectActivated += TeleportVFX;
            }
        }
        public void TeleportVFX()
        {
            scale.newDuration = effectComponent.effectData.genericFloat;
            scale.UpdateDuration();
            if (lightIntensityCurve)
            {
                lightIntensityCurve.timeMax = effectComponent.effectData.genericFloat;
            }
            if (effectComponent.effectData.genericBool)
            {
                if (effectComponent.effectData.rootObject && effectComponent.effectData.rootObject.TryGetComponent<CharacterBody>(out CharacterBody body))
                {
                    if (body.modelLocator && body.modelLocator.modelTransform && body.modelLocator.modelTransform.TryGetComponent(out CharacterModel model))
                    {
                        Flash(model, effectComponent.effectData.genericFloat, reverse);
                    }
                }
            }
        }

        public void Flash(CharacterModel characterModel, float duration, bool reverse)
        {
            if (characterModel)
            {
                TemporaryOverlayInstance flashOverlay = TemporaryOverlayManager.AddOverlay(characterModel.gameObject); // Flash
                flashOverlay.duration = duration;
                flashOverlay.animateShaderAlpha = true;
                flashOverlay.alphaCurve = reverse ? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f) : AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                flashOverlay.originalMaterial = ChaosSnapManager.tempOverlayMaterial;
                flashOverlay.destroyComponentOnEnd = true;
                flashOverlay.inspectorCharacterModel = characterModel;
            }
        }
    }
}
