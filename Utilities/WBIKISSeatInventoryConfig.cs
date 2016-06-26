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
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class WBIKISSeatInventoryConfig : MonoBehaviour
    {
        public static float maxSeatVolume = 300.0f;

        public void Awake()
        {
            if (Utils.IsModInstalled("KIS") == false)
                return;

            WBIKISInventoryWrapper inventory;
            int seatIndex = 0;

            //Try to get the max EVA volume from the KIS config settings
            ConfigNode nodeKISSettings = GameDatabase.Instance.GetConfigNode("KIS/settings/KISConfig");
            if (nodeKISSettings != null)
            {
                if (nodeKISSettings.HasNode("EvaInventory"))
                {
                    ConfigNode nodeEVAInventory = nodeKISSettings.GetNode("EvaInventory");
                    if (nodeEVAInventory.HasValue("maxVolume"))
                        maxSeatVolume = float.Parse(nodeEVAInventory.GetValue("maxVolume"));
                }
            }

            //No go through each part and find a ModuleKISInventory
            //If found, and we find parts with seat inventory volume,
            //then Set the seat index.
            foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                if (part.name == "kerbalEVA" || part.name == "kerbalEVAfemale" || part.name == "kerbalEVA_RD")
                    continue;

                seatIndex = 0;
                foreach (PartModule partModule in part.partPrefab.Modules)
                {
                    if (partModule.moduleName == "ModuleKISInventory")
                    {
                        try
                        {
                            inventory = new WBIKISInventoryWrapper(partModule);
                            if (inventory.maxVolume <= 0.001f)
                            {
                                inventory.maxVolume = maxSeatVolume;
                                inventory.podSeat = seatIndex;
                                inventory.invType = WBIKISInventoryWrapper.InventoryType.Pod;
                                seatIndex += 1;
                            }
                        }

                        catch (Exception ex)
                        {
                            Debug.Log("WBIKISSeatInventoryConfig encountered an error while setting up a seat inventory: " + ex.Message);
                            continue;
                        }
                    }
                }
            }

        }
    }

}
