using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
    public class ResourceHelper
    {
        public static float GetResourceMass(Part part)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceList resources = part.Resources;
            PartResourceDefinition resourceDef;
            float totalResourceMass = 0f;

            foreach (PartResource resource in resources)
            {
                //Find definition
                resourceDef = definitions[resource.resourceName];

                if (resourceDef != null)
                    totalResourceMass += (float)(resourceDef.density * resource.amount);
            }

            return totalResourceMass;
        }

        public static float GetResourceCost(Part part, bool maxAmount = false)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceList resources = part.Resources;
            PartResourceDefinition resourceDef;
            float totalCost = 0f;

            foreach (PartResource resource in resources)
            {
                //Find definition
                resourceDef = definitions[resource.resourceName];
                if (resourceDef != null)
                {
                    if (!maxAmount)
                        totalCost += (float)(resourceDef.unitCost * resource.amount);
                    else
                        totalCost += (float)(resourceDef.unitCost * resource.maxAmount);
                }

            }

            return totalCost;
        }

        public static void SetResourceValues(string resourceName, Part part, float amount, float maxAmout)
        {
            if (part.Resources.Contains(resourceName))
            {
                PartResource resource = part.Resources[resourceName];

                resource.amount = amount;
                resource.maxAmount = maxAmout;
            }
        }

        public static void RemoveResource(string resourceName, Part part)
        {
            if (part.Resources.Contains(resourceName))
            {
                PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                PartResource resource = part.Resources[resourceName];
                int resourceID = definitions[resourceName].id;

                part.Resources.dict.Remove(resourceID);
            }
        }

        public static void AddResource(string resourceName, double amount, double maxAmount, Part part)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            //First, does the resource definition exist?
            if (definitions.Contains(resourceName))
            {
                part.Resources.Add(resourceName, amount, maxAmount, true, true, false, true, PartResource.FlowMode.Both);
            }
        }

        public static void DepleteResource(string resourceName, Part part)
        {
            if (part.Resources.Contains(resourceName))
                part.Resources[resourceName].amount = 0f;
        }

        public static double ConsumeResource(List<PartResource> resources, double amountRequested)
        {
            double amountAcquired = 0;
            double amountRemaining = amountRequested;

            foreach (PartResource resource in resources)
            {
                //Do we have more than enough?
                if (resource.amount >= amountRemaining)
                {
                    //We got what we wanted, yay. :)
                    amountAcquired += amountRemaining;

                    //reduce the part resource's current amount
                    resource.amount -= amountRemaining;

                    //Done
                    break;
                }

                //PartResource's amount < amountRemaining
                //Drain the resource dry
                else
                {
                    amountAcquired += resource.amount;

                    resource.amount = 0;
                }
            }

            return amountAcquired;
        }

        public static PartResourceDefinition DefinitionForResource(string resourceName)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            if (definitions.Contains(resourceName))
                return definitions[resourceName];

            return null;
        }

        public static double GetTotalResourceSpaceAvailable(string resourceName, Vessel vessel)
        {
            double amount = GetTotalResourceAmount(resourceName, vessel);
            double maxAmount = GetTotalResourceMaxAmount(resourceName, vessel);

            return maxAmount = amount;
        }

        public static double GetTotalResourceMaxAmount(string resourceName, Vessel vessel)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            List<Part> parts;
            double totalResources = 0f;

            //First, does the resource definition exist?
            if (definitions.Contains(resourceName))
            {
                //The definition exists, now see if the vessel has the resource
                parts = vessel.parts;
                foreach (Part part in parts)
                {
                    if (part.Resources.Count > 0)
                    {
                        foreach (PartResource res in part.Resources)
                            if (res.resourceName == resourceName)
                                totalResources += res.maxAmount;
                    }
                }
            }

            return totalResources;
        }

        public static double GetTotalResourceAmount(string resourceName, Vessel vessel)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            List<Part> parts;
            double totalResources = 0f;

            //First, does the resource definition exist?
            if (definitions.Contains(resourceName))
            {
                //The definition exists, now see if the vessel has the resource
                parts = vessel.parts;
                foreach (Part part in parts)
                {
                    if (part.Resources.Count > 0)
                    {
                        foreach (PartResource res in part.Resources)
                            if (res.resourceName == resourceName)
                                totalResources += res.amount;
                    }
                }
            }

            return totalResources;
        }

        public static bool VesselHasResource(string resourceName, Vessel vessel)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            List<PartResource> resources;
            List<Part> parts;
            int resourceID;

            //First, does the resource definition exist?
            if (definitions.Contains(resourceName))
            {
                resources = new List<PartResource>();
                resourceID = definitions[resourceName].id;

                //The definition exists, now see if the vessel has the resource
                parts = vessel.parts;
                foreach (Part part in parts)
                {
                    if (part.Resources.Contains(resourceName))
                        return true;
                }
            }

            return false;
        }

        public static float CapacityRemaining(List<PartResource> resources)
        {
            float capacityRemaining = 0;

            foreach (PartResource resource in resources)
                capacityRemaining += (float)(resource.maxAmount - resource.amount);

            return capacityRemaining;
        }
    }
}
