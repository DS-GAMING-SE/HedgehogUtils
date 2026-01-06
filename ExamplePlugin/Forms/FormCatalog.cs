using BepInEx.Configuration;
using RiskOfOptions.Options;
using RiskOfOptions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using HedgehogUtils.Internal;

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
            InitializeFormConfigs();
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

            formsCatalog = formsCatalog.OrderBy(form => form.name).ToArray();

            string allForms = string.Concat(formsCatalog.Select(x => x.ToString() + "\n"));
            Log.Message("FormDef(s) added to formCatalog. formCatalog now contains:\n"+allForms);
        }

        // I will make all modded forms require risk of options to sort out controls. Otherwise I'd have to put effort into some kind of form picker ui wheel and that would be mega complicated
        public static void InitializeFormConfigs()
        {
            List<KeyCode> usedKeys = new List<KeyCode>();
            foreach (FormDef form in formsCatalog)
            {
                if (form.defaultKeyBind != KeyCode.None)
                {
                    form.keybind = HedgehogUtilsPlugin.instance.Config.Bind<KeyboardShortcut>("Controls", form.ToString() + " Transform Key", new KeyboardShortcut(form.defaultKeyBind), "The key you press to transform into the " + form.ToString() + " form. This config is automatically generated.");
                    if (usedKeys.Contains(form.defaultKeyBind))
                    {
                        Log.Warning("Form " + form.ToString() + " shares the same default keybind of " + form.defaultKeyBind.ToString() + " with some other form(s).", Config.Logs.All);
                    }
                    else
                    {
                        usedKeys.Add(form.defaultKeyBind);
                    }
                    continue;
                }
            }
            if (HedgehogUtilsPlugin.riskOfOptionsLoaded)
            {
                InitializeFormConfigRiskOfOptions();
            }
        }

        public static void InitializeFormConfigRiskOfOptions()
        {
            foreach (FormDef form in formsCatalog)
            {
                ModSettingsManager.AddOption(new KeyBindOption(form.keybind));
            }
        }
    }
}
