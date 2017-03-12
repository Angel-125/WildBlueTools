using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;
using KSP;
using KSPAchievements;

//Courtesy of MrHappyFace
namespace ContractsPlus.Contracts
{
    public class WBIExpCompleteParam : ContractParameter
    {
        string experimentID = string.Empty;
        string targetBody;
        string situations = string.Empty;

        bool isRegistered;

        public WBIExpCompleteParam()
        {
        }

        public WBIExpCompleteParam(string experiment, CelestialBody body, string situationsString)
        {
            this.experimentID = experiment;
            this.targetBody = body.name;
            this.situations = situationsString;
        }

        protected override string GetHashString()
        {
            return Guid.NewGuid().ToString();
        }

        protected override string GetTitle()
        {
            return "Complete the experiment";
        }

        protected override void OnRegister()
        {
            if (isRegistered)
                return;

            isRegistered = true;
            GameEvents.OnExperimentDeployed.Add(OnExperimentDeployed);
        }

        protected override void OnUnregister()
        {
            isRegistered = false;
            GameEvents.OnExperimentDeployed.Remove(OnExperimentDeployed);
        }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("experimentID", experimentID);
            node.AddValue("targetBody", targetBody);

            if (string.IsNullOrEmpty(situations) == false)
                node.AddValue("situations", situations);
        }

        protected override void OnLoad(ConfigNode node)
        {
            experimentID = node.GetValue("experimentID");
            targetBody = node.GetValue("targetBody");
            situations = node.GetValue("situations");
        }

        protected void setComplete()
        {
            WBIResearchContract contract = (WBIResearchContract)Root;
            contract.experimentCompleted = true;
            base.SetComplete();
        }

        private void OnExperimentDeployed(ScienceData data)
        {
            //data.subjectID example: WBICryogenicResourceStudy@MinmusInSpaceHigh
            if (data.subjectID.Contains(experimentID) && data.subjectID.Contains(targetBody))
            {
                //Make sure the situation matches
                if (string.IsNullOrEmpty(situations))
                {
                    setComplete();
                    return;
                }

                if (situations == "any")
                {
                    setComplete();
                    return;
                }

                //Flying InSpace Landed Spashed
                string[] situationRequirements = situations.Split(new char[] { ';' });
                for (int index = 0; index < situationRequirements.Length; index++)
                {
                    switch (situationRequirements[index])
                    {
                        case "SPLASHED":
                            if (data.subjectID.Contains("Splashed"))
                            {
                                setComplete();
                            }
                            break;

                        case "FLYING":
                            if (data.subjectID.Contains("Flying"))
                            {
                                setComplete();
                            }
                            break;

                        case "PRELAUNCH":
                        case "LANDED":
                            if (data.subjectID.Contains("Landed"))
                            {
                                setComplete();
                            }
                            break;

                        case "ESCAPING":
                        case "SUB_ORBITAL":
                        case "ORBITING":
                        default:
                            if (data.subjectID.Contains("InSpace"))
                            {
                                setComplete();
                            }
                            break;
                    }
                }

            }
        }
    }
}
