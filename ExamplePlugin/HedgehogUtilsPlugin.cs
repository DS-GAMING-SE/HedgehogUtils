using BepInEx;
using R2API;
using RoR2;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using HedgehogUtils.Internal;
using R2API.Networking;
using HedgehogUtils.Forms;

namespace HedgehogUtils
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExampleFormsMod/ExampleFormsMod.dll to test out.

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    [BepInDependency(LookingGlass.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class HedgehogUtilsPlugin : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor, Prefix, and the PluginName !
        public const string PluginGUID = "com.ds_gaming.HedgehogUtils";
        public const string PluginAuthor = "ds_gaming";
        public const string PluginName = "HedgehogUtils";
        public const string PluginVersion = "1.1.3";

        public const string Prefix = "DS_GAMING_HEDGEHOG_UTILS_";

        public static HedgehogUtilsPlugin instance;

        public static bool lookingGlassLoaded = false;
        public static bool riskOfOptionsLoaded = false;

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            instance = this;
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            lookingGlassLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LookingGlass.PluginInfo.PLUGIN_GUID);
            Log.Message("Looking Glass exists? " + lookingGlassLoaded);

            riskOfOptionsLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
            Log.Message("Risk of Options exists? " + riskOfOptionsLoaded);

            Assets.Initialize();

            Buffs.RegisterBuffs();

            Boost.OnHooks.Initialize();

            #region Forms

            Forms.SuperForm.Items.RegisterItems();

            Forms.SuperForm.SuperFormDef.Initialize();

            Forms.SuperForm.ChaosEmeraldInteractable.Initialize();

            Forms.OnHooks.Initialize();

            Forms.SuperForm.Stats.Initialize();

            Forms.SuperForm.Artifact.Initialize();
            #endregion Forms

            #region Launch
            Launch.DamageTypes.Initialize();

            Launch.LaunchManager.Initialize();

            Launch.OnHooks.Initialize();
            #endregion

            #region Misc
            Miscellaneous.MomentumStats.Initialize();
            #endregion

            Language.Initialize();

            States.RegisterStates();

            if (riskOfOptionsLoaded)
            {
                HedgehogUtils.Config.RiskOfOptionsSetup();
            }

            NetworkingAPI.RegisterMessageType<NetworkTransformation>();

            new Internal.ContentPacks().Initialize();
        }
    }
}
