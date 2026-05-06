using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Miscellaneous
{
    public class FadeTrailAndLightWithDestroy : MonoBehaviour
    {
        private EffectManagerHelper emh;
        private DestroyOnTimer destroyOnTimer;
        private DisableParticleEmissionAndDestroyOnTimer disableParticleDestroy;
        public TrailRenderer trail;
        public LightIntensityCurve lightIntensityCurve;

        private void Awake()
        {
            destroyOnTimer = GetComponent<DestroyOnTimer>();
            disableParticleDestroy = GetComponent<DisableParticleEmissionAndDestroyOnTimer>();
        }
        private void OnEnable()
        {
            if (destroyOnTimer) destroyOnTimer.enabled = false;
            if (lightIntensityCurve) lightIntensityCurve.enabled = false;
            if (trail) trail.emitting = true;
        }
        private void Start()
        {
            if (disableParticleDestroy) // dumbass costed me hours
            {
                emh = GetComponent<EffectManagerHelper>();
                disableParticleDestroy.efh = emh;
            }
        }
        public void StartDisable()
        {
            if (destroyOnTimer) { destroyOnTimer.enabled = true; }
            if (disableParticleDestroy) { disableParticleDestroy.DisableParticlesStartTimer(); }
            if (lightIntensityCurve) { lightIntensityCurve.enabled = true; }
            if (trail) { trail.emitting = false; }
        }
    }
}
