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

        [KSPField(isPersistant = true)]
        public float bloakLevel = 100f;

        [KSPField()]
        private int prevVesselPartCount;

        [KSPField()]
        private bool wasBloaking;

        float bloakAmount;
        float bloakEnd;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get the list of parts in the vessel
            prevVesselPartCount = part.vessel.parts.Count;
            wasBloaking = isBloaking;

            // Debug UI
            Fields["wasBloaking"].guiActive = debugMode;
            Fields["prevVesselPartCount"].guiActive = debugMode;
            Fields["bloakLevel"].guiActive = debugMode;

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
                if (isBloaking)
                {
                    bloakAmount = 0.02f;
                    bloakEnd = 0.05f;
                    ScreenMessages.PostScreenMessage("Bloaked!", 5.0f, ScreenMessageStyle.UPPER_LEFT);
                }
                else
                {
                    bloakAmount = 0.02f;
                    bloakEnd = 100f;
                    ScreenMessages.PostScreenMessage("Un-Bloaked!", 5.0f, ScreenMessageStyle.UPPER_LEFT);
                }
            }

            // If we aren't bloaking then return
            if (!isBloaking)
            {
                if (bloakLevel < 100f)
                {
                    bloakLevel = Mathf.Lerp(bloakLevel, bloakEnd, bloakAmount);
                    if (bloakLevel > 99.5)
                        bloakLevel = 100f;
                    UpdateOpacity();
                }
                return;
            }

            // Check E.C.
            if (!ConsumeResources())
            {
                isBloaking = false;
                return;
            }

            // Lerp down the bloak level
            if (bloakLevel > 0.05f)
            {
                bloakLevel = Mathf.Lerp(bloakLevel, bloakEnd, bloakAmount);
                if (bloakLevel < 0.055f)
                    bloakLevel = 0.05f;
                UpdateOpacity();
            }

            // Check changes in part count
            if (part.vessel.parts.Count != prevVesselPartCount)
            {
                prevVesselPartCount = part.vessel.parts.Count;
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
            float opacity = bloakLevel / 100.0f;
            int count = part.vessel.parts.Count;
            
            foreach (Part vesselPart in part.vessel.parts)
            {
                vesselPart.SetOpacity(opacity);
            }
        }
    }
}
