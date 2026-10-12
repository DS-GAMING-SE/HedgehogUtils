using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HedgehogUtils.Forms;
using HedgehogUtils.Internal;
using System.Linq;

namespace HedgehogUtils.Forms.SuperForm
{
    public class SuperFormDef
    {
        public static FormDef superFormDef;

        public static void Initialize()
        {
            Dictionary<SkinDef, RenderReplacements> superRenderDictionary = new Dictionary<SkinDef, RenderReplacements>();
            superFormDef = Forms.CreateFormDef(HedgehogUtilsPlugin.Prefix+"SUPER_FORM", Buffs.superFormBuff, Config.SuperFormDuration().Value, true, true, Config.ConsumeEmeraldsOnUse().Value,
            1, Config.SuperFormInvincible().Value, true, true, new SerializableEntityStateType(typeof(EntityStates.SuperSonic)), new SerializableEntityStateType(typeof(EntityStates.SuperSonicTransformation)), superRenderDictionary,
                typeof(SuperSonicHandler), new AllowedBodyList { whitelist = false }, KeyCode.G);

            superFormDef.setIsEnabledFunc = (self) => 
            { 
                return RunArtifactManager.instance.IsArtifactEnabled(Artifact.chaosEmeraldArtifactDef) && FormDef.AnySelectedSurvivorCanUseForm(self); 
            };

            FormCatalog.AddFormDefs(new FormDef[]
            {
                superFormDef
            });

            Assets.superFormWarning.GetComponent<Miscellaneous.DestroyOnExitForm>().neededForm = superFormDef;
        }

        [SystemInitializer(typeof(ItemCatalog))]
        public static void InitializeFormItemRequirements()
        {
            Log.Message("NeededItems initialized", Config.Logs.All);
            superFormDef.neededItems = new NeededItem[] { Items.yellowEmerald.itemIndex, Items.redEmerald.itemIndex, Items.blueEmerald.itemIndex, Items.cyanEmerald.itemIndex, Items.grayEmerald.itemIndex, Items.greenEmerald.itemIndex, Items.purpleEmerald.itemIndex };
        }

        public static void UpdateConsumeEmeraldsConfig(object sender, EventArgs args)
        {
            superFormDef.consumeItems = Config.ConsumeEmeraldsOnUse().Value;
        }

        public static void UpdateSuperFormInvincibleConfig(object sender, EventArgs args)
        {
            superFormDef.invincible = Config.SuperFormInvincible().Value;
        }

        public static void UpdateSuperFormDurationConfig(object sender, EventArgs args)
        {
            superFormDef.duration = Config.SuperFormDuration().Value;
        }
    }
}
