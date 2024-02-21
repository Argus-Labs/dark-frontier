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

using System.Collections.Generic;
using ArgusLabs.DF.Core;
using ArgusLabs.DF.Core.Space;
using UnityEngine;

namespace ArgusLabs.DF.UI.GameplayHUD
{
    public class LiveTextNotifUIManager : MonoBehaviour
    {
        [SerializeField] private LiveTextNotifEntry[] _entries;
        [SerializeField] private Sprite[] _eventIcons; //0:departure, 1:arrival, 2:conquer

        private List<GameNotification> _notificationList;
        private bool _isFirstUpdate;
        private EventManager _eventManager;

        public void Init(EventManager eventManager)
        {
            _eventManager = eventManager;
            _notificationList = new List<GameNotification>();
            for (int i = 0; i < _entries.Length; i++)
            {
                _entries[i].Init((planetPos) => _eventManager.GameplayEvents.PlanetIndexSelectPlanet?.Invoke(planetPos), _eventIcons);
            }
            _isFirstUpdate = true;
        }

        private void OnEnable()
        {
            if(_eventManager != null)
            {
                _eventManager.GameplayEvents.PlanetControlChanged += OnPlanetControlChange;
                _eventManager.GameplayEvents.EnergyTransferArrived += OnEnergyTransferArrived;
                _eventManager.GameplayEvents.EnergyTransferStarted += OnEnergyTransferStarted;
                _eventManager.GameplayEvents.FirstUpdateDone += OnFirstUpdateDone;
            }
        }

        private void OnFirstUpdateDone()
        {
            _isFirstUpdate = false;
        }

        private void OnDisable()
        {
            if (_eventManager != null)
            {
                _eventManager.GameplayEvents.PlanetControlChanged -= OnPlanetControlChange;
                _eventManager.GameplayEvents.EnergyTransferArrived -= OnEnergyTransferArrived;
                _eventManager.GameplayEvents.EnergyTransferStarted -= OnEnergyTransferStarted;
                _eventManager.GameplayEvents.FirstUpdateDone -= OnFirstUpdateDone;
            }
        }

        private void OnEnergyTransferStarted(ClientEnergyTransfer transfer, Vector2 shipPosition, float rotation)
        {
            if (!transfer.Destination.Owner.IsLocal && !transfer.ShipOwner.IsLocal) return; //if neither sender nor receiver is the local player, then no need to make notification
            GameNotification newNotif = new GameNotification()
            {
                type = GameNotificationType.Departure,
                eventOwner = transfer.ShipOwner,
                sourcePlanet = transfer.Source, //note that this can be null
                destinationPlanet = transfer.Destination
            };
            AddToQueue(newNotif);
        }

        private void OnEnergyTransferArrived(ClientEnergyTransfer transfer)
        {
            if (!transfer.Destination.Owner.IsLocal && !transfer.ShipOwner.IsLocal) return; //if neither sender nor receiver is the local player, then no need to make notification
            GameNotification newNotif = new GameNotification()
            {
                type = GameNotificationType.Arrival,
                eventOwner = transfer.ShipOwner,
                sourcePlanet = transfer.Source, //note that this can be null
                destinationPlanet = transfer.Destination
            };
            AddToQueue(newNotif);
        }

        private void OnPlanetControlChange(Planet takenPlanet, Player newOwner, Player prevOwner)
        {
            if (_isFirstUpdate) return;
            if (!prevOwner.IsLocal && !newOwner.IsLocal) return;
            GameNotification newNotif = new GameNotification()
            {
                type = GameNotificationType.Conquer,
                eventOwner = newOwner,
                conqueredPlayer = prevOwner,
                sourcePlanet = takenPlanet,
            };
            Debug.Log("taken planet: " + newNotif.destinationPlanet);
            AddToQueue(newNotif);
        }

        private void AddToQueue(GameNotification newNotification)
        {
            _notificationList.Insert(0, newNotification);
            if (_notificationList.Count > 6) _notificationList.RemoveAt(6);
            for (int i = 0; i < 6; i++)
            {
                if (i >= _notificationList.Count) break;
                _entries[i].SetNotification(_notificationList[i]);
            }
        }
    }

    public struct GameNotification
    {
        public GameNotificationType type;
        public Player eventOwner; //conqueror or ship owner in departure/arrival notificaiton
        public Player conqueredPlayer; //the previous owner of conquered planet in conquer notification
        public Planet sourcePlanet; //in departure/arrival this can be null if it's from undiscovered region, in conquer this is the conquered planet
        public Planet destinationPlanet; //in conquer notification, destination planet will be null
    }

    public enum GameNotificationType
    {
        Departure,
        Arrival,
        Conquer
    }
}
