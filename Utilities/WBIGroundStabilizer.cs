using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using ModuleWheels;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
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

    public class WBIGroundStabilizer : ModuleAnimateGenericSFX
    {
        FixedUpdateHelper fixedUpdateHelper;

        [KSPField()]
        public bool stabilizeWhenDeployed = true;

        Vector3 vesselPos = Vector3.zero;

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Disable Docking Port")]
        public void ToggleDockingPort()
        {
            ModuleDockingNode dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();

            if (dockingNode != null)
            {
                if (dockingNode.isEnabled)
                {
                    dockingNode.enabled = false;
                    dockingNode.isEnabled = false;
                    Events["ToggleDockingPort"].guiName = "Enable Docking Port";
                }

                else
                {
                    dockingNode.enabled = true;
                    dockingNode.isEnabled = true;
                    Events["ToggleDockingPort"].guiName = "Disable Docking Port";
                }
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (fixedUpdateHelper == null && HighLogic.LoadedSceneIsFlight)
            {
                fixedUpdateHelper = this.part.gameObject.AddComponent<FixedUpdateHelper>();
                fixedUpdateHelper.onFixedUpdateDelegate = OnUpdateFixed;
                fixedUpdateHelper.enabled = true;
            }

            ModuleDockingNode dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();
            if (!dockingNode)
                return;

            if (dockingNode.isEnabled)
            {
                Events["ToggleDockingPort"].guiName = "Disable Docking Port";
            }

            else
            {
                Events["ToggleDockingPort"].guiName = "Enable Docking Port";
            }
        }

        public void OnUpdateFixed()
        {
            try
            {
                bool isDeployed = false;
                bool stabilizeVessel = false;

                //Is the animation deployed?
                if (Events["Toggle"].guiName == startEventGUIName)
                    isDeployed = false;
                else if (Events["Toggle"].guiName == endEventGUIName)
                    isDeployed = true;

                //See if we should stabilize the vessel
                if (isDeployed && stabilizeWhenDeployed)
                    stabilizeVessel = true;
                else if (!isDeployed && !stabilizeWhenDeployed)
                    stabilizeVessel = true;

                //If the legs are deployed then apply downward force
                if ((aniState == animationStates.LOCKED || aniState == animationStates.CLAMPED) && stabilizeVessel)
                {
                    this.part.vessel.verticalSpeed = 0f;
                    this.part.vessel.horizontalSrfSpeed = 0f;
                    this.part.vessel.velocityD = Vector3.zero;
                    this.part.vessel.SetWorldVelocity(Vector3.zero);
                    this.part.vessel.acceleration = Vector3.zero;
                    this.part.vessel.angularVelocity = Vector3.zero;
                    this.part.vessel.geeForce = 0f;
                    this.part.vessel.angularMomentum = Vector3.zero;

                    Part[] parts = this.part.vessel.parts.ToArray();
                    Part currentPart;
                    for (int index = 0; index < parts.Length; index++)
                    {
                        currentPart = parts[index];
                        currentPart.Rigidbody.velocity = Vector3.zero; //*= 0.0001f;
                        currentPart.Rigidbody.angularVelocity = Vector3.zero; //*= 0.0001f;
                    }
                }
            }
            catch { }
        }
    }
}
