using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Reflection;

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
    public static class Utils
    {
        public const float CelsiusToKelvin = 272.15f;
        public const float StefanBoltzmann = 5.67e-8f;

        public static bool IsBiomeUnlocked(Vessel vessel)
        {
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
            CelestialBody celestialBody = vessel.mainBody;
            double lattitude = ResourceUtilities.Deg2Rad(vessel.latitude);
            double longitude = ResourceUtilities.Deg2Rad(vessel.longitude);
            CBAttributeMapSO.MapAttribute biome = ResourceUtilities.GetBiome(lattitude, longitude, FlightGlobals.currentMainBody);

            return biome;
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
