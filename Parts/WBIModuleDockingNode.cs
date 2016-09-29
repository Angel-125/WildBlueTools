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
    [KSPModule("Docking Node Helper")]
    public class WBIDockingNodeHelper : PartModule
    {
        ModuleDockingNode dockingNode;

        [KSPEvent(guiName = "FFControl from Here", guiActive = true)]
        public void ControlFromHere()
        {
            dockingNode.MakeReferenceTransform();
            TurnAnimationOn();
        }

        [KSPEvent(guiName = "Set as Target", guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 200f)]
        public void SetNodeTarget()
        {
            WBIDockingNodeHelper dockingModule = null;

            //Turn off all the glowing docking ports.
            foreach (Part part in this.vessel.parts)
            {
                //See if the part has a docking module
                dockingModule = part.FindModuleImplementing<WBIDockingNodeHelper>();
                if (dockingModule == null)
                    continue;

                //It does! Now turn off the glow animation
                if (dockingModule != this)
                    dockingModule.TurnAnimationOff();
            }

            //Turn our animation on
            TurnAnimationOn();

            //And call the real SetAsTarget
            dockingNode.SetAsTarget();
        }

        [KSPEvent(guiName = "Unset Target", guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 200f)]
        public void UnsetNodeTarget()
        {
            TurnAnimationOff();

            dockingNode.UnsetTarget();
        }

        public void TurnAnimationOn()
        {
            ModuleAnimateGeneric glowAnim = null;

            //Get our glow animation (if any)
            glowAnim = this.part.FindModuleImplementing<ModuleAnimateGeneric>();
            if (glowAnim == null)
                return;

            //Ok, now turn on our glow panel if it isn't already.            
            if (glowAnim.Events["Toggle"].guiName == glowAnim.startEventGUIName)
                glowAnim.Toggle();
        }

        public void TurnAnimationOff()
        {
            ModuleAnimateGeneric glowAnim = this.part.FindModuleImplementing<ModuleAnimateGeneric>();

            if (glowAnim == null)
                return;

            //Turn off the glow animation
            if (glowAnim.Events["Toggle"].guiName == glowAnim.endEventGUIName)
                glowAnim.Toggle();
        }

        public override void OnStart(StartState st)
        {
            base.OnStart(st);

            dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();

            //Hide the native events
            if (dockingNode != null)
            {
                dockingNode.Events["SetAsTarget"].guiActiveUnfocused = false;
                dockingNode.Events["UnsetTarget"].guiActiveUnfocused = false;
                //dockingNode.Events["MakeReferenceTransform"].guiActive = false;
            }
        }
    }
}
