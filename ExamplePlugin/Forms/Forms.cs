using BepInEx;
using BepInEx.Configuration;
using EntityStates;
using HarmonyLib;
using HedgehogUtils.Forms.SuperForm;
using HedgehogUtils.Internal;
using HG;
using R2API;
using Rebindables;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Serialization;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace HedgehogUtils.Forms
{
    public static class Forms
    {
        public static Dictionary<FormDef, GameObject> formToHandlerPrefab = new Dictionary<FormDef, GameObject>();

        public static Dictionary<FormDef, GameObject> formToHandlerObject = new Dictionary<FormDef, GameObject>();

        public static Dictionary<FormDef, FormHandler> formToHandler = new Dictionary<FormDef, FormHandler>();

        // Look at the tooltips in the FormDef class for more information on what all of these parameters mean
        public static FormDef CreateFormDef(string name, BuffDef buff, float duration, bool requiresItems, bool shareRequirements, bool consumeItems, int maxTransforms, bool invincible, bool flight, bool superAnimations, SerializableEntityStateType formState, SerializableEntityStateType transformState,
            Dictionary<SkinDef, RenderReplacements> renderDictionary, Type handlerComponent, AllowedBodyList allowedBodyList, KeyCode defaultKeyBind)
        {
            FormDef form = ScriptableObject.CreateInstance<FormDef>();
            form.cachedName = name;
            form.buff = buff;
            form.duration = duration;
            form.requiresItems = requiresItems;
            form.shareRequirements = shareRequirements;
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
            form.keyBind = RebindAPI.RegisterModKeybind(new ModKeybind(form.cachedName, defaultKeyBind, 10));

            // Creating handler prefab
            GameObject handlerPrefab = PrefabAPI.CreateEmptyPrefab(form.cachedName + " " + form.handlerComponent.Name);
            FormHandler handlerObjectComponent = (FormHandler)handlerPrefab.AddComponent(handlerComponent);
            handlerObjectComponent.form = form;
            if (form.requiresItems)
            {
                handlerPrefab.AddComponent(form.shareRequirements ? typeof(SyncedItemTracker) : typeof(UnsyncedItemTracker));
            }
            //PrefabAPI.RegisterNetworkPrefab(handlerPrefab);
            formToHandlerPrefab.Add(form, handlerPrefab);
            Log.Message("FormDef "+form.cachedName+" created. Created new "+form.handlerComponent.Name+" prefab");

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
                return body.HasBuff(form.buff);
            }
            return false;
        }

        public static void AddSkinForForm(SkinDef skin, RenderReplacements render, ref FormDef form)
        {
            if (form.renderDictionary == null) form.renderDictionary = new Dictionary<SkinDef, RenderReplacements>();
            form.renderDictionary.Add(skin, render);
        }

        [SystemInitializer(typeof(BodyCatalog), typeof(FormCatalog))]
        private static void FormComponentsForEveryone()
        {
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
        [Tooltip("Name should be the name token for the transformation. Eg. \"DS_GAMING_HEDGEHOG_UTILS_SUPER_FORM\". The actual name of the form should be handled using LanguageAPI. See Tokens.cs for an example of how that works")]
        public string cachedName { get { return _cachedName; } set { name = value; _cachedName = value; } }
        private string _cachedName;

        [Tooltip("The buff given to you when you're transformed. This should be a buff unique to the form you're making.\nUse this buff for applying whatever stat increases you want.\nThis will be applied as a timed buff and will end the form when it goes away.")]
        public BuffDef buff;

        [Tooltip("The duration of the transformation in seconds. The actual duration of the form may be slightly longer to account for the transformation animation. If duration is <=0, a normal buff will be used instead of a timed buff")]
        public float duration;

        [Tooltip("Whether or not the form requires having certain items in order to transform. The specific items needed to transform are defined in neededItems once the ItemsCatalog is done.")]
        public bool requiresItems;

        [Tooltip("The item or items that are needed to transform. NeededItem struct stores a RoR2.ItemIndex and an int for how many of that item is needed. You can also just use the ItemIndex here if you won't need multiple of the same item, there is an implicit cast")]
        public NeededItem[] neededItems;

        [Tooltip("If transformation requirements, such as needed items and max amount of times you can transform, will be shared amongst all players. Any player will be able to transform if any other player or combination of players have the needed items.")]
        public bool shareRequirements;

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

        [Tooltip("The entity state used by the \"HedgehogUtilsForms\" entity state machine while transformed. Should be a subclass of FormStateBase")]
        public SerializableEntityStateType formState;

        [Tooltip("The entity state used by the \"Body\" entity state machine for the transformation animation that will transition you into the form. Should be a subclass of TransformationBase. If the EntityState is null, the transformation will be instant")]
        public SerializableEntityStateType transformState;

        [Tooltip("Stores the material and mesh changes that will be applied when transforming based on what skin you're using.\nKey is the SkinDef. Render Replacements is a struct containing the RendererInfos and mesh for each renderer on your character. All arrays in the RenderReplacements struct, as well as the defaultRendererInfos of the character, must be the same length. An array can be left null if unneeded.")]
        public Dictionary<SkinDef, RenderReplacements> renderDictionary;

        [Tooltip("The component that will track information about your form, such as whether all necessary items have been collected. This component will be put on a gameObject that will be created at the beginning of every stage and will stay for the duration of the stage.\nIf you're unsure what to put here, use typeof(FormHandler).\nYou can create a subclass of FormHandler and put it here if you want to add code, such as an extra requirement for transforming.")]
        public Type handlerComponent;

        [Tooltip("Contains information on what characters are allowed to transform.\nIf whitelist, any body name listed under bodyNames will be allowed. If not whitelist, any body name not listed under bodyNames will be allowed.\nBody name refers to the name that survivors and enemies use internally. If you're unsure about what body name means, look into RoR2 BodyCatalog related stuff.\nBodyList can be null.")]
        public AllowedBodyList allowedBodyList;

        [Tooltip("If it is possible for the form to be activated this stage. This value is set by setIsEnabledFunc at the beginning of every stage.")]
        public bool enabled { get; internal set; }
        
        [Tooltip("A function that decides whether the FormHandler of this form will be created at the beginning of the stage, thus making the form usable. Use this for any enabling or disabling of forms via config, artifacts, or any other arbitrary reason that would keep it disabled for an entire stage or run.\nBy default this checks if any survivor is playing a character than can use the form, based on the allowedBodyList.")]
        [FormerlySerializedAs("enabled")]
        public Func<FormDef, bool> setIsEnabledFunc = (self) => { return AnySelectedSurvivorCanUseForm(self); }; 

        public static bool AnySelectedSurvivorCanUseForm(FormDef form)
        {
            if (form.allowedBodyList.bodyNames != null)
            {
                foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
                {
                    if (form.allowedBodyList.BodyIsAllowed(BodyCatalog.FindBodyIndex(player.master.bodyPrefab)))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return !form.allowedBodyList.whitelist;
            }
        }

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
            return RoR2.Language.GetString(this.cachedName, RoR2.Language.currentLanguageName);
        }

        public ModKeybind keyBind;

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
            if (bodyNames == null || string.IsNullOrEmpty(bodyName)) return !whitelist;
            else
            {
                return !(whitelist ^ bodyNames.Contains(bodyName));
            }
        }

        public bool BodyIsAllowed(BodyIndex bodyIndex)
        {
            return BodyIsAllowed(BodyCatalog.GetBodyName(bodyIndex));
        }
    }

    public struct RenderReplacements
    {
        public CharacterModel.RendererInfo[] rendererInfo;
        public Mesh[] mesh;
        public AssetReferenceT<Mesh>[] meshAddress;
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