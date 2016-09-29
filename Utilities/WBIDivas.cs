using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Reflection;
//using KIS;

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
    public class WBIDivas : PartModule
    {
        private const double SPAWN_WAIT_TIME = 0.25f;

        [KSPField()]
        public string objects = string.Empty;

        [KSPField(isPersistant = true)]
        public string currentDiva = string.Empty;

        WBIResourceSwitcher switcher;
        bool spawnTimer;
        bool waitForStartComplete;
        DateTime timeStamp;
        double elapsedTime;
        string[] objectNames;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Object names
            if (string.IsNullOrEmpty(objects) == false)
                objectNames = objects.Split(new char[] { ';' });

            //Get switcher
            switcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();
            if (switcher != null)
            {
                switcher.onModuleRedecorated += new ModuleRedecoratedEvent(switcher_onModuleRedecorated);
                waitForStartComplete = true;
            }
        }

        void switcher_onModuleRedecorated(ConfigNode templateNode)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                 setupIVA(templateNode);
            }
        }

        protected ConfigNode getDivasNode(ConfigNode templateNode)
        {
            ConfigNode nodeDivas = null;

            //If the part currently does not have an IVA then we're done.
            if (this.part.CrewCapacity == 0)
            {
                Debug.Log("Part does not have crew capacity, exiting.");
                return null;
            }

            //Get the divas node
            nodeDivas = templateNode.GetNode("DIVAS");
            if (nodeDivas == null)
            {
                Debug.Log("Template does not have a DIVAS node, exiting.");
                return null;
            }

            //Get the name of the DIVA
            if (nodeDivas.HasValue("name"))
            {
                string divaName = nodeDivas.GetValue("name");
                currentDiva = divaName;
            }
            else
            {
                Debug.Log("DIVAS node is unnamed, exiting.");
                return null;
            } 
            
            return nodeDivas;
        }

        protected ProtoCrewMember[] removeCrew()
        {
            Debug.Log("removeCrew called");
            ProtoCrewMember[] crewList = null;
            ProtoCrewMember crew;

            //If we have no crew, then we're done
            if (this.part.protoModuleCrew.Count == 0)
                return null;

            crewList = this.part.protoModuleCrew.ToArray();
            Debug.Log("crew count: " + crewList.Length);
            for (int index = 0; index < crewList.Length; index++)
            {
                crew = crewList[index];

                //Remove kerbal from old seat
                this.part.RemoveCrewmember(crew);
                crew.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                GameEvents.onVesselChange.Fire(this.part.vessel);
                Debug.Log(crew.KerbalRef.crewMemberName + " temporarily removed from " + this.part.partInfo.title);
            }

            return crewList;
        }

        protected void restoreCrew(ProtoCrewMember[] crewList, ConfigNode nodeDivas)
        {
            ProtoCrewMember crew;
            int seatIndex = 0;
            InternalSeat destinationSeat = null;
            string[] seatAssignments = nodeDivas.GetValues("seat");
            InternalSeat[] internalSeats = this.part.internalModel.seats.ToArray();

            Dictionary<string, InternalSeat> seats = new Dictionary<string, InternalSeat>();

            //Map the seats
            if (seatAssignments == null || seatAssignments.Length == 0)
            {
                return;
            }
            for (int index = 0; index < internalSeats.Length; index++)
            {
                seats.Add(internalSeats[index].seatTransformName, internalSeats[index]);
            }

            //Clear the seat list and build it back based upon the seat assignment order.
            //That way the game will put kerbals in the correct spots when the part is occupied.
            this.part.internalModel.seats.Clear();
            for (int index = 0; index < seatAssignments.Length; index++)
            {
                destinationSeat = seats[seatAssignments[index]];
                this.part.internalModel.seats.Add(destinationSeat);
            }
            
            //Make sure we have a crew list
            if (crewList == null)
            {
                Debug.Log("No crew to restore, part was unoccupied, exiting");
                return;
            }

            //Disable CrewHatchController
            CrewHatchController.fetch.DisableInterface();

            //Assign crew to seats
            for (int index = 0; index < crewList.Length; index++)
            {
                crew = crewList[index];

                //Get a new seat assignment
                destinationSeat = seats[seatAssignments[seatIndex]];
                seatIndex += 1;

                //If we've run out of seat's that's bad...
                if (destinationSeat == null)
                {
                    Debug.Log("Not enough seats to restore the crew!!!");
                    break;
                }

                //Add kerbal to new seat
                this.part.AddCrewmemberAt(crew, this.part.internalModel.seats.IndexOf(destinationSeat));
                this.part.vessel.SpawnCrew();
                this.part.RegisterCrew();
                GameEvents.onVesselChange.Fire(this.part.vessel);

                Debug.Log("crew " + crew.KerbalRef.crewMemberName + " seated at " + crew.seat.seatTransformName);

                //Move inventory?
            }

            //It takes time for KSP to catch up. Give the game a bit of time...
            spawnTimer = true;
            timeStamp = DateTime.Now;
            elapsedTime = 0;
        }

        protected void changeInternalView(ConfigNode nodeDivas)
        {
            string visibleObjects = nodeDivas.GetValue("objects");
            string objectName;
            Transform target;

            //Go through all the objects we know of and either hide or show them as needed.
            for (int index = 0; index < objectNames.Length; index++)
            {
                //Get the object name
                objectName = objectNames[index];

                //Find the transform
                target = part.internalModel.FindModelTransform(objectName);

                //Hide the transform if the template has no visible objects.
                if (string.IsNullOrEmpty(visibleObjects))
                {
                    target.gameObject.SetActive(false);
                }

                //Now show/hide the transform
                else
                {
                    if (visibleObjects.Contains(objectName))
                        target.gameObject.SetActive(true);
                    else
                        target.gameObject.SetActive(false);
                }
            }

            //Clear out the props and their associated prop modules
//            clearProps();

            //Load the props for the new view.
//            loadProps(nodeDivas);
        }

        protected void clearProps()
        {
            InternalProp[] props = this.part.internalModel.props.ToArray();
            InternalProp prop = null;
            InternalModule[] internalModules;
            InternalModule module = null;

            for (int index = 0; index < props.Length; index++)
            {
                prop = props[index];
                prop.enabled = false;
                this.part.internalModel.props.Remove(prop);

                if (prop.internalModules.Count > 0)
                {
                    internalModules = prop.internalModules.ToArray();
                    for (int moduleIndex = 0; moduleIndex < internalModules.Length; moduleIndex++)
                    {
                        module = internalModules[moduleIndex];
                        prop.internalModules.Remove(module);
                        module.gameObject.DestroyGameObject();
                    }
                }
                prop.internalModules.Clear();
                prop.gameObject.DestroyGameObject();
            }
        }

        protected void loadProps(ConfigNode nodeDivas)
        {
            Debug.Log("Loading props");

            //Do we have any props?
            if (nodeDivas.HasValue("divaProps") == false)
            {
                Debug.Log("DIVA does not have props, exiting");
                return;
            }

            ConfigNode[] divaConfigs = GameDatabase.Instance.GetConfigNodes("DIVA_PROPS");
            ConfigNode divaProps = null;
            string name, divaPropsName;
            List<ConfigNode> propConfigs = new List<ConfigNode>();

            //Get the props name
            divaPropsName = nodeDivas.GetValue("divaProps");

            //Find the template's prop list
            for (int index = 0; index < divaConfigs.Length; index++)
            {
                divaProps = divaConfigs[index];
                if (divaProps.HasValue("name"))
                {
                    name = divaProps.GetValue("name");
                    if (name == divaPropsName)
                        break;

                    else
                        divaProps = null;
                }
            }

            //Did we find any props?
            if (divaProps == null)
            {
                Debug.Log("No props found, exiting");
                return;
            }

            //Now get all the props
            if (divaProps.HasNode("PROP") == false)
            {
                Debug.Log("No PROP nodes found, exiting");
                return;
            }
            divaConfigs = divaProps.GetNodes("PROP");

            //Load em up
            for (int index = 0; index < divaConfigs.Length; index++)
            {
                this.part.internalModel.AddProp(divaConfigs[index]);
                if (divaConfigs[index].HasNode("MODULE"))
                    this.part.internalModel.AddPropModule(divaConfigs[index]);
            }
            Debug.Log("Prop count: " + this.part.internalModel.props.Count);
        }

        /*
        protected void changeInternalView(ConfigNode nodeDivas)
        {
            Debug.Log("changeInternalView called");

            ConfigNode ivaNode = null;
            ConfigNode internalConfig = null;
            ConfigNode[] internalConfigs = GameDatabase.Instance.GetConfigNodes("INTERNAL");
            string ivaName;
            if (internalConfigs == null || internalConfigs.Length == 0)
            {
                Debug.Log("No internal configs found!!!");
                return;
            }

            //Find the IVA config we want
            for (int index = 0; index < internalConfigs.Length; index++)
            {
                ivaNode = internalConfigs[index];
                if (ivaNode.HasValue("name"))
                {
                    ivaName = ivaNode.GetValue("name");
                    if (ivaName == currentDiva)
                    {
                        internalConfig = ivaNode;
                        break;
                    }
                }
                else
                {
                    Debug.Log("ivaNode does not have 'name' field!");
                }
            }

            //Make sure we got the config
            if (internalConfig == null)
            {
                Debug.Log("Could not find IVA named " + currentDiva);
                return;
            }

            //Ok, clear out the seats and props
            Debug.Log("Clearing out the old IVA");
//            this.part.DespawnIVA();
            this.part.internalModel.seats.Clear();
            this.part.internalModel.props.Clear();
            GameEvents.onVesselChange.Fire(this.part.vessel);

            //Set up the IVA
            Debug.Log("Setting up new IVA");
            this.part.InternalModelName = currentDiva;
            this.part.internalModel.internalConfig = internalConfig;
            this.part.internalModel.internalName = currentDiva;
            this.part.internalModel.Load(internalConfig);
//            this.part.internalModel.Initialize(this.part);
//            this.part.SpawnIVA();
            GameEvents.onVesselChange.Fire(this.part.vessel);

            Debug.Log(this.part.partInfo.title + " IVA changed to " + currentDiva);
        }
         */

        protected void setupIVA(ConfigNode templateNode)
        {
            Debug.Log("setupIVA called");
            ConfigNode nodeDivas = getDivasNode(templateNode);
            if (nodeDivas == null)
                return;

            //Remove crew from the part
            ProtoCrewMember[] crewList = removeCrew();

            //Change the internal config
            changeInternalView(nodeDivas);

            //Add crew back to part
            restoreCrew(crewList, nodeDivas);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (waitForStartComplete)
            {
                if (this.part.started)
                {
                    waitForStartComplete = false;
                    setupIVA(switcher.CurrentTemplate);
                }
            }

            if (spawnTimer)
            {
                elapsedTime = (DateTime.Now - timeStamp).TotalSeconds;
                if (elapsedTime >= SPAWN_WAIT_TIME)
                {
                    spawnTimer = false;
                    this.part.vessel.SpawnCrew();
                    GameEvents.onVesselChange.Fire(this.part.vessel);
                    CameraManager.ICameras_ResetAll();
                    CrewHatchController.fetch.EnableInterface();
                }
            }
        }

    }
}
