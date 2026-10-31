using BepInEx.Configuration;
using HedgehogUtils.Internal;
using R2API;
using Rebindables;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Forms
{
    // Thank you VarianceAPI for most of this stuff (conceptually)
    public static class FormCatalog
    {
        public static FormDef[] formsCatalog = Array.Empty<FormDef>();

        public static ResourceAvailability availability = default(ResourceAvailability);

        [SystemInitializer]
        private static void SystemInit()
        {
            Log.Message("FormCatalog initialized", Config.Logs.All);
            BakeFormIndices();
            RoR2.UI.MainMenu.MainMenuController.OnMainMenuInitialised += Forms.BakeRenderDictionaries;
            availability.MakeAvailable();
        }
        public static void AddFormDefs(FormDef[] forms)
        {
            string formNamesListed = string.Concat(forms.Select(x => x.ToString() + "\n"));
            if (availability.available)
            {
                Log.Warning("Forms "+formNamesListed+" are trying to be added after the catalog is initialized");
                return;
            }

            Log.Message("Adding new FormDef(s) to catalog.\n"+ formNamesListed, Config.Logs.All);
            int length = formsCatalog.Length;
            Array.Resize(ref formsCatalog, length + forms.Length);
            for (int i = 0; i < forms.Length; i++)
            {
                // Adding form to catalog
                formsCatalog[length + i] = forms[i];
            }

            formsCatalog = formsCatalog.OrderBy(form => form.cachedName).ToArray();

            string allForms = string.Concat(formsCatalog.Select(x => x.ToString() + "\n"));
            Log.Message("FormDef(s) added to formCatalog. formCatalog now contains:\n"+allForms);
        }
        private static void BakeFormIndices()
        {
            Forms.formHandlerObjects = new GameObject[formsCatalog.Length];
            Forms.formHandlers = new FormHandler[formsCatalog.Length];
            Forms.formHandlerPrefabs = new GameObject[formsCatalog.Length];
            for (int i = 0;i < formsCatalog.Length;i++)
            {
                Forms.formHandlerPrefabs[i] = formsCatalog[i].handlerPrefab;
            }
        }
    }
}
