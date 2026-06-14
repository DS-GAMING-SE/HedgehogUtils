using EntityStates;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using System;
using HedgehogUtils.Forms.EntityStates;
using RoR2.ContentManagement;
using HG;

namespace HedgehogUtils.Forms
{
    public class FormComponent : NetworkBehaviour
    {
        public EntityStateMachine formStateMachine;

        [Tooltip("The form you have selected. Not necessarily the form you are currently in, but the one that you're focused on. Attempting to transform will transform you into this form.")]
        public FormDef targetedForm;

        [Tooltip("The form you're currently in. If not transformed into anything, this will be null.")]
        public FormDef activeForm;

        [Tooltip("The first FormDef is the form you were PREVIOUSLY in\nThe second FormDef is the one you're in now.")]
        public event Action<FormDef, FormDef> OnFormChanged;

        public string skinNameToken;

        private CharacterModel.RendererInfo[] defaultRendererInfos;
        private Mesh[] defaultMeshes;
        private CharacterModel.RendererInfo[] formRendererInfos;
        private Mesh[] formMeshes;
        private bool formModelApplied;

        public CharacterBody body;
        private EntityStateMachine bodyState;
        private CharacterModel model;
        private Animator modelAnimator;
        private LocalUser localUser;

        [Tooltip("Use the form's formIndex as the index of the array.")]
        public int[] numberOfTimesTransformed = Array.Empty<int>();

        [Tooltip("Use the form's formIndex as the index of the array.")]
        public ItemTracker[] formToItemTracker = Array.Empty<ItemTracker>();

        protected bool initialized;
        protected bool initStart;

        private void Start()
        {
            body = base.GetComponent<CharacterBody>();
            initStart = true;
            if (!body || (!body.isPlayerControlled && !(BodyCatalog.GetBodyName(body.bodyIndex).Contains("Turret"))))
            {
                this.enabled = false;
                return;
            }
            Init();
        }
        
        private void OnEnable()
        {
            if (!body || !initStart) { return; }
            Init();
        }
        private void OnDestroy()
        {
            if (body && body.skillLocator)
            {
                foreach (var item in body.skillLocator.AllSkills)
                {
                    item.onSkillChanged -= UpdateRequireFormSkillDefs;
                }
            }
        }

        private void Init()
        {
            if (initialized) { return; }

            model = body.modelLocator.modelTransform.GetComponent<CharacterModel>();
            modelAnimator = model.transform.GetComponent<Animator>();
            formStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "HedgehogUtilsForms");
            if (body.skillLocator)
            {
                foreach (var item in body.skillLocator.AllSkills)
                {
                    item.onSkillChanged += UpdateRequireFormSkillDefs;
                }
            }
            bodyState = EntityStateMachine.FindByCustomName(base.gameObject, "Body");
            skinNameToken = GetSkinNameToken();
            defaultMeshes = new Mesh[model.baseRendererInfos.Length];
            formMeshes = new Mesh[model.baseRendererInfos.Length];

            PrepareForms();
            Array.Resize(ref numberOfTimesTransformed, FormCatalog.formsCatalog.Length);

            initialized = true;
        }

        private void PrepareForms()
        {
            Array.Resize(ref formToItemTracker, FormCatalog.formsCatalog.Length);
            foreach (FormDef form in FormCatalog.formsCatalog)
            {
                if (!form.enabled) continue;
                if (Forms.formToHandler.TryGetValue(form, out FormHandler handler) && form.requiresItems)
                {
                    CreateTrackerForForm(form);
                }

                if (skinNameToken != null && form.renderDictionary.TryGetValue(skinNameToken, out RenderReplacements renderReplacements))
                {
                    if (renderReplacements.rendererInfo != null)
                    {
                        for (int i = 0; i < renderReplacements.rendererInfo.Length; i++)
                        {
                            if (renderReplacements.meshAddress != null && renderReplacements.meshAddress[i] != null && renderReplacements.meshAddress[i].RuntimeKeyIsValid())
                            { 
                                AssetAsyncReferenceManager<Mesh>.LoadAsset(renderReplacements.meshAddress[i], AsyncReferenceHandleUnloadType.OnSceneUnload); 
                            }
                            if (renderReplacements.rendererInfo[i].defaultMaterialAddress != null && renderReplacements.rendererInfo[i].defaultMaterialAddress.RuntimeKeyIsValid())
                            {
                                AssetAsyncReferenceManager<Material>.LoadAsset(renderReplacements.rendererInfo[i].defaultMaterialAddress, AsyncReferenceHandleUnloadType.OnSceneUnload);
                            }
                        }
                    }
                }
            }
        }

        private void CreateTrackerForForm(FormDef form)
        {
            ItemTracker itemTracker = body.gameObject.AddComponent<ItemTracker>();
            itemTracker.form = form;
            itemTracker.body = body;
            formToItemTracker[(int)form.formIndex] = itemTracker;
        }

        private void FixedUpdate()
        {
            if (body.hasAuthority && body.isPlayerControlled)
            {
                if (localUser == null) localUser = Helpers.FindLocalUser(body);
                DecideTargetForm();
            }
        }

        private void DecideTargetForm()
        {
            targetedForm = null;
            foreach (FormDef form in FormCatalog.formsCatalog)
            {
                if (!form.keybind.Value.Equals(BepInEx.Configuration.KeyboardShortcut.Empty) && Input.GetKeyDown(form.keybind.Value.MainKey) && !localUser.isUIFocused && bodyState.state is GenericCharacterMain)
                {
                    targetedForm = form;
                    
                    if (targetedForm != activeForm && Forms.formToHandler.TryGetValue(targetedForm, out FormHandler handler) &&
                        formStateMachine && (formStateMachine.state is not FormStateBase || ((FormStateBase)(formStateMachine.state)).CanBeOverridden()))
                    {
                        if (handler.CanTransform(this))
                        {
                            Transform();
                            break;
                        }
                    }
                }
            }
        }
        public void Transform()
        {
            Transform(targetedForm);
        }

        public void Transform(FormDef form)
        {
            if (!bodyState) { return; }
            if (!Forms.formToHandler.TryGetValue(form, out FormHandler handler)) { return; }
            bool transformSuccess;
            if (form.transformState.stateType != null)
            {
                TransformationBase transformState = (TransformationBase)EntityStateCatalog.InstantiateState(form.transformState.stateType);
                transformState.fromTeamSuper = handler.teamSuper;

                transformSuccess = bodyState.SetInterruptState(transformState, InterruptPriority.Frozen);
            }
            else
            {
                transformSuccess = true;
                SetNextForm(form);
            }

            if (transformSuccess)
            {
                numberOfTimesTransformed[(int)form.formIndex] += 1;
                if (NetworkServer.active)
                {
                    handler.OnTransform(this);
                }
                else
                {
                    new NetworkTransformation(GetComponent<NetworkIdentity>().netId, form.formIndex).Send(NetworkDestination.Server);
                }
            }
        }

        public void SetNextForm(FormDef form)
        {
            if (form != null)
            {
                FormStateBase formState = (FormStateBase)EntityStateCatalog.InstantiateState(form.formState.stateType);
                formState.form = form;
                this.formStateMachine.SetNextState(formState);
            }
            else
            {
                this.formStateMachine.SetNextStateToMain();
            }
        }

        internal void OnTransform(FormDef form)
        {
            FormDef previousForm = activeForm;
            this.activeForm = form;
            UpdateAllRequireFormSkillDefs();
            OnFormChanged?.Invoke(previousForm, activeForm);
            if (!form) { return; }
            SuperModel(skinNameToken);
        }

        internal void TransformEnd()
        {
            if (body.HasBuff(activeForm.buff))
            {
                if (activeForm.duration > 0)
                {
                    body.RemoveOldestTimedBuff(activeForm.buff);
                }
                else
                {
                    body.RemoveBuff(activeForm.buff);
                }
            }
            FormDef previousForm = activeForm;
            this.activeForm = null;
            UpdateAllRequireFormSkillDefs();
            OnFormChanged?.Invoke(previousForm, activeForm);
            ResetModel();
        }
        private void UpdateAllRequireFormSkillDefs()
        {
            if (body.skillLocator)
            {
                foreach (var item in body.skillLocator.allSkills)
                {
                    UpdateRequireFormSkillDefs(item);
                }
            }
        }

        private void UpdateRequireFormSkillDefs(GenericSkill genericSkill)
        {
            if (genericSkill.currentSkillOverride != -1 && genericSkill.skillDef && genericSkill.skillDef is SkillDefs.IRequiresFormSkillDef formDef)
            {
                if (activeForm != formDef.requiredForm)
                {
                    GenericSkill.SkillOverride skillOverride = genericSkill.skillOverrides[genericSkill.currentSkillOverride];
                    genericSkill.UnsetSkillOverride(skillOverride.source, skillOverride.skillDef, skillOverride.priority);
                }
            }
        }
        public string GetSkinNameToken()
        {
            ModelSkinController skin = model.GetComponent<ModelSkinController>();
            if (skin && skin.skins.Length > body.skinIndex) // heretic causing errors without this check
            {
                return skin.skins[body.skinIndex].nameToken;
            }
            return null;
        }

        public int GetNumberOfTimesTransformed(FormDef form)
        {
            return numberOfTimesTransformed[(int)form.formIndex];
        }

        private void SuperModel(string skinNameToken)
        {
            if (modelAnimator && activeForm.superAnimations) // Animations
            {
                modelAnimator.SetFloat("isSuperFloat", 1f);
            }

            if (!GetSuperModel(skinNameToken)) return;

            defaultRendererInfos = ArrayUtils.Clone(model.baseRendererInfos);
            for (int i = 0; i < model.baseRendererInfos.Length; i++)
            {
                formRendererInfos[i].renderer = defaultRendererInfos[i].renderer; // sets renderers to your current character instead of the prefab
            }
            model.baseRendererInfos = formRendererInfos;

            ApplyMeshes(model.baseRendererInfos, formMeshes, true);

            model.forceUpdate = true;
            formModelApplied = true;
        }

        public void ResetModel()
        {
            if (!formModelApplied) return;
            model.baseRendererInfos = defaultRendererInfos;
            if (modelAnimator) // Animations
            {
                modelAnimator.SetFloat("isSuperFloat", 0f);
            }
            ApplyMeshes(model.baseRendererInfos, defaultMeshes, false);

            model.materialsDirty = true;
            formModelApplied = false;
        }

        private void ApplyMeshes(CharacterModel.RendererInfo[] renderer, Mesh[] mesh, bool setPreviousToDefault)
        {
            if (renderer == null || mesh == null) return;
            for (int i = 0; i < renderer.Length; i++)
            {
                if (!renderer[i].renderer || !mesh[i]) continue;
                if (renderer[i].renderer is MeshRenderer)
                {
                    MeshFilter filter = renderer[i].renderer.GetComponent<MeshFilter>();
                    if (setPreviousToDefault) defaultMeshes[i] = filter.mesh;
                    filter.mesh = mesh[i];

            }
                else
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = renderer[i].renderer as SkinnedMeshRenderer;
                    if (skinnedMeshRenderer != null)
                    {
                        if (setPreviousToDefault) defaultMeshes[i] = skinnedMeshRenderer.sharedMesh;
                        skinnedMeshRenderer.sharedMesh = mesh[i];
                    }
                }
            }
        }

        private bool GetSuperModel(string skinName)
        {
            if (activeForm.renderDictionary == null || string.IsNullOrEmpty(skinName)) 
            {
                return false; 
            }

            if (activeForm.renderDictionary.TryGetValue(skinName, out RenderReplacements renderReplacements))
            {
                formRendererInfos = ArrayUtils.Clone(renderReplacements.rendererInfo);
                formMeshes = new Mesh[model.baseRendererInfos.Length];
                for (int i = 0; i < model.baseRendererInfos.Length; i++)
                {
                    // memop my beloved
                    if (renderReplacements.meshAddress != null && renderReplacements.meshAddress[i] != null && renderReplacements.meshAddress[i].RuntimeKeyIsValid())
                    {
                        if (AssetAsyncReferenceManager<Mesh>.handles.TryGetValue(renderReplacements.meshAddress[i].RuntimeKey.ToString(), out AssetAsyncReferenceManager<Mesh>.AsyncHandleItem handle))
                        {
                            if (!handle.loadHandle.Result)
                            {
                                handle.loadHandle.WaitForCompletion();
                            }
                            formMeshes[i] = handle.loadHandle.Result;
                        }
                        else
                        {
                            formMeshes[i] = renderReplacements.meshAddress[i].LoadAssetAsync<Mesh>().WaitForCompletion();
                        }
                    }
                    else if (renderReplacements.mesh != null)
                    {
                        formMeshes[i] = renderReplacements.mesh[i];
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class ItemTracker : MonoBehaviour
    {
        public FormDef form;

        private FormHandler formHandler;

        private SyncedItemTracker syncedItemTracker;

        public CharacterBody body;

        public Inventory inventory;

        private bool itemsDirty;

        public bool allItems;

        public int numItemsCollected;

        private bool eventsSubscribed;

        // HOW DO I GET THE INVENTORY?!?!?
        private void Start()
        {
            if (Forms.formToHandler.TryGetValue(form, out FormHandler handler))
            {
                formHandler = handler;
                if (typeof(SyncedItemTracker).IsAssignableFrom(handler.itemTracker.GetType()))
                {
                    syncedItemTracker = handler.itemTracker as SyncedItemTracker;
                }
            }
        }

        private void OnDisable()
        {
            SubscribeEvents(false);
        }

        private void FixedUpdate()
        {
            if (!eventsSubscribed)
            {
                if (body)
                {
                    if (body.inventory)
                    {
                        inventory = body.inventory;
                        SubscribeEvents(true);
                    }
                }
            }
            else
            {
                if (itemsDirty)
                {
                    CheckItems();
                }
            }
        }

        public void SubscribeEvents(bool subscribe)
        {
            if (inventory)
            {
                if (eventsSubscribed ^ subscribe)
                {
                    if (subscribe)
                    {
                        inventory.onInventoryChanged += SetItemsDirty;
                        if (syncedItemTracker)
                        {
                            syncedItemTracker.CheckHighestItemCountEvent += HighestItemCount;
                        }
                        eventsSubscribed = true;
                        SetItemsDirty();
                    }
                    else
                    {
                        inventory.onInventoryChanged -= SetItemsDirty;
                        if (syncedItemTracker)
                        {
                            syncedItemTracker.CheckHighestItemCountEvent -= HighestItemCount;
                        }
                        eventsSubscribed = false;
                    }
                }
            }
        }

        public void SetItemsDirty()
        {
            itemsDirty = true;
        }

        public void CheckItems()
        {
            itemsDirty = false;
            numItemsCollected = 0;
            if (!form) { Log.Error("No form?", Config.Logs.All); allItems= false; return; }
            if (!inventory) { Log.Error("No inventory?", Config.Logs.All); allItems = false; return; }
            bool allItemsTemp = true;
            foreach (NeededItem item in form.neededItems)
            {
                if (item == ItemIndex.None) { Log.Error("No item?", Config.Logs.All); return; }
                if (inventory.GetItemCountEffective(item) > 0)
                {
                    numItemsCollected += Math.Min(item.count, inventory.GetItemCountEffective(item));
                    if (inventory.GetItemCountEffective(item) < item.count && allItemsTemp)
                    {
                        allItemsTemp = false;
                        Log.Message(body.GetDisplayName() + " player missing items needed for form " + form.ToString() + ": \n" + (new NeededItem { item = item.item, count = item.count - inventory.GetItemCountEffective(item) }).ToString(), Config.Logs.All);
                    }
                }
                else
                {
                    allItemsTemp = false;
                }
            }
            // FormHandler's item tracking counts numItemsCollected before it gets updated here
            allItems = allItemsTemp;
            Log.Message(body.GetDisplayName() + " player's items needed for form " + form.ToString() +"\nNumber of items: " + numItemsCollected + "\nAll: " + allItems, Config.Logs.All);
            if (syncedItemTracker)
            {
                syncedItemTracker.CheckHighestItemCount();
            }
        }

        private void HighestItemCount(SyncedItemTracker.CheckHighestItemCountEventArgs args)
        {
            if (args.highestItemCount < numItemsCollected)
            {
                args.highestItemCount = numItemsCollected;
            }
        }
    }
}