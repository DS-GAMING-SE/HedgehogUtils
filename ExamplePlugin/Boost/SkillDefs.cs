using EntityStates;
using HedgehogUtils.Boost.EntityStates;
using JetBrains.Annotations;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Boost
{
    public class SkillDefs
    {
        public interface IBoostSkill
        {
            public SerializableEntityStateType boostIdleState { get; set; }
            public SerializableEntityStateType brakeState { get; set; }
            public Color boostHUDColor { get; set; }
        }

        public class BoostSkillDef : SkillDef, IBoostSkill
        {
            public SerializableEntityStateType boostIdleState { get; set; }
            public SerializableEntityStateType brakeState { get; set; }

            public Color boostHUDColor { get; set; }

            public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                return new InstanceData { boostLogic = skillSlot.GetComponent<BoostLogic>() };
            }

            public override EntityState InstantiateNextState([NotNull] GenericSkill skillSlot)
            {
                if (!skillSlot.characterBody || !skillSlot.characterBody.characterMotor) { return base.InstantiateNextState(skillSlot); }
                return DetermineNextBoostState(skillSlot, activationState, boostIdleState);
            }
            public static EntityState DetermineNextBoostState([NotNull] GenericSkill skillSlot, SerializableEntityStateType boost, SerializableEntityStateType boostIdle)
            {
                ICharacterFlightParameterProvider flight = skillSlot.GetComponent<ICharacterFlightParameterProvider>();
                if (skillSlot.characterBody.characterMotor.isGrounded || Helpers.Flying(flight))
                {
                    if (skillSlot.characterBody.inputBank.moveVector == Vector3.zero)
                    {
                        return InstantiateState(skillSlot, boostIdle);
                    }
                    else
                    {
                        return InstantiateState(skillSlot, boost);
                    }
                }
                else
                {
                    return InstantiateAirBoost(skillSlot, boost);
                }
            }
            public static EntityState DetermineNextBoostState([NotNull] GenericSkill skillSlot, SerializableEntityStateType boost, SerializableEntityStateType boostIdle, SerializableEntityStateType airBoost)
            {
                ICharacterFlightParameterProvider flight = skillSlot.GetComponent<ICharacterFlightParameterProvider>();
                if (skillSlot.characterBody.characterMotor.isGrounded || Helpers.Flying(flight))
                {
                    if (skillSlot.characterBody.inputBank.moveVector == Vector3.zero)
                    {
                        return InstantiateState(skillSlot, boostIdle);
                    }
                    else
                    {
                        return InstantiateState(skillSlot, boost);
                    }
                }
                else
                {
                    return InstantiateState(skillSlot, airBoost);
                }
            }

            public override bool IsReady([NotNull] GenericSkill skillSlot)
            {
                return base.IsReady(skillSlot) && (!(skillSlot.skillInstanceData is InstanceData) || ((InstanceData)skillSlot.skillInstanceData).boostLogic.boostAvailable);
            }
            // Idk why I made these static methods for making entity states that are both exactly the same.
            // Probably a result of me changing things a bunch internally until the methods were no longer needed, but I didn't notice
            public static EntityState InstantiateBoostIdle(GenericSkill skillSlot, SerializableEntityStateType boostIdle)
            {
                return InstantiateState(skillSlot, boostIdle);
            }
            public static EntityState InstantiateBoost(GenericSkill skillSlot, SerializableEntityStateType boost)
            {
                return InstantiateState(skillSlot, boost);
            }
            public static EntityState InstantiateState(GenericSkill skillSlot, SerializableEntityStateType state)
            {
                EntityState entityState = EntityStateCatalog.InstantiateState(state.stateType);
                ISkillState skillState = entityState as ISkillState;
                if (skillState != null)
                {
                    skillState.activatorSkillSlot = skillSlot;
                }
                return entityState;
            }
            public static EntityState InstantiateAirBoost(GenericSkill skillSlot, SerializableEntityStateType boost)
            {
                EntityState entityState = EntityStateCatalog.InstantiateState(boost.stateType);
                ISkillState skillState = entityState as ISkillState;
                if (skillState != null)
                {
                    skillState.activatorSkillSlot = skillSlot;
                }
                if (typeof(EntityStates.Boost).IsAssignableFrom(boost.stateType))
                {
                    ((EntityStates.Boost)entityState).airBoosting = true;
                }
                return entityState;
            }

            public class InstanceData : BaseSkillInstanceData
            {
                public BoostLogic boostLogic;
            }
        }

        public class RequiresFormBoostSkillDef : Forms.SkillDefs.RequiresFormSkillDef, IBoostSkill
        {
            public SerializableEntityStateType boostIdleState { get; set; }
            public SerializableEntityStateType brakeState { get; set; }
            public Color boostHUDColor { get; set; }

            public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                InstanceData oldData = (InstanceData)base.OnAssigned(skillSlot);
                return new BoostInstanceData { boostLogic = skillSlot.GetComponent<BoostLogic>(), formComponent = oldData.formComponent };
            }

            public override EntityState InstantiateNextState([NotNull] GenericSkill skillSlot)
            {
                if (!skillSlot.characterBody || !skillSlot.characterBody.characterMotor) { return base.InstantiateNextState(skillSlot); }
                return BoostSkillDef.DetermineNextBoostState(skillSlot, activationState, boostIdleState);
            }
            public override bool IsReady([NotNull] GenericSkill skillSlot)
            {
                return base.IsReady(skillSlot) && (!(skillSlot.skillInstanceData is BoostInstanceData) || ((BoostInstanceData)skillSlot.skillInstanceData).boostLogic.boostAvailable);
            }

            protected class BoostInstanceData : InstanceData
            {
                public BoostLogic boostLogic;
            }
        }
    }
}
