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
    /// <summary>
    /// An extension to the stock ModuleAsteroidResource, 
    /// </summary>
    public class WBIModuleAsteroidResource : ModuleAsteroidResource
    {
        [KSPField]
        public bool debugMode;

        /// <summary>
        /// Resource is guaranteed to be present in a magic boulder.
        /// </summary>
        [KSPField]
        public bool magicBoulderGuaranteed;

        /// <summary>
        /// List of resources to assimilate into this resource if the asteroid is a magic boulder.
        /// Separate resources by semicolon.
        /// </summary>
        [KSPField]
        public string resourcesToAssimilate = string.Empty;

        /// <summary>
        /// How much of the assimilated resource to assimilate.
        /// </summary>
        [KSPField]
        public float assimilateFraction = 1.0f;

        /// <summary>
        /// Have we converted the resource?
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool resourceConverted;

        /// <summary>
        /// Flag to indicate whether or not the asteroid is a magic boulder.
        /// </summary>
        public bool isMagicBoulder;

        public override void OnStart(StartState state)
        {
            //Check for magic boulder & upgrade resource accordingly.
            ModuleAsteroid asteroid = this.part.FindModuleImplementing<ModuleAsteroid>();
            if (asteroid != null)
            {
                //Set magic boulder flag
                if (asteroid.currentState == 2)
                    isMagicBoulder = true;

                //Guarantee the resource if the asteroid is a magic boulder
                if (isMagicBoulder && magicBoulderGuaranteed)
                {
                    //Set pressenceChance
                    presenceChance = 100;

                    //Setup our abundance and convert resources if needed.
                    if (!resourceConverted)
                    {
                        //Setup abundance
                        SetupAbundance();

                        //Convert the other resources if needed.
                        if (!string.IsNullOrEmpty(resourcesToAssimilate))
                            AssimilateResources();

                        //Set the flag
                        resourceConverted = true;
                    }
                }
            }

            base.OnStart(state);
        }

        /// <summary>
        /// Assimilates resources specified in resourcesToAssimilate by taking their abundance and display abundance
        /// and adding it to this asteroid resource. Resistance is futile.
        /// </summary>
        public virtual void AssimilateResources()
        {
            List<ModuleAsteroidResource> asteroidResources = this.part.FindModulesImplementing<ModuleAsteroidResource>();
            int resourceCount = asteroidResources.Count;
            ModuleAsteroidResource asteroidResource = null;
            float assimilateAbundance = 0f;
            float assimilateDisplayAbundance = 0f;
            for (int index = 0; index < resourceCount; index++)
            {
                asteroidResource = asteroidResources[index];
                if (resourcesToAssimilate.Contains(asteroidResource.resourceName))
                {
                    assimilateAbundance = asteroidResource.abundance * assimilateFraction;
                    assimilateDisplayAbundance = asteroidResource.displayAbundance * assimilateFraction;

                    abundance += assimilateAbundance;
                    displayAbundance += assimilateDisplayAbundance;

                    asteroidResource.abundance -= assimilateAbundance;
                    asteroidResource.displayAbundance -= assimilateDisplayAbundance;
                }
            }
        }

        /// <summary>
        /// Sets up our abundance.
        /// </summary>
        public virtual void SetupAbundance()
        {
            //Get the ore resource
            List<ModuleAsteroidResource> asteroidResources = this.part.FindModulesImplementing<ModuleAsteroidResource>();
            int resourceCount = asteroidResources.Count;
            ModuleAsteroidResource oreResource = null;
            for (int index = 0; index < resourceCount; index++)
            {
                if (asteroidResources[index].resourceName == "Ore")
                {
                    oreResource = asteroidResources[index];
                    break;
                }
            }

            //Not sure why but display abundance isn't the same as abundance so we'll fudge this...
            float displayFactor = 0.875f;
            if (oreResource != null)
                displayFactor = oreResource.displayAbundance / oreResource.abundance;

            //Take an average of all the resource high/low values
            float averageRange = 0f;
            float totalAverageRange = 0f;
            for (int index = 0; index < resourceCount; index++)
            {
                averageRange = ((float)asteroidResources[index].lowRange + (float)asteroidResources[index].highRange) / 2.0f;
                totalAverageRange += averageRange;
            }

            //Now let's get a random range for the resource
            float resourceRange = UnityEngine.Random.Range((float)lowRange, (float)highRange);

            //Set abundance
            abundance = resourceRange / totalAverageRange;

            //Set display abundance
            displayAbundance = abundance * displayFactor;
        }
    }
}
