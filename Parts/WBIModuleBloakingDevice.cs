using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WildBlueIndustries
{
    [KSPModule("Bloaking Device")]
    public class WBIModuleBloakingDevice : PartModule   
    {
        [KSPField()]
        public bool debugMode = true;

        [KSPField(guiName = "Bloaking Device", isPersistant = true, guiActive = true, guiActiveEditor = false)]
        [UI_Toggle(enabledText = "Bloaked!", disabledText = "Un-Bloaked!")]
        public bool isBloaking;

        [KSPField(guiName = "Bloak Level", isPersistant = true, guiActive = true, guiActiveEditor = false)]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.0f)]
        public float bloakLevel = 50f;

        [KSPField()]
        private float prevBloakLevel;

        [KSPField()]
        private int prevVesselPartCount;

        [KSPField()]
        private bool wasBloaking;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get the list of parts in the vessel
            prevBloakLevel = bloakLevel;
            prevVesselPartCount = part.vessel.parts.Count;
            wasBloaking = isBloaking;

            // Debug UI
            Fields["wasBloaking"].guiActive = debugMode;
            Fields["prevVesselPartCount"].guiActive = debugMode;
            Fields["prevBloakLevel"].guiActive = debugMode;

            UpdateOpacity();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Check bloak status change
            if (isBloaking != wasBloaking)
            {
                wasBloaking = isBloaking;
                UpdateOpacity();
                if (isBloaking)
                {
                    ScreenMessages.PostScreenMessage("Bloaked!", 5.0f, ScreenMessageStyle.UPPER_LEFT);
                }
                else
                {
                    ScreenMessages.PostScreenMessage("Un-Bloaked!", 5.0f, ScreenMessageStyle.UPPER_LEFT);
                }
            }

            // If we aren't bloaking then return
            if (!isBloaking)
            {
                return;
            }

            // Check E.C.
            if (!ConsumeResources())
            {
                isBloaking = false;
                wasBloaking = false;
                UpdateOpacity();
                ScreenMessages.PostScreenMessage("Un-Bloaked!", 5.0f, ScreenMessageStyle.UPPER_LEFT);
                return;
            }

            // Check changes in bloak level
            if (!bloakLevel.Equals(prevBloakLevel) || part.vessel.parts.Count != prevVesselPartCount)
            {
                prevVesselPartCount = part.vessel.parts.Count;
                prevBloakLevel = bloakLevel;
                UpdateOpacity();
            }
        }

        [KSPAction("Toggle Bloak", KSPActionGroup.None)]
        public void ToggleLightsAction(KSPActionParam param)
        {
            isBloaking = !isBloaking;
        }

        public bool ConsumeResources()
        {
            string errorStatus = string.Empty;
            int count = resHandler.inputResources.Count;
            float rateMultiplier = part.vessel.parts.Count * (1 - (bloakLevel / 100.0f));

            resHandler.UpdateModuleResourceInputs(ref errorStatus, rateMultiplier, 0.1, true, true);
            for (int index = 0; index < count; index++)
            {
                if (!resHandler.inputResources[index].available)
                    return false;
            }

            return true;
        }

        public void UpdateOpacity()
        {
            float opacity = isBloaking ? (bloakLevel / 100.0f) : 1.0f;
            int count = part.vessel.parts.Count;
            
            foreach (Part vesselPart in part.vessel.parts)
            {
                vesselPart.SetOpacity(opacity);
            }
        }
    }
}
