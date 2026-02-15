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
        public interface IRequiresFormSkillDef
        {
            FormDef requiredForm { get; set; }
        }

        public class RequiresFormSkillDef : SkillDef, IRequiresFormSkillDef
        {
            public FormDef requiredForm { get; set; }
        }
    }
}
