﻿using EntityStates;
using HG;
using R2API;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BepInEx;
using RoR2;
using HarmonyLib;
using BepInEx.Configuration;
using RiskOfOptions.Options;
using RiskOfOptions;
using HedgehogUtils.Internal;
using HedgehogUtils.Forms.SuperForm;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace HedgehogUtils.Forms
{
    public static class Forms
    {
        public static Dictionary<FormDef, GameObject> formToHandlerPrefab = new Dictionary<FormDef, GameObject>();

        public static Dictionary<FormDef, GameObject> formToHandlerObject = new Dictionary<FormDef, GameObject>();

        public static Dictionary<FormDef, FormHandler> formToHandler = new Dictionary<FormDef, FormHandler>();

        // Look at the tooltips in the FormDef class for more information on what all of these parameters mean
        public static FormDef CreateFormDef(string name, BuffDef buff, float duration, bool requiresItems, bool shareItems, bool consumeItems, int maxTransforms, bool invincible, bool flight, bool superAnimations, SerializableEntityStateType formState, SerializableEntityStateType transformState,
            Dictionary<string, RenderReplacements> renderDictionary, Type handlerComponent, AllowedBodyList allowedBodyList, KeyCode defaultKeyBind)
        {
            FormDef form = ScriptableObject.CreateInstance<FormDef>();
            form.name = name;
            form.buff = buff;
            form.duration = duration;
            form.requiresItems = requiresItems;
            form.shareItems = shareItems;
            form.consumeItems = consumeItems;
            form.maxTransforms = maxTransforms;
            form.invincible = invincible;
            form.flight = flight;
            form.superAnimations = superAnimations;
            form.formState = formState;
            form.transformState = transformState;
            form.renderDictionary = renderDictionary;
            if (!typeof(FormHandler).IsAssignableFrom(handlerComponent))
            {
                Log.Warning("handlerComponent of type "+handlerComponent.Name+" is not assignable from FormHandler.");
            };
            form.handlerComponent = handlerComponent;
            form.allowedBodyList = allowedBodyList;
            form.defaultKeyBind = defaultKeyBind;

            // Creating handler prefab
            GameObject handlerPrefab = PrefabAPI.InstantiateClone(Assets.mainAssetBundle.LoadAsset<GameObject>("SuperSonicHandler"), form.name + " " + form.handlerComponent.Name);
            FormHandler handlerObjectComponent = (FormHandler)handlerPrefab.AddComponent(handlerComponent);
            handlerObjectComponent.form = form;
            if (form.requiresItems)
            {
                handlerPrefab.AddComponent(form.shareItems ? typeof(SyncedItemTracker) : typeof(UnsyncedItemTracker));
            }
            //PrefabAPI.RegisterNetworkPrefab(handlerPrefab);
            formToHandlerPrefab.Add(form, handlerPrefab);
            Log.Message("FormDef "+form.name+" created. Created new "+form.handlerComponent.Name+" prefab");

            return form;
        }

        public static FormDef GetFormDef(FormIndex formIndex)
        {
            if (!FormCatalog.availability.available) { Log.Warning("Can't get FormDef from FormIndex before catalog is initialized"); }
            return ArrayUtils.GetSafe(FormCatalog.formsCatalog, (int)formIndex);
        }

        public static bool GetIsInForm(GameObject gameObject, FormDef form)
        {
            if (gameObject)
            {
                FormComponent component = gameObject.GetComponent<FormComponent>();
                if (component)
                {
                    return component.activeForm == form;
                }
            }
            return false;
        }

        public static bool GetIsInForm(CharacterBody body, FormDef form)
        {
            if (body)
            {
                return GetIsInForm(body.gameObject, form);
            }
            return false;
        }

        // This takes a RenderReplacements struct, aka just a mesh and material that you'll change into when Super and using the given skin. Mesh or material can be null if you don't want them to change
        public static void AddSkinForForm(string skinToken, RenderReplacements render, ref FormDef form)
        {
            form.renderDictionary.Add(skinToken, render);
        }

        [SystemInitializer(typeof(BodyCatalog), typeof(FormCatalog))]
        public static void SuperSonicComponentsForEveryone()
        {
            Log.Message("SuperSonicComponentsForEveryone");
            foreach (GameObject body in BodyCatalog.allBodyPrefabs)
            {
                foreach (FormDef form in FormCatalog.formsCatalog)
                {
                    if (form.allowedBodyList.BodyIsAllowed(BodyCatalog.FindBodyIndex(body)) && body.GetComponent<CharacterBody>() && EntityStateMachine.FindByCustomName(body, "Body") && !body.GetComponent<FormComponent>())
                    {
                        body.AddComponent<FormComponent>();

                        EntityStateMachine superSonicState = body.AddComponent<EntityStateMachine>();
                        superSonicState.customName = "HedgehogUtilsForms";
                        superSonicState.mainStateType = new SerializableEntityStateType(typeof(BaseState));
                        superSonicState.initialStateType = new SerializableEntityStateType(typeof(BaseState));

                        NetworkStateMachine network = body.GetComponent<NetworkStateMachine>();
                        if (network)
                        {
                            Helpers.Append(ref network.stateMachines, new List<EntityStateMachine> { superSonicState });
                        }
                        break;
                    }
                }
            }
        }
    }

    public class FormDef : ScriptableObject
    {
        //Name should be the name token for the transformation. Eg. "DS_GAMING_SUPER_FORM". The actual name of the form should be handled using LanguageAPI. See Tokens.cs for an example of how that works

        [Tooltip("The buff given to you when you're transformed. This should be a buff unique to the form you're making.\nUse this buff for applying whatever stat increases you want.\nThis will be applied as a timed buff and will end the form when it goes away.")]
        public BuffDef buff;

        [Tooltip("The duration of the transformation in seconds. The actual duration of the form may be slightly longer to account for the transformation animation. If duration is <=0, a normal buff will be used instead of a timed buff")]
        public float duration;

        [Tooltip("Whether or not the form requires having certain items in order to transform. The specific items needed to transform are defined in neededItems once the ItemsCatalog is done.")]
        public bool requiresItems;

        [Tooltip("The item or items that are needed to transform. NeededItem struct stores a RoR2.ItemDef and a uint for how many of that item is needed. You can also just use ItemDefs here if you won't need multiple of the same item, there is an implicit cast")]
        public NeededItem[] neededItems;

        [Tooltip("If needed items will be shared amongst all players. Any player will be able to transform if any other player or combination of players have the needed items.")]
        public bool shareItems;

        [Tooltip("If needed items will be removed when transforming.")]
        public bool consumeItems;

        [Tooltip("The maximum number of times the same player is allowed to transform into this form every stage. If number is <=0, there will be no limit.")]
        public int maxTransforms;

        [Tooltip("If you will be immune to all damage while transformed.")]
        public bool invincible;

        [Tooltip("If you will be able to fly while transformed.")]
        public bool flight;

        [Tooltip("If you will use Super Sonic's animations or stay with default animations.\nSuper Sonic's animations include hovering in his idles, hovering when moving on the ground, replacing his \"falling\" animations with flying, and some animations made under the assumption that his quills are pointed up.")]
        public bool superAnimations;

        [Tooltip("The entity state used by the \"SonicForms\" entity state machine while transformed. Should be a subclass of SonicFormBase")]
        public SerializableEntityStateType formState;

        [Tooltip("The entity state used by the \"Body\" entity state machine for the transformation animation that will transition you into the form. Should be a subclass of TransformationBase. If the EntityState is null, the transformation will be instant")]
        public SerializableEntityStateType transformState;

        [Tooltip("Stores the material and mesh changes that will be applied when transforming based on what skin you're using.\nKey is the string token of the skin. RenderReplacements is a struct containing a material and mesh.\nPutting null for material or mesh will make them not change when transforming.")]
        public Dictionary<string, RenderReplacements> renderDictionary;

        [Tooltip("The component that will track information about your form, such as whether all necessary items have been collected. This component will be put on a gameObject that will be created at the beginning of every stage and will stay for the duration of the stage.\nIf you're unsure what to put here, use typeof(FormHandler).\nYou can create a subclass of FormHandler and put it here if you want to add code, such as an extra requirement for transforming.")]
        public Type handlerComponent;

        [Tooltip("Contains information on what characters are allowed to transform.\nIf whitelist, any body name listed under bodyNames will be allowed. If not whitelist, any body name not listed under bodyNames will be allowed.\nBody name refers to the name that survivors and enemies use internally. If you're unsure about what body name means, look into RoR2 BodyCatalog related stuff")]
        public AllowedBodyList allowedBodyList;

        [Tooltip("The default keybind players press to transform into the form. Don't get too attached to this, it's likely these keybinds will need to be changed if forms happen to overlap. If two forms overlap the same key and both can be transformed into, the first form alphabetically by name token will be selected. \nIf set to Keybind.None, there will be no keybind for activating the form. You can make your own way of transforming into the form.")]
        public KeyCode defaultKeyBind;

        public FormIndex formIndex 
        { 
            get 
            {
                if (!FormCatalog.availability.available) { Log.Warning("Can't get FormIndex before catalog is initialized"); return FormIndex.None; }
                return (FormIndex)Array.IndexOf(FormCatalog.formsCatalog, this); 
            }
        }

        public override string ToString()
        {
            return RoR2.Language.GetString(this.name, RoR2.Language.currentLanguageName);
        }

        public ConfigEntry<KeyboardShortcut> keybind;

        public int numberOfNeededItems
        {
            get
            {
                if (neededItems != null)
                {
                    int num = 0;
                    foreach (NeededItem item in neededItems)
                    {
                        num += item.count;
                    }
                    return num;
                }
                else
                {
                    return -1;
                }
            }
        }
    }

    public struct NeededItem
    {
        public ItemIndex item;

        public int count;

        public static implicit operator ItemIndex(NeededItem x) => x.item;

        public static implicit operator NeededItem(ItemIndex x) => new NeededItem { item = x, count = 1 };

        public override string ToString()
        {
            if (!ItemCatalog.availability.available) { Log.Warning("NeededItem.ToString() called before ItemCatalog initialized"); return ""; }
            return RoR2.Language.GetString(ItemCatalog.GetItemDef(this.item).nameToken, RoR2.Language.currentLanguageName) + " (" + count + ")\n";
        }
    }

    public struct AllowedBodyList
    {
        public bool whitelist;

        public string[] bodyNames;

        public bool BodyIsAllowed(string bodyName)
        {
            return !(whitelist ^ bodyNames.Contains(bodyName));
        }

        public bool BodyIsAllowed(BodyIndex bodyIndex)
        {
            return BodyIsAllowed(BodyCatalog.GetBodyName(bodyIndex));
        }
    }

    public struct RenderReplacements
    {
        public Material material;
        public Mesh mesh;
    }

    // Handles neededItem sharing and who has permission to transform
    //
    // Assuming all items have been collected across the team...
    // None: Only the player with ALL items can transform
    // MajorityRule: The player(s) with the MAJORITY number of the needed items can transform
    // Contributor: Players that have AT LEAST ONE of the needed items can transform
    // All: Anyone, whether they HAVE ANY ITEMS OR NOT, can transform
    public enum FormItemSharing
    {
        All,
        Contributor,
        MajorityRule,
        None
    }

    // Do not set this value. This value is set automatically at runtime
    public enum FormIndex
    {
        None = -1
    }
}