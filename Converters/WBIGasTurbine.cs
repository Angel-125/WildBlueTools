using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyright 2017, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIGasTurbine : ModuleResourceConverter
    {
        const float kBaseEfficiencyBonus = 0.01f;
        const float kBaseSpoolPowerLevel = 0.1f;
        const float kBaseSpoolTurbine = 0.1f;

        enum ETurbineStates
        {
            Off,
            SpoolingUp,
            SpoolingDown,
            ThrottleControlled
        }

        public KSPActionGroup defaultActionGroup;
        [KSPField(guiName = "Output Limit", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 10f)]
        public float outputPercent = 100.0f;

        [KSPField(guiName = "Power Output", guiActive = true, guiUnits = "EC/sec", guiFormat = "F2")]
        public double powerOutput = 0f;

        [KSPField()]
        public string smokeEffect = string.Empty;

        [KSPField()]
        public string turbineEffect = string.Empty;

        [KSPField()]
        public string startEffect = string.Empty;

        [KSPField()]
        public string stopEffect = string.Empty;

        [KSPField()]
        public string flameoutEffect = string.Empty;

        [KSPField()]
        public float spoolTime = 8.0f;

        [KSPField()]
        public string fuelGaugeResource = "LiquidFuel";

        [KSPField()]
        public string stagingIconURL = string.Empty;

        [KSPField(isPersistant = true)]
        public float powerLevel = 0;

        [KSPField(isPersistant = true)]
        public int turbineStateID = 0;

        //Optional stuff
        [KSPField()]
        public string turbineTransformName = string.Empty;

        [KSPField()]
        public string turbineAxis = string.Empty;

        [KSPField()]
        public float turbineMaxRPS = 30.0f;

        [KSPField()]
        public float turbineMinRPS = 2.0f;

        protected float maxTotalFuelGaugeResource = 0f;
        protected double ecBaseOutput;
        protected StageIconInfoBox infoBox = null;
        protected ResourceBroker resourceBroker;
        protected float lastOutputPercent;
        Transform turbineTransform;
        Vector3 turbineRotationAxis;
        ETurbineStates turbineState = ETurbineStates.Off;
        float turbineSpinFactor = 0f;
        float lastThrottleSetting = 0f;

        [KSPAction()]
        public void ToggleGeneratorAction(KSPActionParam param)
        {
            ToggleGenerator();
        }

        [KSPEvent(guiActive = true)]
        public void ToggleGenerator()
        {
            if (turbineState == ETurbineStates.Off || turbineState == ETurbineStates.SpoolingDown)
                TurnOnGenerator();
            else
                TurnOffGenerator();
        }

        public void TurnOnGenerator()
        {
            lastThrottleSetting = FlightInputHandler.state.mainThrottle;

            turbineState = ETurbineStates.SpoolingUp;
            turbineStateID = (int)turbineState;
            if (!string.IsNullOrEmpty(startEffect))
                this.part.Effect(startEffect, 1.0f);

            //Setup info box
            if (infoBox != null)
                infoBox.Expand();

            setupGUI();
        }

        public void TurnOffGenerator()
        {
            turbineState = ETurbineStates.SpoolingDown;
            turbineStateID = (int)turbineState;
            if (!string.IsNullOrEmpty(stopEffect))
                this.part.Effect(stopEffect, 1.0f);

            //Clear info box
            if (infoBox != null)
                infoBox.Collapse();

            setupGUI();
        }

        public void SetOutputPercent(float newOutputPercent)
        {
            outputPercent = newOutputPercent;
            lastOutputPercent = outputPercent;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            turbineState = (ETurbineStates)turbineStateID;
            GameEvents.onStageActivate.Add(OnStageActivate);
            GameEvents.onVesselWasModified.Add(OnVesselModified);
            GameEvents.onVesselChange.Add(OnVesselChange);

            setupGUI();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Optional turbine transform
            if (!string.IsNullOrEmpty(turbineTransformName) && !string.IsNullOrEmpty(turbineAxis))
            {
                turbineTransform = this.part.FindModelTransform(turbineTransformName);

                string[] axisValues = turbineAxis.Split(',');
                float value;
                if (axisValues.Length == 3)
                {
                    if (float.TryParse(axisValues[0], out value))
                        turbineRotationAxis.x = value;
                    if (float.TryParse(axisValues[1], out value))
                        turbineRotationAxis.y = value;
                    if (float.TryParse(axisValues[2], out value))
                        turbineRotationAxis.z = value;
                }
            }

            //Output percent
            lastOutputPercent = outputPercent;

            //Power output display
            ResourceRatio[] outputs = outputList.ToArray();
            ResourceRatio output;
            for (int index = 0; index < outputs.Length; index++)
            {
                output = outputs[index];
                if (output.ResourceName == "ElectricCharge")
                {
                    ecBaseOutput = output.Ratio;
                    break;
                }
            }

            //Info box
            setupInfoBox();
        }

        public void Destroy()
        {
            GameEvents.onStageActivate.Remove(OnStageActivate);
            GameEvents.onVesselWasModified.Remove(OnVesselModified);
            GameEvents.onVesselChange.Remove(OnVesselChange);
        }

        public void OnStageActivate(int stageID)
        {
            if (stageID == this.part.inverseStage && (turbineState == ETurbineStates.Off || turbineState == ETurbineStates.SpoolingDown))
                TurnOnGenerator();
        }

        public void OnVesselChange(Vessel ves)
        {
            if (ves != this.part.vessel)
                return;

            setupInfoBox();
            if (infoBox != null && IsActivated)
                infoBox.Expand();
        }

        public void OnVesselModified(Vessel ves)
        {
            getMaxTotalFuel();
        }

        public override void OnUpdate()
        {
            WBIGasTurbine gasTurbine = null;

            base.OnUpdate();

            //Play effects
            if (turbineState != ETurbineStates.Off)
            {
                if (!string.IsNullOrEmpty(smokeEffect))
                    this.part.Effect(smokeEffect, powerLevel);
                if (!string.IsNullOrEmpty(turbineEffect))
                    this.part.Effect(turbineEffect, powerLevel);
            }

            //output percent symmetry
            if (outputPercent != lastOutputPercent)
            {
                lastOutputPercent = outputPercent;

                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    gasTurbine = symmetryPart.FindModuleImplementing<WBIGasTurbine>();
                    gasTurbine.SetOutputPercent(outputPercent);
                }
            }
        }

        protected override void PreProcessing()
        {
            base.PreProcessing();
            float bonusEfficiency = 0f;
            float outputLimit = outputPercent / 100.0f;

            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (turbineState == ETurbineStates.Off)
            {
                if (EfficiencyBonus > 0f)
                {
                    SetEfficiencyBonus(0f);
                    updatePowerDisplay();
                    updateFuelGauge();
                }
                return;
            }

            switch (turbineState)
            {
                case ETurbineStates.SpoolingUp:
                    if (powerLevel < kBaseSpoolPowerLevel)
                    {
                        powerLevel = Mathf.MoveTowards(powerLevel, kBaseSpoolPowerLevel, TimeWarp.fixedDeltaTime / spoolTime);
                        turbineSpinFactor = Mathf.MoveTowards(turbineSpinFactor, turbineMinRPS, TimeWarp.fixedDeltaTime);
                    }

                    if (powerLevel >= kBaseSpoolPowerLevel)
                    {
                        turbineSpinFactor = 1.0f;
                        turbineState = ETurbineStates.ThrottleControlled;
                        turbineStateID = (int)turbineState;
                        StartResourceConverter();
                    }

                    bonusEfficiency = kBaseEfficiencyBonus * powerLevel;
                    break;

                case ETurbineStates.SpoolingDown:
                    if (powerLevel > kBaseSpoolPowerLevel)
                    {
                        powerLevel = Mathf.MoveTowards(powerLevel, kBaseSpoolPowerLevel, TimeWarp.fixedDeltaTime / spoolTime);
                        turbineSpinFactor = Mathf.MoveTowards(turbineSpinFactor, turbineMinRPS, TimeWarp.fixedDeltaTime);
                        bonusEfficiency = kBaseEfficiencyBonus * powerLevel;
                    }

                    //power down completely
                    else
                    {
                        StopResourceConverter();

                        powerLevel = Mathf.MoveTowards(powerLevel, 0, TimeWarp.fixedDeltaTime / spoolTime);
                        if (powerLevel <= 0.0001f)
                        {
                            powerLevel = 0f;
                            turbineSpinFactor = 0f;
                            turbineState = ETurbineStates.Off;
                            turbineStateID = (int)turbineState;
                            if (!string.IsNullOrEmpty(turbineEffect))
                                this.part.Effect(turbineEffect, 0f);
                            if (!string.IsNullOrEmpty(smokeEffect))
                                this.part.Effect(smokeEffect, 0f);
                        }
                    }
                    break;

                case ETurbineStates.ThrottleControlled:
                    //If we're missing inputs then flameout.
                    if (status.ToLower().Contains("missing"))
                    {
                        StopResourceConverter();
                        turbineState = ETurbineStates.SpoolingDown;
                        turbineStateID = (int)turbineState;
                        if (!string.IsNullOrEmpty(flameoutEffect))
                            this.part.Effect(flameoutEffect, 1.0f);
                    }

                    //Set production efficiency bonus and power level based on throttle
                    if (FlightGlobals.ActiveVessel == this.part.vessel)
                        lastThrottleSetting = FlightInputHandler.state.mainThrottle;
                    if (lastThrottleSetting > 0)
                    {
                        powerLevel = Mathf.MoveTowards(powerLevel, lastThrottleSetting * outputLimit, TimeWarp.fixedDeltaTime / spoolTime);
                        bonusEfficiency = powerLevel;
                    }

                    else
                    {
                        powerLevel = Mathf.MoveTowards(powerLevel, kBaseSpoolPowerLevel, TimeWarp.fixedDeltaTime / spoolTime);
                        bonusEfficiency = powerLevel;
                    }

                    //Make sure we achieve minimum power levels.
                    if (powerLevel <= kBaseSpoolPowerLevel)
                    {
                        powerLevel = kBaseSpoolPowerLevel;
                        bonusEfficiency = kBaseEfficiencyBonus;
                    }
                    break;

                default:
                    break;
            }

            //Set efficiency bonus
            SetEfficiencyBonus(bonusEfficiency);
            updatePowerDisplay();
            updateFuelGauge();

            //Spin the turbine (optional)
            if (turbineTransform != null && turbineRotationAxis != null)
            {
                if (powerLevel > kBaseSpoolPowerLevel)
                    turbineTransform.Rotate(turbineRotationAxis * (turbineMaxRPS * powerLevel));
                else
                    turbineTransform.Rotate(turbineRotationAxis * turbineMinRPS * turbineSpinFactor);
            }
        }

        protected void setupInfoBox()
        {
            if (infoBox != null)
                return;

            getMaxTotalFuel();
            resourceBroker = new ResourceBroker();
            ResourceRatio[] inputs = inputList.ToArray();
            Color clr;
            for (int index = 0; index < inputs.Length; index++)
            {
                if (inputs[index].ResourceName == fuelGaugeResource)
                {
                    infoBox = this.part.stackIcon.StageIcon.DisplayInfo();
                    infoBox.SetMessage(fuelGaugeResource);
                    clr = XKCDColors.GreenYellow;  //XKCDColors.BrightOlive;
                    clr.a = 0.55f;
                    infoBox.SetMsgTextColor(clr);
                    clr = XKCDColors.Asparagus;
                    clr.a = 0.5f;
                    infoBox.SetMsgBgColor(clr);
                    infoBox.SetProgressBarBgColor(clr);
                    infoBox.Collapse();
                    break;
                }
            }
        }

        protected void getMaxTotalFuel()
        {
            maxTotalFuelGaugeResource = (float)ResourceHelper.GetTotalResourceMaxAmount(fuelGaugeResource, this.part.vessel);
        }

        protected void updateFuelGauge()
        {
            if (infoBox == null)
                return;

            float totalAmount = (float)resourceBroker.AmountAvailable(this.part, fuelGaugeResource, TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);

            infoBox.SetValue(totalAmount, 0f, maxTotalFuelGaugeResource);
        }

        protected void updatePowerDisplay()
        {
            if (IsActivated && status.Contains("load"))
            {
                //Get the numerical value (*somebody* didn't seem to make this convenient to obtain :( )
                string powerOutputDisplay = status.Substring(0, status.IndexOf("%"));
                double load;
                if (double.TryParse(powerOutputDisplay, out load))
                {
                    load = load / 100f;
                    load = load * ecBaseOutput;
                    powerOutput = load;
                }
            }

            else
            {
                powerOutput = 0f;
            }
        }

        /*
        protected void createParticleFX(string effectName, ref GameObject fx, ref KSPParticleEmitter emitter)
        {
            fx = (GameObject)GameObject.Instantiate(UnityEngine.Resources.Load(effectName));
            fx.transform.parent = this.part.transform;
            fx.transform.position = exhaustTransform.position;

            emitter = fx.GetComponent<KSPParticleEmitter>();
            emitter.localVelocity = Vector3.zero;
            emitter.useWorldSpace = true;
            emitter.emit = false;
            emitter.enabled = false;
            emitter.minEnergy = 0;
            emitter.minEmission = 0;
        }

        protected void createSoundFX(ref FXGroup group, string sndPath, bool loop, float maxDistance = 200f)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            group = new FXGroup(sndPath);
            group.audio = this.part.gameObject.AddComponent<AudioSource>();
            group.audio.volume = GameSettings.SHIP_VOLUME;
            group.audio.rolloffMode = AudioRolloffMode.Linear;
            group.audio.dopplerLevel = 0f;
            group.audio.spatialBlend = 1f;
            group.audio.maxDistance = maxDistance;
            group.audio.loop = loop;
            group.audio.playOnAwake = false;
            if (GameDatabase.Instance.ExistsAudioClip(sndPath))
                group.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
            this.part.fxGroups.Add(group);
        }
        */

        protected void setupGUI()
        {
            Actions["ToggleGeneratorAction"].guiName = ToggleActionName;

            if (turbineState == ETurbineStates.Off || turbineState == ETurbineStates.SpoolingDown) 
                Events["ToggleGenerator"].guiName = StartActionName;
            else
                Events["ToggleGenerator"].guiName = StopActionName;

            //Hide base class events and actions
            Events["StartResourceConverter"].guiActive = false;
            Events["StartResourceConverter"].guiActiveEditor = false;
            Events["StopResourceConverter"].guiActive = false;
            Events["StopResourceConverter"].guiActiveEditor = false;
            Actions["StartResourceConverterAction"].actionGroup = KSPActionGroup.None;
            Actions["StopResourceConverterAction"].actionGroup = KSPActionGroup.None;
            Actions["ToggleResourceConverterAction"].actionGroup = KSPActionGroup.None;
            Actions["StartResourceConverterAction"].active = false;
            Actions["StopResourceConverterAction"].active = false;
            Actions["ToggleResourceConverterAction"].active = false;
        }
    }
}
