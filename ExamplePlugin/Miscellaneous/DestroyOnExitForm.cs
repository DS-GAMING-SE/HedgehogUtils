using RoR2;
using UnityEngine;
using HedgehogUtils.Forms;

namespace HedgehogUtils.Miscellaneous
{
    public class DestroyOnExitForm : MonoBehaviour
    {
        protected FormComponent formComponent;

        protected EffectComponent effectComponent;

        protected EffectManagerHelper emh;

        public FormDef neededForm;

        private void Start()
        {
            effectComponent = base.GetComponent<EffectComponent>();
            emh = base.GetComponent<EffectManagerHelper>();
            if (emh)
            {
                emh.UnparentOnReturnToPool = true;
            }
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
                if (emh)
                {
                    emh.ReturnToPool();
                }
                else
                {
                    GameObject.Destroy(gameObject);
                }
            }
        }
    }
}