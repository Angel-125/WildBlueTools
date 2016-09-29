using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.IO;

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
    public enum EInvalidTemplateReasons
    {
        TemplateIsValid,
        TechNotUnlocked,
        InvalidIndex,
        RequiredModuleNotFound,
        NoTemplates,
        TagsNotFound
    }

    public class TemplateManager
    {
        public Part part = null;
        public Vessel vessel = null;
        public LogDelegate logDelegate = null;
        public ConfigNode[] templateNodes;
        public string templateNodeName;
        public string templateTags;
        private static List<string> partTokens;
        protected static Dictionary<string, string> techNodeTitles;

        #region API
        public TemplateManager(Part part, Vessel vessel, LogDelegate logDelegate, string template = "nodeTemplate", string templateTags = null)
        {
            this.part = part;
            this.vessel = vessel;
            this.logDelegate = logDelegate;

            this.templateNodeName = template;
            this.templateTags = templateTags;

            this.templateNodes = GameDatabase.Instance.GetConfigNodes(template);
            if (templateNodes == null)
            {
                Log("nodeTemplatesModel templateNodes == null!");
                return;
            }
        }

        public void FilterTemplates()
        {
            List<ConfigNode> templates = new List<ConfigNode>();
            string[] potentialTemplates = templateNodeName.Split(new char[] { ';' });
            ConfigNode[] templateConfigs;

            foreach (string potentialTemplate in potentialTemplates)
            {
                templateConfigs = GameDatabase.Instance.GetConfigNodes(potentialTemplate);
                if (templateConfigs == null)
                    continue;

                //Find valid templates
                foreach (ConfigNode config in templateConfigs)
                {
                    EInvalidTemplateReasons templateReason = CanUseTemplate(config);
                    if (templateReason == EInvalidTemplateReasons.TemplateIsValid)
                        templates.Add(config);
                }
            }

            //Done
            this.templateNodes = templates.ToArray();
            Log(templateNodeName + " has " + templates.Count + " templates.");
            ConfigNode node;
            for (int index = 0; index < this.templateNodes.Length; index++)
            {
                node = this.templateNodes[index];
                Log("Template " + index + ": " + node.GetValue("shortName") + ", " + node.GetValue("name"));
            }
        }

        public ConfigNode this[string templateName]
        {
            get
            {
                int index = FindIndexOfTemplate(templateName);

                return this.templateNodes[index];
            }
        }

        public ConfigNode this[long index]
        {
            get
            {
                return this.templateNodes[index];
            }
        }

        public static EInvalidTemplateReasons CheckNeeds(string neededMod)
        {
            string modToCheck = neededMod;
            bool checkInverse = false;
            if (neededMod.StartsWith("!"))
            {
                checkInverse = true;
                modToCheck = neededMod.Substring(1, neededMod.Length - 1);
            }

            bool isInstalled = AssemblyLoader.loadedAssemblies.Any(a => a.name == modToCheck);

            if (isInstalled && checkInverse == false)
                return EInvalidTemplateReasons.TemplateIsValid;
            else if (isInstalled && checkInverse)
                return EInvalidTemplateReasons.RequiredModuleNotFound;
            else if (!isInstalled && checkInverse)
                return EInvalidTemplateReasons.TemplateIsValid;
            else
                return EInvalidTemplateReasons.RequiredModuleNotFound;
        }

        public static string GetTechTreeTitle(ConfigNode nodeTemplate)
        {
            string value;
            
            if (ResearchAndDevelopment.Instance != null)
            {
                value = nodeTemplate.GetValue("TechRequired");
                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                //Build cache if needed
                if (techNodeTitles == null)
                {
                    techNodeTitles = new Dictionary<string, string>();
                    ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("TechTree");
                    nodes = nodes[0].GetNodes("RDNode");

                    foreach (ConfigNode node in nodes)
                        techNodeTitles.Add(node.GetValue("id"), node.GetValue("title"));
                }

                //Now find the title
                if (techNodeTitles.ContainsKey(value))
                    return techNodeTitles[value];
                else
                    return string.Empty;
            }

            return string.Empty;
        }

        public static bool TemplateTechResearched(ConfigNode nodeTemplate)
        {
            string value;

            //If we are in sandbox mode, then we're done.
            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
                return true;

            value = nodeTemplate.GetValue("TechRequired");
            if (string.IsNullOrEmpty(value))
                return true;

            if (ResearchAndDevelopment.GetTechnologyState(value) != RDTech.State.Available)
                return false;

            return true;
        }

        public EInvalidTemplateReasons CanUseTemplate(ConfigNode nodeTemplate)
        {
            string value;
            PartModule requiredModule;
            EInvalidTemplateReasons invalidTemplateReason;

            //Make sure the vessel object is set
            if (this.vessel == null)
                this.vessel = this.part.vessel;

            //If we need a specific mod then check for it.
            value = nodeTemplate.GetValue("needs");
            if (string.IsNullOrEmpty(value) == false)
            {
                invalidTemplateReason = TemplateManager.CheckNeeds(value);

                if (invalidTemplateReason != EInvalidTemplateReasons.TemplateIsValid)
                    return invalidTemplateReason;
            }

            //If we need a specific module then check for it.
            value = nodeTemplate.GetValue("requiresModule");
            if (string.IsNullOrEmpty(value) == false)
            {
                requiredModule = this.part.Modules[value];
                if (requiredModule == null)
                {
                    return EInvalidTemplateReasons.RequiredModuleNotFound;
                }
            }

            //If we need a specific template type then check for it.
            //Only templates with the appropriate tag will be accepted.
            if (string.IsNullOrEmpty(templateTags) == false)
            {
                if (nodeTemplate.HasValue("templateTags") == false)
                    return EInvalidTemplateReasons.TagsNotFound;

                value = nodeTemplate.GetValue("templateTags");
                string[] tags = value.Split(new char[] { ';' });
                foreach (string tag in tags)
                {
                    if (templateTags.Contains(tag))
                        return EInvalidTemplateReasons.TemplateIsValid;
                }
                return EInvalidTemplateReasons.TagsNotFound;
            }

            return EInvalidTemplateReasons.TemplateIsValid;
        }

        public EInvalidTemplateReasons CanUseTemplate(string templateName)
        {
            int index = FindIndexOfTemplate(templateName);

            return CanUseTemplate(index);
        }

        public EInvalidTemplateReasons CanUseTemplate(int index)
        {
            if (this.templateNodes == null)
                return EInvalidTemplateReasons.NoTemplates;

            if (index < 0 || index > templateNodes.Count<ConfigNode>())
                return EInvalidTemplateReasons.InvalidIndex;

            return CanUseTemplate(templateNodes[index]);
        }

        public int FindIndexOfTemplate(string templateName)
        {
            int templateIndex = -1;
            int totalTemplates = -1;
            string shortName;

            //Get total template count
            if (this.templateNodes == null)
                return -1;
            totalTemplates = this.templateNodes.Count<ConfigNode>();

            //Loop through the templates and find the one matching the desired template name
            //the GUI friendly shortName
            for (templateIndex = 0; templateIndex < totalTemplates; templateIndex++)
            {
                shortName = this.templateNodes[templateIndex].GetValue("shortName");
                if (!string.IsNullOrEmpty(shortName))
                {
                    if (shortName == templateName)
                        return templateIndex;
                }
            }

            return -1;
        }

        public int GetPrevTemplateIndex(int startIndex)
        {
            int prevIndex = startIndex;

            if (this.templateNodes == null)
                return -1;

            if (this.templateNodes.Count<ConfigNode>() == 0)
                return -1;

            //Get prev index in template array
            prevIndex = prevIndex - 1;
            if (prevIndex < 0)
                prevIndex = this.templateNodes.Count<ConfigNode>() - 1;

            return prevIndex;
        }

        public int GetNextTemplateIndex(int startIndex)
        {
            int nextIndex = startIndex;

            if (this.templateNodes == null)
                return -1;

            if (this.templateNodes.Count<ConfigNode>() == 0)
                return -1;

            //Get next index in template array
            nextIndex = (nextIndex + 1) % this.templateNodes.Count<ConfigNode>();

            return nextIndex;
        }

        public int GetPrevUsableIndex(int startIndex)
        {
            int totalTries = this.templateNodes.Count<ConfigNode>();
            int prevIndex = startIndex;
            ConfigNode template;

            do
            {
                prevIndex = GetPrevTemplateIndex(prevIndex);
                template = this[prevIndex];
                totalTries -= 1;

                if (CanUseTemplate(template) == EInvalidTemplateReasons.TemplateIsValid)
                    return prevIndex;
            }

            while (totalTries > 0);

            return -1;
        }

        public int GetNextUsableIndex(int startIndex)
        {
            int totalTries = this.templateNodes.Count<ConfigNode>();
            int nextIndex = startIndex;
            ConfigNode template;

            do
            {
                nextIndex = GetNextTemplateIndex(nextIndex);
                template = this[nextIndex];
                totalTries -= 1;

                if (CanUseTemplate(template) == EInvalidTemplateReasons.TemplateIsValid)
                    return nextIndex;
            }

            while (totalTries > 0);        

            return -1;
        }

        #endregion

        #region Helpers
        public virtual void Log(object message)
        {
            if (logDelegate != null)
                logDelegate(message);
        }
        #endregion
    }
}
