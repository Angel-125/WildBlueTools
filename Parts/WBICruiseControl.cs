using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public struct WBICruisePropellant
    {
        public string name;
        public int resourceID;
        public double density;
        public double requestMass;
        public double requestUnits;
    }

    [KSPModule("Cruise Control")]
    public class WBICruiseControl: PartModule
    {
        const float kBasePowerLevel = 0.5f;
        const string kNoSubOrbitMsg = "Cruise Control isn't supported while the vessel is sub-orbital.";
        const float kMessageDuration = 5.0f;

        [KSPField]
        public bool debugMode = false;

        [KSPField(guiName = "Cruise Control", isPersistant = true, guiActive = true, guiActiveEditor = false)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool cruiseControlIsActive;

        [KSPField(guiName = "Cruise Throttle", isPersistant = true, guiActive = true, guiActiveEditor = false)]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0f)]
        public float cruiseThrottle;

        [KSPField(guiName = "Fuel Reserve (%)", isPersistant = true, guiActive = true, guiActiveEditor = false)]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 10f)]
        public float fuelReservePercent = 0f;

        /// <summary>
        /// This is a cheat that essentially multiplies the mass flow rate used to calculate delta v during timewarp.
        /// All it does is let you burn your fuel faster than normal, thus creating more delta v faster.
        /// It does NOT give you extra resources, it just lets you use them more quickly.
        /// </summary>
        [KSPField]
        public float fuelBurnMultiplier = 1.0f;

        [KSPField(isPersistant = true)]
        public bool originalBreakableJoints;

        [KSPField(isPersistant = true)]
        public bool cheatOptionSet;

        [KSPField]
        public double totalMass;

        [KSPField]
        public double fuelMass;

        [KSPField]
        public double finalMass;

        [KSPField]
        public double deltaV;

        public float lastThrottle;
        public float lastFuelReserve;
        public bool wasActive;

        protected ModuleEnginesFX engine;
        protected MultiModeEngine engineSwitcher;
        protected Dictionary<string, ModuleEnginesFX> multiModeEngines = new Dictionary<string, ModuleEnginesFX>();
        protected WBICruisePropellant[] propellants;

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine("Can perform engine burns during timewarp.");
            info.AppendLine(" ");
            info.AppendLine("<b>Cruise Control:</b> Must be ON before entering timewarp burn.");
            info.AppendLine(" ");
            info.AppendLine("<b>Cruise Throttle:</b> % of max thrust to use during timewarp burn.");
            info.AppendLine(" ");
            info.AppendLine("<b>Fuel Reserve:</b> % of fuel remaining before halting timewarp burn.");
            info.AppendLine(" ");
            info.AppendLine("Set cruise throttle and fuel reserve before timewarping.");
            info.AppendLine("Cannot adjust controls during timewarp.");

            return info.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get state
            lastThrottle = cruiseThrottle;
            lastFuelReserve = fuelReservePercent;
            wasActive = cruiseControlIsActive;

            Fields["totalMass"].guiActive = debugMode;
            Fields["fuelMass"].guiActive = debugMode;
            Fields["finalMass"].guiActive = debugMode;
            Fields["deltaV"].guiActive = debugMode;

            setupEngines();
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //Update cheat option
            if (cruiseControlIsActive && !cheatOptionSet)
            {
                cheatOptionSet = true;
                originalBreakableJoints = CheatOptions.UnbreakableJoints;
                CheatOptions.UnbreakableJoints = true;
            }
            else if (!cruiseControlIsActive && cheatOptionSet)
            {
                CheatOptions.UnbreakableJoints = originalBreakableJoints;
                cheatOptionSet = false;
            }

            //Update symmetry parts with cruise control state and throttle.
            if (cruiseControlIsActive != wasActive || fuelReservePercent != lastFuelReserve || cruiseThrottle != lastThrottle)
            {
                lastThrottle = cruiseThrottle;
                lastFuelReserve = fuelReservePercent;
                wasActive = cruiseControlIsActive;

                WBICruiseControl cruiseControl;
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    cruiseControl = symmetryPart.FindModuleImplementing<WBICruiseControl>();
                    cruiseControl.cruiseControlIsActive = cruiseControlIsActive;
                    cruiseControl.cruiseThrottle = cruiseThrottle;
                    cruiseControl.fuelReservePercent = fuelReservePercent;
                    cruiseControl.cheatOptionSet = cheatOptionSet;
                    cruiseControl.originalBreakableJoints = originalBreakableJoints;
                }
            }

            //Get current engine
            getCurrentEngine();
            if (propellants == null)
                return;

            //Check operation status
            bool guiActive = engine.isOperational & engine.EngineIgnited;
            Fields["cruiseControlIsActive"].guiActive = guiActive;
            Fields["cruiseThrottle"].guiActive = guiActive;
            Fields["fuelReservePercent"].guiActive = guiActive;

            //Make sure cruise control is active and we're in timewarp.
            if (!cruiseControlIsActive || TimeWarp.CurrentRateIndex == 0)
            {
                totalMass = 0;
                fuelMass = 0;
                finalMass = 0;
                deltaV = 0;
                this.part.vessel.IgnoreGForces(500);
                return;
            }

            //Make sure we're not suborbital
            if (this.part.vessel.situation == Vessel.Situations.SUB_ORBITAL)
            {
                ScreenMessages.PostScreenMessage(kNoSubOrbitMsg, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                cruiseControlIsActive = false;
                return;
            }

            float throttleRatio = cruiseThrottle / 100.0f;
            this.part.Effect(engine.runningEffectName, kBasePowerLevel * throttleRatio);

            //Get engine ISP
            float isp = engine.realIsp;

            //Request fuel mass
            fuelMass = getFuelMass();
            if (!cruiseControlIsActive)
                return;

            //Calculate delta-v
            totalMass = this.part.vessel.GetTotalMass();
            finalMass = totalMass - fuelMass;
            deltaV = isp * 9.81 * Math.Log(totalMass / finalMass);

            //Adjust orbit
            double currentTime = Planetarium.GetUniversalTime();
            Vector3d upVector = this.part.transform.up;
            upVector *= deltaV;
            Orbit orbit = this.part.vessel.orbit;
            Vector3d position = orbit.getRelativePositionAtUT(currentTime);
            orbit.UpdateFromStateVectors(position, orbit.getOrbitalVelocityAtUT(currentTime) + upVector.xzy, orbit.referenceBody, currentTime);
            orbit.Init();
            orbit.UpdateFromUT(currentTime);

            //Apply heat - Doesn't seem to work in timewarp
//            if (TimeWarp.CurrentRate < 1000)
//                this.part.AddThermalFlux(engine.heatProduction * TimeWarp.fixedDeltaTime);
        }

        protected void getCurrentEngine()
        {
            ModuleEnginesFX currentEngine = null;

            //If we have multiple engines, make sure we have the current one.
            if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    currentEngine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    currentEngine = multiModeEngines[engineSwitcher.secondaryEngineID];
            }

            //Setup propellants if needed
            if (currentEngine != engine)
                getPropellants();

            engine = currentEngine;
        }

        protected void setupEngines()
        {
            //See if we have multiple engines that we need to support
            engineSwitcher = this.part.FindModuleImplementing<MultiModeEngine>();
            List<ModuleEnginesFX> engines = this.part.FindModulesImplementing<ModuleEnginesFX>();
            ModuleEnginesFX moduleEngine = null;

            //Find all the engines in the part and record their properties.
            for (int index = 0; index < engines.Count; index++)
            {
                moduleEngine = engines[index];
                multiModeEngines.Add(moduleEngine.engineID, moduleEngine);
            }

            //Get whichever multimode engine is the active one.
            if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];
            }

            //Just get the first engine in the list.
            else if (engines.Count > 0)
            {
                engine = multiModeEngines.Values.ToArray()[0];
            }

            //Get propellants
            getPropellants();
        }

        protected double getFuelMass()
        {
            double unitsObtained;
            double maxAmount;
            double demand;
            double fuelMass = 0;
            WBICruisePropellant propellant;
            float reserveRatio = fuelReservePercent / 100.0f;

            for (int index = 0; index < propellants.Length; index++)
            {
                propellant = propellants[index];

                //Make sure we haven't dipped below our fuel reserve
                FlightGlobals.ActiveVessel.rootPart.GetConnectedResourceTotals(propellant.resourceID, out unitsObtained, out maxAmount, true);
                if (unitsObtained / maxAmount <= reserveRatio)
                {
                    cruiseControlIsActive = false;
                    TimeWarp.SetRate(0, false);
                    return 0;
                }

                //Request the desired amount of units
                demand = propellant.requestUnits * TimeWarp.fixedDeltaTime * (cruiseThrottle / 100.0f) * fuelBurnMultiplier;
                unitsObtained = this.part.RequestResource(propellant.name, demand, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                if (unitsObtained / demand < 0.0001f)
                {
                    cruiseControlIsActive = false;
                    TimeWarp.SetRate(0, false);
                    return 0;
                }

                //Add the resource mass to the fuel mass
                fuelMass += unitsObtained * propellant.density;
            }

            return fuelMass;
        }

        protected void getPropellants()
        {
            PartResourceDefinition definition;
            WBICruisePropellant cruisePropellant;
            List<WBICruisePropellant> cruisePropellants = new List<WBICruisePropellant>();

            propellants = null;
            foreach (Propellant propellant in engine.propellants)
            {
                definition = PartResourceLibrary.Instance.GetDefinition(propellant.name);

                //Create a new cruise propellant
                cruisePropellant = new WBICruisePropellant();
                cruisePropellant.name = propellant.name;
                cruisePropellant.resourceID = propellant.id;
                cruisePropellant.density = definition.density;
                cruisePropellant.requestMass = definition.density * propellant.ratio;
                cruisePropellant.requestUnits = cruisePropellant.requestMass / definition.density;

                //Add to the list
                cruisePropellants.Add(cruisePropellant);
            }

            if (cruisePropellants.Count > 0)
                propellants = cruisePropellants.ToArray();
        }
    }
}
