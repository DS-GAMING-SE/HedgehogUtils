using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using HedgehogUtils.Internal;
using HedgehogUtils.Forms;

namespace HedgehogUtils.Forms.SuperForm
{
    public static class Items
    {
        // Emerald item tier
        internal static ItemTierDef emeraldTier;

        // Definition of all the different chaos emeralds.
        internal static ItemDef yellowEmerald;
        internal static ItemDef redEmerald;
        internal static ItemDef grayEmerald;
        internal static ItemDef blueEmerald;
        internal static ItemDef cyanEmerald;
        internal static ItemDef greenEmerald;
        internal static ItemDef purpleEmerald;

        internal static void RegisterItems()
        {
            emeraldTier = ScriptableObject.CreateInstance<ItemTierDef>();
            emeraldTier.tier = ItemTier.AssignedAtRuntime;
            emeraldTier.isDroppable = true;
            emeraldTier.canRestack = false;
            emeraldTier.pickupRules = ItemTierDef.PickupRules.Default;
            emeraldTier.name = HedgehogUtilsPlugin.Prefix + "_CHAOS_EMERALD_TIER";
            emeraldTier.highlightPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightTier1Item.prefab").WaitForCompletion();
            emeraldTier.dropletDisplayPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Tier1Orb.prefab").WaitForCompletion();
            emeraldTier.canScrap = false;
            emeraldTier.colorIndex = ColorCatalog.ColorIndex.Tier1Item;
            emeraldTier.darkColorIndex = ColorCatalog.ColorIndex.Tier1ItemDark;
            emeraldTier.bgIconTexture = Assets.mainAssetBundle.LoadAsset<Texture>("texBGEmerald");

            Content.AddItemTierDef(emeraldTier);

            // The first string input on this method, the name of the itemDef, is an internal name and CANNOT have spaces or other special characters
            // THIS was the reason the mastery skin wasn't working. THIS was the reason RunReports were breaking

            /* Addressables.
             * You'd need to build your assetbundle using addressables
             * instead of AssetBundleBrowser */

            //"88448356f06897a4e930138476d4dd77"
            yellowEmerald = AddNewItem("ChaosEmeraldYellow", "YELLOW_EMERALD", true, emeraldTier,
                Assets.mainAssetBundle.LoadAsset<Sprite>("texYellowEmeraldIcon"),
                CreateEmeraldPrefab("YellowEmerald.prefab"));

            //"dfe3adc87c7521949857c5602f0934c9"
            redEmerald = AddNewItem("ChaosEmeraldRed", "RED_EMERALD", true, emeraldTier,
                Assets.mainAssetBundle.LoadAsset<Sprite>("texRedEmeraldIcon"),
                CreateEmeraldPrefab("RedEmerald.prefab"));

            //"40624dec01670ce4a8881303114ceb5f"
            grayEmerald = AddNewItem("ChaosEmeraldGray", "GRAY_EMERALD", true, emeraldTier,
                Assets.mainAssetBundle.LoadAsset<Sprite>("texGrayEmeraldIcon"),
                CreateEmeraldPrefab("GrayEmerald.prefab"));

            //"ecc7d20392775a94db1554b7f2a349c8"
            blueEmerald = AddNewItem("ChaosEmeraldBlue", "BLUE_EMERALD", true, emeraldTier,
                Assets.mainAssetBundle.LoadAsset<Sprite>("texBlueEmeraldIcon"),
                CreateEmeraldPrefab("BlueEmerald.prefab"));

            //"86245b10a6b4e3345b2be747808a3a47"
            cyanEmerald = AddNewItem("ChaosEmeraldCyan", "CYAN_EMERALD", true, emeraldTier,
                Assets.mainAssetBundle.LoadAsset<Sprite>("texCyanEmeraldIcon"),
                CreateEmeraldPrefab("CyanEmerald.prefab"));

            //"fb712aea301e1ea4ab80b3fb92ed2281"
            greenEmerald = AddNewItem("ChaosEmeraldGreen", "GREEN_EMERALD", true, emeraldTier,
                Assets.mainAssetBundle.LoadAsset<Sprite>("texGreenEmeraldIcon"),
                CreateEmeraldPrefab("GreenEmerald.prefab"));

            //"29100898f13222d42b393e00cea7e671"
            purpleEmerald = AddNewItem("ChaosEmeraldPurple", "PURPLE_EMERALD", true, emeraldTier,
                Assets.mainAssetBundle.LoadAsset<Sprite>("texPurpleEmeraldIcon"),
                CreateEmeraldPrefab("PurpleEmerald.prefab"));
        }

        internal static GameObject CreateEmeraldPrefab(string assetName)
        {
            GameObject emerald = Assets.mainAssetBundle.LoadAsset<GameObject>(assetName);
            ModelPanelParameters panel = emerald.AddComponent<ModelPanelParameters>();
            panel.focusPointTransform = emerald.transform.Find("FocusPoint");

            panel.cameraPositionTransform = emerald.transform.Find("FocusPoint/CameraPosition");

            panel.minDistance = 0.6f;
            panel.maxDistance = 1.5f;
            return emerald;
        }

        // simple helper method
        internal static ItemDef AddNewItem(string itemName, string token, bool canRemove, ItemTierDef itemTier, Sprite icon, GameObject pickupModelPrefab)
        {
            string prefix = HedgehogUtilsPlugin.Prefix;
            ItemDef itemDef = ScriptableObject.CreateInstance<ItemDef>();
            itemDef.name = itemName;
            itemDef.tier = ItemTier.AssignedAtRuntime;
            itemDef.pickupModelPrefab = pickupModelPrefab;
            itemDef.pickupIconSprite = icon;
            itemDef.canRemove = canRemove;
            //itemDef.deprecatedTier = ItemTier.AssignedAtRuntime;
            itemDef._itemTierDef = itemTier;

            itemDef.nameToken = prefix + token; // stylised name
            itemDef.pickupToken = prefix + token + "_PICKUP";
            itemDef.descriptionToken = prefix + token + "_DESC";
            itemDef.loreToken = prefix + token + "_LORE";
            itemDef.tags = new[]
            {
                ItemTag.CannotCopy,
                ItemTag.CannotSteal,
                ItemTag.CannotDuplicate,
                ItemTag.WorldUnique,
                ItemTag.AIBlacklist
            };

            itemDef.CreatePickupDef();

            Content.AddItemDef(itemDef);

            return itemDef;
        }
    }
}