using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIJukebox : InternalModule
    {
        [KSPField]
        public string powerButtonName = "PowerButton";

        [KSPField]
        public string leftButtonName = "LeftButton";

        [KSPField]
        public string rightButtonName = "RightButton";

        [KSPField]
        public string musicPath = "WildBlueIndustries/000WildBlueTools/Music";

        static int totalFiles = 0;
        static string[] musicPathFiles;

        AudioSource musicFile;
        int songIndex = 0;
        bool isPlaying = false;

        public void Start()
        {
            musicFile = gameObject.AddComponent<AudioSource>();
            if (musicFile == null)
                Debug.Log("[WBIJukebox] - musicFile is null!");
            getMusicFiles();
            setupButtons();
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (!isPlaying)
                return;

            if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA &&
                CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.Internal)
            {
                musicFile.Stop();
                isPlaying = false;
            }
        }

        public void OnPowerButtonClick()
        {
            if (WBIMainSettings.EnableDebugLogging)
                Debug.Log("[WBIJukebox] - OnPowerButtonClick called. Total songs: " + totalFiles);
            if (totalFiles == 0)
                return;

            //Play a random song
            if (!isPlaying)
            {
                isPlaying = true;
                songIndex = UnityEngine.Random.Range(0, totalFiles);
                playSong();
            }
            else
            {
                isPlaying = false;
                if (musicFile != null)
                    musicFile.Stop();
            }
        }

        public void OnLeftButtonClick()
        {
            if (totalFiles == 0)
                return;

            songIndex -= 1;
            if (songIndex < 0)
                songIndex = totalFiles - 1;
            isPlaying = true;
            playSong();
        }

        public void OnRightButtonClick()
        {
            if (totalFiles == 0)
                return;

            songIndex += 1;
            if (songIndex > totalFiles - 1)
                songIndex = 0;
            isPlaying = true;
            playSong();
        }

        protected void playSong()
        {
            if (WBIMainSettings.EnableDebugLogging)
                Debug.Log("[WBIJukebox] - playSong called.");
            if (songIndex < 0 || songIndex > totalFiles - 1)
            {
                if (WBIMainSettings.EnableDebugLogging)
                    Debug.Log("[WBIJukebox] - songIndex is out of range! Value: " + songIndex);
                return;
            }
            if (musicFile == null)
            {
                if (WBIMainSettings.EnableDebugLogging)
                    Debug.Log("[WBIJukebox] - musicFile is null!");
                return;
            }

            string fileURL = musicPathFiles[songIndex];
            if (WBIMainSettings.EnableDebugLogging)
                Debug.Log("[WBIJukebox] - Play song: " + fileURL);

            musicFile.clip = GameDatabase.Instance.GetAudioClip(fileURL);
            musicFile.volume = GameSettings.MUSIC_VOLUME;
            musicFile.loop = true;
            musicFile.Play();
        }

        protected void getMusicFiles()
        {
            //Find all the .wav files in the music folder
            if (totalFiles == 0)
            {
                List<string>pathFiles = new List<string>();
                string completeMusicPath = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/" + musicPath;
                string[] musicPaths = Directory.GetFiles(completeMusicPath);
                string[] pathComponents;
                char[] delimiter = new char[] { '/' };
                for (int index = 0; index < musicPaths.Length; index++)
                {
                    completeMusicPath = musicPaths[index];
                    if (completeMusicPath.ToLower().Contains(".ogg"))
                    {
                        completeMusicPath = completeMusicPath.Replace("\\", "/");
                        pathComponents = completeMusicPath.Split(delimiter);
                        completeMusicPath = pathComponents[pathComponents.Length - 1].Replace(".ogg", "");
                        completeMusicPath = musicPath + "/" + completeMusicPath;
                        if (WBIMainSettings.EnableDebugLogging)
                            Debug.Log("[WBIJukebox] - Adding: " + completeMusicPath);
                        pathFiles.Add(completeMusicPath);
                    }
                }
                if (pathFiles.Count > 0)
                {
                    musicPathFiles = pathFiles.ToArray();
                    totalFiles = musicPathFiles.Length;
                    Debug.Log("[WBIJukebox] - Total songs found: " + totalFiles);
                }
            }
        }

        protected void setupButtons()
        {
            ButtonClickWatcher clickWatcher;
            GameObject goButton;
            Transform trans;
            if (!string.IsNullOrEmpty(powerButtonName))
            {
                trans = internalProp.FindModelTransform(powerButtonName);
                if (trans != null)
                {
                    goButton = trans.gameObject;
                    if (goButton != null)
                    {
                        clickWatcher = goButton.GetComponent<ButtonClickWatcher>();
                        if (clickWatcher == null)
                        {
                            clickWatcher = goButton.AddComponent<ButtonClickWatcher>();
                        }
                        clickWatcher.clickDelegate = OnPowerButtonClick;
                    }
                }
            }

            if (!string.IsNullOrEmpty(leftButtonName))
            {
                trans = internalProp.FindModelTransform(leftButtonName);
                if (trans != null)
                {
                    goButton = trans.gameObject;
                    if (goButton != null)
                    {
                        clickWatcher = goButton.GetComponent<ButtonClickWatcher>();
                        if (clickWatcher == null)
                        {
                            clickWatcher = goButton.AddComponent<ButtonClickWatcher>();
                        }
                        clickWatcher.clickDelegate = OnLeftButtonClick;
                    }
                }
            }

            if (!string.IsNullOrEmpty(rightButtonName))
            {
                trans = internalProp.FindModelTransform(rightButtonName);
                if (trans != null)
                {
                    goButton = trans.gameObject;
                    if (goButton != null)
                    {
                        clickWatcher = goButton.GetComponent<ButtonClickWatcher>();
                        if (clickWatcher == null)
                        {
                            clickWatcher = goButton.AddComponent<ButtonClickWatcher>();
                        }
                        clickWatcher.clickDelegate = OnRightButtonClick;
                    }
                }
            }
        }
    }
}
