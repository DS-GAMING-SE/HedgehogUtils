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
        }

        private static GameObject CreateLaunchHitEffect(GameObject baseHitEffect, string name, Color ringColor, Color beamColor)
        {
            GameObject hitEffect = PrefabAPI.InstantiateClone(baseHitEffect, name);
            GameObject.Destroy(hitEffect.GetComponent<ParticleSystem>());
            ParticleSystem.MainModule ring = hitEffect.transform.Find("Ring").gameObject.GetComponent<ParticleSystem>().main;
            ring.startColor = ringColor;
            ParticleSystem.MainModule beam = hitEffect.transform.Find("Beams").gameObject.GetComponent<ParticleSystem>().main;
            beam.startColor = beamColor;
            GameObject.Destroy(hitEffect.transform.Find("Mesh").gameObject);
            GameObject.Destroy(hitEffect.transform.Find("Point Light").gameObject);
            AddNewEffectDef(hitEffect, "");
            return hitEffect;
        }

        #region Super Form
        public static Material superFormOverlay;
        public static GameObject superFormTransformationEffect;
        public static GameObject transformationEmeraldSwirl;
        public static GameObject superFormAura;
        public static GameObject superFormWarning;
        public static LoopSoundDef superLoopSoundDef;
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
            transformationEmeraldSwirl = Assets.LoadEffect("SonicChaosEmeraldSwirl");

            superFormAura = Assets.LoadAsyncedEffect("SonicSuperAura");

            superFormWarning = Assets.LoadAsyncedEffect("SonicSuperWarning");
            AsyncOperationHandle<Material> asyncOutlineMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/LunarGolem/matLunarGolemShield.mat");
            asyncOutlineMaterial.Completed += delegate (AsyncOperationHandle<Material> x)
            {
                superFormOverlay = x.Result;
                superFormOverlay.SetColor("_TintColor", new Color(1, 0.8f, 0.4f, 1));
                superFormOverlay.SetColor("_EmissionColor", new Color(1, 0.8f, 0.4f, 1));
                superFormOverlay.SetFloat("_OffsetAmount", 0.01f);
            };

            superLoopSoundDef = ScriptableObject.CreateInstance<LoopSoundDef>();
            superLoopSoundDef.startSoundName = "Play_hedgehogutils_super_loop";
            superLoopSoundDef.stopSoundName = "Stop_hedgehogutils_super_loop";
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