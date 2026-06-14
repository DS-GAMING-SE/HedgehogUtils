using EntityStates;
using HedgehogUtils.Boost;
using HedgehogUtils.Forms.SuperForm;
using HedgehogUtils.Internal;
using HedgehogUtils.Miscellaneous;
using R2API;
using RoR2;
using RoR2.Audio;
using RoR2.ContentManagement;
using RoR2BepInExPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.ResourceManagement.AsyncOperations;
using static RoR2.VFXAttributes;

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
        public static GameObject boostFlashEffectBase;
        public static GameObject boostAuraEffectBase;
        internal static Material boostMaterialBase;

        public static GameObject boostHUD;
        #endregion

        #region Launch
        public static GameObject launchAuraEffect;
        public static GameObject launchCritAuraEffect;

        public static GameObject launchHitEffect;
        public static GameObject launchCritHitEffect;

        public static GameObject launchWallCollisionEffect;
        public static GameObject launchWallCollisionLargeEffect;
        #endregion

        public static void BoostAndLaunch()
        {
            boostAuraEffectBase = CreateBoostAuraPrefabBase();
            boostFlashEffectBase = CreateBoostFlashPrefabBase();
            
            powerBoostFlashEffect = MaterialSwap(Assets.LoadEffect("SonicPowerBoostFlash", true), RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.matDistortionFaded_mat, "Distortion");
            powerBoostAuraEffect = Assets.LoadAsyncedEffect("SonicPowerBoostAura");

            boostHUD = Assets.mainAssetBundle.LoadAsset<GameObject>("BoostMeter");
            boostHUD.AddComponent<BoostHUD>();

            #region Launch
            /*launchAuraEffect = CreateNewBoostAura(HedgehogUtilsPlugin.Prefix + "LAUNCH_AURA_VFX",
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
                new Color(0.3f, 0f, 0f));*/
            Color windColor = new Color(0.4f, 0.4f, 0.4f);
            launchAuraEffect = CreateBoostAuraEffect("LaunchProjectileAura", null, windColor, Color.black, windColor, 1f);
            Color critColor = new Color(0.9f, 0.15f, 0.15f);
            launchCritAuraEffect = CreateBoostAuraEffect("LaunchCritProjectileAura", null, critColor, Color.black, critColor, 1f);
            #endregion

            AsyncOperationHandle<GameObject> asyncHit = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ArmorReductionOnHit.PulverizedEffect_prefab);
            asyncHit.Completed += delegate (AsyncOperationHandle<GameObject> x)
            {
                launchHitEffect = CreateLaunchHitEffect(x.Result, "HedgehogUtilsLaunchHitEffect", new Color(1f, 0.8f, 0.4f), new Color(0.8f, 0.8f, 0.8f));
                launchCritHitEffect = CreateLaunchHitEffect(x.Result, "HedgehogUtilsLaunchCritHitEffect", new Color(1f, 0.1f, 0.2f), new Color(0.9f, 0.7f, 0.7f));
            };

            AsyncOperationHandle<GameObject> asyncWallCollision = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_SurvivorPod.PodGroundImpact_prefab);
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
        #region Boost
        private static GameObject CreateBoostAuraPrefabBase()
        {
            //GameObject boostAura = Assets.mainAssetBundle.LoadAsset<GameObject>("BoostAuraPrefab");
            GameObject boostAura = LoadEffect("BoostAuraPrefab", "", true, 0.45f , false);
            var fadeDestroy = boostAura.AddComponent<FadeTrailAndLightWithDestroy>();
            fadeDestroy.trail = boostAura.transform.GetChild(2).GetComponent<TrailRenderer>();
            fadeDestroy.lightIntensityCurve = boostAura.transform.GetChild(1).gameObject.AddComponent<LightIntensityCurve>();
            fadeDestroy.lightIntensityCurve.light = fadeDestroy.lightIntensityCurve.GetComponent<Light>();
            fadeDestroy.lightIntensityCurve.curve = AnimationCurve.EaseInOut(0, 1f, 1f, 0f);
            fadeDestroy.lightIntensityCurve.timeMax = 0.25f;
            fadeDestroy.stopParticles = true;

            VFXAttributes boostVFX = boostAura.AddComponent<VFXAttributes>();
            boostVFX.vfxPriority = VFXPriority.Always;
            boostVFX.optionalLights = [boostAura.transform.GetChild(1).GetComponent<Light>()];

            ParticleSystemRenderer burst = boostAura.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystemRenderer>();
            ParticleSystemRenderer auraConstant = boostAura.transform.GetChild(0).GetChild(1).GetComponent<ParticleSystemRenderer>();
            ParticleSystemRenderer auraConstantInner = boostAura.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<ParticleSystemRenderer>();
            Mesh dome = AssetAsyncReferenceManager<Mesh>.LoadAsset(new AssetReferenceT<Mesh>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Common.mdlVFXDome_fbx_mdVFXDome_)).WaitForCompletion();
            burst.mesh = dome;
            auraConstant.mesh = dome;
            auraConstantInner.mesh = dome;

            boostMaterialBase = AssetAsyncReferenceManager<Material>.LoadAsset(new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.matDustExhaust_mat)).WaitForCompletion();
            boostMaterialBase.SetFloat("_AlphaBoost", 2f);
            boostMaterialBase.SetFloat("_SrcBlend", 1f);
            boostMaterialBase.SetFloat("_DstBlend", 1f);
            boostMaterialBase.SetColor("_TintColor", new Color(0.1f, 0.1f, 0.1f));
            burst.sharedMaterial = boostMaterialBase;
            auraConstant.sharedMaterial = boostMaterialBase;
            auraConstantInner.sharedMaterial = boostMaterialBase;

            return boostAura;
        }
        private static GameObject CreateBoostFlashPrefabBase()
        {
            GameObject boostFlash = Assets.LoadEffect("BoostFlashPrefab", "", true, 0.55f, false);
            VFXAttributes boostVFX = boostFlash.GetComponent<VFXAttributes>();
            boostVFX.DoNotPool = false;
            boostVFX.optionalLights = [boostFlash.transform.GetChild(1).GetComponent<Light>()];

            boostFlash.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystemRenderer>().mesh = AssetAsyncReferenceManager<Mesh>.LoadAsset(new AssetReferenceT<Mesh>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Common.mdlVFXDome_fbx_mdVFXDome_)).WaitForCompletion();

            boostFlash.transform.GetChild(0).GetChild(1).GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetAsyncReferenceManager<Material>.LoadAsset(new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.matDistortionFaded_mat)).WaitForCompletion();
            
            LightIntensityCurve curve = boostFlash.transform.GetChild(1).gameObject.AddComponent<LightIntensityCurve>();
            curve.light = curve.gameObject.AddComponent<Light>();
            curve.curve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            curve.timeMax = 0.5f;
            return boostFlash;
        }
        public static GameObject CreateBoostAuraEffect(string name, Color color)
        {
            return CreateBoostAuraEffect(name, Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampTritone_png).WaitForCompletion(), color, color, color, 1f);
        }
        public static GameObject CreateBoostAuraEffect(string name, Texture remapTex, Color color)
        {
            return CreateBoostAuraEffect(name, remapTex, Color.white, color, color, 1f);
        }
        public static GameObject CreateBoostAuraEffect(string name, Color tintColor, Color lightColor, Color trailColor, float size)
        {
            return CreateBoostAuraEffect(name, Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampTritone_png).WaitForCompletion(), tintColor, lightColor, trailColor, size);
        }
        public static GameObject CreateBoostAuraEffect(string name, Texture remapTex, Color tintColor, Color lightColor, Color trailColor, float size)
        {
            return CreateBoostAuraEffect(name, CreateBoostAuraOuterMaterial(name, tintColor, remapTex), CreateBoostAuraInnerMaterial(name, tintColor, remapTex), lightColor, trailColor, size);
        }
        public static Material CreateBoostAuraOuterMaterial(string name, Color tintColor, Texture remapTex)
        {
            Material constantMat = new Material(boostMaterialBase);
            constantMat.name = $"mat{name}Constant";
            constantMat.SetColor("_TintColor", tintColor);
            if (remapTex) constantMat.SetTexture("_RemapTex", remapTex);
            constantMat.SetFloat("_Boost", 1.5f);
            constantMat.SetFloat("_AlphaBoost", 5f);
            constantMat.SetInt("_Cull", 2);
            constantMat.SetTexture("_MainTex", mainAssetBundle.LoadAsset<Texture>("texBoostEffect"));
            constantMat.SetTextureScale("_MainTex", new Vector2(2.5f, 1f));
            constantMat.SetTextureOffset("_MainTex", new Vector2(0, 0.15f));
            constantMat.SetVector("_CutoffScroll", new Vector4(2f, 10f, -4f, 4f));
            constantMat.SetFloat("_DepthOffset", -0.15f);

            return constantMat;
        }
        public static Material CreateBoostAuraInnerMaterial(string name, Color tintColor, Texture remapTex)
        {
            Material constantInnerMat = new Material(boostMaterialBase);
            constantInnerMat.name = $"mat{name}ConstantInner";
            constantInnerMat.SetColor("_TintColor", tintColor);
            if (remapTex) constantInnerMat.SetTexture("_RemapTex", remapTex);
            constantInnerMat.SetFloat("_Boost", 1f);
            constantInnerMat.SetFloat("_AlphaBoost", 6f);
            constantInnerMat.SetInt("_Cull", 1);
            constantInnerMat.SetTexture("_MainTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3.texCylinderGradient_Horz_png).WaitForCompletion());
            constantInnerMat.SetTextureScale("_MainTex", new Vector2(1f, 0.45f));
            constantInnerMat.SetTextureOffset("_MainTex", new Vector2(0, 0.15f));
            constantInnerMat.SetTexture("_Cloud1Tex", mainAssetBundle.LoadAsset<Texture>("texCloudTriangles"));
            constantInnerMat.SetTextureScale("_Cloud1Tex", new Vector2(3f, 0.3f));
            constantInnerMat.SetVector("_CutoffScroll", new Vector4(20f, 20f, -30f, 10f));

            return constantInnerMat;
        }
        public static GameObject CreateBoostAuraEffect(string name, Material outerMat, Material innerMat, Color lightColor, Color trailColor, float size)
        {
            GameObject boostAura = PrefabAPI.InstantiateClone(boostAuraEffectBase, name);
            boostAura.transform.GetChild(0).GetChild(1).GetComponent<ParticleSystemRenderer>().sharedMaterial = outerMat;
            boostAura.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<ParticleSystemRenderer>().sharedMaterial = innerMat;

            var light = boostAura.transform.GetChild(1).GetComponent<Light>();
            var trail = boostAura.transform.GetChild(2).GetComponent<TrailRenderer>();
            if (lightColor != Color.black)
            {
                light.color = lightColor;
            }
            else
            {
                GameObject.Destroy(light.gameObject);
            }
            if (trailColor != Color.black)
            {
                var gradient = trail.GetColorGradientCopy();
                gradient.colorKeys = [new GradientColorKey(trailColor, 0f)];
                trail.SetColorGradient(gradient);
            }
            else
            {
                GameObject.Destroy(trail.gameObject);
            }

            ResizeBoostAura(ref boostAura, size);

            return boostAura;
        }
        public static GameObject CreateBoostFlashEffect(string name, Color color)
        {
            return CreateBoostFlashEffect(name, Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampTritone_png).WaitForCompletion(), color, color, true, 1f);
        }
        public static GameObject CreateBoostFlashEffect(string name, Texture remapTex, Color color)
        {
            return CreateBoostFlashEffect(name, remapTex, Color.white, color, true, 1f);
        }
        public static GameObject CreateBoostFlashEffect(string name, Color tintColor, Color lightColor, bool distortion, float size)
        {
            return CreateBoostFlashEffect(name, Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampTritone_png).WaitForCompletion(), tintColor, lightColor, distortion, size);
        }
        public static GameObject CreateBoostFlashEffect(string name, Texture remapTex, Color tintColor, Color lightColor, bool distortion, float size)
        {
            return CreateBoostFlashEffect(name, CreateBoostFlashMaterial(name, remapTex, tintColor), lightColor, distortion, size);
        }
        public static Material CreateBoostFlashMaterial(string name, Texture remapTex, Color tintColor)
        {
            Material flashMat = new Material(boostMaterialBase);
            flashMat.name = $"mat{name}";
            flashMat.SetColor("_TintColor", tintColor);
            if (remapTex) flashMat.SetTexture("_RemapTex", remapTex);
            flashMat.SetFloat("_Boost", 1.5f);
            flashMat.SetFloat("_AlphaBoost", 5f);
            flashMat.SetTexture("_MainTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Common.texRampVerticalSmoothFalloff_png).WaitForCompletion());
            flashMat.DisableKeyword("CLOUDOFFSET");
            flashMat.SetTexture("_Cloud1Tex", mainAssetBundle.LoadAsset<Texture>("texBoostEffect"));
            flashMat.SetTextureScale("_Cloud1Tex", new Vector2(2.5f, 1f));
            flashMat.SetVector("_CutoffScroll", new Vector4(-20f, 45f, 20f, 15f));
            return flashMat;
        }
        public static GameObject CreateBoostFlashEffect(string name, Material material, Color lightColor, bool distortion, float size)
        {
            GameObject boostFlash = PrefabAPI.InstantiateClone(boostFlashEffectBase, name);

            ResizeBoostFlash(ref boostFlash, size);

            boostFlash.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystemRenderer>().sharedMaterial = material;

            boostFlash.transform.GetChild(1).GetComponent<Light>().color = lightColor;

            if (!distortion) GameObject.Destroy(boostFlash.transform.GetChild(0).GetChild(1).gameObject);

            AddNewEffectDef(boostFlash);

            return boostFlash;
        }

        public static void ResizeBoostAura(ref GameObject boostAura, float size)
        {
            Transform transform = boostAura.transform.GetChild(0);
            transform.localScale = new Vector3(size * 0.6f, size * 0.6f, size * 0.6f);
            transform.localPosition = new Vector3(0, 0.1f, -0.7f + (0.6f * (1 - size)));
        }
        public static void ResizeBoostFlash(ref GameObject boostFlash, float size)
        {
            Transform transform = boostFlash.transform.GetChild(0);
            transform.localScale = new Vector3(size, size, size);
            transform.localPosition = new Vector3(0, 0.1f, -1.6f + (1f - size));
        }
        #endregion
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
        public static Material superAuraMaterialBase;
        public static Material superAuraMaterial;
        public static Material superFormGlowingMaterial;
        public static Material superFormOverlay;
        public static Material rainbowGlowMaterial;
        public static Material rainbowGlowSubtleMaterial;
        public static GameObject superFormTransformationEffect;
        public static GameObject transformationEmeraldSwirl;
        public static GameObject superFormAura;
        public static GameObject superFormWarning;
        public static LoopSoundDef superLoopSoundDef;
        public static GameObject superFormPPVolume;
        public static GameObject chaosEmeraldDropletPrefab;
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

            superFormAura = Assets.LoadAsyncedEffect("SuperFormAura");
            var superFormAuraVfx = superFormAura.AddComponent<VFXAttributes>();
            superFormAuraVfx.DoNotPool = false;
            superFormAuraVfx.optionalLights = [superFormAura.transform.Find("Point Light").GetComponent<Light>()];

            ReplaceRainbow(superFormAura.transform.Find("Rainbow"), true);
            superAuraMaterialBase = new Material(Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Parry.matParryWave_mat).WaitForCompletion());
            superAuraMaterialBase.SetFloat("_AlphaBoost", 1.2f);
            superAuraMaterialBase.SetFloat("_DepthOffset", -3f);
            superAuraMaterialBase.SetTextureScale("_Cloud1Tex", new Vector2(0.7f, 0.7f));
            superAuraMaterialBase.SetTexture("_Cloud2Tex", AssetAsyncReferenceManager<Texture>.LoadAsset(new AssetReferenceT<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.texCloudWaterFoam2_psd)).WaitForCompletion());
            superAuraMaterialBase.SetTextureScale("_Cloud2Tex", new Vector2(0.7f, 0.7f));
            superAuraMaterialBase.SetTexture("_RemapTex", AssetAsyncReferenceManager<Texture>.LoadAsset(new AssetReferenceT<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampTritoneSmoothed_png)).WaitForCompletion());
            superAuraMaterialBase.SetVector("_CutoffScroll", new Vector4(0, -3, 1, -5));
            superAuraMaterialBase.EnableKeyword("VERTEXCOLOR");

            superAuraMaterial = CreateSuperAuraMaterial("SuperFormAura", AssetAsyncReferenceManager<Texture>.LoadAsset(new AssetReferenceT<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_Common_ColorRamps.texRampConstructLaser_png)).WaitForCompletion());
            superFormAura.GetComponent<ParticleSystemRenderer>().sharedMaterial = superAuraMaterial;
            superFormAura.transform.Find("Sparks").GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetAsyncReferenceManager<Material>.LoadAsset(new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.matTracerLessBright_mat)).WaitForCompletion();

            superFormWarning = Assets.LoadEffect("SonicSuperWarning", "", true, 0f);
            superFormWarning.AddComponent<Miscellaneous.DestroyOnExitForm>();

            //FormDefs haven't been initialized yet so I gotta wait before I can set the Form for the DestroyOnExitForm. That is done in SuperFormDef initialize
            EffectComponent warningEffect = superFormWarning.GetComponent<EffectComponent>();
            warningEffect.parentToReferencedTransform = true;
            warningEffect.applyScale = true;

            AsyncOperationHandle<Material> asyncOutlineMaterial = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarGolem.matLunarGolemShield_mat);
            asyncOutlineMaterial.Completed += delegate (AsyncOperationHandle<Material> x)
            {
                superFormOverlay = new Material(x.Result);
                superFormOverlay.SetColor("_TintColor", new Color(1, 0.8f, 0.4f, 1));
                superFormOverlay.SetColor("_EmissionColor", new Color(1, 0.8f, 0.4f, 1));
                superFormOverlay.SetFloat("_OffsetAmount", 0.01f);
            };
            AsyncOperationHandle<Material> asyncGlowingMaterial = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Huntress.matHuntressFlashBright_mat);
            asyncGlowingMaterial.Completed += delegate (AsyncOperationHandle<Material> x)
            {
                superFormGlowingMaterial = new Material(x.Result);
                superFormGlowingMaterial.SetColor("_TintColor", new Color(1, 0.8f, 0.4f, 1));
            };

            superLoopSoundDef = ScriptableObject.CreateInstance<LoopSoundDef>();
            superLoopSoundDef.startSoundName = "Play_hedgehogutils_super_loop";
            superLoopSoundDef.stopSoundName = "Stop_hedgehogutils_super_loop";

            superFormPPVolume = mainAssetBundle.LoadAsset<GameObject>("SonicSuperPostProcess");
            PostProcessVolume postProcess = superFormPPVolume.GetComponent<PostProcessVolume>();
            postProcess.sharedProfile = Addressables.LoadAssetAsync<PostProcessProfile>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_title_PostProcessing.ppLocalGrandparent_asset).WaitForCompletion();

            chaosEmeraldDropletPrefab = mainAssetBundle.LoadAsset<GameObject>("ChaosEmeraldOrb");
            var chaosEmeraldDropletStartSound = chaosEmeraldDropletPrefab.AddComponent<PlaySoundOnEvent>();
            chaosEmeraldDropletStartSound.triggeringEvent = PlaySoundOnEvent.PlaySoundEvent.Start;
            chaosEmeraldDropletStartSound.soundEvent = "Play_UI_item_spawn_tier2";
            var chaosEmeraldDropletLandSound = chaosEmeraldDropletPrefab.AddComponent<PlaySoundOnEvent>();
            chaosEmeraldDropletLandSound.triggeringEvent = PlaySoundOnEvent.PlaySoundEvent.Destroy;
            chaosEmeraldDropletLandSound.soundEvent = "Play_hedgehogutils_emerald_spawn";
            var chaosEmeraldDropletLandSound2 = chaosEmeraldDropletPrefab.AddComponent<PlaySoundOnEvent>();
            chaosEmeraldDropletLandSound2.triggeringEvent = PlaySoundOnEvent.PlaySoundEvent.Destroy;
            chaosEmeraldDropletLandSound2.soundEvent = "Play_UI_item_land_tier2";
            Transform chaosEmeraldDropletVFXTransform = chaosEmeraldDropletPrefab.transform.GetChild(0);
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.BossOrb_prefab).Completed += (x) =>
            {
                chaosEmeraldDropletVFXTransform.GetComponent<TrailRenderer>().sharedMaterial = x.Result.transform.GetChild(0).GetComponent<TrailRenderer>().sharedMaterial;
                Material chaosEmeraldRingMat = new Material(x.Result.transform.GetChild(0).GetChild(1).GetComponent<ParticleSystemRenderer>().sharedMaterial);
                chaosEmeraldRingMat.SetTexture("_RemapTex", mainAssetBundle.LoadAsset<Texture>("texRampRainbow"));
                chaosEmeraldRingMat.SetFloat("_SrcBlend", 1f);
                chaosEmeraldRingMat.SetFloat("_DstBlend", 1f);
                chaosEmeraldRingMat.SetFloat("_AlphaBoost", 3f);
                chaosEmeraldRingMat.SetFloat("_AlphaBias", 0.4f);
                chaosEmeraldDropletVFXTransform.GetChild(1).GetComponent<ParticleSystemRenderer>().sharedMaterial = chaosEmeraldRingMat;
            };
            Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Child.matChildStarGlow_mat).Completed += (x) =>
            {
                chaosEmeraldDropletVFXTransform.GetChild(0).GetComponent<ParticleSystemRenderer>().sharedMaterial = x.Result;
            };
        }
        private static void UpdateAuraColors(GameObject aura, Color color)
        {
            aura.transform.Find("Point Light").GetComponent<Light>().color = color;
            var glowMainModule = aura.transform.Find("DistanceGlow").GetComponent<ParticleSystem>().main;
            glowMainModule.startColor = color;
            var sparkMainModule = aura.transform.Find("Sparks").GetComponent<ParticleSystem>().main;
            sparkMainModule.startColor = color;
        }
        public static Material CreateSuperAuraMaterial(string name, Texture remapTexture)
        {
            Material material = new Material(superAuraMaterialBase);
            material.name = $"mat{name}";
            if (remapTexture) material.SetTexture("_RemapTex", remapTexture);
            return material;
        }
        public static GameObject CreateSuperAura(string name, Color color)
        {
            return CreateSuperAura(name, color, color, null);
        }
        public static GameObject CreateSuperAura(string name, Color color, Texture remapTexture)
        {
            return CreateSuperAura(name, color, Color.white, CreateSuperAuraMaterial(name, remapTexture));
        }
        public static GameObject CreateSuperAura(string name, Color color, Color auraTintColor, Material material)
        {
            GameObject newAura = PrefabAPI.InstantiateClone(superFormAura, name);
            if (auraTintColor != Color.white)
            {
                var mainModule = newAura.GetComponent<ParticleSystem>().main;
                mainModule.startColor = auraTintColor;
            }
            if (material) newAura.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            UpdateAuraColors(newAura, color);
            return newAura;
        }
        public static void ReplaceRainbow(Transform particle, bool subtle = false)
        {
            ParticleSystemRenderer rainbowAura = particle.GetComponent<ParticleSystemRenderer>();
            rainbowAura.sharedMaterial = subtle ? rainbowGlowSubtleMaterial : rainbowGlowMaterial;
            rainbowAura.mesh = Addressables.LoadAssetAsync<Mesh>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.mdlVFXDonut1_fbx_donut1Mesh_).WaitForCompletion();
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
        public static GameObject lockOnIndicator;
        public static Material lockOnUIRemap;
        public static void Miscellaneous()
        {
            rainbowGlowMaterial = new Material(Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Elites_EliteBead.matEliteBeadSpikeGrowthRing_mat).WaitForCompletion());
            rainbowGlowMaterial.SetTexture("_RemapTex", mainAssetBundle.LoadAsset<Texture>("texRampRainbow"));
            rainbowGlowMaterial.SetFloat("_Boost", 10f);
            rainbowGlowSubtleMaterial = new Material(rainbowGlowMaterial);
            rainbowGlowSubtleMaterial.SetFloat("_Boost", 1.5f);

            AsyncOperationHandle<Material> asyncRingMaterial = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Elites_EliteAurelionite.matEliteAurelioniteAffixOverlay_mat);
            asyncRingMaterial.Completed += delegate (AsyncOperationHandle<Material> x)
            {
                ringMaterial = new Material(x.Result);
                ringMaterial.SetFloat("_NormalStrength", 0);
                ringMaterial.SetColor("_Color", new Color(0.9f, 0.8f, 0.1f, 1));
            };

            chaosSnapMaterial = new Material(Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Huntress.matHuntressSwipe_mat).WaitForCompletion());
            chaosSnapMaterial.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampLightning2_png).WaitForCompletion());
            chaosSnapInEffect = CreateChaosSnapEffect("ChaosSnapInVFX", true);
            chaosSnapOutEffect = CreateChaosSnapEffect("ChaosSnapOutVFX", false);

            chaosSnapSoundEventDef = CreateNetworkSoundEventDef("Play_hedgehogutils_teleport");
            chaosSnapLargeSoundEventDef = CreateNetworkSoundEventDef("Play_hedgehogutils_teleport_large");

            lockOnIndicator = mainAssetBundle.LoadAsset<GameObject>("LockOnIndicator");
            var lockOnIndicatorComponent = lockOnIndicator.AddComponent<LockOnIndicator>();
            AsyncOperationHandle<Sprite> asyncTexCursorRevolver = Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_UI.texCursorRevolver_png_texCursorRevolver_);
            asyncTexCursorRevolver.Completed += delegate (AsyncOperationHandle<Sprite> x)
            {
                var mainSprite = lockOnIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>();
                mainSprite.sprite = x.Result;
                lockOnIndicatorComponent.main = mainSprite;
            };
            AsyncOperationHandle<Sprite> asyncTexCrosshair2 = Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_UI.texCrosshair2_png_texCrosshair2_);
            asyncTexCrosshair2.Completed += delegate (AsyncOperationHandle<Sprite> x)
            {
                var darkenSprite = lockOnIndicator.transform.GetChild(1).GetComponent<SpriteRenderer>();
                darkenSprite.sprite = x.Result;
                lockOnIndicatorComponent.darken = darkenSprite;
            };
            var lockOnNibHolder = lockOnIndicator.transform.GetChild(3);
            AsyncOperationHandle<Sprite> asyncTexCrosshairNibBar = Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_UI.texCrosshairNibBar_png_texCrosshairNibBar_);
            asyncTexCrosshairNibBar.Completed += delegate (AsyncOperationHandle<Sprite> x)
            {
                lockOnIndicatorComponent.nibTop = LockOnNibs(lockOnNibHolder.GetChild(0), x.Result);
                lockOnIndicatorComponent.nibLeft = LockOnNibs(lockOnNibHolder.GetChild(1), x.Result);
                lockOnIndicatorComponent.nibBottom = LockOnNibs(lockOnNibHolder.GetChild(2), x.Result);
                lockOnIndicatorComponent.nibRight = LockOnNibs(lockOnNibHolder.GetChild(3), x.Result);
                lockOnIndicatorComponent.scaleCurves = lockOnIndicator.GetComponentsInChildren<ObjectScaleCurve>();
            };

            var lockOnStartScaleCurve = lockOnIndicator.transform.GetChild(2).gameObject.AddComponent<ObjectScaleCurve>();
            lockOnStartScaleCurve.timeMax = 0.2f;
            lockOnStartScaleCurve.useOverallCurveOnly = true;
            lockOnStartScaleCurve.overallCurve = AnimationCurve.Linear(0f, 3f, 1f, 0f);
            lockOnIndicatorComponent.start = lockOnStartScaleCurve.GetComponent<SpriteRenderer>();
        }
        private static SpriteRenderer LockOnNibs(Transform nibHolder, Sprite sprite)
        {
            var scaleCurve = nibHolder.gameObject.AddComponent<ObjectScaleCurve>();
            scaleCurve.timeMax = 0.2f;
            scaleCurve.useOverallCurveOnly = false;
            scaleCurve.curveX = AnimationCurve.Constant(0f, 1f, 1f);
            scaleCurve.curveY = AnimationCurve.EaseInOut(0, 6f, 1f, 1f);
            scaleCurve.curveZ = AnimationCurve.Constant(0f, 1f, 1f);
            scaleCurve.overallCurve = AnimationCurve.Constant(0f, 1f, 1f);
            var spriteRenderer = nibHolder.GetChild(0).GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            return spriteRenderer;
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
            distortionRenderer.sharedMaterial = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.matInverseDistortion_mat).WaitForCompletion();
            flashRenderer.sharedMaterial = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.matGenericFlash_mat).WaitForCompletion();

            EffectComponent effectComponent = effect.AddComponent<EffectComponent>();
            effectComponent.applyScale = true;
            effectComponent.applyScaleFirst = true;
            effectComponent.positionAtReferencedTransform = false;
            ChaosSnapVFX chaosSnapVFXComponent = effect.AddComponent<ChaosSnapVFX>();
            chaosSnapVFXComponent.reverse = teleportIn;
            chaosSnapVFXComponent.temporaryOverlayMaterial = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Huntress.matHuntressFlashBright_mat).WaitForCompletion();

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
        [Obsolete]
        public static GameObject CreateNewBoostFlash(string name, float size, float alpha, Color color1, Color color2, Color color3, Color lightColor)
        {
            GameObject newFlash = PrefabAPI.InstantiateClone(powerBoostFlashEffect, name);
            AddNewEffectDef(newFlash);

            ParticleSystem.MainModule main = newFlash.transform.Find("BlueCone").GetComponent<ParticleSystem>().main;
            main.startSize = new ParticleSystem.MinMaxCurve(main.startSize.constant * size);

            ParticleSystem.MainModule main2 = newFlash.transform.Find("BlueCone/BlueCone2").GetComponent<ParticleSystem>().main;
            main2.startSize = new ParticleSystem.MinMaxCurve(main2.startSize.constant * size);

            ParticleSystemRenderer renderer = newFlash.transform.Find("BlueCone").GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = CreateNewBoostMaterial(alpha, color1, color2, color3);

            ParticleSystemRenderer renderer2 = newFlash.transform.Find("BlueCone/BlueCone2").GetComponent<ParticleSystemRenderer>();
            renderer2.sharedMaterial = CreateNewBoostMaterial(alpha, color1, color2, color3);

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
        [Obsolete]
        public static GameObject CreateNewBoostAura(string name, float size, float alpha, Color color1, Color color2, Color color3, Color lightColor)
        {
            GameObject newAura = PrefabAPI.InstantiateClone(powerBoostAuraEffect, name);
            newAura.transform.Find("Aura").localScale *= size;
            newAura.transform.Find("Aura").GetComponent<MeshRenderer>().sharedMaterial = CreateNewBoostMaterial(alpha, color1, color2, color3);
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
        [Obsolete]
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
            return LoadEffect(resourceName, "", false, 5f);
        }

        private static GameObject LoadEffect(string resourceName, string soundName)
        {
            return LoadEffect(resourceName, soundName, false, 5f);
        }

        private static GameObject LoadEffect(string resourceName, bool parentToTransform)
        {
            return LoadEffect(resourceName, "", parentToTransform, 5f);
        }
        private static GameObject LoadEffect(string resourceName, bool parentToTransform, float destroyOnTimer)
        {
            return LoadEffect(resourceName, "", parentToTransform, destroyOnTimer);
        }

        private static GameObject LoadAsyncedEffect(string resourceName)
        {
            GameObject newEffect = mainAssetBundle.LoadAsset<GameObject>(resourceName);

            newEffect.AddComponent<NetworkIdentity>();

            return newEffect;
        }

        private static GameObject LoadEffect(string resourceName, string soundName, bool parentToTransform, float destroyOnTimer, bool addNewEffectDef = true)
        {
            GameObject newEffect = mainAssetBundle.LoadAsset<GameObject>(resourceName);

            if (!newEffect)
            {
                Log.Error("Failed to load effect: " + resourceName + " because it does not exist in the AssetBundle");
                return null;
            }
            if (destroyOnTimer > 0)
            {
                newEffect.AddComponent<DestroyOnTimer>().duration = destroyOnTimer;
            }
            newEffect.AddComponent<NetworkIdentity>();
            var vfx = newEffect.AddComponent<VFXAttributes>();
            vfx.vfxPriority = VFXAttributes.VFXPriority.Always;
            var effect = newEffect.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = parentToTransform;
            effect.positionAtReferencedTransform = true;
            effect.soundName = soundName;

            if (addNewEffectDef) AddNewEffectDef(newEffect, soundName);

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