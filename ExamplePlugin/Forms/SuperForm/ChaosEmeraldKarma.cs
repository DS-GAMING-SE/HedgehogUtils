using RoR2;
using RoR2.Karma;
using System;
using System.Collections.Generic;
using System.Text;

namespace HedgehogUtils.Forms.SuperForm
{
    [RegisterKarmaTrigger("ChaosEmeraldKarma", 1)]
    public class ChaosEmeraldKarma : BaseKarmaTrigger
    {
        private int frameCount;
        public override void Subscribe()
        {
            Inventory.onInventoryChangedGlobal += OnInventoryChangedGlobal;
        }

        public override void Unsubscribe()
        {
            Inventory.onInventoryChangedGlobal -= OnInventoryChangedGlobal;
        }
        // Formhandlers take a frame to update HasItems so I gotta do this weird shit
        private void OnInventoryChangedGlobal(Inventory inventory)
        {
            if (!SuperFormDef.superFormDef.enabled || !SuperFormDef.superFormDef.GetFormHandler() || !Config.KarmaFromChaosEmeralds().Value) return;

            frameCount = 0;
            RoR2Application.onFixedUpdate -= TryAddKarma;
            RoR2Application.onFixedUpdate += TryAddKarma;
        }
        private void TryAddKarma()
        {
            if (frameCount < 4)
            {
                frameCount++;
                return;
            }
            RoR2Application.onFixedUpdate -= TryAddKarma;
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                if (player.master.bodyInstanceObject && player.master.bodyInstanceObject.TryGetComponent<FormComponent>(out var form) && CanTransform(form))
                {
                    MarkAsCompleted();
                    return;
                }
            }
        }
        public bool CanTransform(FormComponent formComponent)
        {
            return SuperFormDef.superFormDef.GetFormHandler().CanTransform(formComponent);
        }
    }
}
