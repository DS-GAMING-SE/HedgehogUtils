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
            Dictionary<string, RenderReplacements> superRenderDictionary = new Dictionary<string, RenderReplacements>();
            superFormDef = Forms.CreateFormDef(HedgehogUtilsPlugin.Prefix+"SUPER_FORM", Buffs.superFormBuff, StaticValues.superSonicDuration, true, true, Config.ConsumeEmeraldsOnUse().Value,
            1, true, true, true, new SerializableEntityStateType(typeof(EntityStates.SuperSonic)), new SerializableEntityStateType(typeof(EntityStates.SuperSonicTransformation)), superRenderDictionary,
                typeof(SuperSonicHandler), new AllowedBodyList { whitelist = false, bodyNames = Array.Empty<string>() }, KeyCode.V);

            FormCatalog.AddFormDefs(new FormDef[]
            {
                superFormDef
            });
        }

        [SystemInitializer(typeof(ItemCatalog))]
        public static void InitializeFormItemRequirements()
        {
            Log.Message("NeededItems initialized");
            superFormDef.neededItems = new NeededItem[] { Items.yellowEmerald.itemIndex, Items.redEmerald.itemIndex, Items.blueEmerald.itemIndex, Items.cyanEmerald.itemIndex, Items.grayEmerald.itemIndex, Items.greenEmerald.itemIndex, Items.purpleEmerald.itemIndex };
        }

        public static void UpdateConsumeEmeraldsConfig(object sender, EventArgs args)
        {
            superFormDef.consumeItems = Config.ConsumeEmeraldsOnUse().Value;
        }
    }
}
