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
    public class WBIAnimation : ExtendedPartModule
    {
        protected const int kDefaultAnimationLayer = 2;

        [KSPField()]
        public int animationLayer = kDefaultAnimationLayer;

        [KSPField()]
        public string animationName;

        [KSPField()]
        public string startEventGUIName;

        [KSPField()]
        public string endEventGUIName;

        [KSPField(isPersistant = true)]
        public bool guiIsVisible = true;

        //Helper objects
        public bool isDeployed = false;
        protected AnimationState animationState;

        #region User Events & API
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ToggleAnimation", active = true, externalToEVAOnly = false, unfocusedRange = 3.0f, guiActiveUnfocused = true)]
        public virtual void ToggleAnimation()
        {
            //Play animation for current state
            PlayAnimation(isDeployed);

            //Toggle state
            isDeployed = !isDeployed;
            if (isDeployed)
            {
                Events["ToggleAnimation"].guiName = endEventGUIName;
            }
            else
            {
                Events["ToggleAnimation"].guiName = startEventGUIName;
            }

            Log("Animation toggled new gui name: " + Events["ToggleAnimation"].guiName);
        }

        [KSPAction("ToggleAnimation")]
        public virtual void ToggleAnimationAction(KSPActionParam param)
        {
            ToggleAnimation();
        }

        public virtual void ToggleAnimation(bool deployed)
        {
            isDeployed = deployed;

            //Play animation for current state
            PlayAnimation(isDeployed);

            if (isDeployed)
                Events["ToggleAnimation"].guiName = endEventGUIName;
            else
                Events["ToggleAnimation"].guiName = startEventGUIName;
        }

        public virtual void showGui(bool isVisible)
        {
            guiIsVisible = isVisible;
            Events["ToggleAnimation"].guiActive = isVisible;
            Events["ToggleAnimation"].guiActiveEditor = isVisible;
            Events["ToggleAnimation"].guiActiveUnfocused = isVisible;
        }

        #endregion

        #region Overrides
        public override void OnLoad(ConfigNode node)
        {
            string value;
            base.OnLoad(node);

            value = node.GetValue("isDeployed");
            if (string.IsNullOrEmpty(value) == false)
                isDeployed = bool.Parse(value);

            try
            {
                SetupAnimations();
            }

            catch (Exception ex)
            {
                Log("Error encountered while attempting to setup animations: " + ex.ToString());
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("isDeployed", isDeployed.ToString());
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            SetupAnimations();
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);

            animationName = protoNode.GetValue("animationName");

            endEventGUIName = protoNode.GetValue("endEventGUIName");

            startEventGUIName = protoNode.GetValue("startEventGUIName");
        }

        #endregion

        #region Helpers
        public virtual void SetupAnimations()
        {
            Log("SetupAnimations called.");

            Animation[] animations = this.part.FindModelAnimators(animationName);
            if (animations == null)
            {
                Log("No animations found.");
                return;
            }
            if (animations.Length == 0)
            {
                Log("No animations found.");
                return;
            }

            Animation anim = animations[0];
            if (anim == null)
                return;

            //Set layer
            animationState = anim[animationName];
            anim[animationName].layer = animationLayer;

            //Set toggle button
            Events["ToggleAnimation"].guiActive = guiIsVisible;
            Events["ToggleAnimation"].guiActiveEditor = guiIsVisible;

            if (isDeployed)
            {
                Events["ToggleAnimation"].guiName = endEventGUIName;

                anim[animationName].normalizedTime = 1.0f;
                anim[animationName].speed = 10000f;
            }
            else
            {
                Events["ToggleAnimation"].guiName = startEventGUIName;

                anim[animationName].normalizedTime = 0f;
                anim[animationName].speed = -10000f;
            }
            anim.Play(animationName);
        }

        public virtual void PlayAnimation(bool playInReverse = false)
        {
            if (string.IsNullOrEmpty(animationName))
                return;

            float animationSpeed = playInReverse == false ? 1.0f : -1.0f;
            Animation anim = this.part.FindModelAnimators(animationName)[0];

            if (playInReverse)
            {
                anim[animationName].time = anim[animationName].length;
                anim[animationName].speed = animationSpeed;
                anim.Play(animationName);
            }

            else
            {
                anim[animationName].speed = animationSpeed;
                anim.Play(animationName);
            }
        }

        #endregion
    }
}
