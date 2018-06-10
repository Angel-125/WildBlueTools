using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

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
    public class WBIEngineRepLoss: PartModule
    {
        const float repLossDuration = 5.0f;

        [KSPField]
        public string repLossEngineIDs;

        [KSPField]
        public string repLossMessage = "Using this engine in the home world atmosphere or near other crewed vessels is making you unpopular!";

        [KSPField]
        public float repLossPerSec = 0.5f;

        [KSPField(isPersistant = true)]
        public bool initialActivationCheck = true;

        [KSPField]
        public float initialActivationRepFactor = 5.0f;

        bool showDebug = true;
        List<ModuleEnginesFX> engineList;
        bool playerInformed = false;

        void debugLog(string message)
        {
            if (!showDebug)
                return;

            Debug.Log("[WBIEngineRepLoss] -" + message);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Grab all the engines
            engineList = this.part.FindModulesImplementing<ModuleEnginesFX>();
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return;
            if (engineList.Count == 0)
                return;
            
            //Get current engine
            ModuleEnginesFX engine = null;
            ModuleEnginesFX checkEngine = null;
            int count = engineList.Count;
            for (int index = 0; index < count; index++)
            {
                checkEngine = engineList[index];
                if (checkEngine.EngineIgnited && checkEngine.isOperational && repLossEngineIDs.Contains(checkEngine.engineID))
                {
                    engine = engineList[index];
                    break;
                }
            }
            if (engine == null)
                return;

            //Check for throttle. Applies after initial activation of the engine.
            if (engine.currentThrottle <= 0.00001f && !initialActivationCheck)
            {
                playerInformed = false;
                return;
            }

            //Check for nearby vessels
            bool crewedVesselsNearby = false;
            if (FlightGlobals.VesselsLoaded.Count > 1)
            {
                Vessel[] vessels = FlightGlobals.VesselsLoaded.ToArray();
                Vessel currentVessel;
                int totalVessels = vessels.Length;
                for (int index = 0; index < totalVessels; index++)
                {
                    currentVessel = vessels[index];
                    if (currentVessel != this.part.vessel && currentVessel.GetCrewCount() > 0)
                    {
                        crewedVesselsNearby = true;
                        break;
                    }
                }
            }

            //Check for atmosphere and homeworld.
            if (!crewedVesselsNearby)
            {
                if (!this.part.vessel.mainBody.isHomeWorld)
                    return;
                if (this.part.vessel.mainBody.isHomeWorld && this.part.vessel.atmDensity <= 0.001f)
                    return;
            }

            //Calculate rep loss
            float repLossPerFrame = repLossPerSec * TimeWarp.deltaTime;

            //Account for initial activation
            if (initialActivationCheck)
            {
                initialActivationCheck = false;
                repLossPerFrame = repLossPerSec * initialActivationRepFactor;
            }

            //Incur penalty
            Reputation.Instance.AddReputation(-repLossPerFrame, TransactionReasons.Any);

            //Inform player
            if (!playerInformed)
            {
                playerInformed = true;
                ScreenMessages.PostScreenMessage(Localizer.Format(repLossMessage), repLossDuration, ScreenMessageStyle.UPPER_CENTER);
            }
        }
    }
}
