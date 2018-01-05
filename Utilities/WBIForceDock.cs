using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using ModuleWheels;

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
    public class WBIForceDock : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool isForceDocked = false;

        [KSPEvent(guiName = "Force Dock", guiActive = false, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = 10f)]
        public void ForceDock()
        {
            ModuleDockingNode dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();

            if (dockingNode == null)
                return;

            if (dockingNode.otherNode == null)
            {
                ScreenMessages.PostScreenMessage("Not close enough to dock.", 5.0f);
                return;
            }

            bool dockContact = dockingNode.CheckDockContact(dockingNode, dockingNode.otherNode, 5.0f, 0f, 0f);
            if (!dockContact)
            {
                ScreenMessages.PostScreenMessage("Not close enough to dock.", 5.0f);
                return;
            }

            if (dockingNode.NodeIsTooFar())
            {
                ScreenMessages.PostScreenMessage("Not close enough to dock.", 5.0f);
                return;
            }

            dockingNode.otherNode.DockToVessel(dockingNode);
            dockingNode.otherNode.Events["Undock"].guiActive = true;
            Events["UndockVessel"].guiActive = true;
            isForceDocked = true;
        }

        [KSPEvent(guiName = "Undock")]
        public void UndockVessel()
        {
            ModuleDockingNode dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();

            if (dockingNode == null)
                return;

            if (dockingNode.otherNode == null)
            {
                ScreenMessages.PostScreenMessage("No port to dock from.", 5.0f);
                return;
            }

            dockingNode.otherNode.Undock();
            Events["UndockVessel"].guiActive = false;
            isForceDocked = false;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            ModuleDockingNode dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();

            if (dockingNode == null)
                return;

            if (isForceDocked)
            {
                dockingNode.otherNode.Events["Undock"].guiActive = true;
                Events["UndockVessel"].guiActive = true;
            }
        }
    }
}
