using System;
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
    internal class WBIRTWrapper
    {
        static Assembly rtAssembly;
        static Type typeAPI;
        static Type typeModuleRTAntenna;
        static MethodInfo miHasConnectionToKSC;
        static MethodInfo miSetState;
        static FieldInfo fiPacketSize;
        static FieldInfo fiPacketInterval;
        static FieldInfo fiResourceCost;

        public PartModule rtPartModule;

        public WBIRTWrapper(PartModule partModule)
        {
            if (rtAssembly == null)
            {
                foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
                {
                    if (loadedAssembly.name == "RemoteTech")
                    {
                        rtAssembly = loadedAssembly.assembly;
                        break;
                    }
                }

                if (rtAssembly == null)
                {
                    Debug.Log("No RemoteTech assembly found");
                    return;
                }
            }

            rtPartModule = partModule;

            if (typeModuleRTAntenna == null)
            {
                typeModuleRTAntenna = rtAssembly.GetTypes().First(t => t.Name.Equals("ModuleRTAntenna"));
                if (typeModuleRTAntenna == null)
                {
                    Debug.Log("No ModuleRTAntenna found");
                }

                miSetState = typeModuleRTAntenna.GetMethod("SetState");
                fiPacketSize = typeModuleRTAntenna.GetField("RTPacketSize");
                fiPacketInterval = typeModuleRTAntenna.GetField("RTPacketInterval");
                fiResourceCost = typeModuleRTAntenna.GetField("RTPacketResourceCost");
            }
        }

        public WBIRTWrapper()
        {
            if (rtAssembly == null)
            {
                foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
                {
                    if (loadedAssembly.name == "RemoteTech")
                    {
                        rtAssembly = loadedAssembly.assembly;
                        break;
                    }
                }

                if (rtAssembly == null)
                {
                    Debug.Log("No RemoteTech assembly found");
                    return;
                }
            }

            //Get the methods and properties we need.
            typeAPI = rtAssembly.GetTypes().First(t => t.Name.Equals("API"));
            if (typeAPI == null)
            {
                Debug.Log("No API found");
                return;
            }

            miHasConnectionToKSC = typeAPI.GetMethod("HasConnectionToKSC");
            if (miHasConnectionToKSC == null)
            {
                Debug.Log("No HasConnectionToKSC found");
                return;
            }
        }

        public void SetState(bool state)
        {
            if (miSetState == null)
                return;

            miSetState.Invoke(rtPartModule, new object[] { state });
        }

        public float PacketSize
        {
            get
            {
                if (fiPacketSize == null)
                    return -1.0f;

                return (float)fiPacketSize.GetValue(rtPartModule);
            }
        }

        public float PacketInterval
        {
            get
            {
                if (fiPacketInterval == null)
                    return -1.0f;

                return (float)fiPacketInterval.GetValue(rtPartModule);
            }
        }

        public float ResourceCost
        {
            get
            {
                if (fiResourceCost == null)
                    return -1.0f;

                return (float)fiResourceCost.GetValue(rtPartModule);
            }
        }

        public bool HasConnectionToKSC(Guid id)
        {
            if (miHasConnectionToKSC == null)
            {
                Debug.Log("No method found to check for connection to KSC");
                return false;
            }

            object[] parametersArray = new object[] { id };
            bool result = (bool)miHasConnectionToKSC.Invoke(null, parametersArray);
            return result;
        }
    }
}
