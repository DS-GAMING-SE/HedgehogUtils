using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace HedgehogUtils
{
    public static class Buffs
    {
        public static BuffDef superFormBuff;

        public static BuffDef launchedBuff;

        public static BuffDef chaosSnapRandomIdentifierBuff;

        internal static void RegisterBuffs()
        {
            superFormBuff = AddNewBuff("bdHedgehogSuperForm",
                Assets.mainAssetBundle.LoadAsset<Sprite>("texSuperBuffIcon"),
                Color.white,
                false,
                false,
                false);
            launchedBuff = AddNewBuff("bdHedgehogLaunch",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/Weak").iconSprite,
                new Color(1f, 1f, 1f),
                false,
                false,
                true);
            chaosSnapRandomIdentifierBuff = AddNewBuff("bdHedgehogChaosSnap",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/Weak").iconSprite,
                new Color(1f, 1f, 1f),
                false,
                false,
                true);
        }

        // simple helper method
        internal static BuffDef AddNewBuff(string buffName, Sprite buffIcon, Color buffColor, bool canStack, bool isDebuff, bool hidden)
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.name = buffName;
            buffDef.buffColor = buffColor;
            buffDef.canStack = canStack;
            buffDef.isDebuff = isDebuff;
            buffDef.eliteDef = null;
            buffDef.iconSprite = buffIcon;
            buffDef.isHidden = hidden;

            Internal.Content.AddBuffDef(buffDef);

            return buffDef;
        }
    }
}