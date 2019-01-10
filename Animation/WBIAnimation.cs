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

        [KSPField]
        public string startSoundURL = string.Empty;

        [KSPField]
        public float startSoundPitch = 1.0f;

        [KSPField]
        public float startSoundVolume = 0.5f;

        [KSPField]
        public string loopSoundURL = string.Empty;

        [KSPField]
        public float loopSoundPitch = 1.0f;

        [KSPField]
        public float loopSoundVolume = 0.5f;

        [KSPField]
        public string stopSoundURL = string.Empty;

        [KSPField]
        public float stopSoundPitch = 1.0f;

        [KSPField]
        public float stopSoundVolume = 0.5f;

        [KSPField]
        public KSPActionGroup defaultActionGroup;

        [KSPField]
        public bool playAnimationLooped = false;

        //Helper objects
        public bool isDeployed = false;
        public bool isMoving = false;
        public bool isLooping = false;
        public Animation animation = null;
        protected AnimationState animationState;
        protected AudioSource loopSound = null;
        protected AudioSource startSound = null;
        protected AudioSource stopSound = null;

        #region User Events & API
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ToggleAnimation", active = true, externalToEVAOnly = false, unfocusedRange = 3.0f, guiActiveUnfocused = true)]
        public virtual void ToggleAnimation()
        {
            //Play animation for current state, but skip if we are currently looping the animation.
            //This will ensure that when we stopp looping the animation, its cycle will complete without playing in reverse.
            if (!isLooping)
                PlayAnimation(isDeployed);

            //Toggle state
            isDeployed = !isDeployed;
            if (isDeployed)
            {
                if (playAnimationLooped)
                    isLooping = true;
                Events["ToggleAnimation"].guiName = endEventGUIName;
            }
            else
            {
                if (playAnimationLooped)
                    isLooping = false;
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
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (animation == null)
                return;

            //Play end
            else if (animation.isPlaying == false && isMoving)
            {
                if (!playAnimationLooped || !isLooping)
                {
                    isMoving = false;
                    playEnd();
                    animationComplete();
                }
                else if (isLooping)
                {
                    PlayAnimation();
                }
            }
        }

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
            setupSounds();
            showGui(guiIsVisible);

            if ((int)defaultActionGroup > 0)
                Actions["ToggleAnimationAction"].actionGroup = defaultActionGroup;
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);

            animationName = protoNode.GetValue("animationName");

            endEventGUIName = protoNode.GetValue("endEventGUIName");

            startEventGUIName = protoNode.GetValue("startEventGUIName");
        }

        protected virtual void animationComplete()
        {
        }

        public void playStart()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (startSound != null)
                startSound.Play();

            if (loopSound != null)
                loopSound.Play();
        }

        public void playEnd()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (stopSound != null)
                stopSound.Play();

            if (loopSound != null)
                loopSound.Stop();
        }

        #endregion

        #region Helpers
        protected virtual void setupSounds()
        {
            if (!string.IsNullOrEmpty(startSoundURL))
            {
                startSound = gameObject.AddComponent<AudioSource>();
                startSound.clip = GameDatabase.Instance.GetAudioClip(startSoundURL);
                startSound.pitch = startSoundPitch;
                startSound.volume = GameSettings.SHIP_VOLUME * startSoundVolume;
            }

            if (!string.IsNullOrEmpty(loopSoundURL))
            {
                loopSound = gameObject.AddComponent<AudioSource>();
                loopSound.clip = GameDatabase.Instance.GetAudioClip(loopSoundURL);
                loopSound.loop = true;
                loopSound.pitch = loopSoundPitch;
                loopSound.volume = GameSettings.SHIP_VOLUME * loopSoundVolume;
            }

            if (!string.IsNullOrEmpty(stopSoundURL))
            {
                stopSound = gameObject.AddComponent<AudioSource>();
                stopSound.clip = GameDatabase.Instance.GetAudioClip(stopSoundURL);
                stopSound.pitch = stopSoundPitch;
                stopSound.volume = GameSettings.SHIP_VOLUME * stopSoundVolume;
            }
        }

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

            animation = animations[0];
            if (animation == null)
                return;

            //Set layer
            animationState = animation[animationName];
            animation[animationName].layer = animationLayer;

            //Set toggle button
            Events["ToggleAnimation"].guiActive = guiIsVisible;
            Events["ToggleAnimation"].guiActiveEditor = guiIsVisible;

            if (isDeployed)
            {
                Events["ToggleAnimation"].guiName = endEventGUIName;

                animation[animationName].normalizedTime = 1.0f;
                animation[animationName].speed = 10000f;
            }
            else
            {
                Events["ToggleAnimation"].guiName = startEventGUIName;

                animation[animationName].normalizedTime = 0f;
                animation[animationName].speed = -10000f;
            }
            animation.Play(animationName);
        }

        public virtual void PlayAnimation(bool playInReverse = false)
        {
            if (string.IsNullOrEmpty(animationName))
                return;
            if (animation == null)
                return;

            float animationSpeed = playInReverse == false ? 1.0f : -1.0f;

            if (HighLogic.LoadedSceneIsFlight)
                animation[animationName].speed = animationSpeed;
            else
                animation[animationName].speed = animationSpeed * 1000;

            if (playInReverse)
                animation[animationName].time = animation[animationName].length;

            animation.Play(animationName);

            isMoving = true;
            playStart();
        }

        #endregion
    }
}
