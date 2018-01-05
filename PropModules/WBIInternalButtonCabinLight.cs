using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public enum ELightStates
    {
        Off,
        On,
        MainColor,
        AltColor,
    }

    public class WBIInternalButtonCabinLight : InternalModule
    {
        [KSPField]
        public string buttonName;

        [KSPField]
        public string lightName;

        [KSPField]
        public string cockpitLampName;

        [KSPField]
        public string buttonColorOn;

        [KSPField]
        public string buttonColorOff;

        [KSPField]
        public float lightIntensityOn;

        [KSPField]
        public float lightIntensityOff;

        [KSPField]
        public bool setLightColor;

        [KSPField]
        public string primaryLightColor = string.Empty;

        [KSPField]
        public string secondaryLightColor = string.Empty;

        protected ELightStates lightState;
        protected Material colorShiftMaterial;
        protected Color colorButtonOn;
        protected Color colorButtonOff;
        protected Color mainLightColor;
        protected Color altLightColor;
        protected WBIPropStateHelper propStateHelper;
        protected WBILight lightModule;

        public void Start()
        {
            lightModule = this.part.FindModuleImplementing<WBILight>();
            if (lightModule != null)
            {
                if (lightModule.isDeployed && !setLightColor)
                    lightState = ELightStates.On;
                else if (lightModule.isDeployed && setLightColor)
                    lightState = ELightStates.MainColor;
            }

            propStateHelper = this.part.FindModuleImplementing<WBIPropStateHelper>();
            if (propStateHelper != null)
            {
                string value = propStateHelper.LoadProperty(internalProp.propID, "lightState");
                if (string.IsNullOrEmpty(value) == false)
                    lightState = (ELightStates)(int.Parse(value));
            }

            //Get the transform and setup click watcher
            Transform trans = internalProp.FindModelTransform(buttonName);
            if (trans != null)
            {
                GameObject goButton = trans.gameObject;
                if (goButton != null)
                {
                    ButtonClickWatcher clickWatcher = goButton.GetComponent<ButtonClickWatcher>();
                    if (clickWatcher == null)
                    {
                        clickWatcher = goButton.AddComponent<ButtonClickWatcher>();
                    }
                    clickWatcher.clickDelegate = OnButtonClick;
                }
            }

            //Setup button
            string[] rgbString = buttonColorOn.Split(new char[] { ',' });
            colorButtonOn = new Color(float.Parse(rgbString[0]), float.Parse(rgbString[1]), float.Parse(rgbString[2]));

            rgbString = buttonColorOff.Split(new char[] { ',' });
            colorButtonOff = new Color(float.Parse(rgbString[0]), float.Parse(rgbString[1]), float.Parse(rgbString[2]));

            Renderer colorShiftRenderer = internalProp.FindModelComponent<Renderer>(buttonName);
            colorShiftMaterial = colorShiftRenderer.material;

            if (setLightColor)
            {
                rgbString = primaryLightColor.Split(new char[] { ',' });
                mainLightColor = new Color(float.Parse(rgbString[0]), float.Parse(rgbString[1]), float.Parse(rgbString[2]));

                rgbString = secondaryLightColor.Split(new char[] { ',' });
                altLightColor = new Color(float.Parse(rgbString[0]), float.Parse(rgbString[1]), float.Parse(rgbString[2]));
            }

            SetButtonColor();
    
            //Setup lights
            SetupLights();
        }

        public void SetButtonColor()
        {
            if (lightState != ELightStates.Off)
                colorShiftMaterial.SetColor("_EmissiveColor", colorButtonOn);
            else
                colorShiftMaterial.SetColor("_EmissiveColor", colorButtonOff);
        }

        public void OnButtonClick()
        {
            switch (lightState)
            {
                case ELightStates.Off:
                default:
                    if (setLightColor)
                        lightState = ELightStates.MainColor;
                    else
                        lightState = ELightStates.On;
                    break;

                case ELightStates.MainColor:
                    lightState = ELightStates.AltColor;
                    break;

                case ELightStates.AltColor:
                    lightState = ELightStates.Off;
                    break;
            }
            int state = (int)lightState;

            if (propStateHelper != null)
                propStateHelper.SaveProperty(internalProp.propID, "lightState", state.ToString());

            SetupLights();

            SetButtonColor();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (lightModule != null)
            {
                if (lightModule.isDeployed && lightState == ELightStates.Off)
                    SetupLights(true);
                else if (lightModule.isDeployed == false && lightState != ELightStates.Off)
                    SetupLights(false);
            }
        }

        public void SetupLights(bool lightsAreOn)
        {
            if (lightsAreOn)
                lightState = ELightStates.On;
            else
                lightState = ELightStates.Off;
            SetButtonColor();

            SetupLights();
        }

        public void SetupLights()
        {
            Light[] lights = internalModel.FindModelComponents<Light>();
            bool lightsAreOn = false;

            if (lightState != ELightStates.Off)
                lightsAreOn = true;

            if (lights != null && lights.Length > 0)
            {
                foreach (Light light in lights)
                {
                    //Set enabled/disabled and color
                    if (light.name.Contains(lightName))
                    {
                        switch (lightState)
                        {
                            case ELightStates.Off:
                            default:
                                light.enabled = false;
                                break;

                            case ELightStates.On:
                            case ELightStates.MainColor:
                                light.enabled = true;
                                light.color = mainLightColor;
                                break;

                            case ELightStates.AltColor:
                                light.enabled = true;
                                light.color = altLightColor;
                                break;
                        }
                    }
                }
            }

            //Set the emissives for the lamps
            if (string.IsNullOrEmpty(cockpitLampName) == false)
            {
                Transform lampTransform = internalModel.FindModelTransform(cockpitLampName);
                Renderer rendererMaterial;
                Color colorOn = new Color(1,1,1,1);
                Color colorOff = new Color(0,0,0,0);

                if (lampTransform != null)
                {
                    rendererMaterial = lampTransform.GetComponent<Renderer>();
                    if (lightsAreOn)
                        rendererMaterial.material.SetColor("_EmissiveColor", colorOn);
                    else
                        rendererMaterial.material.SetColor("_EmissiveColor", colorOff);
                }
            }

            //External light
            if (lightModule != null)
            {
                if (lightsAreOn && lightModule.isDeployed == false)
                    lightModule.ToggleAnimation();
                else if (lightsAreOn == false && lightModule.isDeployed)
                    lightModule.ToggleAnimation();
            }
        }

    }
}
