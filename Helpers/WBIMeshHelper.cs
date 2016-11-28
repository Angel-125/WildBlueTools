using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIMeshHelper : ExtendedPartModule
    {
        [KSPField]
        public string objects = string.Empty;

        [KSPField(isPersistant = true)]
        public int selectedObject = 0;

        [KSPField()]
        public string guiNames = string.Empty;

        protected List<List<Transform>> objectTransforms = new List<List<Transform>>();
        protected Dictionary<string, int> meshIndexes = new Dictionary<string, int>();
        protected List<string> objectNames = new List<string>();
        protected bool showGui = false;
        protected bool showPrev = true;
        protected bool editorOnly = false;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next variant", active = true)]
        public virtual void NextMesh()
        {
            int nextIndex = selectedObject;

            nextIndex = (nextIndex + 1) % this.objectNames.Count;

            setObject(nextIndex);

            if (objectNames.Count > 0)
            {
                nextIndex = (nextIndex + 1) % this.objectNames.Count;
                Events["NextMesh"].guiName = objectNames[nextIndex];
            }

        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Prev variant", active = true)]
        public virtual void PrevMesh()
        {
            int nextIndex = selectedObject;

            nextIndex = (nextIndex - 1) % this.objectNames.Count;

            setObject(nextIndex);

            if (objectNames.Count > 0)
            {
                nextIndex = (nextIndex - 1) % this.objectNames.Count;
                Events["NextMesh"].guiName = objectNames[nextIndex];
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            parseObjectNames();
            setObject(selectedObject);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("selectedObject", selectedObject.ToString());

            node.AddValue("showGui", showGui.ToString());
        }

        public virtual void OnEditorAttach()
        {
            Events["NextMesh"].active = showGui;
            Events["NextMesh"].guiActive = showGui;
            Events["NextMesh"].guiActiveEditor = showGui;
            Events["PrevMesh"].active = showGui && showPrev;
            Events["PrevMesh"].guiActive = showGui && showPrev;
            Events["PrevMesh"].guiActiveEditor = showGui && showPrev;
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);

            string value = protoNode.GetValue("selectedObject");
            if (string.IsNullOrEmpty(value) == false)
                selectedObject = int.Parse(value);

            value = protoNode.GetValue("showGui");
            if (string.IsNullOrEmpty(value) == false)
                showGui = bool.Parse(value);

            value = protoNode.GetValue("showPrev");
            if (string.IsNullOrEmpty(value) == false)
                showPrev = bool.Parse(value);

            value = protoNode.GetValue("editorOnly");
            if (string.IsNullOrEmpty(value) == false)
                editorOnly = bool.Parse(value);

            objects = protoNode.GetValue("objects");
            guiNames = protoNode.GetValue("guiNames");
            if (HighLogic.LoadedSceneIsEditor)
            {
                parseObjectNames();
                setObject(selectedObject);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            this.part.OnEditorAttach += OnEditorAttach;

            if (editorOnly && HighLogic.LoadedSceneIsEditor == false)
                showGui = false;

            Events["NextMesh"].active = showGui;
            Events["NextMesh"].guiActive = showGui;
            Events["NextMesh"].guiActiveEditor = showGui;

            Events["PrevMesh"].active = showGui && showPrev;
            Events["PrevMesh"].guiActive = showGui && showPrev;
            Events["PrevMesh"].guiActiveEditor = showGui && showPrev;

            if (objectTransforms.Count == 0)
            {
                parseObjectNames();
                setObject(selectedObject);
            }

            if (objectNames.Count > 0)
            {
                int nextIndex = (selectedObject + 1) % this.objectNames.Count;
                if (nextIndex == objectNames.Count)
                    nextIndex = 0;
                if (string.IsNullOrEmpty(objectNames[nextIndex]) == false)
                    Events["NextMesh"].guiName = objectNames[nextIndex];
            }
        }

        protected void parseObjectNames()
        {
            string[] elements;
            string[] objectBatchNames = objects.Split(';');

            objectTransforms.Clear();
            objectNames.Clear();
            meshIndexes.Clear();

            if (objectBatchNames.Length >= 1)
            {
                objectTransforms.Clear();
                for (int batchCount = 0; batchCount < objectBatchNames.Length; batchCount++)
                {
                    List<Transform> newObjects = new List<Transform>();
                    string[] namedObjects = objectBatchNames[batchCount].Split(',');
                    for (int objectCount = 0; objectCount < namedObjects.Length; objectCount++)
                    {
                        Transform newTransform = part.FindModelTransform(namedObjects[objectCount].Trim(' '));
                        if (newTransform != null)
                        {
                            newObjects.Add(newTransform);
                        }
                        else
                        {
                            //Log("cannot find " + namedObjects[objectCount]);
                        }
                    }
                    if (newObjects.Count > 0) objectTransforms.Add(newObjects);
                }
            }

            //Go through each entry and split up the entry into its template name and mesh index
            elements = objects.Split(';');
            for (int index = 0; index < elements.Count<string>(); index++)
                meshIndexes.Add(elements[index], index);

            if (guiNames != null)
            {
                elements = guiNames.Split(';');
                foreach (string element in elements)
                    objectNames.Add(element);
            }
        }

        protected void setObject(int objectNumber, bool startHidden = true)
        {
            Collider collider = null;

            if (objectTransforms.Count == 0)
            {
                Log("objectTransforms.Count = 0! Exiting");
                return;
            }

            if (startHidden)
            {
                for (int i = 0; i < objectTransforms.Count; i++)
                {
                    for (int j = 0; j < objectTransforms[i].Count; j++)
                    {
                        objectTransforms[i][j].gameObject.SetActive(false);
                        collider = objectTransforms[i][j].gameObject.GetComponent<Collider>();
                        if (collider != null)
                        {
                            collider.enabled = false;
                        }
                    }
                }
            }

            //If we have no object selected then just exit.
            if (objectNumber == -1)
                return;

            // enable the selected one last because there might be several entries with the same object, and we don't want to disable it after it's been enabled.
            for (int i = 0; i < objectTransforms[objectNumber].Count; i++)
            {
                objectTransforms[objectNumber][i].gameObject.SetActive(true);

                collider = objectTransforms[objectNumber][i].gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }

            selectedObject = objectNumber;
        }

        protected void setObjects(List<int> objects)
        {
            setObject(-1);

            foreach (int objectId in objects)
                setObject(objectId, false);
        }

        protected void showAll()
        {
            setObject(-1);

            for (int objectIndex = 0; objectIndex < objectTransforms.Count; objectIndex++)
                setObject(objectIndex, false);
        }

        protected void hideAll()
        {
            setObject(-1);
        }

    }
}
