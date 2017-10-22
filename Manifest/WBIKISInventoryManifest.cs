using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using KSP.UI.Screens;
using KSP.Localization;


/*
Source code copyrighgt 2017, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public struct WBIInventoryManifestItem
    {
        public float volume;
        public int quantity;
        public string partName;
        public ConfigNode partConfigNode;
    }

    public class WBIKISInventoryManifest : WBIManifest
    {
        public const string kInventoryNode = "INVENTORY_ITEM";
        public const string kPartName = "partName";
        public const string kQuantity = "quantity";
        public const string kVolume = "volume";
        public const string kInventoryType = "WBIKISInventoryManifest";
        public const string kPartNode = "PART";
        public const string kPartConfig = "PART_CONFIG";

        public List<WBIInventoryManifestItem> inventoryItems = new List<WBIInventoryManifestItem>();

        public static List<WBIKISInventoryManifest> GetManifestsForDestination(string destinationID)
        {
            List<WBIKISInventoryManifest> manifestList = new List<WBIKISInventoryManifest>();

            //Find all the manifests that match the destination
            WBIKISInventoryManifest manifest;
            List<ConfigNode> nodes = WBIManifestScenario.Instance.GetManifestConfigs(destinationID, WBIKISInventoryManifest.kInventoryType);
            foreach (ConfigNode node in nodes)
            {
                manifest = new WBIKISInventoryManifest();
                manifest.Load(node);
                manifestList.Add(manifest);
            }

            return manifestList;
        }

        public WBIKISInventoryManifest()
        {
            manifestType = kInventoryType;
        }

        public override void Load(ConfigNode node)
        {
            base.Load(node);

            ConfigNode[] itemNodes = node.GetNodes(kInventoryNode);
            ConfigNode itemNode;
            WBIInventoryManifestItem inventoryItem;
            for (int index = 0; index < itemNodes.Length; index++)
            {
                itemNode = itemNodes[index];

                inventoryItem = new WBIInventoryManifestItem();
                inventoryItem.quantity = int.Parse(itemNode.GetValue(kQuantity));
                inventoryItem.partName = itemNode.GetValue(kPartName);
                inventoryItem.volume = float.Parse(itemNode.GetValue(kVolume));
                inventoryItem.partConfigNode = itemNode.GetNode(kPartConfig);

                inventoryItems.Add(inventoryItem);
            }
        }

        public override void Save(ConfigNode node)
        {
            base.Save(node);

            WBIInventoryManifestItem inventoryItem;
            ConfigNode itemNode;
            int totalItems = inventoryItems.Count;
            for (int index = 0; index < totalItems; index++)
            {
                inventoryItem = inventoryItems[index];

                itemNode = new ConfigNode(kInventoryNode);
                itemNode.AddValue(kPartName, inventoryItem.partName);
                itemNode.AddValue(kQuantity, inventoryItem.quantity);
                itemNode.AddValue(kVolume, inventoryItem.volume);
                itemNode.AddNode(kPartConfig, inventoryItem.partConfigNode);

                node.AddNode(itemNode);
            }
        }
    }
}
