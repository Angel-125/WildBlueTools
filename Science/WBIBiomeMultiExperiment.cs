using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: GPLV3

If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIBiomeMultiExperiment : ModuleScienceExperiment
    {
       [KSPField(isPersistant = true, guiActive = true, guiName = "Experiment")]
        public string status = string.Empty;

        [KSPField]
        public float minimumDistanceToRerurn = 0f;

        [KSPField(isPersistant = true)]
        public bool checkForRerun;

        [KSPField(isPersistant = true)]
        public double previousLongitude;

        [KSPField(isPersistant = true)]
        public double previousLatitude;

        [KSPField(isPersistant = true)]
        public double distanceFromPreviousLocation;

        public override void OnUpdate()
        {
            base.OnUpdate();

            //Check minimum distance for rerun
            if (HighLogic.LoadedSceneIsFlight)
                checkForMinDistance();
        }

        protected void checkForMinDistance()
        {
            //Setup the baseline
            status = "Ready";
            Events["DeployExperiment"].guiActive = true;
            Events["DeployExperimentExternal"].guiActiveUnfocused = true;

            //If the experiment has been deployed and we require a minimum distance to rerun, then hide the GUI
            if (minimumDistanceToRerurn > 0 && Deployed && 
                (this.part.vessel.situation == Vessel.Situations.LANDED || this.part.vessel.situation == Vessel.Situations.PRELAUNCH || this.part.vessel.situation == Vessel.Situations.SPLASHED))
            {
                //Record our current location if we aren't presently checking for rerun.
                if (!checkForRerun)
                {
                    checkForRerun = true;
                    previousLongitude = this.part.vessel.longitude;
                    previousLatitude = this.part.vessel.latitude;
                }

                else
                {
                    //Get current location
                    double longitude = this.part.vessel.longitude;
                    double latitude = this.part.vessel.latitude;
                    double planetRadius = this.part.vessel.mainBody.Radius;

                    //Calculate distance traveled. If we haven't traveled far enough, hide the experiment GUI.
                    /*
                    distanceFromPreviousLocation = planetRadius * Math.Acos(Math.Sin(previousLatitude) * 
                        Math.Sin(latitude) + Math.Cos(previousLatitude) * 
                        Math.Cos(latitude) * Math.Cos(Math.Abs(longitude - previousLongitude)));
                    distanceFromPreviousLocation /= 100.0f;
                    //distanceFromPreviousLocation = calculateDistance();
                     */
                    Vector2d prevLoc = new Vector2d(previousLongitude, previousLatitude);
                    Vector2d curLoc = new Vector2d(longitude, latitude);
                    Vector2d locTravel = curLoc - prevLoc;
                    distanceFromPreviousLocation = locTravel.magnitude * 9.52381f;

                    //If we traveled the minimum distance then reset the experiment
                    if (distanceFromPreviousLocation >= minimumDistanceToRerurn)
                    {
                        CleanUpExperimentExternal();
                        checkForRerun = false;
                        Deployed = false;
                        status = "Ready";
                        Events["DeployExperiment"].guiActive = true;
                        Events["DeployExperimentExternal"].guiActiveUnfocused = true;
                    }

                    //Update status
                    else
                    {
                        status = string.Format("Must travel {0:f2}km", (minimumDistanceToRerurn - distanceFromPreviousLocation));
                        Events["DeployExperiment"].guiActive = false;
                        Events["DeployExperimentExternal"].guiActiveUnfocused = false;
                    }
                }
            }
        }

    }
}
