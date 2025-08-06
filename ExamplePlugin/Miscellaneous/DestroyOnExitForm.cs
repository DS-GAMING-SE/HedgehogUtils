using RoR2;
using UnityEngine;
using HedgehogUtils.Forms;

namespace HedgehogUtils.Miscellaneous
{
    public class DestroyOnExitForm : MonoBehaviour
    {
        protected FormComponent formComponent;

        protected EffectComponent effectComponent;

        public FormDef neededForm;

        private void Start()
        {
            effectComponent = base.GetComponent<EffectComponent>();
            if (effectComponent && effectComponent.effectData != null && effectComponent.effectData.rootObject)
            {
                formComponent = effectComponent.effectData.rootObject.GetComponent<FormComponent>();
                if (formComponent)
                {
                    formComponent.OnFormChanged += FormChanged;
                }
            }
        }

        private void FormChanged(FormDef previous, FormDef current)
        {
            if (current != neededForm)
            {
                Destroy(base.gameObject);
            }
        }
    }
}