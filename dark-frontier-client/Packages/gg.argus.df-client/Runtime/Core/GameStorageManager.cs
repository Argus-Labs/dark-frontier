// Copyright 2024 Argus Labs
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Threading.Tasks;
using ArgusLabs.DF.Core.Space;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace ArgusLabs.DF.Core
{
    // TODO: Route PlayerPrefs through here as well.
    // PlayerPrefs are compatible with most platforms, but not all.
    // Outside of weekend projects, PlayerPrefs is frequently outgrown.
    public class GameStorageManager : IDisposable
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void FlushIndexedDB();
#endif
        private const string HistoryFileName = "darkfrontier-sectors-2023-11-06-1-{0}.cache";
        private const string PersistentPlayerUidKey = "signInId";
        private const string PersistentPersonaKey = "personaTag";
        private const string PersistentBetaCodeKey = "betaKey";
        private const string SkipHomePlanetSearch = "skipHomePlanetSearch";

        private BinaryReader _reader;
        private BinaryWriter _writer;

        private string HistoryPathA => Path.Combine(Application.persistentDataPath, HistoryFileName.Replace("{0}", "a"));
        private string HistoryPathB => Path.Combine(Application.persistentDataPath, HistoryFileName.Replace("{0}", "b"));
        public bool HasHistory => _reader is not null;
        public bool PlayerHasClaimedBetaKey => PlayerPrefs.HasKey(PersistentBetaCodeKey);
        public bool PlayerHasClaimedPersonaTag => PlayerPrefs.HasKey(PersistentPersonaKey);
        public bool PlayerHasValidPersonaTag => !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(PersistentPersonaKey));
        public bool IsFirstTimePlayer => !(PlayerHasClaimedBetaKey && PlayerHasValidPersonaTag);
        
        public bool WillSkipHomePlanetSearch
        {
            get => PlayerPrefs.GetInt(SkipHomePlanetSearch, 0) == 1;
            set => PlayerPrefs.SetInt(SkipHomePlanetSearch, value ? 1 : 0);
        }

        public GameStorageManager(bool willClearAllData)
        {
            if (willClearAllData)
                Clear();

            // There are situations where this would be a bad idea, but this is cheap.
            Initialize();
        }

        public void LogStoredPersonaTag() => Debug.Log($"PersonaTag: {PlayerPrefs.GetString(PersistentPersonaKey)}");
        public string PlayerInfo =>
              $"uid: {PlayerPrefs.GetString(PersistentPlayerUidKey)}\n"
            + $"personatag: {PlayerPrefs.GetString(PersistentPersonaKey)}\n"
            + $"betakey: {PlayerPrefs.GetString(PersistentBetaCodeKey)}";
        
        public void EmergencyStorePlayerUid(string uid)
        {
            PlayerPrefs.SetString(PersistentPlayerUidKey, uid);
        }

        public bool TryLoadOrStorePlayerUid(Func<string> uidCreationCallback, out string uid)
        {
            uid = string.Empty;

            if (PlayerPrefs.HasKey(PersistentPlayerUidKey))
            {
                uid = PlayerPrefs.GetString(PersistentPlayerUidKey);
                return true;
            }
            
            uid = uidCreationCallback();
            
            if (string.IsNullOrWhiteSpace(uid))
            {
                Debug.LogError("UID is null or whitespace.");
                return false;
            }
            
            PlayerPrefs.SetString(PersistentPlayerUidKey, uid);

            return true;
        }
        
        public bool TryStorePersonaTag(string personaTag)
        {
            if (string.IsNullOrWhiteSpace(personaTag))
            {
                Debug.LogError("Persona tag is null or whitespace.");
                return false;
            }
            
            PlayerPrefs.SetString(PersistentPersonaKey, personaTag);
            //persona in playerprefs here works as a flag to mark this device has claimed persona, the persona that will be used will still be the one that is fetched from show-persona

            return true;
        }

        public bool StoreBetaKey(string betaKey)
        {
            if (string.IsNullOrWhiteSpace(betaKey))
            {
                Debug.LogError("Beta key is null or whitespace.");
                return false;
            }
            
            PlayerPrefs.SetString(PersistentBetaCodeKey, betaKey);
            //similar to persona, this works as a flag to mark this device has claimed beta key so the game don't ask the player again in different session

            return true;
        }

        public async ValueTask ProcessExistingHistory(Action<BinaryReader> readAction)
        {
            for (int i = 0; _reader.BaseStream.Position < _reader.BaseStream.Length; ++i)
            {
                readAction(_reader);

                if (Application.exitCancellationToken.IsCancellationRequested)
                    break;
                
                if (i % 500 == 0)
                    await Task.Yield();
            }
        }
        
        private void WriteToHistory(Action<BinaryWriter> writeAction)
        {
            writeAction(_writer);
            _writer.Flush();
            ForceExternalFlush();
        }
        
        public (bool wasSuccess, Exception ex) TryWriteToHistory(Sector sector)
        {
            // We're catching this internally because it's going to frequently throw an Exception when the game is quitting.
            // A simple check to see if the app is quitting isn't enough to bypass the false alarm.
            // There is a way to achieve this, but I don't think it's worth the effort.
            
            try
            {
                WriteToHistory(writer =>
                {
                    writer.Write(sector.Position.x);
                    writer.Write(sector.Position.y);
                    writer.Write((int)sector.Environment);
                    writer.Write(sector.HasPlanet);

                    if (!sector.HasPlanet)
                        return;

                    writer.Write(sector.Planet.LocationHash);
                    writer.Write(sector.Planet.PerlinValue);
                });
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
            
            return (true, null);
        }

        private static void ForceExternalFlush()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FlushIndexedDB();
#endif
        }
        
        private void Clear()
        {
            PlayerPrefs.DeleteKey(PersistentPlayerUidKey);
            PlayerPrefs.DeleteKey(PersistentPersonaKey);
            PlayerPrefs.DeleteKey(PersistentBetaCodeKey);
            PlayerPrefs.DeleteKey(SkipHomePlanetSearch);

            if (File.Exists(HistoryPathA))
                File.Delete(HistoryPathA);
            
            if (File.Exists(HistoryPathB))
                File.Delete(HistoryPathB);
            
            Debug.Log("All player data and history cleared.");
        }
        
        private void Initialize()
        {
            string cachePathA = HistoryPathA;
            string cachePathB = HistoryPathB;
            
            try
            {
                if (File.Exists(cachePathA) && File.Exists(cachePathB))
                {
                    var infoA = new FileInfo(cachePathA);
                    var infoB = new FileInfo(cachePathB);
                    
                    Debug.Log($"{infoA.Name}: {infoA.Length}");
                    Debug.Log($"{infoB.Name}: {infoB.Length}");
                    
                    if (infoA.Length > infoB.Length)
                    {
                        Debug.Log($"{infoA.Name}.Length > {infoB.Name}.Length");
                        Debug.Log($"Swapping: a <-> b");
                        (cachePathA, cachePathB) =
                            (cachePathB, cachePathA);
                    }
                }
                else if (File.Exists(cachePathA))
                {
                    Debug.Log($"Only {cachePathB} exists.");
                    Debug.Log($"Swapping: a <-> b");
                    (cachePathA, cachePathB) =
                        (cachePathB, cachePathA);
                }
            
                // Read from the longest file.
                string planetPositionsInputFullPath = cachePathB;
                
                // Write to the shortest file.
                string planetPositionsOutputFullPath = cachePathA;
                
                Debug.Log($"Writing to: {planetPositionsOutputFullPath}");
                
                // Open in create mode which will overwrite as needed.
                _writer = new BinaryWriter(File.Open(planetPositionsOutputFullPath, FileMode.Create));
                
                if (!File.Exists(planetPositionsInputFullPath))
                {
                    Debug.Log($"{planetPositionsInputFullPath} doesn't exist yet.");
                    return;
                }
                
                Debug.Log($"Reading from: {planetPositionsInputFullPath}");
                _reader = new BinaryReader(File.OpenRead(planetPositionsInputFullPath));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Dispose()
        {
            _reader?.Close();
            _writer?.Close();
            ForceExternalFlush();
        }
    }
}