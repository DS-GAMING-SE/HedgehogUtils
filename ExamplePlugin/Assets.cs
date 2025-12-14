using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using R2API;
using UnityEngine.AddressableAssets;
using System.Reflection;
using HedgehogUtils.Internal;
using HedgehogUtils.Forms.SuperForm;
using RoR2.Audio;
using static RoR2.VFXAttributes;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Rendering.PostProcessing;
using HedgehogUtils.Miscellaneous;

namespace HedgehogUtils
{
    public static class Assets
    {
        private const string assetbundleName = "hedgehogutilsbundle";
        private const string dllName = "HedgehogUtils.dll";

        internal static AssetBundle mainAssetBundle;

        public static void Initialize()
        {
            LoadAssetBundle();
            LoadSoundbank();
            Miscellaneous();
            BoostAndLaunch();
            SuperForm();
        }

        internal static void LoadAssetBundle()
        {
            try
            {
                if (mainAssetBundle == null)
                {
                    mainAssetBundle = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location.Replace(dllName, assetbundleName));
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to load assetbundle. Make sure your assetbundle name is setup correctly\n" + e);
                return;
            }
        }

        internal static void LoadSoundbank()
        {
            using (Stream manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("HedgehogUtils.HedgehogUtilsBank.bnk"))
            {
                byte[] array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
                SoundAPI.SoundBanks.Add(array);
            }
        }

        #region Boost
        internal static GameObject powerBoostFlashEffect;
        internal static GameObject powerBoostAuraEffect;

        public static GameObject boostHUD;
        #endregion

        #region Launch
        internal static GameObject launchAuraEffect;
        internal static GameObject launchCritAuraEffect;

        internal static GameObject launchHitEffect;
        internal static GameObject launchCritHitEffect;

        internal static GameObject launchWallCollisionEffect;
        internal static GameObject launchWallCollisionLargeEffect;
        #endregion

        public static void BoostAndLaunch()
        {
            powerBoostFlashEffect = MaterialSwap(Assets.LoadEffect("SonicPowerBoostFlash", true), "RoR2/Base/Common/VFX/matDistortionFaded.mat", "Distortion");
            powerBoostAuraEffect = Assets.LoadAsyncedEffect("SonicPowerBoostAura");

            boostHUD = Assets.mainAssetBundle.LoadAsset<GameObject>("BoostMeter");

            #region Launch
            launchAuraEffect = CreateNewBoostAura(HedgehogUtilsPlugin.Prefix + "LAUNCH_AURA_VFX",
                1,
                0.4f,
                new Color(1f, 1f, 1f),
                new Color(0.7f, 0.7f, 0.7f),
                new Color(0.4f, 0.45f, 0.5f),
                Color.black);
            launchCritAuraEffect = CreateNewBoostAura(HedgehogUtilsPlugin.Prefix + "LAUNCH_CRIT_AURA_VFX",
                1,
                0.4f,
                new Color(1f, 1f, 1f),
                new Color(0.7f, 0.7f, 0.7f),
                new Color(0.8f, 0.1f, 0.2f),
                new Color(0.3f, 0f, 0f));
            #endregion

            AsyncOperationHandle<GameObject> asyncHit = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ArmorReductionOnHit/PulverizedEffect.prefab");
            asyncHit.Completed += delegate (AsyncOperationHandle<GameObject> x)
            {
                launchHitEffect = CreateLaunchHitEffect(x.Result, "HedgehogUtilsLaunchHitEffect", new Color(1f, 0.8f, 0.4f), new Color(0.8f, 0.8f, 0.8f));
                launchCritHitEffect = CreateLaunchHitEffect(x.Result, "HedgehogUtilsLaunchCritHitEffect", new Color(1f, 0.1f, 0.2f), new Color(0.9f, 0.7f, 0.7f));
            };

            AsyncOperationHandle<GameObject> asyncWallCollision = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/SurvivorPod/PodGroundImpact.prefab");
            asyncWallCollision.Completed += delegate (AsyncOperationHandle<GameObject> x)
            {
                // Launch Wall Large
                launchWallCollisionLargeEffect = PrefabAPI.InstantiateClone(x.Result, "HedgehogUtilsLaunchWallCollisionLarge");
                launchWallCollisionLargeEffect.AddComponent<NetworkIdentity>();
                launchWallCollisionLargeEffect.GetComponent<ShakeEmitter>().wave.amplitude = 0.4f;
                GameObject.Destroy(launchWallCollisionLargeEffect.GetComponent<AlignToNormal>());
                GameObject.Destroy(launchWallCollisionLargeEffect.transform.Find("Particles/Flash").gameObject);
                GameObject.Destroy(launchWallCollisionLargeEffect.transform.Find("Particles/Sparks").gameObject);
                GameObject.Destroy(launchWallCollisionLargeEffect.transform.Find("Particles/Point Light").gameObject);
                ParticleSystem.ShapeModule dustLargeShapeModule = launchWallCollisionLargeEffect.transform.Find("Particles/Dust, Directional").GetComponent<ParticleSystem>().shape;
                dustLargeShapeModule.radius = 6f;
                launchWallCollisionLargeEffect.GetComponent<EffectComponent>().soundName = "";


                // Launch Wall
                launchWallCollisionEffect = PrefabAPI.InstantiateClone(x.Result, "HedgehogUtilsLaunchWallCollision");
                launchWallCollisionEffect.AddComponent<NetworkIdentity>();
                GameObject.Destroy(launchWallCollisionEffect.GetComponent<AlignToNormal>());
                GameObject.Destroy(launchWallCollisionEffect.transform.Find("Particles/Flash").gameObject);
                GameObject.Destroy(launchWallCollisionEffect.transform.Find("Particles/Sparks").gameObject);
                GameObject.Destroy(launchWallCollisionEffect.transform.Find("Particles/Point Light").gameObject);
                GameObject.Destroy(launchWallCollisionEffect.transform.Find("Particles/Debris, 3D").gameObject);
                GameObject.Destroy(launchWallCollisionEffect.GetComponent<ShakeEmitter>());
                GameObject dustDirectional = launchWallCollisionEffect.transform.Find("Particles/Dust, Directional").gameObject;
                ParticleSystem.MainModule dustDirectionalMain = dustDirectional.GetComponent<ParticleSystem>().main;
                ParticleSystem.MinMaxCurve dustDirectionalLifetimeCurve = dustDirectionalMain.startLifetime;
                dustDirectionalLifetimeCurve.constantMax = 0.3f;
                dustDirectionalLifetimeCurve.constantMax = 0.6f;
                dustDirectionalMain.startLifetime = dustDirectionalLifetimeCurve;
                ParticleSystem.MinMaxCurve dustDirectionalSizeCurve = dustDirectionalMain.startSize;
                dustDirectionalSizeCurve.constantMin = 0.4f;
                dustDirectionalSizeCurve.constantMax = 1.3f;
                dustDirectionalMain.startSize = dustDirectionalSizeCurve;

                GameObject dust = launchWallCollisionEffect.transform.Find("Particles/Dust").gameObject;
                ParticleSystem.MainModule dustMain = dust.GetComponent<ParticleSystem>().main;
                ParticleSystem.MinMaxCurve dustLifetimeCurve = dustMain.startLifetime;
                dustLifetimeCurve.constantMin = 0.6f;
                dustLifetimeCurve.constantMax = 1.4f;
                dustMain.startLifetime = dustLifetimeCurve;
                ParticleSystem.MinMaxCurve dustSizeCurve = dustMain.startSize;
                dustSizeCurve.constantMin = 1.5f;
                dustSizeCurve.constantMax = 3f;
                dustMain.startSize = dustSizeCurve;

                ParticleSystem debris = launchWallCollisionEffect.transform.Find("Particles/Debris").gameObject.GetComponent<ParticleSystem>();
                ParticleSystem.EmissionModule debrisEmission = debris.emission;
                ParticleSystem.Burst debrisBurst = debrisEmission.GetBurst(0);
                debrisBurst.count = 10;
                debris.emission.SetBurst(0, debrisBurst);

                /*ParticleSystem debris3D = launchWallCollisionEffect.transform.Find("Particles/Debris, 3D").gameObject.GetComponent<ParticleSystem>();
                ParticleSystem.EmissionModule debris3DEmission = debris3D.emission;
                ParticleSystem.Burst debris3DBurst = debris3DEmission.GetBurst(0);
                debris3DBurst.count = 3;
                debris3D.emission.SetBurst(0, debris3DBurst);*/

                launchWallCollisionEffect.GetComponent<EffectComponent>().soundName = "";

                AddNewEffectDef(launchWallCollisionEffect, "Play_hedgehogutils_launch_collide");
                AddNewEffectDef(launchWallCollisionLargeEffect, "Play_hedgehogutils_launch_collide_large");
            };
        }

        private static GameObject CreateLaunchHitEffect(GameObject baseHitEffect, string name, Color ringColor, Color beamColor)
        {
            GameObject hitEffect = PrefabAPI.InstantiateClone(baseHitEffect, name);
            hitEffect.AddComponent<NetworkIdentity>();
            GameObject.Destroy(hitEffect.GetComponent<ParticleSystem>());
            ParticleSystem.MainModule ring = hitEffect.transform.Find("Ring").gameObject.GetComponent<ParticleSystem>().main;
            ring.startColor = ringColor;
            ParticleSystem.MainModule beam = hitEffect.transform.Find("Beams").gameObject.GetComponent<ParticleSystem>().main;
            beam.startColor = beamColor;
            GameObject.Destroy(hitEffect.transform.Find("Mesh").gameObject);
            GameObject.Destroy(hitEffect.transform.Find("Point Light").gameObject);
            hitEffect.GetComponent<EffectComponent>().soundName = "Play_beetle_guard_impact";
            AddNewEffectDef(hitEffect, "");
            return hitEffect;
        }

        #region Super Form
        public static Material superFormOverlay;
        public static Material rainbowGlowMaterial;
        public static Material rainbowGlowSubtleMaterial;
        public static GameObject superFormTransformationEffect;
        public static GameObject transformationEmeraldSwirl;
        public static GameObject superFormAura;
        public static GameObject superFormWarning;
        public static LoopSoundDef superLoopSoundDef;
        public static GameObject superFormPPVolume;
        #endregion

        public static void SuperForm()
        {
            superFormTransformationEffect = Assets.LoadEffect("SonicSuperTransformation");
            if (superFormTransformationEffect)
            {
                ShakeEmitter shakeEmitter = superFormTransformationEffect.AddComponent<ShakeEmitter>();
                shakeEmitter.amplitudeTimeDecay = true;
                shakeEmitter.duration = 0.7f;
                shakeEmitter.radius = 200f;
                shakeEmitter.scaleShakeRadiusWithLocalScale = false;

                shakeEmitter.wave = new Wave
                {
                    amplitude = 0.7f,
                    frequency = 40f,
                    cycleOffset = 0f
                };
            }
            ReplaceRainbow(superFormTransformationEffect.transform.Find("Rainbow"));
            transformationEmeraldSwirl = Assets.LoadEffect("SonicChaosEmeraldSwirl");

            superFormAura = Assets.LoadAsyncedEffect("SonicSuperAura");

            ReplaceRainbow(superFormAura.transform.Find("Rainbow"), true);

            superFormWarning = Assets.LoadEffect("SonicSuperWarning");
            superFormWarning.AddComponent<Miscellaneous.DestroyOnExitForm>();
            //FormDefs haven't been initialized yet so I gotta wait before I can set the Form for the DestroyOnExitForm. That is done in SuperFormDef initialize
            EffectComponent warningEffect = superFormWarning.GetComponent<EffectComponent>();
            warningEffect.parentToReferencedTransform = true;

            AsyncOperationHandle<Material> asyncOutlineMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/LunarGolem/matLunarGolemShield.mat");
            asyncOutlineMaterial.Completed += delegate (AsyncOperationHandle<Material> x)
            {
                superFormOverlay = new Material(x.Result);
                superFormOverlay.SetColor("_TintColor", new Color(1, 0.8f, 0.4f, 1));
                superFormOverlay.SetColor("_EmissionColor", new Color(1, 0.8f, 0.4f, 1));
                superFormOverlay.SetFloat("_OffsetAmount", 0.01f);
            };

            superLoopSoundDef = ScriptableObject.CreateInstance<LoopSoundDef>();
            superLoopSoundDef.startSoundName = "Play_hedgehogutils_super_loop";
            superLoopSoundDef.stopSoundName = "Stop_hedgehogutils_super_loop";

            superFormPPVolume = mainAssetBundle.LoadAsset<GameObject>("SonicSuperPostProcess");
            PostProcessVolume postProcess = superFormPPVolume.GetComponent<PostProcessVolume>();
            postProcess.sharedProfile = Addressables.LoadAssetAsync<PostProcessProfile>("RoR2/Base/title/PostProcessing/ppLocalGrandparent.asset").WaitForCompletion();
        }
        public static void ReplaceRainbow(Transform particle, bool subtle = false)
        {
            ParticleSystemRenderer rainbowAura = particle.GetComponent<ParticleSystemRenderer>();
            rainbowAura.sharedMaterial = subtle ? rainbowGlowSubtleMaterial : rainbowGlowMaterial;
            rainbowAura.mesh = Addressables.LoadAssetAsync<Mesh>("RoR2/Base/Common/VFX/mdlVFXDonut1.fbx").WaitForCompletion();
        }

        public static Material ringMaterial;
        #region Chaos Snap
        // Look into using texRampLightning2
        public static GameObject chaosSnapInEffect;
        public static GameObject chaosSnapOutEffect;
        public static Material chaosSnapMaterial;
        public static NetworkSoundEventDef chaosSnapSoundEventDef;
        public static NetworkSoundEventDef chaosSnapLargeSoundEventDef;
        #endregion
        public static void Miscellaneous()
        {
            rainbowGlowMaterial = new Material(Addressables.LoadAssetAsync<Material>("RoR2/DLC2/Elites/EliteBead/matEliteBeadSpikeGrowthRing.mat").WaitForCompletion());
            rainbowGlowMaterial.SetTexture("_RemapTex", mainAssetBundle.LoadAsset<Texture>("texRampRainbow"));
            rainbowGlowMaterial.SetFloat("_Boost", 10f);
            rainbowGlowSubtleMaterial = new Material(rainbowGlowMaterial);
            rainbowGlowSubtleMaterial.SetFloat("_Boost", 1.5f);

            AsyncOperationHandle<Material> asyncRingMaterial = Addressables.LoadAssetAsync<Material>("RoR2/DLC2/Elites/EliteAurelionite/matEliteAurelioniteAffixOverlay.mat");
            asyncRingMaterial.Completed += delegate (AsyncOperationHandle<Material> x)
            {
                ringMaterial = new Material(x.Result);
                ringMaterial.SetFloat("_NormalStrength", 0);
                ringMaterial.SetColor("_Color", new Color(0.9f, 0.8f, 0.1f, 1));
            };

            chaosSnapMaterial = new Material(Addressables.LoadAssetAsync<Material>("RoR2/Base/Huntress/matHuntressSwipe.mat").WaitForCompletion());
            chaosSnapMaterial.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampLightning2.png").WaitForCompletion());
            chaosSnapInEffect = CreateChaosSnapEffect("ChaosSnapInVFX", true);
            chaosSnapOutEffect = CreateChaosSnapEffect("ChaosSnapOutVFX", false);

            chaosSnapSoundEventDef = CreateNetworkSoundEventDef("Play_hedgehogutils_teleport");
            chaosSnapLargeSoundEventDef = CreateNetworkSoundEventDef("Play_hedgehogutils_teleport_large");
        }

        public static GameObject CreateChaosSnapEffect(string assetBundlePrefabName, bool teleportIn)
        {
            GameObject effect = mainAssetBundle.LoadAsset<GameObject>(assetBundlePrefabName);
            VFXAttributes vfxAttributes = effect.AddComponent<VFXAttributes>();
            vfxAttributes.vfxPriority = VFXPriority.Medium;
            ParticleSystem teleportLine = effect.transform.Find("TeleportLine").GetComponent<ParticleSystem>();
            ParticleSystemRenderer teleportLineRenderer = teleportLine.GetComponent<ParticleSystemRenderer>();
            ParticleSystem distortion = effect.transform.Find("Distortion").GetComponent<ParticleSystem>();
            ParticleSystemRenderer distortionRenderer = distortion.GetComponent<ParticleSystemRenderer>();
            ParticleSystem flash = effect.transform.Find("Flash").GetComponent<ParticleSystem>();
            ParticleSystemRenderer flashRenderer = flash.GetComponent<ParticleSystemRenderer>();
            effect.AddComponent<DestroyOnParticleEnd>().trackedParticleSystem = teleportLine;
            // Scale Particle Duration
            ScaleParticleSystemDuration scaleParticleSystemDuration = effect.AddComponent<ScaleParticleSystemDuration>();
            scaleParticleSystemDuration.initialDuration = 0.5f;
            scaleParticleSystemDuration.particleSystems = [teleportLine, distortion, flash];
            // Light
            Transform light = effect.transform.Find("Point Light");
            vfxAttributes.optionalLights = [light.GetComponent<Light>()];
            LightIntensityCurve lightCurve = light.gameObject.AddComponent<LightIntensityCurve>();
            lightCurve.curve = teleportIn ? AnimationCurve.EaseInOut(0, 0, 1, 1) : AnimationCurve.EaseInOut(0, 1, 1, 0);
            lightCurve.timeMax = 0.5f;
            // Material swapping
            teleportLineRenderer.sharedMaterial = chaosSnapMaterial;
            distortionRenderer.sharedMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matInverseDistortion.mat").WaitForCompletion();
            flashRenderer.sharedMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matGenericFlash.mat").WaitForCompletion();

            EffectComponent effectComponent = effect.AddComponent<EffectComponent>();
            effectComponent.applyScale = true;
            effectComponent.applyScaleFirst = true;
            effectComponent.positionAtReferencedTransform = false;
            effect.AddComponent<ChaosSnapVFX>().reverse = teleportIn;

            AddNewEffectDef(effect);

            return effect;
        }

        public static GameObject MaterialSwap(GameObject prefab, string assetPath, string pathToParticle = "")
        {
            Transform transform = prefab.transform.Find(pathToParticle);
            if (transform)
            {
                transform.GetComponent<ParticleSystemRenderer>().sharedMaterial = Addressables.LoadAssetAsync<Material>(assetPath).WaitForCompletion();
            }
            return prefab;
        }
        public static GameObject MaterialSwap(GameObject prefab, Material material, string pathToParticle = "")
        {
            Transform transform = prefab.transform.Find(pathToParticle);
            if (transform)
            {
                transform.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            }
            return prefab;
        }

        // Use this to create a new boost flash prefab to be used when activating a custom boost skill
        // name: An internal name for the prefab. Doesn't really matter what this is as long as it's not the same as anything else
        // size: The size of the effect. Power Boost defaults to 1. Super Boost defaults to 1.3
        // alpha: How visible the effect will be. Power Boost defaults to 1.3. Super Boost defaults to 1.6
        // color1: The innermost color of the effect
        // color2: The color between the innermost color and the edge color
        // color3: The color of the edge of the boost effect
        // lightColor: The color of the light emitted
        public static GameObject CreateNewBoostFlash(string name, float size, float alpha, Color color1, Color color2, Color color3, Color lightColor)
        {
            GameObject newFlash = PrefabAPI.InstantiateClone(powerBoostFlashEffect, name);
            AddNewEffectDef(newFlash);

            ParticleSystem.MainModule main = newFlash.transform.Find("BlueCone").GetComponent<ParticleSystem>().main;
            main.startSize = new ParticleSystem.MinMaxCurve(main.startSize.constant * size);

            ParticleSystem.MainModule main2 = newFlash.transform.Find("BlueCone/BlueCone2").GetComponent<ParticleSystem>().main;
            main2.startSize = new ParticleSystem.MinMaxCurve(main2.startSize.constant * size);

            ParticleSystemRenderer renderer = newFlash.transform.Find("BlueCone").GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateNewBoostMaterial(alpha, color1, color2, color3);

            ParticleSystemRenderer renderer2 = newFlash.transform.Find("BlueCone/BlueCone2").GetComponent<ParticleSystemRenderer>();
            renderer2.material = CreateNewBoostMaterial(alpha, color1, color2, color3);

            ParticleSystem.MainModule color = newFlash.transform.Find("BlueCone/StartFlash").GetComponent<ParticleSystem>().main;
            if (lightColor == Color.black)
            {
                ParticleSystem.LightsModule light = newFlash.transform.Find("BlueCone/StartFlash").GetComponent<ParticleSystem>().lights;
                light.enabled = false;
            }
            else
            {
                color.startColor = lightColor;
                newFlash.transform.Find("BlueCone/StartFlash/Point Light").GetComponent<Light>().color = lightColor;
            }

            return newFlash;
        }

        // Use this to create a new boost aura prefab to be used constantly while using a custom boost skill
        // name: An internal name for the prefab. Doesn't really matter what this is as long as it's not the same as anything else
        // size: The size of the effect. Power Boost defaults to 1. Super Boost defaults to 1.3
        // alpha: How visible/strong the effect will be. Power Boost defaults to 0.65. Super Boost defaults to 0.8
        // color1: The innermost color of the effect
        // color2: The color between the innermost color and the edge color
        // color3: The color of the edge of the boost effect
        // lightColor: The color of the light emitted
        public static GameObject CreateNewBoostAura(string name, float size, float alpha, Color color1, Color color2, Color color3, Color lightColor)
        {
            GameObject newAura = PrefabAPI.InstantiateClone(powerBoostAuraEffect, name);
            newAura.transform.Find("Aura").localScale *= size;
            newAura.transform.Find("Aura").GetComponent<MeshRenderer>().material = CreateNewBoostMaterial(alpha, color1, color2, color3);
            if (lightColor == Color.black)
            {
                newAura.transform.Find("Point Light").GetComponent<Light>().enabled = false;
            }
            else
            {
                newAura.transform.Find("Point Light").GetComponent<Light>().color = lightColor;
            }
            return newAura;
        }

        private static Material CreateNewBoostMaterial(float alpha, Color color1, Color color2, Color color3)
        {
            Material newMaterial = new Material(Assets.mainAssetBundle.LoadAsset<Material>("matPowerBoost"));
            newMaterial.SetFloat("_AlphaBoost", alpha);
            newMaterial.SetColor("_Color1", color1);
            newMaterial.SetColor("_Color2", color2);
            newMaterial.SetColor("_Color3", color3);

            return newMaterial;
        }


        private static NetworkSoundEventDef CreateNetworkSoundEventDef(string eventName)
        {
            NetworkSoundEventDef networkSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            networkSoundEventDef.akId = AkSoundEngine.GetIDFromString(eventName);
            networkSoundEventDef.eventName = eventName;

            Content.AddNetworkSoundEventDef(networkSoundEventDef);

            return networkSoundEventDef;
        }

        private static GameObject LoadEffect(string resourceName)
        {
            return LoadEffect(resourceName, "", false);
        }

        private static GameObject LoadEffect(string resourceName, string soundName)
        {
            return LoadEffect(resourceName, soundName, false);
        }

        private static GameObject LoadEffect(string resourceName, bool parentToTransform)
        {
            return LoadEffect(resourceName, "", parentToTransform);
        }

        private static GameObject LoadAsyncedEffect(string resourceName)
        {
            GameObject newEffect = mainAssetBundle.LoadAsset<GameObject>(resourceName);

            newEffect.AddComponent<NetworkIdentity>();

            return newEffect;
        }

        private static GameObject LoadEffect(string resourceName, string soundName, bool parentToTransform)
        {
            GameObject newEffect = mainAssetBundle.LoadAsset<GameObject>(resourceName);

            if (!newEffect)
            {
                Log.Error("Failed to load effect: " + resourceName + " because it does not exist in the AssetBundle");
                return null;
            }

            newEffect.AddComponent<DestroyOnTimer>().duration = 12;
            newEffect.AddComponent<NetworkIdentity>();
            var vfx = newEffect.AddComponent<VFXAttributes>();
            vfx.vfxPriority = VFXAttributes.VFXPriority.Always;
            vfx.DoNotPool = true;
            var effect = newEffect.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = parentToTransform;
            effect.positionAtReferencedTransform = true;
            effect.soundName = soundName;

            AddNewEffectDef(newEffect, soundName);

            return newEffect;
        }

        private static void AddNewEffectDef(GameObject effectPrefab)
        {
            AddNewEffectDef(effectPrefab, "");
        }

        private static void AddNewEffectDef(GameObject effectPrefab, string soundName)
        {
            EffectDef newEffectDef = new EffectDef();
            newEffectDef.prefab = effectPrefab;
            newEffectDef.prefabEffectComponent = effectPrefab.GetComponent<EffectComponent>();
            newEffectDef.prefabName = effectPrefab.name;
            newEffectDef.prefabVfxAttributes = effectPrefab.GetComponent<VFXAttributes>();
            newEffectDef.spawnSoundEventName = soundName;

            Content.AddEffectDef(newEffectDef);
        }
    }
}