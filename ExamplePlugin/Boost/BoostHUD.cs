using EntityStates;
using RoR2.UI;
using RoR2;
using RoR2.Audio;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using BepInEx.Configuration;

namespace HedgehogUtils.Boost
{
    public class BoostHUD : MonoBehaviour
    {
        //thanks red mist
        private BoostLogic boostLogic;
        private CharacterBody body;
        private GameObject lastBodyObject;
        private SkillDefs.IBoostSkill boostSkillDef;
        protected bool boostHudActive;

        public GameObject boostMeter;

        public Image meterBackground;
        public Image meterFill;

        public Image meterBackgroundOuter;
        public Image meterFillOuter;

        public RawImage meterBackgroundBackup;
        public RawImage meterFillBackup;

        public Image infiniteFill;
        public Image infiniteBackground;

        private Color fillUnavailableColor = new Color(0.8f, 0, 0, 1);
        private Color backgroundDefaultColor = new Color (0, 0, 0, 0.5f);

        private HUD hud;
        private float fadeTimer;

        private int backupBackgroundNum;
        private int backupFillNum;

        private void Awake()
        {
            this.hud = base.GetComponent<HUD>();
            if (HedgehogUtilsPlugin.riskOfOptionsLoaded)
            {
                ConfigEntry<float> configX = Config.BoostMeterLocationX();
                ConfigEntry<float> configY = Config.BoostMeterLocationY();
                configX.SettingChanged += (orig, self) => { this.boostMeter.GetComponent<RectTransform>().anchoredPosition = new Vector2(configX.Value, configY.Value); };
                configY.SettingChanged += (orig, self) => { this.boostMeter.GetComponent<RectTransform>().anchoredPosition = new Vector2(configX.Value, configY.Value); };
            }
        }
        public virtual void PrepareBoostMeter()
        {
            RectTransform rectTransform = this.boostMeter.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.localScale = new Vector3(1f, 1f, 1f);
            rectTransform.localPosition = Vector3.zero;

            rectTransform.anchoredPosition = new Vector2(Config.BoostMeterLocationX().Value, Config.BoostMeterLocationY().Value);

            this.meterBackground = this.boostMeter.transform.Find("Background").gameObject.GetComponent<Image>();
            this.meterFill = this.boostMeter.transform.Find("Background/Fill").gameObject.GetComponent<Image>();
            this.meterBackgroundOuter = this.boostMeter.transform.Find("BackgroundOuter").gameObject.GetComponent<Image>();
            this.meterFillOuter = this.boostMeter.transform.Find("BackgroundOuter/FillOuter").gameObject.GetComponent<Image>();
            this.meterBackgroundBackup = this.boostMeter.transform.Find("BackgroundBackup").gameObject.GetComponent<RawImage>();
            this.meterFillBackup = this.boostMeter.transform.Find("BackgroundBackup/FillBackup").gameObject.GetComponent<RawImage>();
            this.infiniteBackground = this.boostMeter.transform.Find("InfiniteBackground").gameObject.GetComponent<Image>();
            this.infiniteFill = this.boostMeter.transform.Find("InfiniteBackground/InfiniteFill").gameObject.GetComponent<Image>();
        }
        public virtual void Update()
        {
            if (!this.hud.targetBodyObject)
            {
                this.boostMeter.gameObject.SetActive(false);
                return;
            }
            if (this.hud.targetBodyObject != lastBodyObject)
            {
                if (body && body.skillLocator && body.skillLocator.utility)
                {
                    body.skillLocator.utility.onSkillChanged -= OnSkillChanged;
                }
                boostLogic = this.hud.targetBodyObject.GetComponent<BoostLogic>();
                body = this.hud.targetBodyObject.GetComponent<CharacterBody>();
                if (body && body.skillLocator && body.skillLocator.utility)
                {
                    body.skillLocator.utility.onSkillChanged += OnSkillChanged;
                    BoostSkill();
                }
            }
            lastBodyObject = this.hud.targetBodyObject;
            if (boostLogic && boostLogic.boostExists)
            {
                if (this.boostMeter)
                {
                    if (!this.boostHudActive)
                    {
                        this.boostHudActive = true;
                        PrepareBoostMeter();
                    }
                    BoostMeterVisuals();
                    return;
                }
            }
            else if (this.boostMeter)
            {
                this.boostMeter.gameObject.SetActive(false);
            }
        }

        public virtual void BoostMeterVisuals()
        {
            this.boostMeter.gameObject.SetActive(true);
            UpdateMeterBackground();
            UpdateMeterFill();
            UpdateMeterFading();
        }

        public virtual void UpdateMeterFading()
        {
            if (boostLogic.boostMeter >= boostLogic.maxBoostMeter && !boostLogic.boostBeingUsed)
            {
                meterFill.fillAmount = 1;
                fadeTimer += Time.fixedDeltaTime;
                if (boostSkillDef == null) { Log.Warning("No skill"); }
                Color fill = Color.Lerp(boostSkillDef.boostHUDColor, boostSkillDef.boostHUDColor.AlphaMultiplied(0), fadeTimer);
                Color background = Color.Lerp(backgroundDefaultColor, new Color(0, 0, 0, 0), fadeTimer);
                if (boostLogic.boostRegen < boostLogic.boostMeterDrain && !boostLogic.alwaysMaxBoost)
                {
                    meterBackground.gameObject.SetActive(true);
                    meterFill.color = fill;
                    meterBackground.color = background;
                    infiniteBackground.gameObject.SetActive(false);
                }
                else
                {
                    infiniteBackground.gameObject.SetActive(true);
                    infiniteFill.color = fill;
                    infiniteBackground.color = background;
                    meterBackground.gameObject.SetActive(false);
                }
                meterFillOuter.color = fill;
                meterBackgroundOuter.color = background;
                meterFillBackup.color = fill;
                meterBackgroundBackup.color = background;

            }
            else
            {
                fadeTimer = 0;
                if (boostLogic.boostRegen < boostLogic.boostMeterDrain && !boostLogic.alwaysMaxBoost)
                {
                    meterBackground.gameObject.SetActive(true);
                    meterFill.color = boostLogic.boostAvailable ? boostSkillDef.boostHUDColor : fillUnavailableColor;
                    meterBackground.color = backgroundDefaultColor;
                    infiniteBackground.gameObject.SetActive(false);
                }
                else
                {
                    infiniteBackground.gameObject.SetActive(true);
                    infiniteFill.color = boostSkillDef.boostHUDColor;
                    infiniteBackground.color = backgroundDefaultColor;
                    meterBackground.gameObject.SetActive(false);
                }
                meterFillOuter.color = boostLogic.boostAvailable ? boostSkillDef.boostHUDColor : fillUnavailableColor;
                meterBackgroundOuter.color = backgroundDefaultColor;
                meterFillBackup.color = boostLogic.boostAvailable ? boostSkillDef.boostHUDColor : fillUnavailableColor;
                meterBackgroundBackup.color = backgroundDefaultColor;
            }
        }

        public virtual void UpdateMeterFill()
        {
            meterFill.fillAmount = boostLogic.predictedMeter / 100;

            meterFillOuter.fillAmount = ((boostLogic.predictedMeter - 100) % 100) / 100;
            if (boostLogic.maxBoostMeter>100 && boostLogic.maxBoostMeter%100==0 && boostLogic.predictedMeter>=boostLogic.maxBoostMeter)
            {
                meterFillOuter.fillAmount = 1;
            }

            backupFillNum = Math.Max(Mathf.CeilToInt((boostLogic.predictedMeter - 200) / 100),0);
            if (backupFillNum==0)
            {
                meterFillBackup.gameObject.SetActive(false);
            }
            else
            {
                meterFillBackup.uvRect = new Rect(meterBackgroundBackup.uvRect.x, meterBackgroundBackup.uvRect.y, backupFillNum, meterBackgroundBackup.uvRect.height);
                meterFillBackup.gameObject.SetActive(true);
                meterFillBackup.gameObject.GetComponent<RectTransform>().localScale = new Vector3((float)backupFillNum/backupBackgroundNum, 1, 1);
            }
        }

        public virtual void UpdateMeterBackground()
        {
            meterBackground.fillAmount = boostLogic.maxBoostMeter / 100;

            meterBackgroundOuter.fillAmount = ((boostLogic.maxBoostMeter - 100) / 100) - Mathf.Max((Mathf.Floor((boostLogic.predictedMeter - 100) / 100)),0);

            backupBackgroundNum = Math.Max(Mathf.CeilToInt((boostLogic.maxBoostMeter - 200) / 100), 0);
            if (backupBackgroundNum == 0)
            {
                meterBackgroundBackup.gameObject.SetActive(false);
            }
            else
            {
                meterBackgroundBackup.uvRect = new Rect(meterBackgroundBackup.uvRect.x, meterBackgroundBackup.uvRect.y, backupBackgroundNum, meterBackgroundBackup.uvRect.height);
                meterBackgroundBackup.gameObject.SetActive(true);
                meterBackgroundBackup.gameObject.GetComponent<RectTransform>().localScale = new Vector3(0.3f * backupBackgroundNum, 0.3f, 0.3f);
            }
        }
        public void OnSkillChanged(GenericSkill skill)
        {
            BoostSkill();
        }

        private void BoostSkill()
        {
            if (typeof(SkillDefs.IBoostSkill).IsAssignableFrom(body.skillLocator.utility.skillDef.GetType()))
            {
                boostSkillDef = body.skillLocator.utility.skillDef as SkillDefs.IBoostSkill;
            }
        }
    }
}