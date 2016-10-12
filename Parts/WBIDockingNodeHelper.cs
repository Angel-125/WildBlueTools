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
        public bool requiresEVA = false;
        public bool requiresRepairSkill = false;
        public bool keepDockingPorts = false;

        [KSPField]
        public string weldEffect = "RepairSkill";

        [KSPField(isPersistant = true)]
        public bool hasBeenWelded;

        [KSPField(isPersistant = true)]
        public bool watchForDocking = false;

        protected ModuleDockingNode dockingNode;

        [KSPEvent(guiActive = true)]
        public void RotateShip()
        {
            Vessel targetVessel = this.part.vessel.targetObject.GetVessel();
            List<ModuleDockingNode> dockingNodes = targetVessel.FindPartModulesImplementing<ModuleDockingNode>();
            ModuleDockingNode node = dockingNodes[0];

            Vector3 thisEuler = this.part.vessel.vesselTransform.rotation.eulerAngles;
            Vector3 dockEuler = node.part.transform.rotation.eulerAngles;

            Vector3 diff = thisEuler - dockEuler;
            ScreenMessages.PostScreenMessage("diff: " + diff);
            Debug.Log("diff: " + diff);
            this.part.vessel.vesselTransform.Rotate(0,diff.y,0);

            ScreenMessages.PostScreenMessage("This Euler: " + thisEuler);
            ScreenMessages.PostScreenMessage("dock Euler: " + dockEuler);
            Debug.Log("FRED This Euler: " + thisEuler);
            Debug.Log("FRED dock Euler: " + dockEuler);

            //this.part.vessel.vesselTransform.rotation = node.part.transform.rotation;

            //this.part.vessel.vesselTransform.rotation = this.part.vessel.targetObject.GetTransform().rotation;
            //this.part.vessel.vesselTransform.rotation = FlightGlobals.fetch.vesselTargetTransform.rotation;
            //this.part.vessel.vesselTransform.Rotate(0, 10f, 0);
        }

        //Based on code by Shadowmage. Thanks for showing how it's done, Shadowmage! :)
        [KSPEvent(guiName = "Weld Ports", guiActive = false, unfocusedRange = 3.0f)]
        public void WeldPorts()
        {
            AttachNode sourceNode = findAttachNode(this.part);
            AttachNode targetNode = findAttachNode(dockingNode.otherNode.part);
            Part sourcePart = sourceNode.attachedPart;
            Part targetPart = targetNode.attachedPart;

            //Check welding requirements
            if (!canWeldPorts())
                return;

            //Check for docking ports
            if (dockingNode == null)
            {
                Debug.Log("Part does not contain a docking node.");
                return;
            }

            if (dockingNode.otherNode == null)
            {
                Debug.Log("There is no docked vessel to weld.");
                return;
            }

            //Check for parent parts
            if (sourceNode == null)
            {
                Debug.Log("No parent to weld");
                return;
            }
            if (targetNode == null)
            {
                Debug.Log("Docked port has no parent");
                return;
            }
            if (sourcePart == null)
            {
                Debug.Log("No source part found.");
                return;
            }
            if (targetPart == null)
            {
                Debug.Log("No target part found.");
                return;
            }

            //Decouple the attached parts from the docking ports
            sourcePart.decouple(0);
            targetPart.decouple(0);
 
            //If we aren't keeping the docking ports then we need to move the parts together.
            if (!keepDockingPorts)
            {
                //See if we can avoid collisions while moving.
                this.part.SetCollisionIgnores();
                dockingNode.otherNode.part.SetCollisionIgnores();

                //Calculate the distance between the docking ports
                float distance = Mathf.Abs(Vector3.Distance(sourceNode.position, dockingNode.referenceNode.position));
                distance += Mathf.Abs(Vector3.Distance(targetNode.position, dockingNode.otherNode.referenceNode.position));

                //Now move the target part next to the source part
                targetPart.transform.position = Vector3.MoveTowards(targetPart.transform.position, sourcePart.transform.position, distance);
            }

            //Surface-attach the ports back to their parents.                
            else 
            {
                this.part.attachMode = AttachModes.SRF_ATTACH;
                sourcePart.Couple(this.part);
            }

            //Weld the parts
            sourcePart.Couple(targetPart);
            sourceNode.attachedPart = targetPart;
            targetNode.attachedPart = sourcePart;
            sourcePart.fuelLookupTargets.AddUnique(targetPart);
            targetPart.fuelLookupTargets.AddUnique(sourcePart);

            //Update the GUI
            hasBeenWelded = true;
            UpdateWeldGUI();
            WBIDockingNodeHelper otherNodeHelper = dockingNode.otherNode.part.FindModuleImplementing<WBIDockingNodeHelper>();
            if (otherNodeHelper != null)
            {
                otherNodeHelper.hasBeenWelded = true;
                otherNodeHelper.OnDockingStateChanged();
            }
 
            //Cleanup
            FlightGlobals.ForceSetActiveVessel(sourcePart.vessel);
            UIPartActionController.Instance.Deactivate();
            UIPartActionController.Instance.Activate();
            GameEvents.onVesselWasModified.Fire(this.part.vessel);

            //We do this last because the part itself will be going away if we don't keep the ports.
            if (!keepDockingPorts)
            {
                dockingNode.otherNode.part.Die();
                this.part.Die();
            }
        }

        [KSPEvent(guiName = "Control from Here", guiActive = true)]
        public void ControlFromHere()
        {
            watchForDocking = true;
            dockingNode.MakeReferenceTransform();
            TurnAnimationOn();
        }

        [KSPEvent(guiName = "Set as Target", guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 200f)]
        public void SetNodeTarget()
        {
            //Start watching for our docking event.
            watchForDocking = true;

            //GUI update
            Events["UnsetNodeTarget"].guiActive = true;
            Events["SetNodeTarget"].guiActive = false;

            //Turn off all the glowing docking ports.
            List<WBIDockingNodeHelper> dockingHelpers = this.part.vessel.FindPartModulesImplementing<WBIDockingNodeHelper>();
            foreach (WBIDockingNodeHelper dockingHelper in dockingHelpers)
                dockingHelper.TurnAnimationOff();

            //Turn our animation on
            TurnAnimationOn();

            //And call the real SetAsTarget
            dockingNode.SetAsTarget();
        }

        [KSPEvent(guiName = "Unset Target", guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 200f)]
        public void UnsetNodeTarget()
        {
            watchForDocking = false;

            //GUI update
            Events["UnsetNodeTarget"].guiActive = false;
            Events["SetNodeTarget"].guiActive = true; 
            
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

            GameEvents.onSameVesselDock.Add(onSameVesselDock);
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);
            GameEvents.onPartUndock.Add(onPartUndock);

            dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();
            onGameSettingsApplied();

            //Hide the native events
            if (dockingNode != null)
            {
                dockingNode.Events["SetAsTarget"].guiActiveUnfocused = false;
                dockingNode.Events["UnsetTarget"].guiActiveUnfocused = false;
                dockingNode.Events["MakeReferenceTransform"].guiActive = false;
            }

            //Update GUI
            UpdateWeldGUI();
            Events["UnsetNodeTarget"].guiActive = false;
            Events["SetNodeTarget"].guiActive = true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //Workaround: Watch to see when we dock. When we do, update the GUI.
            if (watchForDocking)
            {
                if (dockingNode != null && dockingNode.vesselInfo != null)
                {
                    //Update docking state
                    OnDockingStateChanged();
                }
            }
        }

        public void Destroy()
        {
            GameEvents.onSameVesselDock.Remove(onSameVesselDock);
            GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
        }

        public void onPartUndock(Part undockedPart)
        {
            if (undockedPart == this.part)
                OnDockingStateChanged();
        }

        public void onGameSettingsApplied()
        {
            WBIDockingParameters dockingParameters = HighLogic.CurrentGame.Parameters.CustomParams<WBIDockingParameters>();
            if (dockingParameters == null)
            {
                Debug.Log("Can't find docking parameters");
                return;
            }

            requiresEVA = dockingParameters.WeldRequiresEVA;
            requiresRepairSkill = dockingParameters.WeldRequiresRepairSkill;
            keepDockingPorts = dockingParameters.KeepDockingPorts;

            UpdateWeldGUI();
        }

        public void onSameVesselDock(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> evnt)
        {
            OnDockingStateChanged();
        }

        public void OnDockingStateChanged()
        {
            watchForDocking = false;
            onGameSettingsApplied();
            TurnAnimationOff();
            UpdateWeldGUI();
        }

        public void UpdateWeldGUI()
        {
            if (dockingNode == null)
                dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();
            if (dockingNode == null)
            {
                Debug.Log("Can't find a dockingNode");
                return;
            }

            //Welding GUI
            if (dockingNode.vesselInfo != null)
            {
                Events["WeldPorts"].guiActive = !requiresEVA;
                Events["WeldPorts"].guiActiveUnfocused = requiresEVA;
            }

            //If we've already done the welding then disable the GUI.
            if (hasBeenWelded)
            {
                dockingNode.enabled = false;
                dockingNode.isEnabled = false;
                dockingNode.moduleIsEnabled = false;
                Events["WeldPorts"].guiActiveUnfocused = false;
                Events["WeldPorts"].guiActive = false;

                ModuleAnimateGeneric glowAnim = this.part.FindModuleImplementing<ModuleAnimateGeneric>();
                if (glowAnim != null)
                {
                    if (glowAnim.Events["Toggle"].guiName == glowAnim.endEventGUIName)
                        glowAnim.Toggle();

                    glowAnim.enabled = false;
                    glowAnim.isEnabled = false;
                    glowAnim.moduleIsEnabled = false;
                }

                //If we're welded to a docking port without a WBIDockingNodeHelper then manually disable the other port.
                if (dockingNode != null && dockingNode.otherNode != null)
                {
                    WBIDockingNodeHelper otherNodeHelper = dockingNode.otherNode.part.FindModuleImplementing<WBIDockingNodeHelper>();
                    if (otherNodeHelper == null)
                    {
                        dockingNode.otherNode.enabled = false;
                        dockingNode.otherNode.isEnabled = false;
                        dockingNode.otherNode.moduleIsEnabled = false;
                    }
                }
            }
        }

        protected bool canWeldPorts()
        {
            bool hasWeldEffect = false;

            //Check EVA requirement
            if (requiresEVA && FlightGlobals.ActiveVessel.isEVA == false)
            {
                ScreenMessages.PostScreenMessage("Welding requires a kerbal on EVA with the repair skill.");
                return false;
            }

            //Check skill requirement
            if (requiresRepairSkill && Utils.IsExperienceEnabled())
            {
                List<ProtoCrewMember> crewMembers = FlightGlobals.ActiveVessel.GetVesselCrew();

                foreach (ProtoCrewMember astronaut in crewMembers)
                {
                    if (astronaut.HasEffect(weldEffect))
                    {
                        return true;
                    }
                }
                if (!hasWeldEffect)
                {
                    ScreenMessages.PostScreenMessage("Welding requires a kerbal with the ability to effect repairs.");
                    return false;
                }
            }

            return true;
        }

        protected AttachNode findAttachNode(Part searchPart)
        {
            //Try stack nodes first
            foreach (AttachNode attachNode in searchPart.attachNodes)
            {
                if (attachNode.attachedPart == searchPart.parent && attachNode.attachedPart != null)
                {
                    return attachNode;
                }
                else
                {
                    foreach (Part childPart in searchPart.children)
                    {
                        if (attachNode.attachedPart == childPart)
                        {
                            return attachNode;
                        }
                    }
                }
            }

            //Try for surface attach
            if (searchPart.srfAttachNode != null)
            {
                return searchPart.srfAttachNode;
            }

            Debug.Log("FRED no attach node found");
            return null;
        }
    }
}
