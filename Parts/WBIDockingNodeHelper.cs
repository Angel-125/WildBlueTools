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
    [KSPModule("Docking Node Helper")]
    public class WBIDockingNodeHelper : PartModule
    {
        [KSPField]
        public string weldEffect = "RepairSkill";

        [KSPField(isPersistant = true)]
        public bool watchForDocking = false;

        [KSPField(isPersistant = true)]
        public bool angleSnapOn = false;

        [KSPField]
        public float snapOffset = 0;

        [KSPField]
        public float portRoll = 30;

        [KSPField]
        public float portTorque = 30;

        [KSPField]
        public float acquireTorque = 10;

        [KSPField]
        public float acquireTorqueRoll = 10;

        WBILight dockingLight;
        protected ModuleDockingNode dockingNode;
        protected float originalAcquireTorque;
        protected float originalAcquireTorqueRoll;
        protected float originalsnapOffset;

        //Based on code by Shadowmage & RoverDude. Thanks for showing how it's done guys! :)
        [KSPEvent(guiName = "Weld Ports", guiActive = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void WeldPorts()
        {
            //Check welding requirements
//            if (!canWeldPorts())
//                return;

            AttachNode sourceNode, targetNode;
            Part sourcePart, targetPart, thisPart, parentPart, otherNodePart;
            if (!getNodes(out sourceNode, out targetNode, out sourcePart, out targetPart))
                return;
            parentPart = this.part.parent;
            thisPart = this.part;
            otherNodePart = dockingNode.otherNode.part;

            otherNodePart.decouple();
            thisPart.decouple();

            sourcePart.Couple(targetPart);

            sourceNode.attachedPart = targetPart;
            targetNode.attachedPart = sourcePart;
            sourcePart.fuelLookupTargets.AddUnique(targetPart);
            targetPart.fuelLookupTargets.AddUnique(sourcePart);

            //cleanup global data from all the changing of parts/etc
            FlightGlobals.ForceSetActiveVessel(thisPart.vessel);
            //if you don't de-activate the GUI it will null-ref because the active window belongs to one of the exploding parts below.
            UIPartActionController.Instance.Deactivate();

            //Cleanup
            otherNodePart.Die();
            thisPart.Die();

            //but then we need to re-activate it to make sure that part-right clicking/etc doesn't break
            UIPartActionController.Instance.Activate();

            GameEvents.onVesselWasModified.Fire(part.vessel);
            /*
            AttachNode sourceNode, targetNode;
            Part sourcePart, targetPart, thisPart, parentPart, otherNodePart;
            if (!getNodes(out sourceNode, out targetNode, out sourcePart, out targetPart))
                return;
            parentPart = this.part.parent;
            thisPart = this.part;
            otherNodePart = dockingNode.otherNode.part;

            //Calculate the gap between the docking ports and the offset vector.
            float distance = Mathf.Abs(Vector3.Distance(sourceNode.position, dockingNode.referenceNode.position));
            distance += Mathf.Abs(Vector3.Distance(targetNode.position, dockingNode.otherNode.referenceNode.position));
            Vector3 offset = calculateOffset(parentPart, thisPart, distance);

            //Clear attachment links. This seems to work better than decoupling (as far as FlightIntegrator is concerned)
            clearAttachmentData(thisPart);
            clearAttachmentData(parentPart);

            //Create new links
            linkParts(sourceNode, targetNode, sourcePart, targetPart);

            //Ignore collisions while we shift things around.
            thisPart.SetCollisionIgnores();
            parentPart.SetCollisionIgnores();

            //If we aren't keeping the part then reposition the stuff attached to the docking ports
            if (!WBIDockingParameters.KeepDockingPorts && !keepPartAfterWeld)
            {
                shiftPart(sourcePart, offset);
            }

            //We're keeping the ports, so surface-attach them.
            else
            {
                parentPart = sourcePart;
                thisPart.srfAttachNode.attachedPart = sourcePart;
                thisPart.attachRules.allowSrfAttach = true;
                thisPart.attachJoint = PartJoint.Create(thisPart, sourcePart, thisPart.srfAttachNode, null, AttachModes.SRF_ATTACH);

                otherNodePart.parent = targetPart;
                otherNodePart.srfAttachNode.attachedPart = targetPart;
                otherNodePart.attachRules.allowSrfAttach = true;
                otherNodePart.attachJoint = PartJoint.Create(otherNodePart, targetPart, otherNodePart.srfAttachNode, null, AttachModes.SRF_ATTACH);

                //Show the welded mesh
                ShowWeldedMesh(true);
            }

            //Create the new joint
            if (thisPart.attachMode == AttachModes.STACK)
            {
                sourcePart.attachJoint = PartJoint.Create(sourcePart, parentPart, targetNode, sourceNode, AttachModes.STACK);
            }
            else
            {
                sourcePart.attachRules.srfAttach = true;
                sourcePart.attachJoint = PartJoint.Create(sourcePart, parentPart, targetNode, null, AttachModes.SRF_ATTACH);
            }

            //Cleanup
            FlightGlobals.ForceSetActiveVessel(sourcePart.vessel);
            UIPartActionController.Instance.Deactivate();
            UIPartActionController.Instance.Activate();
            GameEvents.onVesselWasModified.Fire(this.part.vessel);

            //We do this last because the part itself will be going away if we don't keep the ports.
            if (!WBIDockingParameters.KeepDockingPorts && !keepPartAfterWeld)
            {
                dockingNode.otherNode.part.Die();
                this.part.Die();
            }
            */
        }

        [KSPEvent(guiName = "Control from Here", guiActive = true)]
        public void ControlFromHere()
        {
            watchForDocking = true;
            dockingNode.MakeReferenceTransform();
            TurnAnimationOn();
        }

        [KSPEvent(guiName = "Toggle Angle Snap", guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = true, unfocusedRange = 200f)]
        public void ToggleAngleSnap()
        {
            angleSnapOn = !angleSnapOn;

            updateAngleSnap();
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
            //Turn on the docking lights if we have any
            if (dockingLight != null)
            {
                dockingLight.TurnOnLights();
                return;
            }

            //Legacy method of turning on docking lights.
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
            //Turn on the docking lights if we have any
            if (dockingLight != null)
            {
                dockingLight.TurnOffLights();
                return;
            }

            //Legacy method of turning off docking lights.
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
            dockingLight = this.part.FindModuleImplementing<WBILight>();
            onGameSettingsApplied();

            //Hide the native events
            if (dockingNode != null)
            {
                dockingNode.Events["SetAsTarget"].guiActiveUnfocused = false;
                dockingNode.Events["UnsetTarget"].guiActiveUnfocused = false;
                dockingNode.Events["MakeReferenceTransform"].guiActive = false;
                originalsnapOffset = dockingNode.snapOffset;
                originalAcquireTorque = dockingNode.acquireTorque;
                originalAcquireTorqueRoll = dockingNode.acquireTorqueRoll;
            }

            //Update GUI
            UpdateWeldGUI();
            updateAngleSnap();

            //Update docking state
            if (dockingNode != null && dockingNode.vesselInfo != null)
                OnDockingStateChanged();
        }

        protected void updateAngleSnap()
        {
            if (angleSnapOn)
            {
                Events["ToggleAngleSnap"].guiName = "Turn Off Angle Snap";
                dockingNode.snapRotation = true;
                dockingNode.snapOffset = snapOffset;
                //                dockingNode.captureMinRollDot = 0.999f;
                dockingNode.acquireTorque = acquireTorque;
                dockingNode.acquireTorqueRoll = acquireTorqueRoll;
            }
            else
            {
                Events["ToggleAngleSnap"].guiName = "Turn On Angle Snap";
                dockingNode.snapRotation = false;
                dockingNode.snapOffset = originalsnapOffset;
                //                dockingNode.captureMinRollDot = float.MinValue;
                dockingNode.acquireTorque = originalAcquireTorque;
                dockingNode.acquireTorqueRoll = originalAcquireTorqueRoll;
            }
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

        public void OnDestroy()
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
                //Events["WeldPorts"].guiActive = !WBIDockingParameters.WeldRequiresEVA;
            }
        }

        //Based on code by RoverDude. Thanks for showing how it's done. :)
        private Vector3 calculateOffset(Part sourcePart, Part targetPart, float distance)
        {
            var objA = new GameObject();
            var objB = new GameObject();

            Transform targetTransform = objA.transform;
            Transform sourceTransform = objB.transform;

            targetTransform.localPosition = this.part.parent.transform.localPosition;
            sourceTransform.localPosition = this.part.transform.localPosition;

            Vector3 offset = targetTransform.localPosition - sourceTransform.localPosition;
            offset.Normalize();

            offset *= distance;

            return offset;
        }

        //Based on code by RoverDude. Thanks for showing how it's done. :)
        protected void shiftPart(Part movingPart, Vector3 offset)
        {
            if (movingPart.Rigidbody != null && movingPart.physicalSignificance == Part.PhysicalSignificance.FULL)
                movingPart.transform.position += offset;

            movingPart.UpdateOrgPosAndRot(movingPart.vessel.rootPart);

            foreach (Part childPart in movingPart.children)
                shiftPart(childPart, offset);
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
            if (WBIDockingParameters.WeldRequiresEVA && FlightGlobals.ActiveVessel.isEVA == false)
            {
                ScreenMessages.PostScreenMessage("Welding requires a kerbal on EVA with the repair skill.");
                return false;
            }

            //Check skill requirement
            if (WBIDockingParameters.WeldRequiresRepairSkill && Utils.IsExperienceEnabled())
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
            sourceNode.attachedPart = targetPart;
            targetNode.attachedPart = sourcePart;

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

        }

        protected void clearAttachmentData(Part doomed)
        {
            doomed.children.Clear();
            doomed.topNode.attachedPart = null;
            doomed.attachJoint.DestroyJoint();
            doomed.parent = null;

            for (int index = 0; index < doomed.attachNodes.Count; index++)
                doomed.attachNodes[index].attachedPart = null;
        }
    }
}
