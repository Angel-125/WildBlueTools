using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIInternalButtonCabinLight : InternalModule
    {
        [KSPField]
        public string buttonName;

        [KSPField]
        public string lightName;

        [KSPField]
        public string buttonColorOn;

        [KSPField]
        public string buttonColorOff;

        [KSPField]
        public float lightIntensityOn;

        [KSPField]
        public float lightIntensityOff;

        protected bool lightsOn = true;
        protected Material colorShiftMaterial;
        protected Color colorButtonOn;
        protected Color colorButtonOff;
        protected WBIPropStateHelper propStateHelper;

        public void Start()
        {
            propStateHelper = this.part.FindModuleImplementing<WBIPropStateHelper>();
            if (propStateHelper != null)
            {
                string value = propStateHelper.LoadProperty(internalProp.propID, "lightsOn");
                if (string.IsNullOrEmpty(value) == false)
                    lightsOn = bool.Parse(value);
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

            SetButtonColor();
    
            //Setup lights
            SetupLights();
        }

        public void SetButtonColor()
        {
            if (lightsOn)
                colorShiftMaterial.SetColor("_EmissiveColor", colorButtonOn);
            else
                colorShiftMaterial.SetColor("_EmissiveColor", colorButtonOff);
        }

        public void OnButtonClick()
        {
            lightsOn = !lightsOn;
            if (propStateHelper != null)
                propStateHelper.SaveProperty(internalProp.propID, "lightsOn", lightsOn.ToString());

            SetupLights();

            SetButtonColor();
        }

        public void SetupLights()
        {
            Light[] lights = internalModel.FindModelComponents<Light>();

            if (lights != null && lights.Length > 0)
            {
                foreach (Light light in lights)
                {
                    if (light.name == lightName)
                        light.enabled = lightsOn;
                }
            }
        }

    }
}
