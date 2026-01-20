using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HedgehogUtils.Miscellaneous
{
    public class LockOnIndicator : MonoBehaviour
    {
        public SpriteRenderer main;
        public SpriteRenderer darken;
        public SpriteRenderer start;
        public SpriteRenderer nibTop;
        public SpriteRenderer nibLeft;
        public SpriteRenderer nibBottom;
        public SpriteRenderer nibRight;
        public ObjectScaleCurve[] scaleCurves;

        public Color color
        {
            set 
            {
                _color = value;
                main.color = _color;
                nibTop.color = _color;
                nibLeft.color = _color;
                nibBottom.color = _color;
                nibRight.color = _color;
            }
            get
            {
                return _color;
            }
        }
        private Color _color = Color.white;
        public Color secondaryColor
        {
            set
            {
                _secondaryColor = value;
                _secondaryColor.a *= 0.4f;
                start.color = _secondaryColor;
                darken.color = _secondaryColor;
                _secondaryColor = value;
            }
            get
            {
                return _secondaryColor;
            }
        }
        private Color _secondaryColor = Color.white;

        public void UpdateTarget()
        {
            foreach (var item in scaleCurves)
            {
                item.Reset();
            }
        }

        public void SetColors(Color color, Color secondaryColor)
        {
            this.color = color;
            this.secondaryColor = secondaryColor;
        }
    }
}
