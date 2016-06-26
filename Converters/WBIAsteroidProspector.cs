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
    public class WBIAsteroidProspector : WBIProspector
    {
        protected ModuleAsteroid asteroid;
        protected WBIAsteroidSelector asteroidSelector;

        public override void OnStart(StartState state)
        {
            asteroidSelector = this.part.FindModuleImplementing<WBIAsteroidSelector>();
            if (asteroidSelector != null)
            {
                asteroidSelector.onAsteroidSelected += new AsteroidSelectedEvent(asteroidSelector_onAsteroidSelected);
            }
            base.OnStart(state);
        }

        void asteroidSelector_onAsteroidSelected(ModuleAsteroid asteroid)
        {
            this.asteroid = asteroidSelector.asteroid;
        }

        public override string GetInfo()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                //Make sure that we have at least one asteroid attached to the vessel. If we have multiple, then
                //make sure we have designated the asteroid that we'll process.
                if (this.asteroid == null)
                {
                    findSourceAsteroid();
                    if (this.asteroid == null)
                    {
                        outputList.Clear();
                        return base.GetInfo() + "Unknown. Requires an asteroid.";
                    }
                }

                //Now prepare our outputs if needed
                outputList.Clear();
                prepareOutputs();

                return base.GetInfo();
            }

            else
            {
                return base.GetInfo();
            }
        }

        protected void findSourceAsteroid()
        {
            if (this.asteroid != null)
                return;

            List<ModuleAsteroid> asteroids = this.part.vessel.FindPartModulesImplementing<ModuleAsteroid>();

            //No asteroids? We're done.
            if (asteroids.Count == 0)
            {
                ScreenMessages.PostScreenMessage("Please capture an asteroid to process.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            //Only one asteroid? target it and we're done.
            if (asteroids.Count == 1)
            {
                asteroid = asteroids.First<ModuleAsteroid>();
                return;
            } 
            
            //Grab selector if needed.
            if (asteroidSelector == null)
                asteroidSelector = this.part.FindModuleImplementing<WBIAsteroidSelector>();

            //Ask the selector to select an asteroid
            if (asteroidSelector != null)
                this.asteroid = asteroidSelector.SelectAsteroid();
        }

        protected override void prepareOutputsByLocale()
        {
            ResourceRatio outputSource = null;
            string biomeName = Utils.GetCurrentBiome(this.part.vessel).name;
            PartResourceDefinition outputDef = null;
            float totalAbundance = 0f;
            float abundance = 0f;
            float outputMass = 0f;
            float outputUnits = 0f;

            //Get the asteroid to process
            findSourceAsteroid();
            if (this.asteroid == null)
            {
                return;
            }

            //Get the resources
            List<ModuleAsteroidResource> asteroidResources = this.asteroid.part.FindModulesImplementing<ModuleAsteroidResource>();

            foreach (ModuleAsteroidResource summary in asteroidResources)
            {
                outputDef = ResourceHelper.DefinitionForResource(summary.resourceName);
                abundance = summary.abundance;
                outputMass = abundance * yieldMass;
                outputUnits = outputMass / outputDef.density;

                //If the resource is an input resource then add its output mass to the byproductMass.
                if (inputSources.Contains(summary.resourceName))
                {
                    byproductMass += outputMass;
                }

                //If the resource is on our ignore list, then add the output mass to the byproductMass.
                else if (!string.IsNullOrEmpty(ignoreResources) && ignoreResources.Contains(summary.resourceName))
                {
                    byproductMass += outputMass;
                }

                //Legit!
                else if (summary.abundance > 0.001f)
                {
                    totalAbundance += abundance;
                    //                    Debug.Log("FRED " + summary.ResourceName + " abundance: " + abundance + " Ratio: " + outputUnits);
                    outputSource = new ResourceRatio { ResourceName = summary.resourceName, Ratio = outputUnits, FlowMode = "ALL_VESSEL", DumpExcess = true };
                    outputList.Add(outputSource);
                }
            }

            //Leftovers
            byproductMass += (1.0f - totalAbundance) * yieldMass;
            outputUnits = byproductMass / byproductDef.density;
            outputSource = new ResourceRatio { ResourceName = byproduct, Ratio = outputUnits, FlowMode = "ALL_VESSEL", DumpExcess = true };
            outputList.Add(outputSource);

            //            Debug.Log("FRED totalAbundance: " + totalAbundance);
            //            Debug.Log("FRED Slag Units: " + outputUnits);
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            //Do we have an asteroid?
            if (asteroid == null)
            {
                this.status = "Select or acquire an asteroid.";
                StopResourceConverter();
                return base.PrepareRecipe(deltatime);
            }

            return base.PrepareRecipe(deltatime);
        }
    }
}
