using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIEngineHeat: PartModule
    {
        private const float kGlowTempOffset = 600f; //My custom Draper Point...

        [KSPField]
        public string transformNames = string.Empty;

        private List<ModuleEngines> engines = null;
        private float ratio = 0.0f;
        private float currentThrottle = 0.0f;
        List<Transform> meshTargets = null;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            string[] targetTransforms = transformNames.Split(';');
            Transform[] targets = null;
            foreach (string transform in targetTransforms)
            {
                targets = part.FindModelTransforms(transform);
                if (targets == null)
                {
                    Debug.Log("No targets found for " + transform);
                    continue;
                }

                foreach (Transform target in targets)
                {
                    if (meshTargets == null)
                        meshTargets = new List<Transform>();

                    meshTargets.Add(target);
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (meshTargets == null)
                return;

            if (isEngineRunning())
            {
                ratio = Mathf.Lerp(ratio, currentThrottle, 0.001f);
            }
            else if (ratio > 0.0f)
            {
                ratio = Mathf.Lerp(ratio, 0f, 0.001f);
            }
            else
            {
                return;
            }

            //Set the emissive color
            int count = meshTargets.Count;
            Renderer renderer;
            for (int index = 0; index < count; index++)
            {
                renderer = meshTargets[index].GetComponent<Renderer>();
                renderer.material.SetColor("_EmissiveColor", new Color(ratio, ratio, ratio));
            }
        }

        protected bool isEngineRunning()
        {
            if (engines == null)
                engines = this.part.FindModulesImplementing<ModuleEngines>();
            if (engines == null)
                return false;

            int count = engines.Count;
            for (int index = 0; index < count; index++)
            {
                if (engines[index].isOperational && engines[index].EngineIgnited && !engines[index].flameout)
                {
                    currentThrottle = engines[index].currentThrottle;
                    return true;
                }
            }

            return false;
        }
    }
}
