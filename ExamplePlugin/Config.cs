using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using UnityEngine;
using HedgehogUtils.Forms;

namespace HedgehogUtils
{
    public static class Config
    {
        public static ConfigEntry<bool> EnableLogs()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<bool>("Misc.", "Enable Logs", false, "This controls whether this mod will put any information in the logs. This information can help the mod creator fix issues should any come up, but it does impact performance a bit. Default is false.");
        }
        #region Forms
        public static ConfigEntry<bool> AnnounceSuperTransformation()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<bool>("Forms", "Announce Super Transformation", false, "If true, a message will be sent in chat when someone transforms. The message will include the name of the player who transformed. The message won't be sent for anyone who transforms in the 10 second window after someone else transforms. Host's config takes priority. Default is false.");
        }
        public static ConfigEntry<bool> EmeraldsWithoutSonic()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<bool>("Chaos Emeralds", "Spawn Emeralds Without Sonic", false, "Determines whether the Chaos Emeralds are allowed to spawn even if no one is playing as a Sonic character. Host's config takes priority. Default is false.");
        }

        public static ConfigEntry<int> EmeraldsPerStage()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<int>("Chaos Emeralds", "Emeralds Per Stage", 3, "The maximum number of Chaos Emeralds that can spawn in one stage. Host's config takes priority. Default is 3.");
        }

        public static ConfigEntry<int> EmeraldsPerSimulacrumStage()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<int>("Chaos Emeralds", "Emeralds Per Simulacrum Stage", 5, "The maximum number of Chaos Emeralds that can spawn in one stage in Simulacrum. Host's config takes priority. Default is 5.");
        }

        public static ConfigEntry<int> EmeraldCost()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<int>("Chaos Emeralds", "Cost", 50, "How much it costs to buy a Chaos Emerald. Host's config takes priority. Default is 50.\nFor reference:\nChest: 25\nLarge Chest: 50\nAltar of Gold: 200\nLegendary Chest: 400");
        }

        public static ConfigEntry<bool> ConsumeEmeraldsOnUse()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<bool>("Chaos Emeralds", "Consume Emeralds On Use", true, "Determines whether the Chaos Emeralds will be consumed when transforming into Super Sonic. If not, the emeralds will stay but won't be able to be used until the next stage. Host's config takes priority. Default is true.");
        }

        public static ConfigEntry<FormItemSharing> NeededItemSharing() // How do you get configs into their own separate category in RiskOfOptions, like separate from the Sonic mod. I've seen Aerolt and StageAesthetics do it but idk how
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<FormItemSharing>("Chaos Emeralds", "Item Sharing", FormItemSharing.All, "Handles how Chaos Emeralds are shared between teammates and determines who has permission to transform based on the items they have. The restrictions only apply to the first person to transform and don't apply to anyone who transforms in the 10 second window afterwards. Host's config takes priority. Default is All.\n\nAssuming all items have been collected across the team...\nAll: Anyone, whether they HAVE ANY ITEMS OR NOT, can transform.\nContributor: Players that have AT LEAST ONE of the needed items can transform\nMajorityRule: The player(s) with the MAJORITY number of the needed items can transform\nNone: Only the player with ALL items can transform\n\nWARNING: A mod that lets you drop items to your teammates is reccommended if you are changing this setting. Otherwise, transforming may be impossible if items are split between players in certain ways.");
        }

        public static ConfigEntry<bool> LaunchBodyBlacklist()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<bool>("Launch", "Use Body Blacklist", true, "Determines whether certain characters, such as final bosses and worms, are impossible to be launched. Host's config takes priority. Default is true.");
        }

        public static ConfigEntry<float> BoostMeterLocationX()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<float>("Boost", "X Location", 90f, "X Coordinate of the boost meter's location relative to the crosshair. Default is 90.");
        }

        public static ConfigEntry<float> BoostMeterLocationY()
        {
            return HedgehogUtilsPlugin.instance.Config.Bind<float>("Boost", "Y Location", -50f, "Y Coordinate of the boost meter's location relative to the crosshair. Default is -50.");
        }
        #endregion

        #region Risk Of Options
        public static void RiskOfOptionsSetup()
        {
            Sprite icon = (Assets.mainAssetBundle.LoadAsset<Sprite>("texSuperBuffIcon"));
            ModSettingsManager.SetModIcon(icon);

            ModSettingsManager.AddOption(new IntSliderOption(Config.EmeraldsPerStage(), new RiskOfOptions.OptionConfigs.IntSliderConfig() { min = 1, max = 7 }));
            ModSettingsManager.AddOption(new IntSliderOption(Config.EmeraldsPerSimulacrumStage(), new RiskOfOptions.OptionConfigs.IntSliderConfig() { min = 1, max = 7 }));

            ModSettingsManager.AddOption(new IntSliderOption(Config.EmeraldCost(), new RiskOfOptions.OptionConfigs.IntSliderConfig() { min = 0, max = 400 }));

            Config.EmeraldCost().SettingChanged += Forms.SuperForm.ChaosEmeraldInteractable.UpdateInteractableCost;
            ModSettingsManager.AddOption(new CheckBoxOption(Config.ConsumeEmeraldsOnUse()));

            Config.ConsumeEmeraldsOnUse().SettingChanged += Forms.SuperForm.SuperFormDef.UpdateConsumeEmeraldsConfig;

            ModSettingsManager.AddOption(new ChoiceOption(Config.NeededItemSharing()));

            ModSettingsManager.AddOption(new CheckBoxOption(Config.AnnounceSuperTransformation()));

            ModSettingsManager.AddOption(new CheckBoxOption(Config.EnableLogs()));

            ModSettingsManager.AddOption(new CheckBoxOption(Config.LaunchBodyBlacklist()));

            float minLocation = -500;
            float maxLocation = 500;
            ModSettingsManager.AddOption(new SliderOption(BoostMeterLocationX(), new RiskOfOptions.OptionConfigs.SliderConfig() { min = minLocation, max = maxLocation, formatString = "{0:0}" }));
            ModSettingsManager.AddOption(new SliderOption(BoostMeterLocationY(), new RiskOfOptions.OptionConfigs.SliderConfig() { min = minLocation, max = maxLocation, formatString = "{0:0}" }));

        }
        #endregion
    }
}