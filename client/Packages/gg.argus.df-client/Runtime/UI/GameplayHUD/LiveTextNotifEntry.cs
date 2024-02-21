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
using System.Globalization;
using System.Text;
using ArgusLabs.DF.Core;
using ArgusLabs.DF.Core.Space;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.GameplayHUD
{
    public class LiveTextNotifEntry : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _text;

        //these are put here and not PlayerColor because this color scheme only used here
        private Color _enemyNotifColor = new Color(255f / 255f, 149f / 255f, 149f / 255f); 
        private Color _playerNotifColor = new Color(162f / 255f, 216f / 255f, 255f / 255f);

        private Sprite[] _eventIcons; //0: depart, 1: arrival, 2: conquered
        private Action<Vector2Int> _linkTextClicked;
        private StringBuilder _builder;

        public void Init(Action<Vector2Int> linkTextClicked, Sprite[] eventIcons)
        {
            _linkTextClicked = linkTextClicked;
            _builder = new StringBuilder();
            _text.text = "";
            _background.gameObject.SetActive(false);
            _icon.gameObject.SetActive(false);
            _eventIcons = eventIcons;
        }

        public void SetNotification(GameNotification notification)
        {
            _background.gameObject.SetActive(true);
            _icon.gameObject.SetActive(true);
            switch (notification.type)
            {
                case GameNotificationType.Departure: NotifyDeparture(notification);break;
                case GameNotificationType.Arrival: NotifyArrival(notification);break;
                case GameNotificationType.Conquer: NotifyConquer(notification);break;
            }
        }

        public void NotifyDeparture(GameNotification notification)
        {
            _icon.sprite = _eventIcons[0];
            _icon.color = notification.eventOwner.IsLocal ? _playerNotifColor : _enemyNotifColor;
            _background.color = notification.eventOwner.IsLocal ? _playerNotifColor : _enemyNotifColor;
            string sourcePlanetTextColor = GetColor(notification.sourcePlanet);
            string destPlanetTextColor = GetColor(notification.destinationPlanet);

            _builder.Clear();
            _builder.Append(notification.eventOwner.IsLocal ? "Your ship from " : "Enemy ship from ");

            _builder.Append("<link=source>");
            _builder.Append("<color=#");
            _builder.Append(sourcePlanetTextColor);
            _builder.Append("><u>");
            _builder.Append(notification.sourcePlanet != null ? notification.sourcePlanet.Position.ToString() : "(???, ???)");
            _builder.Append("</u></color></link> has departed towards planet ");

            _builder.Append("<link=destination>");
            _builder.Append("<color=#");
            _builder.Append(destPlanetTextColor);
            _builder.Append("><u>");
            _builder.Append(notification.destinationPlanet.Position.ToString());
            _builder.Append("</u></color></link>");

            _text.text = _builder.ToString();
        }

        public void NotifyArrival(GameNotification notification)
        {
            _icon.sprite = _eventIcons[1];
            _icon.color = notification.eventOwner.IsLocal ? _playerNotifColor : _enemyNotifColor;
            _background.color = notification.eventOwner.IsLocal ? _playerNotifColor : _enemyNotifColor;

            string sourcePlanetTextColor = GetColor(notification.sourcePlanet);
            string destPlanetTextColor = GetColor(notification.destinationPlanet);
            _builder.Clear();
            _builder.Append(notification.eventOwner.IsLocal ? "Your ship from " : "Enemy ship from ");

            _builder.Append("<link=source>");
            _builder.Append("<color=#");
            _builder.Append(sourcePlanetTextColor);
            _builder.Append("><u>");
            _builder.Append(notification.sourcePlanet != null ? notification.sourcePlanet.Position.ToString() : "(???, ???)");
            _builder.Append("</u></color></link> has landed on planet ");

            _builder.Append("<link=destination>");
            _builder.Append("<color=#");
            _builder.Append(destPlanetTextColor);
            _builder.Append("><u>");
            _builder.Append(notification.destinationPlanet.Position.ToString());
            _builder.Append("</u></color></link>");

            _text.text = _builder.ToString();
        }

        public void NotifyConquer(GameNotification notification)
        {
            _icon.sprite = _eventIcons[2];
            _icon.color = notification.eventOwner.IsLocal ? _playerNotifColor : _enemyNotifColor;
            _background.color = notification.eventOwner.IsLocal ? _playerNotifColor : _enemyNotifColor;

            string planetTextColor = GetColor(notification.conqueredPlayer);
            _builder.Clear();
            _builder.Append(notification.eventOwner.IsLocal ? "You have conquered " : "Enemy has conquered ");
            _builder.Append(notification.conqueredPlayer.IsAlien ? "neutral planet " : "planet ");
            _builder.Append("<link=planet>");
            _builder.Append("<color=#");
            _builder.Append(planetTextColor);
            _builder.Append("><u>");
            _builder.Append(notification.sourcePlanet.Position.ToString());
            _builder.Append("</u></color></link>");

            _text.text = _builder.ToString();
        }

        private string GetColor(Planet planet)
        {
            if (planet == null) return "FF9595";
            else return GetColor(planet.Owner);
        }

        private string GetColor(Player player)
        {
            if(player.IsAlien)
            {
                return "BBBBBB";
            } 
            else if(player.IsEnemy)
            {
                return "FF9595";
            }
            else
            {
                return "A2D8FF";
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector3 mousePosition = new Vector3(eventData.position.x, eventData.position.y, 0);

            var linkTaggedText = TMP_TextUtilities.FindIntersectingLink(_text, mousePosition, null);

            if (linkTaggedText != -1)
            {
                TMP_LinkInfo linkInfo = _text.textInfo.linkInfo[linkTaggedText];
                string[] planetCoord = linkInfo.GetLinkText().Split(','); //it will be split into [0]: "(xxx", [1]: " yyy)" note the space
                
                if (int.TryParse(planetCoord[0].Substring(1, planetCoord[0].Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out int x))
                {
                    int y = int.Parse(planetCoord[1].Substring(1, planetCoord[1].Length - 2), CultureInfo.InvariantCulture);

                    Vector2Int clickedPlanetPos = new Vector2Int(x, y);

                    _linkTextClicked.Invoke(clickedPlanetPos);
                }
            }
        }
    }
}
