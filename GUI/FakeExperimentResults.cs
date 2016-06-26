using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens.Flight.Dialogs;

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
    public delegate void ResetData(ScienceData data);
    public delegate void KeepData(ScienceData data);
    public delegate void ProcessData(ScienceData data);
    public delegate void TransmitData(ScienceData data);

    public class FakeExperimentResults
    {
        public ResetData resetDelegate;
        public KeepData keepDelegate;
        public ProcessData processDelegate;
        public TransmitData transmitDelegate;
        public Part part;

        protected ModuleScienceLab scienceLab;
        float contextBonus = 0;
        float surfaceBonus = 0;
        float homeworldBonus = 0;

        public void ShowResults(string experimentID, float amount, ModuleScienceLab lab = null)
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ScienceUtil.GetExperimentSituation(part.vessel),
                part.vessel.mainBody, Utils.GetCurrentBiome(part.vessel).name);

            //Kerbin low orbit has a science multiplier of 1.
            ScienceSubject subjectLEO = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.InSpaceLow,
                FlightGlobals.GetHomeBody(), "");

            //This ensures you can re-run the experiment.
            subjectLEO.science = 0f;
            subjectLEO.scientificValue = 1f;

            //Create science data
            ScienceData data = new ScienceData(amount, 1f, 0f, subjectLEO.id, subject.title);

            ShowResults(data, lab);
        }

        public void ShowResults(ScienceData data, ModuleScienceLab lab = null)
        {
            scienceLab = lab;
            bool hasLab = scienceLab != null ? true : false;

            if (lab != null)
            {
                contextBonus = lab.ContextBonus;
                surfaceBonus = lab.SurfaceBonus;
                homeworldBonus = lab.homeworldMultiplier;

                lab.ContextBonus = 0;
                lab.SurfaceBonus = 0;
                lab.homeworldMultiplier = 0;
            }

            //Now show the dialog
            ScienceLabSearch labSearch = new ScienceLabSearch(this.part.vessel, data);
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(part, data, data.transmitValue, data.labBoost, false, "", true, labSearch, Reset, Keep, Transmit, Process);
            ExperimentsResultDialog dlg = ExperimentsResultDialog.DisplayResult(page);
        }

        public void Reset(ScienceData data)
        {
            resetLabBonuses();

            if (resetDelegate != null)
                resetDelegate(data);
        }

        public void Keep(ScienceData data)
        {
            resetLabBonuses();

            if (keepDelegate != null)
                keepDelegate(data);
        }

        public void Process(ScienceData data)
        {
            resetLabBonuses();

            if (processDelegate != null)
                processDelegate(data);
        }

        public void Transmit(ScienceData data)
        {
            resetLabBonuses();

            if (transmitDelegate != null)
                transmitDelegate(data);
        }

        protected void resetLabBonuses()
        {
            if (scienceLab != null)
            {
                scienceLab.ContextBonus = contextBonus;
                scienceLab.SurfaceBonus = surfaceBonus;
                scienceLab.homeworldMultiplier = homeworldBonus;
            }
        }

    }
}
