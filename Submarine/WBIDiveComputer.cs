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
    public class WBIDiveComputer: PartModule
    {
        #region Constants
        public static string kDiveStateIdle = "Cruising";
        public static string kDiveStateDiving = "Diving";
        public static string kDiveStateSurfacing = "Surfacing";
        public const string ICON_PATH = "WildBlueIndustries/000WildBlueTools/Icons/";
        #endregion

        #region Fields
        /// <summary>
        /// Determines whether or not the Part Action Window controls are visible.
        /// </summary>
        [KSPField]
        public bool guiVisible = true;

        /// <summary>
        /// Display string for current state of the dive computer
        /// </summary>
        [KSPField(guiActive = true, guiName = "Dive State")]
        public string diveStateString = kDiveStateIdle;

        /// <summary>
        /// Indicates whether or not to automatically keep the boat level.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Auto-trim", isPersistant = true)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool enableAutoTrim;

        /// <summary>
        /// Current pitch angle of the boat.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Pitch Angle", guiFormat = "f1", guiUnits = "deg.")]
        public double pitchAngle;

        /// <summary>
        /// Pitch angle that will trigger auto-trim. Level is 90-degrees, so +- the angle will trigger auto-trim.
        /// </summary>
        [KSPField]
        public double autoTrimAngleTrigger = 1.5f;

        /// <summary>
        /// Indicates whether or not to maintain current depth
        /// </summary>
        [KSPField(guiActive = true, guiName = "Maintain Current Depth", isPersistant = true)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool maintainDepth;

        /// <summary>
        /// If maintainDepth is enabled, then when the vertical speed reaches +- the speed trigger, the boat will attempt to maintain depth.
        /// </summary>
        [KSPField]
        public double verticalSpeedTrigger = 0.01f;

        /// <summary>
        /// How far it is to the bottom of the sea. Perhaps one should voyage there...
        /// </summary>
        [KSPField(guiActive = true, guiFormat = "f1", guiUnits = "m", guiName = "Depth below keel")]
        public float depthBelowKeel;

        /// <summary>
        /// Current vent state of the boat's ballast system.
        /// </summary>
        [KSPField(isPersistant = true)]
        public BallastVentStates ventState;
        #endregion

        #region Housekeeping
        public bool vesselIsManeuvering;

        List<WBIBallastTank> ballastTanks;
        int partCount;
        double pitchTriggerUp = 91.5f;
        double pitchTriggerDown = 88.5f;
        FlightInputCallback inputCallback;

        GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Height(48), GUILayout.Width(48) };
        public static Texture diveIcon = null;
        public static Texture surfaceIcon = null;
        public static Texture emergencySurfaceIcon = null;
        #endregion

        #region Events
        /// <summary>
        /// Toggles the dive state on and off. Dive state only affects parts marked as ballast tanks. Trim tanks are ignored.
        /// When toggled on, the ballast tanks will fill with ballast.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Toggle Dive")]
        public void ToggleDive()
        {
            if (ballastTanks == null)
                return;

            if (ventState == BallastVentStates.FloodingBallast)
                ventState = BallastVentStates.Closed;
            else
                ventState = BallastVentStates.FloodingBallast;

            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
                ballastTanks[index].SetVentState(ventState);

            updateGUI();
        }

        /// <summary>
        /// KSP Action for toggling the dive state
        /// </summary>
        /// <param name="param">a KSPActionParam containing parameters.</param>
        [KSPAction("Toggle Dive")]
        public void ToggleDiveAction(KSPActionParam param)
        {
            ToggleDive();
        }

        /// <summary>
        /// Toggles the surface state on and off. Dive state only affects parts marked as ballast tanks. Trim tanks are ignored.
        /// When toggled on, the ballast tanks will empty their ballast.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Toggle Surface")]
        public void ToggleSurface()
        {
            if (ballastTanks == null)
                return;

            if (ventState == BallastVentStates.VentingBallast)
                ventState = BallastVentStates.Closed;
            else
                ventState = BallastVentStates.VentingBallast;

            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
                ballastTanks[index].SetVentState(ventState);

            updateGUI();
        }

        /// <summary>
        /// KSP Action for toggling the surface state
        /// </summary>
        /// <param name="param">a KSPActionParam containing parameters.</param>
        [KSPAction("Toggle Surface")]
        public void ToggleSurfaceAction(KSPActionParam param)
        {
            ToggleSurface();
        }

        /// <summary>
        /// Activates emergency surface, telling all ballast tanks to immediately dump their ballast. This affects parts marked as ballast or trim tanks.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Emergency Surface")]
        public void EmergencySurface()
        {
            if (ballastTanks == null)
                return;

            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
            {
                ballastTanks[index].ventState = BallastVentStates.Closed;
                ballastTanks[index].DumpBallast();
            }

            updateGUI();
        }

        /// <summary>
        /// Activates emergency surface, telling all ballast tanks to immediately dump their ballast. This affects parts marked as ballast or trim tanks.
        /// </summary>
        [KSPAction("Emergency Surface")]
        public void EmergencySurfaceAction(KSPActionParam param)
        {
            EmergencySurface();
        }

        public void CloseVents()
        {
            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
                ballastTanks[index].ventState = BallastVentStates.Closed;
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Get ballast tanks
            ballastTanks = this.part.vessel.FindPartModulesImplementing<WBIBallastTank>();

            //Get part count. If it changes then we'll need to find our ballast tanks again.
            partCount = this.part.vessel.parts.Count;

            //Get our pitch trigger
            pitchTriggerDown = 90.0f - autoTrimAngleTrigger;
            pitchTriggerUp = 90 + autoTrimAngleTrigger;

            setupGUI();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (this.part.ShieldedFromAirstream)
                return;

            //Update our list of ballast tanks.
            if (partCount != this.part.vessel.parts.Count)
            {
                partCount = this.part.vessel.parts.Count;
                ballastTanks = this.part.vessel.FindPartModulesImplementing<WBIBallastTank>();
            }
            if (ballastTanks == null)
                return;

            //Update ballast state
            updateBallastState();

            //Check to see if the vessel is maneuvering. If so, then we're done.
            updateManeuverState();

            //Maintain depth if needed.
            updateDepthState();

            //Update trim if needed.
            updateTrimState();

            //Update keel depth
            depthBelowKeel = this.part.vessel.heightFromTerrain;
        }
        #endregion

        #region Helpers
        protected void DrawControllerGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("<color=lightblue><b>" + this.part.partInfo.title + "</b></color>");

            GUILayout.Label("<color=white><b>Status: </b>" + diveStateString + "</color>");
            GUILayout.Label(string.Format("<color=white><b>Depth to bottom: </b>{0:f1}m</color>", depthBelowKeel));

            GUILayout.BeginHorizontal();
            //Dive
            if (GUILayout.Button(diveIcon, buttonOptions))
                ToggleDive();

            //Surface
            if (GUILayout.Button(surfaceIcon, buttonOptions))
                ToggleSurface();

            //Emergency Surface
            if (GUILayout.Button(emergencySurfaceIcon, buttonOptions))
                EmergencySurface();

            GUILayout.EndHorizontal();

            //Enable auto-trim
            bool currentValue = enableAutoTrim;
            currentValue = GUILayout.Toggle(enableAutoTrim, "Enable Auto-trim");
            if (currentValue != enableAutoTrim)
            {
                enableAutoTrim = currentValue;
                CloseVents();
            }

            //Maintain depth
            currentValue = GUILayout.Toggle(maintainDepth, "Maintain current depth");
            if (currentValue != maintainDepth)
            {
                maintainDepth = currentValue;
                CloseVents();
            }

            GUILayout.EndVertical();
        }

        protected void updateManeuverState()
        {
            if (GameSettings.PITCH_UP.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }
            if (GameSettings.PITCH_DOWN.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }

            if (GameSettings.ROLL_LEFT.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }
            if (GameSettings.ROLL_RIGHT.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }

            if (GameSettings.YAW_LEFT.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }
            if (GameSettings.YAW_RIGHT.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }


            if (GameSettings.TRANSLATE_FWD.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }
            if (GameSettings.TRANSLATE_BACK.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }

            if (GameSettings.TRANSLATE_LEFT.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }
            if (GameSettings.TRANSLATE_RIGHT.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }

            if (GameSettings.TRANSLATE_UP.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }
            if (GameSettings.TRANSLATE_DOWN.GetKey())
            {
                vesselIsManeuvering = true;
                return;
            }

            vesselIsManeuvering = false;
        }

        protected void updateDepthState()
        {
            if (!maintainDepth)
                return;
            if (vesselIsManeuvering)
                return;

            //Don't do anything if we're diving or surfacing.
            if (ventState == BallastVentStates.VentingBallast || ventState == BallastVentStates.FloodingBallast)
                return;

            //If we're rising up then flood the ballast
            int count = ballastTanks.Count;
            if (this.part.vessel.verticalSpeed > verticalSpeedTrigger)
            {
                for (int index = 0; index < count; index++)
                    ballastTanks[index].SetVentState(BallastVentStates.FloodingBallast);
            }

            //If we're sinking then empty the ballast
            else if (this.part.vessel.verticalSpeed < -verticalSpeedTrigger)
            {
                for (int index = 0; index < count; index++)
                    ballastTanks[index].SetVentState(BallastVentStates.VentingBallast);
            }

            //All good
            else
            {
                WBIBallastTank ballastTank;
                for (int index = 0; index < count; index++)
                {
                    ballastTank = ballastTanks[index];

                    if (ballastTank.tankType == BallastTankTypes.Ballast)
                    {
                        if (ballastTank.ventState != BallastVentStates.Closed)
                            ballastTank.SetVentState(BallastVentStates.Closed);
                    }
                }
            }
        }

        protected void updateTrimState()
        {
            if (!enableAutoTrim)
                return;
            if (vesselIsManeuvering)
                return;

            //ActiveVessel.upAxis doesn't seem to give me the right results, but ActiveVessel.transform.up does!
            pitchAngle = Vector3d.Angle(FlightGlobals.upAxis, FlightGlobals.ActiveVessel.transform.up);

            int count = ballastTanks.Count;
            WBIBallastTank trimTank;

            //See if we need to pitch upward
            if (pitchAngle > pitchTriggerUp)
            {
                //Vent the forward trim tanks and flood the aft trim tanks.
                for (int index = 0; index < count; index++)
                {
                    trimTank = ballastTanks[index];
                    if (trimTank.tankType == BallastTankTypes.ForwardTrim)
                        trimTank.SetVentState(BallastVentStates.VentingBallast);
                    else if (trimTank.tankType == BallastTankTypes.AftTrim)
                        trimTank.SetVentState(BallastVentStates.FloodingBallast);
                }
            }

            //See if we need to pitch downward
            else if (pitchAngle < pitchTriggerDown)
            {
                //Vent the aft trim tanks and flood the forward trim tanks.
                for (int index = 0; index < count; index++)
                {
                    trimTank = ballastTanks[index];
                    if (trimTank.tankType == BallastTankTypes.ForwardTrim)
                        trimTank.SetVentState(BallastVentStates.FloodingBallast);
                    else if (trimTank.tankType == BallastTankTypes.AftTrim)
                        trimTank.SetVentState(BallastVentStates.VentingBallast);
                }
            }

            //We're level-ish, close all trim tank vents
            else
            {
                for (int index = 0; index < count; index++)
                {
                    trimTank = ballastTanks[index];
                    if (trimTank.tankType == BallastTankTypes.ForwardTrim || trimTank.tankType == BallastTankTypes.AftTrim)
                    {
                        if (trimTank.ventState != BallastVentStates.Closed)
                            trimTank.SetVentState(BallastVentStates.Closed);
                    }
                }
            }
        }

        protected void updateBallastState()
        {
            //Check ballast states. We'll update our state once all the ballast tanks are closed.
            //Different ballast tanks fill/empty at different rates so the dive computer's state
            //needs to detect when all the ballast tanks have finished filling or emptying.
            int count = ballastTanks.Count;
            WBIBallastTank ballastTank;
            for (int index = 0; index < count; index++)
            {
                ballastTank = ballastTanks[index];
                if (ballastTank.ventState != BallastVentStates.Closed && ballastTank.tankType == BallastTankTypes.Ballast)
                    return;
            }

            //At this point all our ballast tanks are closed.
            ventState = BallastVentStates.Closed;
            updateGUI();
        }

        protected void setupGUI()
        {
            Events["ToggleDive"].active = guiVisible;
            Events["ToggleSurface"].active = guiVisible;
            Events["EmergencySurface"].active = guiVisible;
            
            Fields["diveStateString"].guiActive = guiVisible;
            Fields["enableAutoTrim"].guiActive = guiVisible;
            Fields["pitchAngle"].guiActive = guiVisible;
            Fields["maintainDepth"].guiActive = guiVisible;
            Fields["depthBelowKeel"].guiActive = guiVisible;

            //Setup icons
            if (diveIcon != null)
                return;

            string baseIconURL = ICON_PATH;
            ConfigNode settingsNode = GameDatabase.Instance.GetConfigNode("WildBlueSettings");
            if (settingsNode != null)
                baseIconURL = settingsNode.GetValue("iconsFolder");

            diveIcon = GameDatabase.Instance.GetTexture(baseIconURL + "DiveIcon", false);
            surfaceIcon = GameDatabase.Instance.GetTexture(baseIconURL + "SurfaceIcon", false);
            emergencySurfaceIcon = GameDatabase.Instance.GetTexture(baseIconURL + "EmergencySurfaceIcon", false);
        }

        protected void updateGUI()
        {
            switch (ventState)
            {
                case BallastVentStates.Closed:
                default:
                    diveStateString = kDiveStateIdle;
                    break;

                case BallastVentStates.FloodingBallast:
                    diveStateString = kDiveStateDiving;
                    break;

                case BallastVentStates.VentingBallast:
                    diveStateString = kDiveStateSurfacing;
                    break;
            }
        }
        #endregion
    }
}
