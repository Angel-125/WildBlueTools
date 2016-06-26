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
    [KSPModule("Light")]
    public class WBILight : WBIAnimation
    {
        protected const int kDefaultLightAnimationLayer = 3;

        [KSPField]
        public string colorPanelName;

        [KSPField(isPersistant = true)]
        public double ecRequired;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float red;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float green;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float blue;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float level = -1f;

        Light[] lights;
        float intensity;
        float prevRed;
        float prevGreen;
        float prevBlue;
        float prevLevel;

        [KSPAction("Toggle Lights", KSPActionGroup.Light)]
        public void ToggleLightsAction(KSPActionParam param)
        {
            ToggleAnimation();
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);
            string value;

            //If the red, green, blue, level fields don't exist then hide their GUI.
            value = protoNode.GetValue("red");
            if (string.IsNullOrEmpty(value))
            {
                Fields["red"].guiActive = false;
                Fields["red"].guiActiveEditor = false;
            }
            value = protoNode.GetValue("green");
            if (string.IsNullOrEmpty(value))
            {
                Fields["green"].guiActive = false;
                Fields["green"].guiActiveEditor = false;
            }
            value = protoNode.GetValue("blue");
            if (string.IsNullOrEmpty(value))
            {
                Fields["blue"].guiActive = false;
                Fields["blue"].guiActiveEditor = false;
            }

            value = protoNode.GetValue("intensity");
            if (string.IsNullOrEmpty(value) == false)
            {
                intensity = float.Parse(value);
                if (level == -1f)
                    level = 1.0f;
            }
            else
            {
                Fields["level"].guiActive = false;
                Fields["level"].guiActiveEditor = false;
            }
        }

        public override void OnStart(StartState state)
        {
            if (animationLayer == kDefaultAnimationLayer)
                animationLayer = kDefaultLightAnimationLayer;

            base.OnStart(state);
            Animation anim = this.part.FindModelAnimators(animationName)[0];

            anim[animationName].layer = animationLayer;

            //Find the lights
            lights = this.part.gameObject.GetComponentsInChildren<Light>();
            Log("THERE! ARE! " + lights.Length + " LIGHTS!");
            setupLights();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            //Get the required EC
            if (isDeployed && ecRequired > 0f)
            {
                double ecPerTimeTick = ecRequired * TimeWarp.fixedDeltaTime;
                double ecObtained = this.part.RequestResource("ElectricCharge", ecPerTimeTick, ResourceFlowMode.ALL_VESSEL);

                if (ecObtained / ecPerTimeTick < 0.999)
                    ToggleAnimation();
            }

            //If the settings have changed then re-setup the lights.
            if (prevRed != red || prevGreen != green || prevBlue != blue || prevLevel != level)
                setupLights();
        }

        public void TurnOnLights()
        {
            //If the part has an inflatable part and it's not deployed,
            //then don't allow the lights to come on.
            WBIInflatablePartModule inflatable = this.part.FindModuleImplementing<WBIInflatablePartModule>();
            if (inflatable != null)
            {
                if (inflatable.isDeployed == false && inflatable.isInflatable)
                {
                    ScreenMessages.PostScreenMessage("Cannot turn on the lights until the module is inflated.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }

            //Play animation for current state
            PlayAnimation(isDeployed);
            isDeployed = true;
            Events["ToggleAnimation"].guiName = endEventGUIName;
            setupLights();
        }

        public void TurnOffLights()
        {
            //Play animation for current state
            PlayAnimation(isDeployed);
            isDeployed = false;
            Events["ToggleAnimation"].guiName = startEventGUIName;
            setupLights();
        }

        public override void ToggleAnimation()
        {
            //If the part has an inflatable part and it's not deployed,
            //then don't allow the lights to come on.
            WBIInflatablePartModule inflatable = this.part.FindModuleImplementing<WBIInflatablePartModule>();
            if (inflatable != null)
            {
                if (inflatable.isDeployed == false && inflatable.isInflatable)
                {
                    ScreenMessages.PostScreenMessage("Cannot turn on the lights until the module is inflated.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }

            base.ToggleAnimation();
            setupLights();
        }

        protected void setupLights()
        {
            Transform[] targets;
            Renderer rendererMaterial;
            if (lights == null)
                return;
            if (lights.Length == 0)
                return;
            Color color = new Color(red, green, blue, intensity * level);

            foreach (Light light in lights)
            {
                light.color = color;

                if (isDeployed)
                    light.intensity = intensity * level;
                else
                    light.intensity = 0;
            }

            //Get baseline values
            prevRed = red;
            prevGreen = green;
            prevBlue = blue;
            prevLevel = level;

            //If we have a color panel then change its color
            if (string.IsNullOrEmpty(colorPanelName))
                return;

            //Get the renderers
            Renderer[] renderers = this.part.FindModelComponents<Renderer>();
            if (string.IsNullOrEmpty(colorPanelName))
                return;

            //Set up the panel color if the light is on.
            if (isDeployed == false)
                return;

            //Get the target transforms
            targets = this.part.FindModelTransforms(colorPanelName);
            if (targets == null)
                return;

            //Now set the emissive color
            foreach (Transform target in targets)
            {
                rendererMaterial = target.GetComponent<Renderer>();
                rendererMaterial.material.SetColor("_EmissiveColor", color);
            }

        }

    }
}
