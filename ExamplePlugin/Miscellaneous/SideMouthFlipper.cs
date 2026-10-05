using RoR2;
using UnityEngine;
using HedgehogUtils.Forms;

namespace HedgehogUtils.Miscellaneous
{
    public class SideMouthFlipper : MonoBehaviour
    {
        public Transform mouth;
        private float mouthDefaultScaleX;

        public bool mouthDefaultRight;
        public bool currentSideIsRight { get; private set; }

        public const float sideChangeDot = 0.25f;


        public void Start()
        {
            if (!mouth)
            {
                Log.Error("SideMouthFlipper mouth transform does not exist");
                this.enabled = false;
                return;
            }
            mouthDefaultScaleX = mouth.transform.localScale.x;
            SceneCamera.onSceneCameraPreRender += UpdateMouthSide;
        }
        public void UpdateMouthSide(SceneCamera camera)
        {
            if (Vector3.Dot(camera.transform.forward, currentSideIsRight ? mouth.right : -mouth.right) >= sideChangeDot)
            {
                currentSideIsRight = !currentSideIsRight;
                SetMouthSide(mouthDefaultRight == currentSideIsRight);
            }
        }
        public void SetMouthSide(bool isDefaultSide)
        {
            mouth.transform.localScale.Set(isDefaultSide ? mouthDefaultScaleX : -mouthDefaultScaleX, mouth.transform.localScale.y, mouth.transform.localScale.z);
        }
    }
}