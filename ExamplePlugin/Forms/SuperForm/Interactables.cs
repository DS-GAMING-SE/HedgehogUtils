using RoR2;
using UnityEngine;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using EntityStates;
using System;
using BepInEx.Configuration;
using RoR2.Hologram;
using HedgehogUtils.Forms;
using HedgehogUtils.Internal;
using UnityEngine.AddressableAssets;
using HedgehogUtils.Forms.SuperForm.EntityStates;
using HedgehogUtils.Miscellaneous;

namespace HedgehogUtils.Forms.SuperForm
{
    
    public sealed class ChaosEmeraldInteractable : NetworkBehaviour
    {
        public static DirectorPlacementRule placementRule = new DirectorPlacementRule
        {
            placementMode = DirectorPlacementRule.PlacementMode.Random
        };

        public static GameObject prefabBase;

        public static PurchaseInteraction purchaseInteractionBase;

        public static HologramProjector hologramBase;

        private static Vector3 dropVelocity = Vector3.up * 20;

        public PurchaseInteraction purchaseInteraction;

        public EntityStateMachine stateMachine;

        public PickupDisplay pickupDisplay;

        public SpecialObjectAttributes specialObjectAttributes;

        public PickupIndex pickupIndex;
        public UniquePickup uniquePickup;

        [SyncVar]
        public EmeraldColor color;


        public static void Initialize()
        {
            Log.Message("Starting Emerald Interactable Init");
            prefabBase = Assets.mainAssetBundle.LoadAsset<GameObject>("ChaosEmeraldInteractable");

            Assets.MaterialSwap(prefabBase, "RoR2/Base/Common/VFX/matInverseDistortion.mat", "RingParent/PurchaseParticle/Distortion");
            Assets.ReplaceRainbow(prefabBase.transform.Find("RingParent/PurchaseParticle/Rainbow"));

            Log.Message("Loaded Base");
            prefabBase.AddComponent<NetworkIdentity>();

            if (!prefabBase.TryGetComponent<RoR2.PurchaseInteraction>(out purchaseInteractionBase))
            {
                purchaseInteractionBase = prefabBase.AddComponent<RoR2.PurchaseInteraction>();
            }

            var ring = prefabBase.transform.Find("RingParent/Ring");
            var ring2 = prefabBase.transform.Find("RingParent/Ring/RingLOD");

            Log.Message("PurchaseInteraction added");

            prefabBase.GetComponent<Highlight>().targetRenderer = ring.GetComponent<MeshRenderer>();

            GameObject trigger = prefabBase.transform.Find("Trigger").gameObject;

            if (trigger.TryGetComponent<RoR2.EntityLocator>(out RoR2.EntityLocator locator))
            {
                locator.entity = prefabBase;
            }
            else
            {
                trigger.AddComponent<RoR2.EntityLocator>().entity = prefabBase;
            }

            Log.Message("Trigger done");

            hologramBase = prefabBase.AddComponent<RoR2.Hologram.HologramProjector>();
            hologramBase.hologramPivot = prefabBase.transform.Find("Hologram");
            hologramBase.displayDistance = 10;

            purchaseInteractionBase.available = true;
            UpdateInteractableCost();
            purchaseInteractionBase.automaticallyScaleCostWithDifficulty = true;
            purchaseInteractionBase.displayNameToken = HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_NAME";
            purchaseInteractionBase.contextToken = HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_CONTEXT";

            prefabBase.AddComponent<PingInfoProvider>().pingIconOverride = Assets.mainAssetBundle.LoadAsset<Sprite>("texEmeraldInteractableIcon");
            var pickupDisplayTransform = prefabBase.transform.Find("PickupDisplay");
            var pickupDisplay = pickupDisplayTransform.gameObject.AddComponent<PickupDisplay>();
            Log.Message("PickupDisplay done");

            ring.GetComponent<MeshRenderer>().material = Assets.ringMaterial;
            ring2.GetComponent<MeshRenderer>().material = Assets.ringMaterial;

            Log.Message("Material Done");
            
            prefabBase.AddComponent<ChaosEmeraldInteractable>();

            var entityStateMachine = prefabBase.AddComponent<EntityStateMachine>();
            entityStateMachine.customName = "Body";
            entityStateMachine.initialStateType = new SerializableEntityStateType(typeof(EntityState));
            entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(EntityState));

            var networkStateMachine = prefabBase.AddComponent<NetworkStateMachine>();
            Helpers.Append(ref networkStateMachine.stateMachines, new List<EntityStateMachine> { entityStateMachine });

            var inspect = ScriptableObject.CreateInstance<InspectDef>();
            var info = inspect.Info = new RoR2.UI.InspectInfo();

            info.Visual = Assets.mainAssetBundle.LoadAsset<Sprite>("texEmeraldInteractableIcon");
            info.DescriptionToken = HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_INSPECT";
            info.TitleToken = HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_TITLE";
            inspect.Info = info;

            var giip = prefabBase.gameObject.AddComponent<GenericInspectInfoProvider>();
            giip.InspectInfo = inspect;

            var drifter = prefabBase.gameObject.AddComponent<SpecialObjectAttributes>();
            drifter.hullClassification = HullClassification.Human;
            drifter.useSkillHighlightRenderers = true;
            List<Renderer> ringRenderers = [ring.GetComponent<Renderer>(), ring2.GetComponent<Renderer>()];
            drifter.skillHighlightRenderers = ringRenderers;
            drifter.renderersToDisable = ringRenderers;
            drifter.pickupDisplaysToDisable = [pickupDisplay];
            drifter.collisionToDisable = [prefabBase.transform.Find("Trigger").gameObject];
            drifter.childObjectsToDisable = [prefabBase.transform.Find("Hologram").gameObject];
            drifter.indicatorOffset = prefabBase.transform.Find("RingParent");
            drifter.maxDurability = 20;
            drifter.orientToFloor = false;
            drifter.breakoutState = new SerializableEntityStateType(typeof(InteractablePurchased));
            drifter.damageTypeOverride = DamageTypeCombo.Generic | DamageType.Stun1s;
            drifter.damageTypeOverride.AddModdedDamageType(DamageTypes.chaosSnapRandom);

            PrefabAPI.RegisterNetworkPrefab(prefabBase);

            //Content.AddNetworkedObjectPrefab(prefabBase);
        }

        public static void UpdateInteractableCost(object sender, EventArgs args)
        {
            UpdateInteractableCost();
        }

        private static void UpdateInteractableCost()
        {
            purchaseInteractionBase.cost = Config.EmeraldCost().Value;
        }

        private void Start()
        {
            Log.Message("Emerald Start");

            pickupDisplay = base.GetComponentInChildren<PickupDisplay>();
            purchaseInteraction = base.GetComponent<PurchaseInteraction>();
            HologramProjector hologram = base.GetComponent<HologramProjector>();
            specialObjectAttributes = base.GetComponent<SpecialObjectAttributes>();

            purchaseInteraction.costType = purchaseInteraction.cost == 0 ? CostTypeIndex.None : CostTypeIndex.Money;
            hologram.enabled = purchaseInteraction.cost != 0;

            stateMachine = EntityStateMachine.FindByCustomName(gameObject, "Body");

            if (NetworkServer.active)
            {
                purchaseInteraction.onDetailedPurchaseServer.AddListener(OnPurchase);
                int emeraldIndex = Run.instance.stageRng.RangeInt(0, SuperSonicHandler.available.Count);
                this.color = SuperSonicHandler.available[emeraldIndex];
                SuperSonicHandler.available.RemoveAt(emeraldIndex);
            }
            UpdateColor();

        }

        public PickupIndex GetPickupIndex()
        {
            switch (this.color)
            {
                default:
                    return PickupCatalog.FindPickupIndex(Items.yellowEmerald.itemIndex);
                case EmeraldColor.Blue:
                    return PickupCatalog.FindPickupIndex(Items.blueEmerald.itemIndex);
                case EmeraldColor.Red:
                    return PickupCatalog.FindPickupIndex(Items.redEmerald.itemIndex);
                case EmeraldColor.Gray:
                    return PickupCatalog.FindPickupIndex(Items.grayEmerald.itemIndex);
                case EmeraldColor.Green:
                    return PickupCatalog.FindPickupIndex(Items.greenEmerald.itemIndex);
                case EmeraldColor.Cyan:
                    return PickupCatalog.FindPickupIndex(Items.cyanEmerald.itemIndex);
                case EmeraldColor.Purple:
                    return PickupCatalog.FindPickupIndex(Items.purpleEmerald.itemIndex);
            }
        }

        public void UpdateColor()
        {
            if (purchaseInteraction)
            {
                purchaseInteraction.displayNameToken = HedgehogUtilsPlugin.Prefix + "EMERALD_TEMPLE_" + this.color.ToString().ToUpper();
            }

            pickupIndex = GetPickupIndex();
            uniquePickup = new UniquePickup(pickupIndex);
            pickupDisplay.SetPickup(uniquePickup);
        }

        public void OnPurchase(CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults results)
        {
            purchaseInteraction.SetAvailable(false);
            specialObjectAttributes.grabbable = false;
            this.stateMachine.SetNextState(new InteractablePurchased());
        }

        public void DropPickup()
        {
            if (!NetworkServer.active)
            {
                //Debug.LogWarning("[Server] function 'ChaosEmeraldInteractable::DropPickup()' called on client");
                return;
            }
            PickupDropletController.CreatePickupDroplet(uniquePickup, (pickupDisplay.transform).position, base.transform.TransformVector(dropVelocity), false, false);
        }

        public void Disappear()
        {
            pickupDisplay.DestroyModel();
            gameObject.transform.Find("Trigger").gameObject.SetActive(false);
            gameObject.transform.Find("RingParent/Ring").gameObject.SetActive(false);
        }

        public enum EmeraldColor : byte
        {
            Yellow = 0,
            Blue = 1,
            Red = 2,
            Gray = 3,
            Green = 4,
            Cyan = 5,
            Purple = 6
        }

        // I don't know why the commented out parts of the OnSerialize and OnDeserialize methods break the OnPurchase syncing and animation.
        // I have no idea why, but it doesn't really matter since those lines are only for Chaos Emeralds changing color dynamically, which can't happen anyway :)
        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write((byte)color);
                return true;
            }
            bool flag = false;
            /*if ((base.syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write((byte)color);
            }
            */
            return flag;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                this.color = (EmeraldColor)reader.ReadByte();
                return;
            }
            /*int num = (int)reader.ReadPackedUInt32();
            if ((num & 1U) != 0U)
            {
                this.color = (EmeraldColor)reader.ReadByte();
            }
            */
        }

    }
}