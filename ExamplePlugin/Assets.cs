﻿using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using R2API;
using UnityEngine.AddressableAssets;
using System.Reflection;
using HedgehogUtils.Internal;
using HedgehogUtils.Forms.SuperForm;

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
            BoostAndLaunch();
            SuperForm();
        }

        // The assetbundle will contain assets made/imported through Unity, such as images, meshes, prefabs, etc.
        // Put any assets you make in the AssetBundle folder in the Unity project for it to be included in the assetbundle
        // Use the AssetBundle Browser found in the Window menu to build the asset bundle. By default, it will be built to UnityProject/SonicFormsExample/AssetBundles/StandaloneWindows
        // The file for the asset bundle needs to be put in the same folder as the dll when trying to run the mod
        // You can change the default location the assetbundle will be placed when it's built so you won't have to manually move the file every time
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

        #region Boost
        internal static GameObject powerBoostFlashEffect;
        internal static GameObject powerBoostAuraEffect;
        #endregion

        #region Launch
        internal static GameObject launchAuraEffect;
        internal static GameObject launchCritAuraEffect;
        #endregion

        public static void BoostAndLaunch()
        {
            powerBoostFlashEffect = MaterialSwap(Assets.LoadEffect("SonicPowerBoostFlash", true), "RoR2/Base/Common/VFX/matDistortionFaded.mat", "Distortion");
            powerBoostAuraEffect = Assets.LoadAsyncedEffect("SonicPowerBoostAura");

            #region Launch
            launchAuraEffect = CreateNewBoostAura(HedgehogUtilsPlugin.Prefix + "LAUNCH_AURA_VFX",
                1,
                0.2f,
                new Color(1f, 1f, 1f),
                new Color(0.7f, 0.7f, 0.7f),
                new Color(0.4f, 0.45f, 0.5f),
                Color.black);
            launchCritAuraEffect = CreateNewBoostAura(HedgehogUtilsPlugin.Prefix + "LAUNCH_CRIT_AURA_VFX",
                1,
                0.2f,
                new Color(1f, 1f, 1f),
                new Color(0.7f, 0.7f, 0.7f),
                new Color(0.8f, 0.1f, 0.2f),
                new Color(0.3f, 0f, 0f));
            #endregion
        }

        #region Super Form
        public static Material superFormOverlay;
        public static GameObject superFormTransformationEffect;
        public static GameObject transformationEmeraldSwirl;
        public static GameObject superFormAura;
        public static GameObject superFormWarning;
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

            superFormOverlay = new Material(Addressables.LoadAssetAsync<Material>("RoR2/Base/LunarGolem/matLunarGolemShield.mat").WaitForCompletion());
            superFormOverlay.SetColor("_TintColor", new Color(1, 0.8f, 0.4f, 1));
            superFormOverlay.SetColor("_EmissionColor", new Color(1, 0.8f, 0.4f, 1));
            superFormOverlay.SetFloat("_OffsetAmount", 0.01f);
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
        // size: The size of the effect. Power Boost defaults to 1. Super Boost defaults to ---
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

            if (lightColor == Color.black)
            {
                newFlash.transform.Find("BlueCone/StartFlash/Point Light").GetComponent<Light>().enabled = false;
            }
            else
            {
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
            newEffect.AddComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
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