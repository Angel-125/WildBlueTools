﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Reflection;

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
    public static class Utils
    {
        public const float CelsiusToKelvin = 272.15f;
        public const float StefanBoltzmann = 5.67e-8f;
        public const double secondsPerMinute = 60;
        public const double secondsPerHour = 3600;
        public const double secondsPerDayKerbin = 21600;
        public const double secondsPerDayEarth = 86400;

        //Adapted from https://rosettacode.org/wiki/Haversine_formula#C.23
        //Released under GNU Free Documentation License 1.2
        //Returns kilometers
        public static double HaversineDistance(double lon1, double lat1, double lon2, double lat2, CelestialBody body)
        {
            var R = body.Radius / 1000f; // In kilometers
            var dLat = toRadians(lat2 - lat1);
            var dLon = toRadians(lon2 - lon1);
            lat1 = toRadians(lat1);
            lat2 = toRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Asin(Math.Sqrt(a));
            return R * 2 * Math.Asin(Math.Sqrt(a));
        }

        public static double toRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public static bool IsExperienceEnabled()
        {
            if (HighLogic.CurrentGame == null)
                return false;

            GameParameters.AdvancedParams advancedParams = HighLogic.CurrentGame.Parameters.CustomParams<GameParameters.AdvancedParams>();
            if (advancedParams != null)
            {
                return advancedParams.KerbalExperienceEnabled(HighLogic.CurrentGame.Mode);
            }
            return false;
        }

        public static string[] GetTraitsWithEffect(string effectName)
        {
            List<string> traits;
            Experience.ExperienceSystemConfig config = new Experience.ExperienceSystemConfig();
            config.LoadTraitConfigs();
            traits = config.GetTraitsWithEffect(effectName);

            if (traits == null)
            {
                traits = new List<string>();
            }

            return traits.ToArray();
        }

        public static string formatTime(double timeSeconds)
        {
            string timeString;
            double seconds = Math.Abs(timeSeconds);
            double secondsPerDay = GameSettings.KERBIN_TIME ? secondsPerDayKerbin : secondsPerDayEarth;

            double days = Math.Floor(seconds / secondsPerDay);
            seconds -= days * secondsPerDay;

            double hours = Math.Floor(seconds / secondsPerHour);
            seconds -= hours * secondsPerHour;

            double minutes = Math.Floor(seconds / secondsPerMinute);
            seconds -= minutes * secondsPerMinute;

            timeString = string.Format("{0:f0}d {1:f0}h {2:f0}m {3:f2}s", days, hours, minutes, seconds);
            if (timeSeconds < 0f)
                timeString = "-" + timeString;

            return timeString;
        }

        public static bool IsBiomeUnlocked(Vessel vessel)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return false;
            if (vessel == null)
                return false;

            //ResourceMap.Instance.IsBiomeUnlocked is borked. Need to use an alternate method...
            CBAttributeMapSO.MapAttribute biome = Utils.GetCurrentBiome(vessel);
            List<BiomeLockData> biomeLockData = ResourceScenario.Instance.gameSettings.GetBiomeLockInfo();

            foreach (BiomeLockData data in biomeLockData)
                if (data.BiomeName == biome.name)
                    return true;

            return false;
        }

        public static CBAttributeMapSO.MapAttribute GetCurrentBiome(Vessel vessel)
        {
            if (!vessel.Landed && !vessel.Splashed)
                return null;
            CelestialBody celestialBody = vessel.mainBody;
            double lattitude = ResourceUtilities.Deg2Rad(vessel.latitude);
            double longitude = ResourceUtilities.Deg2Rad(vessel.longitude);
            CBAttributeMapSO.MapAttribute biome = ResourceUtilities.GetBiome(lattitude, longitude, FlightGlobals.currentMainBody);

            return biome;
        }

        public static IEnumerable<ResourceCache.AbundanceSummary> GetAbundances(Vessel vessel, HarvestTypes harvestType)
        {
            string biomeName = Utils.GetCurrentBiome(vessel).name;
            int flightGlobalsIndex = vessel.mainBody.flightGlobalsIndex;
            IEnumerable<ResourceCache.AbundanceSummary> abundanceCache;

            //First, try getting from the current biome.
            abundanceCache = ResourceCache.Instance.AbundanceCache.
                Where(a => a.HarvestType == harvestType && a.BodyId == flightGlobalsIndex && a.BiomeName == biomeName);

            //No worky? Try using vessel situation.
            if (abundanceCache.Count() == 0)
            {
                switch (harvestType)
                {
                    case HarvestTypes.Atmospheric:
                        biomeName = "FLYING";
                        break;

                    case HarvestTypes.Oceanic:
                        biomeName = "SPLASHED";
                        break;

                    case HarvestTypes.Exospheric:
                        biomeName = "ORBITING";
                        break;

                    default:
                        biomeName = "PLANETARY";
                        break;
                }

                //Give it another shot.
                abundanceCache = ResourceCache.Instance.AbundanceCache.
                    Where(a => a.HarvestType == harvestType && a.BodyId == flightGlobalsIndex && a.BiomeName == biomeName);
            }

            return abundanceCache;
        }

        public static bool HasResearchedNode(string techNode)
        {
            if (string.IsNullOrEmpty(techNode))
                return false;

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                if (ResearchAndDevelopment.Instance != null)
                {
                    if (ResearchAndDevelopment.GetTechnologyState(techNode) != RDTech.State.Available)
                        return false;
                }
            }

            return true;
        }

        public static void showOnlyEmittersInList(Part part, List<string> emittersToShow)
        {
            KSPParticleEmitter[] emitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            foreach (KSPParticleEmitter emitter in emitters)
            {
                emitter.emit = false;
                emitter.enabled = false;

                if (emittersToShow != null)
                {
                    //If the emitter is on the list then show it
                    if (emittersToShow.Contains(emitter.name))
                    {
                        emitter.emit = true;
                        emitter.enabled = true;
                    }
                }
            }

        }

        //Reflection-like method to set a value on a field of a part module
        //Created to support late-binding of a third-party module
        //That way, if the third-party module changes, my modules that depend upon them
        //won't break when the third-party module's DLL versioning changes.
        public static bool SetField(string name, object value, PartModule module)
        {
            bool result = false;

            result = module.Fields[name].SetValue(value, module.Fields[name].host);

            return result;
        }

        //Reflection-like method to get a value on a field of a part module
        //Created to support late-binding of a third-party module
        //That way, if the third-party module changes, my modules that depend upon them
        //won't break when the third-party module's DLL versioning changes.
        public static object GetField(string name, PartModule module)
        {
            object value = null;

            value = module.Fields[name].GetValue(module.Fields[name].host);

            return value;
        }

        public static bool IsModInstalled(string neededMod)
        {
            //Now check for the required mod
            string modToCheck = neededMod;
            bool checkInverse = false;
            if (neededMod.StartsWith("!"))
            {
                checkInverse = true;
                modToCheck = neededMod.Substring(1, neededMod.Length - 1);
            }

            bool isInstalled = AssemblyLoader.loadedAssemblies.Any(a => a.name == modToCheck);

            if (isInstalled && checkInverse == false)
                return true;
            else if (isInstalled && checkInverse)
                return true;
            else if (!isInstalled && checkInverse)
                return false;
            else
                return false;

/*
            string modToCheck = null;
            bool checkInverse = false;
            bool modFound = false;
            char[] delimiters = { '/' };
            string[] tokens;
            List<string> partTokens = new List<string>();

            //Create the part tokens
            partTokens = new List<string>();
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                //Name
                if (partTokens.Contains(loadedAssembly.name) == false)
                    partTokens.Add(loadedAssembly.name);

                //URL tokens
                tokens = loadedAssembly.url.Split(delimiters);
                foreach (string token in tokens)
                {
                    if (partTokens.Contains(token) == false)
                        partTokens.Add(token);
                }
            }

            //Now check for the required mod
            modToCheck = neededMod;
            if (neededMod.StartsWith("!"))
            {
                checkInverse = true;
                modToCheck = neededMod.Substring(1, neededMod.Length - 1);
            }

            modFound = partTokens.Contains(modToCheck);
            if (modFound && checkInverse == false)
                return true;
            else if (modFound && checkInverse)
                return true;
            else if (!modFound && checkInverse)
                return false;
            else
                return false;
 */
        }

        #region refresh tweakable GUI
        // Code from https://github.com/Swamp-Ig/KSPAPIExtensions/blob/master/Source/Utils/KSPUtils.cs#L62

        private static FieldInfo windowListField;

        /// <summary>
        /// Find the UIPartActionWindow for a part. Usually this is useful just to mark it as dirty.
        /// </summary>
        public static UIPartActionWindow FindActionWindow(this Part part)
        {
            if (part == null)
                return null;

            // We need to do quite a bit of piss-farting about with reflection to 
            // dig the thing out. We could just use Object.Find, but that requires hitting a heap more objects.
            UIPartActionController controller = UIPartActionController.Instance;
            if (controller == null)
                return null;

            if (windowListField == null)
            {
                Type cntrType = typeof(UIPartActionController);
                foreach (FieldInfo info in cntrType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (info.FieldType == typeof(List<UIPartActionWindow>))
                    {
                        windowListField = info;
                        goto foundField;
                    }
                }
                Debug.LogWarning("*PartUtils* Unable to find UIPartActionWindow list");
                return null;
            }
        foundField:

            List<UIPartActionWindow> uiPartActionWindows = (List<UIPartActionWindow>)windowListField.GetValue(controller);
            if (uiPartActionWindows == null)
                return null;

            return uiPartActionWindows.FirstOrDefault(window => window != null && window.part == part);
        }

        #endregion
    }
}
