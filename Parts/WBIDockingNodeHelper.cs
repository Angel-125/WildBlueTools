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
        public string dockingPortMeshName = string.Empty;

        [KSPField]
        public string weldedMeshName = string.Empty;

        [KSPField]
        public bool keepPartAfterWeld;

        [KSPField]
        public string weldEffect = "RepairSkill";

        [KSPField(isPersistant = true)]
        public bool hasBeenWelded;

        [KSPField(isPersistant = true)]
        public bool watchForDocking = false;

        protected ModuleDockingNode dockingNode;

        //Based on code by Shadowmage. Thanks for showing how it's done, Shadowmage! :)
        [KSPEvent(guiName = "Weld Ports", guiActive = false, unfocusedRange = 3.0f)]
        public void WeldPorts()
        {
            //Check welding requirements
            if (!canWeldPorts())
                return;

            AttachNode sourceNode, targetNode;
            Part sourcePart, targetPart, otherNodePart;
            if (!getNodes(out sourceNode, out targetNode, out sourcePart, out targetPart))
                return;
            otherNodePart = dockingNode.otherNode.part;

            //Sadly, decoupling is giving me odd errors in FlightIntegrator
            //Time for some linked list shenanigans. De-link the docking ports
            clearAttachmentData(dockingNode.otherNode);
            clearAttachmentData(dockingNode);

            //See if we can avoid collisions while moving. Seems to help.
            this.part.SetCollisionIgnores();
            otherNodePart.SetCollisionIgnores();

            //If we aren't keeping the docking ports then we need to move the parts together.
            if (!keepDockingPorts && !keepPartAfterWeld)
            {
                //Calculate the distance between the docking ports
                float distance = Mathf.Abs(Vector3.Distance(sourceNode.position, dockingNode.referenceNode.position));
                distance += Mathf.Abs(Vector3.Distance(targetNode.position, dockingNode.otherNode.referenceNode.position));

                //Now move the target part next to the source part
                targetPart.transform.position = Vector3.MoveTowards(targetPart.transform.position, sourcePart.transform.position, distance);
            }

            //Re-link the ports                
            else 
            {
                this.part.parent = sourcePart;
                this.part.srfAttachNode.attachedPart = sourcePart;
                this.part.attachJoint = PartJoint.Create(this.part, sourcePart, this.part.srfAttachNode, null, AttachModes.SRF_ATTACH);

                otherNodePart.parent = targetPart;
                otherNodePart.srfAttachNode.attachedPart = targetPart;
                otherNodePart.attachJoint = PartJoint.Create(otherNodePart, targetPart, otherNodePart.srfAttachNode, null, AttachModes.SRF_ATTACH);

                //Show the welded mesh
                ShowWeldedMesh(true);
            }

            //Link the parts together
            linkParts(sourceNode, targetNode, sourcePart, targetPart);

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
            if (!keepDockingPorts && !keepPartAfterWeld)
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
            Events["UnsetNodeTarget"].guiActiveUnfocused = true;
            Events["SetNodeTarget"].guiActiveUnfocused = false;

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
            Events["UnsetNodeTarget"].guiActiveUnfocused = false;
            Events["SetNodeTarget"].guiActiveUnfocused = true; 
            
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

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            ShowWeldedMesh(false);
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

            //If we have been welded and we should show the welded mesh then do so.
            if (string.IsNullOrEmpty(weldedMeshName) == false)
                ShowWeldedMesh(hasBeenWelded);

            //Update docking state
            if (dockingNode != null && dockingNode.vesselInfo != null)
                OnDockingStateChanged();
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
            ShowWeldedMesh(hasBeenWelded);
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
                Events["UnsetNodeTarget"].guiActiveUnfocused = false;
                Events["SetNodeTarget"].guiActiveUnfocused = false;

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

            else
            {
                Events["UnsetNodeTarget"].guiActiveUnfocused = false;
                Events["SetNodeTarget"].guiActiveUnfocused = true;
            }
        }

        public void ShowWeldedMesh(bool isVisible)
        {
            if (string.IsNullOrEmpty(weldedMeshName) || string.IsNullOrEmpty(dockingPortMeshName))
                return;

            if (isVisible)
            {
                setMeshVisible(weldedMeshName, true);
                setMeshVisible(dockingPortMeshName, false);
            }

            else
            {
                setMeshVisible(weldedMeshName, false);
                setMeshVisible(dockingPortMeshName, true);
            }
        }

        protected void setMeshVisible(string meshName, bool isVisible)
        {
            Transform[] targets;

            //Get the targets
            targets = part.FindModelTransforms(meshName);
            if (targets == null)
                return;

            foreach (Transform target in targets)
            {
                target.gameObject.SetActive(isVisible);
                Collider collider = target.gameObject.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = isVisible;
            }
        }

        protected bool canWeldPorts()
        {
            bool hasWeldEffect = false;

            //Check for docking ports
            if (dockingNode == null)
            {
                Debug.Log("Part does not contain a docking node.");
                return false;
            }

            if (dockingNode.otherNode == null)
            {
                Debug.Log("There is no docked vessel to weld.");
                return false;
            }

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

            Debug.Log("no attach node found");
            return null;
        }

        protected bool getNodes(out AttachNode sourceNode, out AttachNode targetNode, out Part sourcePart, out Part targetPart)
        {
            sourceNode = findAttachNode(this.part);
            targetNode = findAttachNode(dockingNode.otherNode.part);
            sourcePart = sourceNode.attachedPart;
            targetPart = targetNode.attachedPart;

            if (sourceNode == null)
            {
                Debug.Log("No parent to weld");
                return false;
            }
            if (targetNode == null)
            {
                Debug.Log("Docked port has no parent");
                return false;
            }
            if (sourcePart == null)
            {
                Debug.Log("No source part found.");
                return false;
            }
            if (targetPart == null)
            {
                Debug.Log("No target part found.");
                return false;
            }

            Debug.Log("sourcePart: " + sourcePart.partInfo.title);
            Debug.Log("targetPart:" + targetPart.partInfo.title);
            return true;
        }

        protected void linkParts(AttachNode sourceNode, AttachNode targetNode, Part sourcePart, Part targetPart)
        {
            //Reparent the parts
            if (targetPart.parent == dockingNode.otherNode.part && targetPart.parent != null)
                targetPart.parent = sourcePart;
            if (sourcePart.parent == this.part && sourcePart.parent != null)
                sourcePart.parent = targetPart;

            //Setup top nodes
            sourcePart.topNode.attachedPart = targetPart;
            targetPart.topNode.attachedPart = sourcePart;

            //Destroy original joints
            if (sourcePart.attachJoint != null)
                sourcePart.attachJoint.DestroyJoint();
            if (targetPart.attachJoint != null)
                targetPart.attachJoint.DestroyJoint();

            //Set attached parts
            foreach (AttachNode attachNode in sourcePart.attachNodes)
            {
                if (attachNode.attachedPart == this.part)
                    attachNode.attachedPart = targetPart;
            }
            foreach (AttachNode attachNode in targetPart.attachNodes)
            {
                if (attachNode.attachedPart == this.part)
                    attachNode.attachedPart = sourcePart;
            }

            //Set child parts
            if (sourcePart.children.Contains(this.part))
            {
                sourcePart.children.Remove(this.part);
                sourcePart.addChild(targetPart);
            }
            if (targetPart.children.Contains(dockingNode.otherNode.part))
            {
                targetPart.children.Remove(dockingNode.otherNode.part);
                targetPart.addChild(sourcePart);
            }

            //Set lookup targets
            sourcePart.fuelLookupTargets.AddUnique(targetPart);
            targetPart.fuelLookupTargets.AddUnique(sourcePart);

            //Create new joint
            PartJoint joint = PartJoint.Create(targetPart, sourcePart, targetNode, sourceNode, AttachModes.STACK);
            targetPart.attachJoint = joint;
        }

        protected void clearAttachmentData(ModuleDockingNode node)
        {
            node.part.children.Clear();
            node.part.topNode.attachedPart = null;
            node.part.attachJoint.DestroyJoint();
            node.part.parent = null;

            for (int index = 0; index < node.part.attachNodes.Count; index++)
                node.part.attachNodes[index].attachedPart = null;
        }
    }
}
