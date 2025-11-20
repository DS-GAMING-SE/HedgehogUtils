using RoR2;
using UnityEngine;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Networking.NetworkSystem;
using EntityStates;
using UnityEngine.AddressableAssets;
using System;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Utilities;
using HedgehogUtils.Internal;

namespace HedgehogUtils.Forms
{
    public class FormHandler : NetworkBehaviour
    {
        // Basically everything except the SyncVars is only handled by server and won't be accurate for clients
        public INeededItemTracker itemTracker;

        public FormDef form;

        public bool eventsSubscribed = false;

        [SyncVar]
        public bool teamSuper;

        [SyncVar]
        public int numberOfTimesTransformed = 0;

        public const float teamSuperTimerDuration = 10f;
        public float teamSuperTimer;

        
        private void OnEnable()
        {
            if (!(form && FormCatalog.formsCatalog.Contains(form))) { Log.Error("FormHandler does not have a valid formDef set."); }
            if (!Forms.formToHandlerObject.ContainsKey(form))
            {
                itemTracker = GetComponent<INeededItemTracker>();
                Forms.formToHandlerObject.Add(form, gameObject);
                if (!Forms.formToHandler.ContainsKey(form))
                {
                    Forms.formToHandler.Add(form, this);
                }
                Log.Message("FormHandler for form " + form.ToString() + " created");
                return;
            }
            Log.Error("Duplicate instance of formHandler "+ form.ToString() +". Only one should exist at a time.");
        }

        private void OnDisable()
        {
            if (Forms.formToHandlerObject.GetValueSafe(form) == this.gameObject)
            {
                Forms.formToHandlerObject.Remove(form);
                SetEvents(false);
            }
            if (Forms.formToHandler.GetValueSafe(form) == this)
            {
                Forms.formToHandler.Remove(form);
            }
        }

        public virtual void SetEvents(bool active)
        {
            if (active && !eventsSubscribed)
            {
                RoR2Application.onFixedUpdate += OnFixedUpdate;
                eventsSubscribed = true;
            }
            if (!active && eventsSubscribed)
            {
                RoR2Application.onFixedUpdate -= OnFixedUpdate;
                eventsSubscribed = false;
            }
        }

        public virtual bool HasItems(FormComponent component)
        {
            if (form.requiresItems)
            {
                if (itemTracker != null)
                {
                    return itemTracker.ItemRequirementMet(component);
                }
                Log.Error("Form that needs items has no itemTracker");
            }
            return true;
        }

        public virtual bool HasNotTransformedMaxAmountOfTimes(FormComponent component)
        {
            return (form.maxTransforms <= 0 || (numberOfTimesTransformed < form.maxTransforms || (component.GetNumberOfTimesTransformed(form) < form.maxTransforms && !form.shareRequirements)));
        }

        public virtual bool CanTransform(FormComponent component)
        {
            bool hasItems = HasItems(component);
            Log.Message("FormHandler with form " + form.ToString() + "\nTeam Super? " + teamSuper + ". Has Items? " + hasItems + ". Number of transforms? " + numberOfTimesTransformed + "/" + component.GetNumberOfTimesTransformed(form) + " out of max " + form.maxTransforms);
            return (hasItems && HasNotTransformedMaxAmountOfTimes(component)) || teamSuper;
        }

        public virtual void OnTransform(FormComponent component)
        {
            if (!teamSuper)
            {
                if (form.shareRequirements)
                {
                    NetworkteamSuper = true;
                    teamSuperTimer = teamSuperTimerDuration;
                }
                NetworknumberOfTimesTransformed += 1;
                if (form.consumeItems)
                {
                    itemTracker.RemoveItems(component);
                }
                AnnounceTransformation(component);
            }
            TransformEngiTurrets(component);
        }

        public virtual void AnnounceTransformation(FormComponent component)
        {
            if (Config.AnnounceSuperTransformation().Value)
            {
                if (component.body.master && component.body.master.playerCharacterMasterController && component.body.master.playerCharacterMasterController.networkUser)
                {
                    Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                    {
                        baseToken = HedgehogUtilsPlugin.Prefix + "SUPER_FORM_ANNOUNCE_TEXT",
                        subjectAsNetworkUser = component.body.master.playerCharacterMasterController.networkUser,
                        paramTokens = new string[] { RoR2.Language.GetString(form.name) }
                    });
                }
                else
                {
                    Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                    {
                        baseToken = HedgehogUtilsPlugin.Prefix + "SUPER_FORM_ANNOUNCE_TEXT",
                        subjectAsCharacterBody = component.body,
                        paramTokens = new string[] { RoR2.Language.GetString(form.name) }
                    });
                }
            }
        }

        public virtual void TransformEngiTurrets(FormComponent component)
        {
            if (component.body.master)
            {
                if (component.body.master.deployablesList != null)
                {
                    foreach (DeployableInfo deployable in component.body.master.deployablesList)
                    {
                        if (deployable.slot == DeployableSlot.EngiTurret)
                        {
                            if (deployable.deployable.TryGetComponent<CharacterMaster>(out CharacterMaster turretMaster) && turretMaster.GetBodyObject() && turretMaster.GetBodyObject().TryGetComponent<FormComponent>(out FormComponent turretSuper))
                            {
                                turretSuper.targetedForm = form;
                                turretSuper.Transform();
                            }
                        }
                    }
                }
            }
        }

        public virtual void OnFixedUpdate()
        {
            if (teamSuperTimer > 0)
            {
                teamSuperTimer -= Time.deltaTime;
                if (teamSuperTimer <= 0)
                {
                    NetworkteamSuper = false;
                    Log.Message("Team Super window ended");
                }
            }
        }

        public bool NetworkteamSuper
        {
            get
            {
                return teamSuper;
            }
            [param: In]
            set
            {
                base.SetSyncVar<bool>(value, ref teamSuper, 1U);
            }
        }

        public int NetworknumberOfTimesTransformed
        {
            get
            {
                return numberOfTimesTransformed;
            }
            [param: In]
            set
            {
                base.SetSyncVar<int>(value, ref numberOfTimesTransformed, 2U);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(teamSuper);
                writer.Write(numberOfTimesTransformed);
                return true;
            }
            bool flag = false;
            if ((base.syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(teamSuper);
            }
            if ((base.syncVarDirtyBits & 2U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(numberOfTimesTransformed);
            }
            if (!flag)
            {
                writer.WritePackedUInt32(base.syncVarDirtyBits);
            }
            return flag;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                teamSuper = reader.ReadBoolean();
                numberOfTimesTransformed = reader.ReadInt32();
                return;
            }
            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1U) != 0U)
            {
                teamSuper = reader.ReadBoolean();
            }
            if ((num & 2U) != 0U)
            {
                numberOfTimesTransformed = reader.ReadInt32();
            }
        }
    }

    public interface INeededItemTracker
    {
        bool ItemRequirementMet(FormComponent component);

        void RemoveItems(FormComponent component);
    }

    [RequireComponent(typeof(FormHandler))]
    public class SyncedItemTracker : NetworkBehaviour, INeededItemTracker
    {
        public FormHandler handler;
        
        [SyncVar]
        public bool allItems;

        [SyncVar]
        private static byte _serverItemSharing;

        public static FormItemSharing serverItemSharing
        {
            get { return (FormItemSharing)_serverItemSharing; }
        }

        public List<NeededItem> missingItems;

        // This value is only accurate when serverItemSharing is on MajorityRule
        public int highestItemCount;

        public int numNeededItems
        {
            get; private set;
        }

        public bool eventsSubscribed;

        private bool itemsDirty;

        private void OnEnable()
        {
            handler = this.GetComponent<FormHandler>();
            numNeededItems = handler.form.numberOfNeededItems;
            SubscribeEvents(true);
        }

        private void OnDisable()
        {
            SubscribeEvents(false);
        }
        
        public bool ItemRequirementMet(FormComponent component)
        {
            return allItems && CanTakeSharedItems(handler.form, component);
        }

        public void FixedUpdate()
        {
            if (itemsDirty)
            {
                CheckItems();
            }
        }

        public void CheckItems()
        {
            missingItems = new List<NeededItem>();
            itemsDirty = false;
            if (!handler) { Log.Warning("no handler yet"); return; }
            if (handler.form.neededItems == null) { Log.Warning("Form says it needs items but has no list of needed items... curious..."); return; }
            foreach (NeededItem item in handler.form.neededItems)
            {
                int collectiveItemCount = 0;
                foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
                {
                    if (player)
                    {
                        if (!player.master || !player.master.inventory) { continue; }
                        collectiveItemCount += player.master.inventory.GetItemCountEffective(item);
                    }
                }
                if (collectiveItemCount < item.count)
                {
                    NeededItem missing= item;
                    missing.count = item.count - collectiveItemCount;
                    missingItems.Add(missing);
                }
            }
            NetworkallItems = missingItems.Count == 0;
            Log.Message("Missing items for "+ handler.form.ToString() + ": " + string.Concat(missingItems.Select(x => x.ToString())));
        }
        public bool CanTakeSharedItems(FormDef form, FormComponent super)
        {
            if (super.formToItemTracker[(int)form.formIndex])
            {
                ItemTracker itemTracker = super.formToItemTracker[(int)form.formIndex];
                switch (serverItemSharing)
                {
                    case FormItemSharing.None:
                        return itemTracker.allItems;
                    case FormItemSharing.MajorityRule:
                        return itemTracker.numItemsCollected >= highestItemCount;
                    case FormItemSharing.Contributor:
                        return itemTracker.numItemsCollected > 0;
                    default:
                        return true;
                }
            }
            else
            {
                Log.Error("CanTakeSharedItems run without valid ItemTracker");
                return serverItemSharing == FormItemSharing.All;
            }
        }

        public void CheckHighestItemCount()
        {
            if (serverItemSharing != FormItemSharing.MajorityRule) { return; }
            CheckHighestItemCountArgs = new CheckHighestItemCountEventArgs();
            if (CheckHighestItemCountEvent != null)
            {
                foreach (CheckHighestItemCountEventHandler @event in CheckHighestItemCountEvent.GetInvocationList())
                {
                    @event(CheckHighestItemCountArgs);
                }
            }
            highestItemCount = CheckHighestItemCountArgs.highestItemCount;
            Log.Message("highestItemCount " + highestItemCount);
        }

        public event CheckHighestItemCountEventHandler CheckHighestItemCountEvent;
        public delegate void CheckHighestItemCountEventHandler(CheckHighestItemCountEventArgs e);

        public CheckHighestItemCountEventArgs CheckHighestItemCountArgs;

        public class CheckHighestItemCountEventArgs : EventArgs
        {
            public int highestItemCount;
        }

        public void OnInventoryChanged(Inventory inventory)
        {
            if (inventory.TryGetComponent(out CharacterMaster master))
            {
                if (master.playerCharacterMasterController) // Only check items again if a player's inventory changes
                {
                    SetItemsDirty(); // Only check items once per frame
                }
            }
        }

        public void SetItemsDirty()
        {
            itemsDirty = true;
        }

        public void RemoveItems(FormComponent component)
        {
            foreach (NeededItem item in handler.form.neededItems)
            {
                int neededItems = item.count;
                if (component.body.master)
                {
                    int numToConstume = Math.Min(component.body.master.inventory.GetItemCountEffective(item), neededItems);
                    neededItems -= numToConstume;
                    component.body.master.inventory.RemoveTempOrPermanentItem(item, numToConstume);
                    if (neededItems <= 0)
                    {
                        continue;
                    }
                }
                foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
                {
                    int numToConstume = Math.Min(player.master.inventory.GetItemCountEffective(item), neededItems);
                    neededItems -= numToConstume;
                    player.master.inventory.RemoveTempOrPermanentItem(item, numToConstume);
                    if (neededItems <= 0)
                    {
                        break;
                    }
                }
                if (neededItems > 0)
                {
                    Log.Warning("Does not have the items to be removed for transforming");
                }
            }
        }

        public void SubscribeEvents(bool subscribe)
        {
            if (eventsSubscribed ^ subscribe)
            {
                if (subscribe)
                {
                    Inventory.onInventoryChangedGlobal += OnInventoryChanged;
                    eventsSubscribed = true;
                    Config.NeededItemSharing().SettingChanged += UpdateFormItemSharingConfig;
                    if (NetworkServer.active)
                    {
                        Network_serverItemSharing = (byte)Config.NeededItemSharing().Value;
                    }
                    Log.Message("Subscribed to inventory events");
                    CheckItems();
                }
                else
                {
                    Inventory.onInventoryChangedGlobal -= OnInventoryChanged;
                    Config.NeededItemSharing().SettingChanged -= UpdateFormItemSharingConfig;
                    eventsSubscribed = false;
                }
            }
        }

        public void UpdateFormItemSharingConfig(object sender, EventArgs args)
        {
            if (NetworkServer.active)
            {
                Network_serverItemSharing = (byte)Config.NeededItemSharing().Value;
                if (Config.NeededItemSharing().Value == FormItemSharing.MajorityRule)
                {
                    CheckHighestItemCount();
                }
            }
        }

        public bool NetworkallItems
        {
            get
            {
                return allItems;
            }
            [param: In]
            set
            {
                base.SetSyncVar<bool>(value, ref allItems, 1U);
            }
        }

        public byte Network_serverItemSharing
        {
            get
            {
                return _serverItemSharing;
            }
            [param: In]
            set
            {
                base.SetSyncVar<byte>(value, ref _serverItemSharing, 2U);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(allItems);
                writer.Write(_serverItemSharing);
                return true;
            }
            bool flag = false;
            if ((base.syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(allItems);
            }
            if ((base.syncVarDirtyBits & 2U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(_serverItemSharing);
            }
            return flag;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                allItems = reader.ReadBoolean();
                _serverItemSharing = reader.ReadByte();
                return;
            }
            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1U) != 0U)
            {
                allItems = reader.ReadBoolean();
            }
            if ((num & 2U) != 0U)
            {
                _serverItemSharing = reader.ReadByte();
                if (serverItemSharing == FormItemSharing.MajorityRule)
                {
                    CheckHighestItemCount();
                }
            }
        }
    }

    [RequireComponent(typeof(FormHandler))]
    public class UnsyncedItemTracker : MonoBehaviour, INeededItemTracker
    {
        public FormHandler handler;

        private void Start()
        {
            handler = this.GetComponent<FormHandler>();
        }

        public bool ItemRequirementMet(FormComponent component)
        {
            Log.Message("Checking unsynceditemtracker");
            return component.formToItemTracker[(int)handler.form.formIndex].allItems;
        }

        public void RemoveItems(FormComponent component)
        {
            if (component.body)
            {
                if (component.body.master)
                {
                    foreach (NeededItem item in handler.form.neededItems)
                    {
                        if (component.body.master.inventory.GetItemCountEffective(item) >= item.count)
                        {
                            component.body.master.inventory.RemoveTempOrPermanentItem(item, item.count);
                        }
                        else
                        {
                            Log.Warning("Does not have the items to be removed for transforming");
                            component.body.master.inventory.RemoveTempOrPermanentItem(item, component.body.master.inventory.GetItemCountEffective(item));
                        }
                    }
                }
            }
        }
    }
}