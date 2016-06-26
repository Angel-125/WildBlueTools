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
    [KSPModule("RCS")]
    public class WBIModuleRCS : ModuleRCS
    {
        private const float maxSoundDistance = 5.0f;
        private const double kDefaultECRequired = 0;

        public static float soundEffectVolume = GameSettings.SHIP_VOLUME;//0.003f;//GameSettings.SHIP_VOLUME;

        [KSPField(isPersistant = true)]
        public string rcsEffectName;

        [KSPField(isPersistant = true)]
        public string rcsID;

        [KSPField(isPersistant = true)]
        public string soundFilePath;

        [KSPField(guiActive = true)]
        public bool isRCSOn = false;

        public FXGroup soundClip = null;
        protected KSPParticleEmitter[] emitters;
        protected bool soundIsPlaying = false;

        [KSPAction("RCS On")]
        public void ActionRCSOn(KSPActionParam param)
        {
            Enable();
        }

        [KSPAction("RCS Off")]
        public void ActionRCSOff(KSPActionParam param)
        {
            Disable();

            DeactivateFX();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get the emitters
            if (!string.IsNullOrEmpty(rcsEffectName))
                emitters = this.part.GetComponentsInChildren<KSPParticleEmitter>();

            //Load the sound clip
            if (string.IsNullOrEmpty(soundFilePath) == false)
                LoadSoundFX();
        }

        public void LoadSoundFX()
        {
            if (!GameDatabase.Instance.ExistsAudioClip(soundFilePath))
                return;

            soundClip.audio = part.gameObject.AddComponent<AudioSource>();
            soundClip.audio.volume = soundEffectVolume;
            soundClip.audio.maxDistance = maxSoundDistance;

            soundClip.audio.clip = GameDatabase.Instance.GetAudioClip(soundFilePath);
            soundClip.audio.loop = true;

            soundClip.audio.rolloffMode = AudioRolloffMode.Logarithmic;
            soundClip.audio.panStereo = 1f;
            soundClip.audio.spatialBlend = 1f;
            soundClip.audio.dopplerLevel = 0f;

            soundClip.audio.playOnAwake = false;
        }

        public void DeactivateFX()
        {
            if (isRCSOn == false)
                return;

            if (emitters != null)
            {
                foreach (KSPParticleEmitter emitter in emitters)
                {
                    if (emitter.name.Contains(rcsEffectName))
                    {
                        emitter.emit = false;
                        emitter.enabled = false;
                    }
                }
            }

            if (soundClip != null)
            {
                soundClip.audio.Stop();
                soundIsPlaying = false;
            }

            isRCSOn = false;
        }

        public void PlayOrStopSoundFX()
        {
            if (soundClip != null)
            {
                if (isRCSOn && rcsEnabled && soundIsPlaying == false)
                {
                    soundClip.audio.volume = soundEffectVolume;
                    soundClip.audio.Play();
                    soundIsPlaying = true;
                }
                else if (isRCSOn == false || rcsEnabled == false)
                {
                    soundClip.audio.Stop();
                    soundIsPlaying = false;
                }
            }
        }

        public void ShowActiveRCSFX()
        {
            FXGroup thrusterFX;
            KSPParticleEmitter particleEmitter = null;

            isRCSOn = false;
            for (int thrusterIndex = 0; thrusterIndex < this.thrusterFX.Count; thrusterIndex++)
            {
                thrusterFX = this.thrusterFX[thrusterIndex];

                //Shut down the emitters
                if (emitters != null)
                {
                    particleEmitter = emitters[thrusterIndex];

                    particleEmitter.emit = false;
                    particleEmitter.enabled = false;
                }

                //Activate the emitter that's on.
                if (thrusterFX.Active && (thrusterFX.Power > this.thrusterPower / 2.0f))
                {
                    isRCSOn = true;
                    if (emitters != null)
                    {
                        particleEmitter.emit = true;
                        particleEmitter.enabled = true;
                        thrusterFX.Power = 0f;
                    }
                }
            }
        }

        public override void  OnUpdate()
        {
            base.OnUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //If RCS isn't activated, then just make sure the effects are switched off.
            if (this.part.vessel.ActionGroups[KSPActionGroup.RCS] == false || rcsEnabled == false)
            {
                DeactivateFX();
                return;
            }

            //Light up the appropriate RCS emitters.
            ShowActiveRCSFX();

            //Play sound
            PlayOrStopSoundFX();
        }
    }
}