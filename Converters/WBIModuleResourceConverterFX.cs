using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
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
    [KSPModule("Resource Converter")]
    public class WBIModuleResourceConverterFX: ModuleBreakableConverter
    {
        [KSPField()]
        public string startEffect = string.Empty;

        [KSPField()]
        public string stopEffect = string.Empty;

        [KSPField()]
        public string runningEffect = string.Empty;

        Light[] lights;
        KSPParticleEmitter[] emitters;

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (!IsActivated)
            {
                this.part.Effect(runningEffect, 0f);
                this.part.Effect(stopEffect, 0f);
                this.part.Effect(startEffect, 0f);
            }
        }

        public override void OnInactive()
        {
            base.OnInactive();
            StopResourceConverter();
            this.part.Effect(runningEffect, 0f);
            this.part.Effect(stopEffect, 0f);
            this.part.Effect(startEffect, 0f);
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();
            setupLightsAndEmitters();

            if (!string.IsNullOrEmpty(startEffect))
                this.part.Effect(startEffect, 1.0f);
            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(runningEffect, 1.0f);
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
            setupLightsAndEmitters();

            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(runningEffect, 0.0f);
            if (!string.IsNullOrEmpty(stopEffect))
                this.part.Effect(stopEffect, 1.0f);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Find the lights
            lights = this.part.gameObject.GetComponentsInChildren<Light>();
            Log("THERE! ARE! " + lights.Length + " LIGHTS!");

            //Find emitters
            emitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            //Setup lights and emitters
            setupLightsAndEmitters();

            //Setup running sound if the converter is running
            if (IsActivated)
            {
                this.part.InitializeEffects();
                if (!string.IsNullOrEmpty(runningEffect))
                    this.part.Effect(runningEffect, 1.0f);
            }
        }

        public override void OnPartBroken(BaseQualityControl moduleQualityControl)
        {
            base.OnPartBroken(moduleQualityControl);
            StopResourceConverter();
        }

        protected void setupLightsAndEmitters()
        {
            //Turn off lights if any
            if (lights != null)
            {
                for (int index = 0; index < lights.Length; index++)
                    lights[index].intensity = IsActivated ? 1.0f : 0.0f;
            }

            //Turn off emitters if any
            if (emitters != null)
            {
                for (int index = 0; index < emitters.Length; index++)
                {
                    emitters[index].emit = IsActivated;
                    emitters[index].enabled = IsActivated;
                }
            }
        }

        public virtual void Log(object message)
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedScene == GameScenes.LOADINGBUFFER ||
                HighLogic.LoadedScene == GameScenes.PSYSTEM || HighLogic.LoadedScene == GameScenes.SETTINGS)
                return;

            if (!WBIMainSettings.EnableDebugLogging)
                return;

            Debug.Log(this.ClassName + " [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: " + message);
        }
    }
}
