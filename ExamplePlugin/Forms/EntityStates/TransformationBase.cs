using EntityStates;
using RoR2;
using RoR2.Audio;
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace HedgehogUtils.Forms.EntityStates
{
    public class TransformationBase : BaseSkillState
    {
        public FormDef form;

        protected FormComponent formComponent;

        public bool fromTeamSuper = false;

        protected CharacterModel characterModel;

        public override void OnEnter()
        {
            base.OnEnter();
            Transform modelTransform = base.GetModelTransform();
            if (modelTransform)
            {
                this.characterModel = modelTransform.GetComponent<CharacterModel>();
            }
            this.formComponent= base.GetComponent<FormComponent>();
            if (base.isAuthority)
            {
                this.form = formComponent.targetedForm;
            }
        }

        public virtual void Transform()
        {
            if (base.isAuthority)
            {
                this.formComponent.SetNextForm(this.form);
            }
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(fromTeamSuper);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            fromTeamSuper = reader.ReadBoolean();
        }
    }
}