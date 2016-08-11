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
    class WBIInternalButtonAnim : InternalModule
    {
        [KSPField]
        public string buttonName;

        [KSPField]
        public string buttonColorOn;

        [KSPField]
        public string buttonColorOff;

        protected Material colorShiftMaterial;
        protected Color colorButtonOn;
        protected Color colorButtonOff;
        protected WBIPropStateHelper propStateHelper;
        protected bool buttonClicked;
        ModuleAnimateGeneric buttonAnimation;

        public void Start()
        {
            propStateHelper = this.part.FindModuleImplementing<WBIPropStateHelper>();
            if (propStateHelper != null)
            {
                List<ModuleAnimateGeneric> animators = this.part.FindModulesImplementing<ModuleAnimateGeneric>();

                foreach (ModuleAnimateGeneric animator in animators)
                {
                    if (animator.animationName == propStateHelper.animationName)
                    {
                        buttonAnimation = animator;
                        break;
                    }
                }
                
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
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (buttonAnimation != null)
            {
                if (buttonAnimation.Events["Toggle"].guiName == buttonAnimation.endEventGUIName && buttonClicked == false)
                {
                    buttonClicked = true;
                    SetButtonColor();
                }

                else if (buttonAnimation.Events["Toggle"].guiName == buttonAnimation.startEventGUIName && buttonClicked)
                {
                    buttonClicked = false;
                    SetButtonColor();
                }
            }
        }

        public void SetButtonColor()
        {
            if (buttonClicked)
                colorShiftMaterial.SetColor("_EmissiveColor", colorButtonOn);
            else
                colorShiftMaterial.SetColor("_EmissiveColor", colorButtonOff);
        }

        public void OnButtonClick()
        {
            if (buttonAnimation != null)
            {
                buttonClicked = !buttonClicked;
                buttonAnimation.Toggle();
            }

            SetButtonColor();
        }
    }
}
