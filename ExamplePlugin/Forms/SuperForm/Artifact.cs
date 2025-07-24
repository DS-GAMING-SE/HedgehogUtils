using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;


namespace HedgehogUtils.Forms.SuperForm
{
    public static class Artifact
    {
        public static ArtifactDef chaosEmeraldArtifactDef;

        public static void Initialize()
        {
            chaosEmeraldArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            chaosEmeraldArtifactDef.cachedName = HedgehogUtilsPlugin.Prefix + "ARTIFACT_CHAOS_EMERALD";
            chaosEmeraldArtifactDef.nameToken = HedgehogUtilsPlugin.Prefix + "ARTIFACT_CHAOS_EMERALD_NAME";
            chaosEmeraldArtifactDef.descriptionToken = HedgehogUtilsPlugin.Prefix + "ARTIFACT_CHAOS_EMERALD_DESCRIPTION";
            chaosEmeraldArtifactDef.smallIconDeselectedSprite = Assets.mainAssetBundle.LoadAsset<Sprite>("texArtifactChaosEmeraldDisabled");
            chaosEmeraldArtifactDef.smallIconSelectedSprite = Assets.mainAssetBundle.LoadAsset<Sprite>("texArtifactChaosEmeraldEnabled");
            R2API.ContentAddition.AddArtifactDef(chaosEmeraldArtifactDef);
        }
    }
}
