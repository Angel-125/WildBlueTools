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
    class WBIModuleAnimateGeneric : ModuleAnimateGeneric
    {
        private const float EDITOR_ANIMATION_SPEED = 0.1f;

        [KSPField]
        public float partialAnimationSpeed;

        [KSPField(isPersistant = true)]
        public bool isOpen;

        [KSPField(guiName = "Animation", isPersistant = true, guiActiveEditor = true, guiActive = true)]
        [UI_Toggle(disabledText = "Normal", enabledText = "Partial")]
        public bool enablePartialAnimation;

        [KSPField(guiName = "Close Limit", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(minValue = 0, stepIncrement = 1, maxValue = 100)]
        public float closePercent;

        [KSPField(guiName = "Open Limit", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(minValue = 0, stepIncrement = 1, maxValue = 100)]
        public float openPercent;

        public float currentPercent;

        [KSPEvent(guiName = "Partial Open/Close", guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 5.0f)]
        public void ToggleAnimation()
        {
            isOpen = !isOpen;

            if (isOpen)
                deployPercent = openPercent;
            else
                deployPercent = closePercent;
        }

        [KSPAction("Partial Open/Close")]
        public virtual void ToggleAnimationAction(KSPActionParam param)
        {
            if (enablePartialAnimation)
                ToggleAnimation();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (HighLogic.LoadedSceneIsEditor)
                partialAnimationSpeed = EDITOR_ANIMATION_SPEED;

            if (enablePartialAnimation)
            {
                Fields["deployPercent"].guiActive = false;
                Fields["deployPercent"].guiActiveEditor = false;
                Events["ToggleAnimation"].guiActive = true;
                Events["ToggleAnimation"].guiActiveEditor = true;
                Events["ToggleAnimation"].guiActiveUnfocused = true;
                Events["Toggle"].guiActive = false;
                Events["Toggle"].guiActiveEditor = false;
                Events["Toggle"].guiActiveUnfocused = false;

                if (HighLogic.LoadedSceneIsEditor)
                    openPercent = deployPercent;
                else
                    deployPercent = openPercent;

                if (deployPercent > closePercent)
                    isOpen = true;

                currentPercent = deployPercent;
            }

            else //Standard animation mode
            {
                Fields["closePercent"].guiActive = false;
                Fields["closePercent"].guiActiveEditor = false;
                Fields["openPercent"].guiActive = false;
                Fields["openPercent"].guiActiveEditor = false;
                Events["ToggleAnimation"].guiActive = false;
                Events["ToggleAnimation"].guiActiveEditor = false;
                Events["ToggleAnimation"].guiActiveUnfocused = false;
                Events["Toggle"].guiActive = true;
                Events["Toggle"].guiActiveEditor = true;
                Events["Toggle"].guiActiveUnfocused = true;
            }
        }

        public void Update()
        {
            if (enablePartialAnimation)
            {
                Fields["deployPercent"].guiActive = false;
                Fields["deployPercent"].guiActiveEditor = false;
                Fields["closePercent"].guiActive = true;
                Fields["closePercent"].guiActiveEditor = true;
                Fields["openPercent"].guiActive = true;
                Fields["openPercent"].guiActiveEditor = true;
                Events["ToggleAnimation"].guiActive = true;
                Events["ToggleAnimation"].guiActiveEditor = true;
                Events["ToggleAnimation"].guiActiveUnfocused = true;
                Events["Toggle"].guiActive = false;
                Events["Toggle"].guiActiveEditor = false;
                Events["Toggle"].guiActiveUnfocused = false;

                //Open cannot be less than close
                if (openPercent < closePercent)
                    openPercent = closePercent;

                if (isOpen)
                    currentPercent = Mathf.Lerp(currentPercent, closePercent, partialAnimationSpeed);
                else
                    currentPercent = Mathf.Lerp(currentPercent, openPercent, partialAnimationSpeed);

                deployPercent = currentPercent;
            }

            else //Standard animation mode
            {
                Fields["deployPercent"].guiActive = true;
                Fields["deployPercent"].guiActiveEditor = true;
                Fields["closePercent"].guiActive = false;
                Fields["closePercent"].guiActiveEditor = false;
                Fields["openPercent"].guiActive = false;
                Fields["openPercent"].guiActiveEditor = false;
                Events["ToggleAnimation"].guiActive = false;
                Events["ToggleAnimation"].guiActiveEditor = false;
                Events["ToggleAnimation"].guiActiveUnfocused = false;
                Events["Toggle"].guiActive = true;
                Events["Toggle"].guiActiveEditor = true;
                Events["Toggle"].guiActiveUnfocused = true;
            }
        }

    }
}
