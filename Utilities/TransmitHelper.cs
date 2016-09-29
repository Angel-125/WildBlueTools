using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

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
    public delegate void TransmitComplete();

    public struct TransmitItem
    {
        public float science;
        public float reputation;
        public float funds;
        public string title;
    }

    public class TransmitHelper
    {
        protected const string kNoAvailableTransmitter = "No Comms Devices on this vessel. Cannot Transmit Data.";
        protected const string kSoldData = "<color=lime>Transmission complete- <color=white><b>{0:f2}</b></color> Funds added!</color>";
        protected const string kPublishedData = "<color=yellow>Transmission complete- <color=white><b>{0:f2}</b></color> Reputation added!</color>";
        protected const string kSciencedData = "<color=lightblue>Transmission complete- <color=white><b>{0:f2}</b></color> Science added!</color>";

        public TransmitComplete transmitCompleteDelegate = null;
        public Part part = null;
        public bool isTransmitting;

        protected List<TransmitItem> transmitList = new List<TransmitItem>();
        protected FixedUpdateHelper fixedUpdateHelper;
        protected ModuleDataTransmitter transmitterToMonitor;
        protected bool monitorTransmitterStatus;

        public bool TransmitToKSC(ScienceData data)
        {
            if (isTransmitting)
                return true;

            //Package up the data.
            TransmitItem item = new TransmitItem();
            item.science = data.dataAmount;
            item.reputation = 0f;
            item.funds = 0f;
            item.title = data.title;
            transmitList.Add(item);

            //Find an available transmitter. if found, transmit the data.
            return transmit_data(data);
        }

        public bool TransmitToKSC(float science, float reputation, float funds, float dataAmount = -1f, string experimentID = "crewReport")
        {
            if (isTransmitting)
                return true;

            float transmitSize = dataAmount;

            if (transmitSize == -1f)
            {
                if (science > 0f)
                    transmitSize = science * 1.25f;
                else if (reputation > 0f)
                    transmitSize = reputation * 1.25f;
                else
                    transmitSize = funds * 1.25f;
            }

            else
            {
                transmitSize = dataAmount;
            }

            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.SrfLanded, FlightGlobals.GetHomeBody(), "");
            ScienceData data = new ScienceData(transmitSize, 0f, 0, subject.id, "");

            //Package up the data and put it in the queue.
            TransmitItem item = new TransmitItem();
            item.science = science;
            item.reputation = reputation;
            item.funds = funds;
            transmitList.Add(item);

            //Find an available transmitter. if found, transmit the data.
            return transmit_data(data);
        }

        private bool transmit_data(ScienceData data)
        {
            List<ScienceData> dataQueue = new List<ScienceData>();
            List<ModuleDataTransmitter> transmitters = this.part.vessel.FindPartModulesImplementing<ModuleDataTransmitter>();
            ModuleDataTransmitter bestTransmitter = null;

            dataQueue.Add(data);
            foreach (ModuleDataTransmitter transmitter in transmitters)
            {
                if (transmitter.IsBusy() == false)
                {
                    if (bestTransmitter == null)
                        bestTransmitter = transmitter;
                    else if (transmitter.packetSize > bestTransmitter.packetSize)
                        bestTransmitter = transmitter;
                }
            }

            //If we find a transmitter, then set up the transmission.
            if (bestTransmitter != null)
            {
                bestTransmitter.TransmitData(dataQueue);
                monitor_for_completion(bestTransmitter);
                isTransmitting = true;
                return true;
            }

            //No transmitter found this far, but does the user have RemoteTech installed?
            else if (Utils.IsModInstalled("RemoteTech"))
            {
                return transmit_data_rt(dataQueue);
            }

            //No transmitter found.
            else
            {
                //Inform user that there is no available transmitter.
                ScreenMessages.PostScreenMessage(kNoAvailableTransmitter, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
        }

        private bool transmit_data_rt(List<ScienceData> dataQueue)
        {
            /*
            WBIRTWrapper bestRTAntenna = null;
            WBIRTWrapper rtAntenna = null;
            List<PartModule> rtAntennas = new List<PartModule>();
            ModuleDataTransmitter transmitter = null;
             */

            //Do we even have a valid connection to KSC?
            WBIRTWrapper rtWrapper = new WBIRTWrapper();
            if (rtWrapper.HasConnectionToKSC(this.part.vessel.id) == false)
            {
                ScreenMessages.PostScreenMessage(kNoAvailableTransmitter, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            //For now, fake a transmission
            OnTransmitComplete();
            return true;

            /*

            //First, look for an RTAntenna module
            foreach (Part vesselPart in this.part.vessel.Parts)
            {
                foreach (PartModule module in vesselPart.Modules)
                {
                    if (module.moduleName == "ModuleRTAntenna")
                    {
                        rtAntennas.Add(module);
                    }
                }
            }

            //If we didn't find an antenna, then we're done.
            if (rtAntennas.Count == 0)
            {
                ScreenMessages.PostScreenMessage(kNoAvailableTransmitter, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            //Now we need to find the best antenna in terms of packet size.
            foreach (PartModule module in rtAntennas)
            {
                rtAntenna = new WBIRTWrapper(module);
                if (bestRTAntenna == null)
                    bestRTAntenna = rtAntenna;

                else if (rtAntenna.PacketSize > bestRTAntenna.PacketSize)
                    bestRTAntenna = rtAntenna;
            }

            //Once we find the best antenna, set it up.
            bestRTAntenna.SetState(true);
            transmitter = bestRTAntenna.rtPartModule.part.FindModuleImplementing<ModuleDataTransmitter>();
            if (transmitter == null)
            {
                Debug.Log("Unable to create a ModuleDataTransmitter");
                return false;
            }

            //Ok, start transmitting!
            transmitter.TransmitData(dataQueue);
            monitor_for_completion(transmitter);
            isTransmitting = true;
            return true;
             */
        }

        private void monitor_for_completion(ModuleDataTransmitter transmitter)
        {
            transmitterToMonitor = transmitter;
            monitorTransmitterStatus = true;

            if (fixedUpdateHelper == null)
            {
                fixedUpdateHelper = this.part.gameObject.AddComponent<FixedUpdateHelper>();
                fixedUpdateHelper.onFixedUpdateDelegate = OnUpdateFixed;
            }
            fixedUpdateHelper.enabled = true;
        }

        public void OnUpdateFixed()
        {
            if (!monitorTransmitterStatus)
                return;

            if (transmitterToMonitor.statusText.Contains("Done"))
            {
                isTransmitting = false;
                OnTransmitComplete();
                monitorTransmitterStatus = false;
            }
        }

        public void OnTransmitComplete()
        {
            string transmitMessage = "";

            //Get the top item off the list
            TransmitItem item = transmitList[0];
            transmitList.RemoveAt(0);

            if (item.science > 0f && ResearchAndDevelopment.Instance != null)
            {
                transmitMessage = string.Format(kSciencedData, item.science);
                if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
                    ResearchAndDevelopment.Instance.AddScience(item.science, TransactionReasons.ScienceTransmission);
                ScreenMessages.PostScreenMessage(transmitMessage, 5.0f, ScreenMessageStyle.UPPER_LEFT);
            }

            if (item.reputation > 0f && Reputation.Instance != null)
            {
                transmitMessage = string.Format(kPublishedData, item.reputation);
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    Reputation.Instance.AddReputation(item.reputation, TransactionReasons.ScienceTransmission);
                ScreenMessages.PostScreenMessage(transmitMessage, 5.0f, ScreenMessageStyle.UPPER_LEFT);
            }

            if (item.funds > 0f && Funding.Instance != null)
            {
                transmitMessage = string.Format(kSoldData, item.funds);
                Funding.Instance.AddFunds(item.funds, TransactionReasons.ScienceTransmission);
                ScreenMessages.PostScreenMessage(transmitMessage, 5.0f, ScreenMessageStyle.UPPER_LEFT);
            }

            if (transmitCompleteDelegate != null)
                transmitCompleteDelegate();
        }

    }
}
