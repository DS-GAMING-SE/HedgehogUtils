using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using RoR2;
using RoR2.Skills;

namespace HedgehogUtils.Forms
{
    public class SkillDefs
    {
        /*public interface IRequiresFormSkillDef
        {
            FormDef requiredForm { get; set; }
            GenericSkill skillSlot { get; set; }
            object source { get; set; }
            GenericSkill.SkillOverridePriority priority { get; set; }
        }*/
        
        public class RequiresFormSkillDef : SkillDef
        {
            public FormDef requiredForm;

            private GenericSkill skillSlot;
            private object source;
            private GenericSkill.SkillOverridePriority priority;
            
            public override SkillDef.BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                InstanceData instanceData = new RequiresFormSkillDef.InstanceData
                {
                    formComponent = skillSlot.GetComponent<FormComponent>()
                };

                this.skillSlot = skillSlot;
                this.source = skillSlot.skillOverrides[skillSlot.currentSkillOverride].source;
                this.priority = skillSlot.skillOverrides[skillSlot.currentSkillOverride].priority;

                if (instanceData.formComponent.activeForm != requiredForm)
                {
                    skillSlot.UnsetSkillOverride(source, this, priority);
                }
                else
                {
                    instanceData.formComponent.OnFormChanged += OnFormChanged;
                }

                return instanceData;
            }

            /*public static SkillDef.BaseSkillInstanceData OnAssigned<T>(T skillDef, [NotNull] GenericSkill skillSlot) where T : SkillDef, IRequiresFormSkillDef
            {
                InstanceData instanceData = new RequiresFormSkillDef.InstanceData
                {
                    formComponent = skillSlot.GetComponent<FormComponent>()
                };

                skillDef.skillSlot = skillSlot;
                skillDef.source = skillSlot.skillOverrides[skillSlot.currentSkillOverride].source;
                skillDef.priority = skillSlot.skillOverrides[skillSlot.currentSkillOverride].priority;

                if (instanceData.formComponent.activeForm != skillDef.requiredForm)
                {
                    skillSlot.UnsetSkillOverride(skillDef.source, skillDef, skillDef.priority);
                }
                else
                {
                    instanceData.formComponent.OnFormChanged += OnFormChanged;
                }

                return instanceData;
            }*/

            public override void OnUnassigned([NotNull] GenericSkill skillSlot)
            {
                if (skillSlot.skillInstanceData != null && ((InstanceData)skillSlot.skillInstanceData).formComponent)
                {
                    ((InstanceData)skillSlot.skillInstanceData).formComponent.OnFormChanged -= OnFormChanged;
                }
            }

            public void OnFormChanged(FormDef previous, FormDef newForm)
            {
                if (newForm != requiredForm)
                {
                    skillSlot.UnsetSkillOverride(source, this, priority);
                }
            }

            /*public static void OnFormChanged<T>(T skillDef, [NotNull] GenericSkill skillSlot, FormDef previous, FormDef newForm) where T : SkillDef, IRequiresFormSkillDef
            {
                if (newForm != skillDef.requiredForm)
                {
                    skillSlot.UnsetSkillOverride(skillDef.source, skillDef, skillDef.priority);
                }
            }*/

            protected class InstanceData : SkillDef.BaseSkillInstanceData
            {
                public FormComponent formComponent;
            }
        }
    }
}
