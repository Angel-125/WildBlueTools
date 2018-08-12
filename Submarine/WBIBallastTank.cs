using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2018, by Michael Billard (Angel-125)
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
    public enum BallastTankTypes
    {
        Ballast,
        ForwardTrim,
        AftTrim
    }

    public enum BallastVentStates
    {
        Closed,
        FloodingBallast,
        VentingBallast
    }

    [KSPModule("Ballast Tank")]
    public class WBIBallastTank: PartModule
    {
        #region Constants
        public static string kClosed = "Closed";
        public static string kFilling = "Filling";
        public static string kVenting = "Venting";
        #endregion

        #region Fields
        /// <summary>
        /// Name of the part's intake transform.
        /// </summary>
        [KSPField]
        public string intakeTransformName = "intakeTransform";

        /// <summary>
        /// Ballast resource
        /// </summary>
        [KSPField]
        public string ballastResourceName = "IntakeLqd";

        /// <summary>
        /// Name of the venting effect to play when the tank is taking on ballast.
        /// </summary>
        [KSPField]
        public string addBallastEffect = string.Empty;

        /// <summary>
        /// Name of the venting effect to play when the tank is venting ballast.
        /// </summary>
        [KSPField]
        public string ventBallastEffect = string.Empty;

        /// <summary>
        /// How many units per second to fill the ballast tank
        /// </summary>
        [KSPField]
        public double fillRate = 5.0f;

        /// <summary>
        /// How many units per second to vent the ballast tank
        /// </summary>
        [KSPField]
        public double ventRate = 5.0f;

        /// <summary>
        /// Current display state of the ballast tank
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Vent State")]
        public string ventStateString;

        /// <summary>
        /// Type of ballast tank
        /// </summary>
        [KSPField(isPersistant = true)]
        public BallastTankTypes tankType;

        /// <summary>
        /// Current state of the ballast tank
        /// </summary>
        [KSPField(isPersistant = true)]
        public BallastVentStates ventState;
        #endregion

        #region Housekeeping
        protected float baseBuoyancy = 0.0f;
        protected PartResource resourceBallast;
        protected Transform[] intakeTransforms;
        protected Part hostPart;
        #endregion

        #region Events and Actions
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Tank Type: Ballast")]
        public void ToggleTankType()
        {
            switch (tankType)
            {
                case BallastTankTypes.Ballast:
                default:
                    tankType = BallastTankTypes.ForwardTrim;
                    break;

                case BallastTankTypes.ForwardTrim:
                    tankType = BallastTankTypes.AftTrim;
                    break;

                case BallastTankTypes.AftTrim:
                    tankType = BallastTankTypes.Ballast;
                    break;
            }

            updateGUI();

            int count = this.part.symmetryCounterparts.Count;
            WBIBallastTank ballastTank;
            for (int index = 0; index < count; index++)
            {
                ballastTank = this.part.symmetryCounterparts[index].FindModuleImplementing<WBIBallastTank>();
                if (ballastTank != null)
                {
                    ballastTank.tankType = this.tankType;
                    ballastTank.updateGUI();
                }
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Flood Ballast")]
        public void FloodBallast()
        {
            ventState = BallastVentStates.FloodingBallast;
            updateGUI();
            updateSymmetryVentState();
        }

        [KSPAction("Toggle Flood Ballast")]
        public void FloodBallastAction(KSPActionParam param)
        {
            if (ventState == BallastVentStates.FloodingBallast)
                ventState = BallastVentStates.Closed;
            else
                ventState = BallastVentStates.FloodingBallast;

            updateGUI();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Vent Ballast")]
        public void VentBallast()
        {
            ventState = BallastVentStates.VentingBallast;
            updateGUI();
            updateSymmetryVentState();
        }

        [KSPAction("Toggle Vent Ballast")]
        public void VentBallastAction(KSPActionParam param)
        {
            if (ventState == BallastVentStates.VentingBallast)
                ventState = BallastVentStates.Closed;
            else
                ventState = BallastVentStates.VentingBallast;

            updateGUI();
        }

        [KSPEvent(guiActive = true, guiName = "Emergency Surface")]
        public void EmergencySurface()
        {
            DumpBallast();
        }

        [KSPAction("Emergency Surface")]
        public void EmergencySurfaceAction(KSPActionParam param)
        {
            EmergencySurface();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Close Vents")]
        public void CloseVents()
        {
            ventState = BallastVentStates.Closed;
            updateGUI();
            updateSymmetryVentState();
        }

        [KSPAction("Close Vents")]
        public void CloseVentsAction(KSPActionParam param)
        {
            CloseVents();
        }

        public void DumpBallast(bool updateSymmetryParts = true)
        {
            //Set the vent state
            ventState = BallastVentStates.Closed;

            //Clear the resource's ballast.
            if (resourceBallast != null)
                resourceBallast.amount = 0.0f;

            //Clear the ballast for all parts that have it
            else
                this.part.RequestResource(ballastResourceName, double.MaxValue, ResourceFlowMode.ALL_VESSEL);

            //Clear the ballast on symmetry parts
            if (!updateSymmetryParts)
                return;
            int count = this.part.symmetryCounterparts.Count;
            WBIBallastTank ballastTank;
            for (int index = 0; index < count; index++)
            {
                ballastTank = this.part.symmetryCounterparts[index].FindModuleImplementing<WBIBallastTank>();
                if (ballastTank != null && ballastTank.resourceBallast != null)
                    ballastTank.resourceBallast.amount = 0.0f;
            }
        }

        public void SetVentState(BallastVentStates state)
        {
            ventState = state;
            updateGUI();
        }
        #endregion

        #region Overrides
        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine("Controls vessel buoyancy by flooding and venting intake liquids into and out of its tank.");
            info.AppendLine(" ");
            info.AppendLine("Can be setup as a forward or aft trim tank to help keep a boat level.");

            return info.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get the intake transforms
            if (!string.IsNullOrEmpty(intakeTransformName))
                intakeTransforms = this.part.FindModelTransforms(intakeTransformName).ToArray();

            //Grab the base buoyancy
            //Actual buoyancy will vary depending upon how much ballast is in the tank.
            baseBuoyancy = this.part.buoyancy;

            //Get the ballast resource & host part
            if (!string.IsNullOrEmpty(ballastResourceName))
            {
                //Check this part
                if (this.part.Resources.Contains(ballastResourceName))
                {
                    resourceBallast = this.part.Resources[ballastResourceName];
                    hostPart = this.part;
                }

                //Check parent part
                else if (this.part.parent.Resources.Contains(ballastResourceName))
                {
                    resourceBallast = this.part.parent.Resources[ballastResourceName];
                    hostPart = this.part.parent;
                }

                //Vent behavior applies to all ballast tanks as we can't find any that has the ballast resource.
                else
                {
                    hostPart = this.part;
                    resourceBallast = null;
                }
            }

            updateGUI();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!this.part.vessel.Splashed)
            {
                this.part.Effect(addBallastEffect, 0.0f);
                this.part.Effect(ventBallastEffect, 0.0f);
                return;
            }

            //Update our bouyancy
            if (resourceBallast != null)
            {
                float tankBouyancy = (float)(resourceBallast.amount / resourceBallast.maxAmount);
                hostPart.buoyancy = baseBuoyancy * (1 - tankBouyancy);
            }

            //Update ballast amount
            updateBallastResource();

            //Now update effects
            updateEffects();
        }
        #endregion

        #region Helpers
        protected void updateSymmetryVentState()
        {
            int count = this.part.symmetryCounterparts.Count;
            WBIBallastTank ballastTank;
            for (int index = 0; index < count; index++)
            {
                ballastTank = this.part.symmetryCounterparts[index].FindModuleImplementing<WBIBallastTank>();
                if (ballastTank != null)
                {
                    ballastTank.ventState = this.ventState;
                    ballastTank.updateGUI();
                }
            }
        }

        protected void updateBallastResource()
        {
            double maxAmount = 0f;
            double amount = 0;
            PartResourceDefinition resourceDef = null;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            //Get the resource definition
            resourceDef = definitions[ballastResourceName];

            //If we are filling ballast then increase the amount of ballast in the part.
            if (ventState == BallastVentStates.FloodingBallast)
            {
                //Make sure at least one of our intake transforms is underwater.
                if (intakeTransforms == null)
                    return;
                if (!this.part.vessel.mainBody.ocean)
                    return;
                bool intakeIsUnderwater = false;
                for (int index = 0; index < intakeTransforms.Length; index++)
                {
                    if (FlightGlobals.getAltitudeAtPos((Vector3d)intakeTransforms[index].position, this.part.vessel.mainBody) <= 0.0f)
                    {
                        intakeIsUnderwater = true;
                        break;
                    }
                }
                if (!intakeIsUnderwater)
                    return;

                //All good, fill the tank
                if (resourceBallast != null)
                {
                    resourceBallast.amount += fillRate * TimeWarp.fixedDeltaTime;

                    //Close the vents if we've filled the ballast.
                    if (resourceBallast.amount >= resourceBallast.maxAmount)
                    {
                        resourceBallast.amount = resourceBallast.maxAmount;
                        ventState = BallastVentStates.Closed;
                        ventStateString = kClosed;
                    }
                }

                //Fill the vessel
                else
                {
                    this.part.RequestResource(ballastResourceName, -fillRate * TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
                    this.part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount, true);

                    if (amount >= maxAmount)
                    {
                        ventState = BallastVentStates.Closed;
                        ventStateString = kClosed;
                    }
                }
            }

            //If we are venting ballast then reduce the amount of ballast in the part.
            else if (ventState == BallastVentStates.VentingBallast)
            {
                if (resourceBallast != null)
                {
                    resourceBallast.amount -= ventRate * TimeWarp.fixedDeltaTime;

                    //Close the vents if we've emptied the ballast.
                    if (resourceBallast.amount <= 0.001f)
                    {
                        resourceBallast.amount = 0.0f;
                        ventState = BallastVentStates.Closed;
                        ventStateString = kClosed;
                    }
                }

                //Empty the vessel
                else
                {
                    this.part.RequestResource(ballastResourceName, fillRate * TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
                    this.part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount, true);

                    if (amount <= 0.0f)
                    {
                        ventState = BallastVentStates.Closed;
                        ventStateString = kClosed;
                    }
                }
            }
        }

        protected void updateEffects()
        {
            switch (ventState)
            {
                case BallastVentStates.Closed:
                default:
                    this.part.Effect(addBallastEffect, 0.0f);
                    this.part.Effect(ventBallastEffect, 0.0f);
                    break;

                case BallastVentStates.FloodingBallast:
                    this.part.Effect(addBallastEffect, 1.0f);
                    this.part.Effect(ventBallastEffect, 0.0f);
                    break;


                case BallastVentStates.VentingBallast:
                    this.part.Effect(addBallastEffect, 0.0f);
                    this.part.Effect(ventBallastEffect, 1.0f);
                    break;
            }
        }

        protected void updateGUI()
        {
            switch (tankType)
            {
                case BallastTankTypes.Ballast:
                default:
                    Events["ToggleTankType"].guiName = "Tank Type: Ballast";
                    break;

                case BallastTankTypes.ForwardTrim:
                    Events["ToggleTankType"].guiName = "Tank Type: Forward Trim";
                    break;

                case BallastTankTypes.AftTrim:
                    Events["ToggleTankType"].guiName = "Tank Type: Aft Trim";
                    break;
            }

            switch (ventState)
            {
                default:
                case BallastVentStates.Closed:
                    ventStateString = kClosed;
                    break;

                case BallastVentStates.FloodingBallast:
                    ventStateString = kFilling;
                    break;

                case BallastVentStates.VentingBallast:
                    ventStateString = kVenting;
                    break;
            }
        }
        #endregion
    }
}
