﻿using EntityStates;
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

namespace HedgehogUtils.Forms
{
    public class SuperSonicComponent : NetworkBehaviour
    {
        public EntityStateMachine superSonicState;

        [Tooltip("The form you have selected. Not necessarily the form you are currently in, but the one that you're focused on. Attempting to transform will transform you into this form.")]
        public FormDef targetedForm;

        [Tooltip("The form you're currently in. If not transformed into anything, this will be null.")]
        public FormDef activeForm;

        public Material formMaterial;
        public Material defaultMaterial;

        public Mesh formMesh;
        public Mesh defaultMesh;

        public CharacterBody body;
        private CharacterModel model;
        private Animator modelAnimator;

        public Dictionary<FormDef, ItemTracker> formToItemTracker = new Dictionary<FormDef, ItemTracker>();

        private void Start()
        {
            body = base.GetComponent<CharacterBody>();
            if (!body.isPlayerControlled && !(BodyCatalog.GetBodyName(body.bodyIndex).Contains("Turret")))
            {
                Destroy(this);
                return;
            }
            model = body.modelLocator.modelTransform.GetComponent<CharacterModel>();
            modelAnimator = model.transform.GetComponent<Animator>();
            superSonicState = EntityStateMachine.FindByCustomName(base.gameObject, "SonicForms");

            CreateUnsyncItemTrackers();
        }

        public void CreateUnsyncItemTrackers()
        {
            foreach (FormDef form in FormCatalog.formsCatalog)
            {
                if (form.requiresItems)
                {
                    CreateTrackerForForm(form);
                }
            }
        }

        public virtual void CreateTrackerForForm(FormDef form)
        {
            ItemTracker itemTracker = body.gameObject.AddComponent<ItemTracker>();
            itemTracker.form = form;
            itemTracker.body = body;
            formToItemTracker.Add(form, itemTracker);
        }

        public void FixedUpdate()
        {
            if (body.hasAuthority && body.isPlayerControlled)
            {
                DecideTargetForm();
            }
        }

        // Doing it like Input.GetKeyDown(form.keybind.Value.MainKey) will fix hte problem of not being able to transform when pressing any other buttons
        // but will also break people using multiple keybinds at the same time to transform. Does anyone actually do that?
        public void DecideTargetForm()
        {
            targetedForm = null;
            foreach (FormDef form in FormCatalog.formsCatalog)
            {
                if (form.keybind.Value.IsDown())
                {
                    targetedForm = form;
                    
                    if (targetedForm != activeForm && Forms.formToHandler.TryGetValue(targetedForm, out FormHandler handler))
                    {
                        if (handler.CanTransform(this))
                        {
                            Log.Message("Attempt Transform");
                            Transform();
                            break;
                        }
                    }
                }
            }
        }

        public void Transform()
        {
            EntityStateMachine bodyState = EntityStateMachine.FindByCustomName(base.gameObject, "Body");
            if (!Forms.formToHandler.TryGetValue(targetedForm, out FormHandler handler)) { return; }
            bool transformSuccess;
            if (targetedForm.transformState.stateType != null && bodyState)
            {
                TransformationBase transformState = (TransformationBase)EntityStateCatalog.InstantiateState(targetedForm.transformState.stateType);
                transformState.fromTeamSuper = handler.teamSuper;

                transformSuccess = bodyState.SetInterruptState(transformState, InterruptPriority.Frozen);
            }
            else
            {
                transformSuccess = true;
                SetNextForm(targetedForm);
            }

            if (transformSuccess)
            {
                if (NetworkServer.active)
                {
                    //FormHandler.instance.OnTransform();
                    handler.OnTransform(this);
                }
                else
                {
                    new NetworkTransformation(GetComponent<NetworkIdentity>().netId, targetedForm.formIndex).Send(NetworkDestination.Server);
                }
            }
        }

        public void SetNextForm(FormDef form)
        {
            if (form != null)
            {
                SonicFormBase formState = (SonicFormBase)EntityStateCatalog.InstantiateState(form.formState.stateType);
                formState.form = form;
                this.superSonicState.SetNextState(formState);
            }
            else
            {
                this.superSonicState.SetNextStateToMain();
            }
        }

        public void OnTransform(FormDef form)
        {
            this.activeForm = form;
            if (!form) { return; }
            ModelSkinController skin = model.GetComponentInChildren<ModelSkinController>();
            if (!skin) { return; }
            if (skin.skins.Length > body.skinIndex) // heretic causing errors without this check
            {
                GetSuperModel(skin.skins[body.skinIndex].nameToken);
                SuperModel();
            }
        }

        public void TransformEnd()
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
            this.activeForm = null;
            ResetModel();
        }

        // Thank you DxsSucuk
        public void SuperModel()
        {
            defaultMaterial = model.baseRendererInfos[0].defaultMaterial; // Textures
            if (formMaterial)
            {
                model.baseRendererInfos[0].defaultMaterial = formMaterial;
            }
            
            if (modelAnimator && activeForm.superAnimations) // Animations
            {
                modelAnimator.SetFloat("isSuperFloat", 1f);
            }

            if (formMesh) // Model
            {
                defaultMesh = model.mainSkinnedMeshRenderer.sharedMesh;
                model.mainSkinnedMeshRenderer.sharedMesh = formMesh;
            }

            model.materialsDirty = true;
        }

        public void ResetModel()
        {
            model.baseRendererInfos[0].defaultMaterial = defaultMaterial; // Textures

            if (modelAnimator) // Animations
            {
                modelAnimator.SetFloat("isSuperFloat", 0f);
            }

            if (formMesh) // Model
            {
                model.mainSkinnedMeshRenderer.sharedMesh = defaultMesh;
            }

            model.materialsDirty = true;
        }

        public virtual void GetSuperModel(string skinName)
        {
            if (activeForm.renderDictionary == null) 
            {
                if (defaultMesh)
                {
                    formMesh = defaultMesh;
                }
                if (defaultMaterial)
                {
                    formMaterial = defaultMaterial;
                }
                return; 
            }

            if (activeForm.renderDictionary.TryGetValue(skinName, out RenderReplacements replacements))
            {
                formMesh = replacements.mesh;
                formMaterial = replacements.material;
            }
            else
            {
                if (defaultMesh)
                {
                    formMesh = defaultMesh;
                }
                if (defaultMaterial)
                {
                    formMaterial = defaultMaterial;
                }
            }
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
                        Log.Message("inventory found");
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
                        Log.Message("subscribe");
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
            if (!form) { Log.Error("No form?"); allItems= false; return; }
            if (!inventory) { Log.Error("No inventory?"); allItems = false; return; }
            bool allItemsTemp = true;
            foreach (NeededItem item in form.neededItems)
            {
                if (item == ItemIndex.None) { Log.Error("No item?"); return; }
                if (inventory.GetItemCount(item) > 0)
                {
                    numItemsCollected += Math.Min(item.count, inventory.GetItemCount(item));
                    if (inventory.GetItemCount(item) < item.count && allItemsTemp)
                    {
                        allItemsTemp = false;
                        Log.Message(body.GetDisplayName() + " player missing items needed for form " + form.ToString() + ": \n" + (new NeededItem { item = item.item, count = item.count - inventory.GetItemCount(item) }).ToString());
                    }
                }
                else
                {
                    allItemsTemp = false;
                }
            }
            // FormHandler's item tracking counts numItemsCollected before it gets updated here
            allItems = allItemsTemp;
            Log.Message(body.GetDisplayName() + " player's items needed for form " + form.ToString() +"\nNumber of items: " + numItemsCollected + "\nAll: " + allItems);
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