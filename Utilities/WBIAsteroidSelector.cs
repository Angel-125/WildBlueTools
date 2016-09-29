using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015 - 2016, by Michael Billard (Angel-125)
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
    public delegate void AsteroidSelectedEvent(ModuleAsteroid asteroid);

    public class WBIAsteroidSelector : PartModule, IOpsView
    {
        public event AsteroidSelectedEvent onAsteroidSelected;

        [KSPField(isPersistant = true)]
        public string sourceAsteroid = string.Empty;

        public ModuleAsteroid asteroid;

        public ModuleAsteroid SelectAsteroid()
        {
            List<ModuleAsteroid> asteroids = this.part.vessel.FindPartModulesImplementing<ModuleAsteroid>();

            //No asteroids? We're done.
            if (asteroids.Count == 0)
            {
                ScreenMessages.PostScreenMessage("Please capture an asteroid to process.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return null;
            }

            //Only one asteroid? target it and we're done.
            if (asteroids.Count == 1)
            {
                asteroid = asteroids.First<ModuleAsteroid>();
                sourceAsteroid = asteroid.GetVesselName();
                ScreenMessages.PostScreenMessage("Asteroid " + sourceAsteroid + " selected.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return asteroid;
            }

            //Need to prompt user for an asteroid.
            promptForAsteroid();

            return null;
        }

        public List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();
            buttonLabels.Add("AstroTarget");
            return buttonLabels;
        }

        protected void promptForAsteroid()
        {
            Color sourceColor = new Color(1, 1, 0);
            Color destinationColor = new Color(0, 191, 243);

            InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "SelectAsteroidLock");

            ScreenMessages.PostScreenMessage("Please select an asteroid to process. Press ESC to cancel.", 5.0f, ScreenMessageStyle.UPPER_CENTER);

            //Highlight the asteroids
            List<ModuleAsteroid> asteroids = this.part.vessel.FindPartModulesImplementing<ModuleAsteroid>();
            foreach (ModuleAsteroid asteroid in asteroids)
            {
                asteroid.part.Highlight(sourceColor);
                asteroid.part.AddOnMouseDown(onPartMouseDown);
            }

            //Highlight the processor
            this.part.Highlight(destinationColor);
        }

        protected void onPartMouseDown(Part partClicked)
        {
            ModuleAsteroid clickedAsteroid = partClicked.FindModuleImplementing<ModuleAsteroid>();

            if (clickedAsteroid != null)
            {
                InputLockManager.RemoveControlLock("SelectAsteroidLock");

                //Clear the highlighting
                List<ModuleAsteroid> asteroids = this.part.vessel.FindPartModulesImplementing<ModuleAsteroid>();
                foreach (ModuleAsteroid asteroid in asteroids)
                {
                    asteroid.part.Highlight(false);
                    asteroid.part.RemoveOnMouseDown(onPartMouseDown);
                }
                this.part.Highlight(false);

                //Get the clicked asteroid
                this.asteroid = clickedAsteroid;
                sourceAsteroid = this.asteroid.GetVesselName();
                ScreenMessages.PostScreenMessage("Asteroid " + sourceAsteroid + " selected.", 5.0f, ScreenMessageStyle.UPPER_CENTER);

                //Fire event handler
                if (onAsteroidSelected != null)
                    onAsteroidSelected(this.asteroid);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                //Clear the highlighting
                List<ModuleAsteroid> asteroids = this.part.vessel.FindPartModulesImplementing<ModuleAsteroid>();
                foreach (ModuleAsteroid asteroid in asteroids)
                    asteroid.part.Highlight(false);
                this.part.Highlight(false);

                //Remove event handler
                this.part.RemoveOnMouseDown(onPartMouseDown);
            }
        }

        public void DrawOpsWindow(string buttonLabel)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginScrollView(new Vector2(), new GUIStyle(GUI.skin.textArea), new GUILayoutOption[] { GUILayout.Height(480) });

            GUILayout.Label("<color=white>Current Process Target: " + sourceAsteroid + "</color>");

            if (GUILayout.Button("Select Process Target"))
            {
                SelectAsteroid();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public void SetContextGUIVisible(bool isVisible)
        {
        }

        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }
    }
}
