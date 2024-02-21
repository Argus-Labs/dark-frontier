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
using Smonch.CyclopsFramework;

namespace ArgusLabs.DF.Core
{
    public sealed class EnergyTransferRoutine : CyclopsRoutine
    {
        private readonly Action _enterCallback;
        private readonly Action _updateCallback;
        private readonly Action _exitCallback;
        
        public ClientEnergyTransfer Transfer { get; }

        public EnergyTransferRoutine(ClientEnergyTransfer transfer, Action enterCallback, Action updateCallback, Action exitCallback)
            : base(period: transfer.TravelDuration, cycles: 1, Easing.Linear)
        {
            Transfer = transfer;
            _enterCallback = enterCallback;
            _updateCallback = updateCallback;
            _exitCallback = exitCallback;
        }

        protected override void OnEnter()
        {
            JumpTo(Transfer.Progress);
            _enterCallback?.Invoke();
        }

        protected override void OnUpdate(float t)
        {
            Transfer.Progress = t;
            _updateCallback?.Invoke();
        }

        protected override void OnExit()
        {
            _exitCallback?.Invoke();
        }
    }
}