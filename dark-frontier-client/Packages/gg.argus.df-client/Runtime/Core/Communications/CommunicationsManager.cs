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
using System.Threading;
using System.Threading.Tasks;
using ArgusLabs.DF.Core.Configs;
using ArgusLabs.WorldEngineClient.Communications;
using ArgusLabs.WorldEngineClient.Communications.Rpc;
using Nakama;
using Nakama.TinyJson;
using UnityEngine;

namespace ArgusLabs.DF.Core.Communications
{
    public class CommunicationsManager : ClientCommunicationsManager
    {
        public CommunicationsManager(Client client)
            : base(client) { }
        
        public async ValueTask<RpcResult> ReadPlanetsAsync(CurrentStateMsg msg, CancellationToken cancellation = default)
            => await RpcAsync("query/game/planets", msg.ToJson(), cancellation);

        public async ValueTask<SocketResponse<ClaimHomePlanetReceipt>> ClaimHomePlanetAsync(ClaimHomePlanetMsg msg, CancellationToken cancellation = default, float timeout = TimeoutDuration)
            => await SocketRequestAsync<ClaimHomePlanetReceipt>("tx/game/claim-home-planet", msg.ToJson(), cancellation, timeout);

        public async ValueTask<SocketResponse<SendEnergyReceipt>> SendEnergyAsync(SendEnergyMsg msg, CancellationToken cancellation = default, float timeout = TimeoutDuration)
            => await SocketRequestAsync<SendEnergyReceipt>("tx/game/send-energy", msg.ToJson(), cancellation, timeout);

        // While this is a tx, we never receive a receipt, and that's fine for this particular RPC.
        public async ValueTask<RpcResult> DebugEnergyBoostAsync(LocationHashMsg msg, CancellationToken cancellation = default)
            => await RpcAsync("tx/game/debug-energy-boost", msg.ToJson(), cancellation);

        public async Task<(bool, GameConfig)> TryLoadConfigAsync(CancellationToken cancellation = default)
        {
            if (cancellation == default)
                cancellation = DefaultCancellationToken;

            Debug.Log(nameof(TryLoadConfigAsync));

            var payload = new Dictionary<string, object>();
            const string rpcId = "query/game/constant";
            const string constantLabel = "constantLabel";

            // World Config

            Debug.Log("Loading WorldConfig.");

            payload[constantLabel] = WorldConfig.Key;
            RpcResult result = await RpcAsync(rpcId, payload.ToJson(), cancellation);

            Debug.Log("result.Payload >>> " + result.Payload);

            if (!result.WasSuccessful)
            {
                Debug.LogError("Failed to load WorldConfig.");
                return (false, null);
            }

            var worldConfig = WorldConfig.FromJson(result.Payload);
            Debug.Log(worldConfig);

            // Space Areas Config

            Debug.Log("Loading Space Areas Config.");

            payload[constantLabel] = SpaceAreasConfig.Key;
            result = await RpcAsync(rpcId, payload.ToJson(), cancellation);

            Debug.Log(result.Payload);

            if (!(result.WasSuccessful && SpaceAreasConfig.FromJson(result.Payload, out SpaceAreasConfig spaceAreasConfig)))
            {
                Debug.LogError($"Failed to load remote config: {nameof(SpaceAreasConfig)}");
                return (false, null);
            }

            Debug.Log(spaceAreasConfig);

            // Planets Config

            Debug.Log("Loading Planets Config.");

            payload[constantLabel] = PlanetsConfig.Key;
            result = await RpcAsync(rpcId, payload.ToJson(), cancellation);

            Debug.Log(result.Payload);

            if (!result.WasSuccessful)
            {
                Debug.LogError($"Failed to load remote config: {nameof(PlanetsConfig)}");
                return (false, null);
            }

            var planetsConfig = PlanetsConfig.FromJson(result.Payload);

            Debug.Log(planetsConfig);

            var gameConfig = new GameConfig(worldConfig, planetsConfig, spaceAreasConfig);

            return (true, gameConfig);
        }
    }
}