using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

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
    public class WBIModuleMirroredSolarPanel : ModuleDeployableSolarPanel
    {
        [KSPField(isPersistant = true)]
        public int primaryPanelIndex;

        [KSPField()]
        public string panels = string.Empty;

        [KSPField()]
        public string sunCatchers = string.Empty;

        protected string[] transformNames;
        protected string[] suncatcherTransformNames;
        protected bool suncatcherTransformUpdated;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            char[] delimiters = { ';' };
            transformNames = panels.Replace(" ", "").Split(delimiters);
            suncatcherTransformNames = sunCatchers.Replace(" ", "").Split(delimiters);

            setupPanels();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Mirror Panel")]
        public void MirrorPanel()
        {
            primaryPanelIndex += 1;

            if (primaryPanelIndex >= transformNames.Length)
                primaryPanelIndex = 0;

            suncatcherTransformUpdated = false;
            setupPanels();
        }

        protected void setupPanels()
        {
            setPanelVisible(primaryPanelIndex, true);

            for (int index = 0; index < transformNames.Length; index++)
            {
                if (index != primaryPanelIndex)
                    setPanelVisible(index, false);
            }

        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (deployState != ModuleDeployablePart.DeployState.EXTENDED)
                return;
            if (suncatcherTransformUpdated)
                return;

            Transform transform = this.part.FindModelTransforms(suncatcherTransformNames[primaryPanelIndex]).First();
            panelRotationTransform = transform;
            trackingDotTransform = transform;

            suncatcherTransformUpdated = true;
        }

        protected void setPanelVisible(int index, bool isVisible)
        {
            string transformName;
            Transform[] targets;
            Transform target;

            //Sanity checks
            if (transformNames == null || transformNames.Length == 0)
            {
                Debug.Log("transformNames are null");
                return;
            }

            //Get the transform to adjust
            transformName = transformNames[index];

            //Get the targets
            targets = part.FindModelTransforms(transformName);
            if (targets == null)
            {
                Debug.Log("No targets found for " + transformName);
                return;
            }

            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                target = targets[targetIndex];

                target.gameObject.SetActive(isVisible);
                Collider collider = target.gameObject.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = isVisible;
            }

        }

    }
}
