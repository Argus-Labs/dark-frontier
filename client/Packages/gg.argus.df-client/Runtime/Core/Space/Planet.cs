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
using System.Collections.Generic;
using System.Text;
using ArgusLabs.DF.Core.Configs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using static Unity.Mathematics.math;

namespace ArgusLabs.DF.Core.Space
{
    public sealed class Planet
    {
        private readonly PlanetConfig _planetConfig;
        private readonly SpaceAreaConfig _spaceAreaConfig;
        private DateTime _refillStartUtc;
        private double _normalizedRefillStartingAge;
        private double _energy;

        /// <summary> This is the planet's current energy level, evaluated lazily. </summary>
        public double EnergyLevel
        {
            get
            {
                if (!IsClaimed)
                    return _energy;

                static double SCurve(double t)
                {
                    t = saturate(t);
                    t = t * t * (3.0 - (2.0 * t)); // <-- 1st SmoothStep
                    t = t * t * (3.0 - (2.0 * t)); // <-- 2nd SmoothStep
                    return t;
                }

                static double InverseSmoothStep(double t) => .5 - sin(asin(1.0 - 2.0 * t) / 3.0);
                static double InverseSCurve(double t) => InverseSmoothStep(InverseSmoothStep(t));

                double t = _normalizedRefillStartingAge;
                double td = (DateTime.UtcNow - _refillStartUtc).TotalSeconds / EnergyRefillPeriod;
                double result = SCurve(InverseSCurve(t) + td) * EnergyCapacity;

                return MakePrecise(result);
            }
        }

        public bool TrySetEnergyLevel(double energy, ulong worldTick, DateTime refillStartUtc)
        {
            if ((worldTick > 0) && (worldTick <= LastWorldTick))
            {
                if (worldTick == LastWorldTick)
                    return false;
                
                Debug.LogError($"Rejected energy change:{energy} worldTick:{worldTick} LastWorldTick:{LastWorldTick} refillStateUtc:{refillStartUtc}");
                return false;
            }

            if (IsClaimed)
                Debug.Log($"Planet.TrySetEnergyLevel (Name: {Name}) Energy: {energy} incoming worldTick: {worldTick} last planet update tick: {LastWorldTick} ({(DateTime.UtcNow - refillStartUtc).ToString()} since refill started)");

            // Start a lazy refill.
            LastWorldTick = worldTick;
            _energy = clamp(energy, 0, EnergyCapacity);
            _refillStartUtc = refillStartUtc;
            _normalizedRefillStartingAge = _energy / EnergyCapacity;

            return true;
        }
        
        public ulong LastWorldTick { get; private set; }

        public ulong SecondsSinceLastTick => (ulong)(DateTime.UtcNow - _refillStartUtc).TotalSeconds;

        /// <summary> This is the planet position formatted as a cipher text. </summary>
        public string Id { get; set; }

        private string _name;
        
        /// <summary> This is the name of the planet. </summary>
        public string Name => _name ??= HashToName(LocationHash);

        public Player Owner { get; set; } = Player.Alien;

        public bool IsClaimed => !Owner.IsAlien;

        /// <summary> This is the planet's energy capacity. </summary>
        public double EnergyCapacity => _planetConfig.EnergyMax * _spaceAreaConfig.StatBuffMultiplier;

        /// <summary> This is the time in seconds that it takes to refill a planet from empty to full. </summary>
        public double EnergyRefillPeriod => _planetConfig.EnergyRefill * _spaceAreaConfig.StatBuffMultiplier;

        /// <summary> This is the planet's level. </summary>
        public int Level => _planetConfig.Level;

        //public float Scale => remap(0f, 1f, .146f, 1.618f, Level / 10f /*planetSizeCount*/);
        public float Scale => pow(1.618f, 1 + Level) * .618f;
        
        public float Radius => Scale * .5f;

        /// <summary> range stat for energy decay rate when sending energy from this planet </summary>
        public int FullRange => (int)MakePrecise(_planetConfig.Range * _spaceAreaConfig.StatBuffMultiplier);

        public double GetCurrentRange(double energyScale)
        {
            // HEADS UP! We have "unchanging" game rules baked in here
            double energyLevel = max(0, EnergyLevel * energyScale); //note that energyScale is based on EnergyLevel not EnergyCapacity
            double travelCostPerUnit = EnergyCapacity * .95 / FullRange;
            double result = (energyLevel - EnergyCapacity * .05) / travelCostPerUnit;
            result = floor(MakePrecise(result));

            return result;
        }

        /// <summary> the number of ship available to send energy from this planet to other, default at 6 set at constructor </summary>
        public short ExportShipRemaining { get; set; }

        public short MaxExportShipCount { get; private set; }

        /// <summary> speed stat for sending energy </summary>
        public double Speed => _planetConfig.Speed * _spaceAreaConfig.StatBuffMultiplier;

        /// <summary> defense value modifier for taking over the planet </summary>
        public double Defense => _planetConfig.Defense * _spaceAreaConfig.DefenseDebuffMultiplier;
        
        public Vector2Int Position { get; set; }
        
        public float Depth { get; set; }

        public Vector2 WorldPosition => new Vector2(Position.x, Position.y);

        public string LocationHash { get; set; }
        
        public int PerlinValue { get; set; }

        public int LastVisibleFrameIndex { get; set; } = int.MinValue;

        public bool IsVisible => LastVisibleFrameIndex >= Time.frameCount - 1;

        public int Lod { get; set; }
        
        // This is getting used enough to deserve its own getter.
        public int RoundedEnergyLevel => Mathf.RoundToInt((float)EnergyLevel);

        private List<ClientEnergyTransfer> _inboundTransfers;
        private List<ClientEnergyTransfer> _outboundTransfers;
        private int _lastEnergyValue = int.MinValue;
        private string _energyText;
        
        // This should prevent some of the allocations.
        public string CachedEnergyText
        {
            get
            {
                int energy = RoundedEnergyLevel;

                if (energy == _lastEnergyValue)
                    return _energyText;

                _lastEnergyValue = energy;
                _energyText = energy.ToString();

                return _energyText;
            }
        }
        
        public double Progress => EnergyLevel / EnergyCapacity;

        private string _transferTag;
        
        public string PendingEnergyTransferTag
            => _transferTag ??= $"{nameof(Planet)}.PendingTransfers@({Position.x},{Position.y})";
        
        // This isn't the best place for this. It should be in its own system.
        // Please don't let this justify keeping transfers within the Planet class.
        public bool HasIncomingTransfers => _inboundTransfers.Count > 0;
        public bool HasOutboundTransfers => _outboundTransfers.Count > 0;
        
        public Planet(PlanetConfig planetConfig, SpaceAreaConfig spaceAreaConfig)
        {
            _planetConfig = planetConfig;
            _spaceAreaConfig = spaceAreaConfig;
            MaxExportShipCount = 6;
            ExportShipRemaining = MaxExportShipCount;
            // Planets weren't supposed to know about transfers in this way.
            // Could be refactored later, but it's working for now.
            // Also would be better if these collections were allocated as needed.
            _inboundTransfers = new List<ClientEnergyTransfer>();
            _outboundTransfers = new List<ClientEnergyTransfer>();
        }
        
        public override string ToString()
            => $"Id:{Id} OwnerId:{Owner.PersonaTag} GridPosition:{Position} EnergyLevel:{EnergyLevel} MimcHash:{LocationHash} Perlin:{PerlinValue} Name:{Name} Level:{Level}";

        // Produces: XXX-###
        private string HashToName(string hash)
        {
            Assert.IsNotNull(hash);
            Assert.IsTrue(hash.Length >= 60); // ~64
            StringBuilder sb = GenericPool<StringBuilder>.Get();
            sb.Clear();
            
            for (int i = 0; i < 3; ++i)
            {
                int c = 65 + (abs(hash[(5 + i)..(10 + i * 5)].GetHashCode()) % 26);
                sb.Append((char)c);
            }

            sb.Append('-');

            for (int i = 0; i < 3; ++i)
            {
                int n = (abs(hash[(10 + i)..(15 + i * 5)].GetHashCode()) % 10);
                sb.Append(n);
            }
            
            var planetName = sb.ToString();
            sb.Clear();
            GenericPool<StringBuilder>.Release(sb);
            
            return planetName;
        }
        
        //make the OutboundTransfers and InboundTransfers always sorted from the biggest energy transfer
        public void AddOutboundTransfer(ClientEnergyTransfer et)
        {
            --ExportShipRemaining;
            
            // TODO: This looks weird.
            int i = -1;
            while (i < _outboundTransfers.Count - 1 && et.EnergyOnEmbark <  _outboundTransfers[i + 1].EnergyOnEmbark)
            {
                i++;
            }
            _outboundTransfers.Insert(i + 1, et);
        }

        public void AddInboundTransfer(ClientEnergyTransfer et)
        {
            int i = -1;
            while (i < _inboundTransfers.Count - 1 && et.EnergyOnEmbark < _inboundTransfers[i + 1].EnergyOnEmbark)
            {
                i++;
            }
            _inboundTransfers.Insert(i + 1, et);
        }

        public void RemoveOutboundTransfer(ulong id)
        {
            ++ExportShipRemaining;
            
            for (int i = 0; i < _outboundTransfers.Count; i++)
            {
                if (_outboundTransfers[i].Id == id)
                {
                    _outboundTransfers.RemoveAt(i);
                    return;
                }
            }
        }

        public void RemoveInboundTransfer(ulong id)
        {
            for (int i = 0; i < _inboundTransfers.Count; i++)
            {
                if (_inboundTransfers[i].Id == id)
                {
                    _inboundTransfers.RemoveAt(i);
                    return;
                }
            }
        }

        //since the OutboundTransfers is always sorted from biggest energy, then we can just grab the first one
        public int GetBiggestExport()
        {
            return _outboundTransfers.Count > 0 ? _outboundTransfers[0].EnergyOnEmbark : -1;
        }

        public int GetBiggestFriendlyInbound()
        {
            for (int i = 0; i < _inboundTransfers.Count; i++)
            {
                if (_inboundTransfers[i].ShipOwner == Owner)
                {
                    return _inboundTransfers[i].EnergyOnEmbark;
                }
            }
            return -1;
        }

        public int GetBiggestHostileInbound()
        {
            for (int i = 0; i < _inboundTransfers.Count; i++)
            {
                if (_inboundTransfers[i].ShipOwner != Owner)
                {
                    return _inboundTransfers[i].EnergyOnEmbark;
                }
            }
            return -1;
        }
        
        private static double MakePrecise(double x)
        {
            double fracX = frac(x);
            return ((fracX < .001) || ((1.0 - fracX) < .001))
                ? round(x)
                : x;
        }
    }
}