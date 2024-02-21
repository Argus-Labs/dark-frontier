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

using System.Threading.Tasks;
using ArgusLabs.DF.Core.Communications;
using ArgusLabs.WorldEngineClient.Communications;
using ArgusLabs.WorldEngineClient.Communications.Rpc;
using Smonch.CyclopsFramework;
using UnityEngine;

namespace ArgusLabs.DF.Core.States
{
    public class BetaKeyEntryScreen : CyclopsGameState
    {
        public EventManager Events { get; set; }
        public CommunicationsManager CommunicationsManager { get; set; }
        public GameStorageManager GameStorageManager { get; set; }
        public Player Player { get; set; }
        public bool IsBetaKeyRequired { get; set; }

        private CyclopsStateMachine _stateMachine;

        protected override void OnEnter()
        {
            bool isKeyClaimed = GameStorageManager.PlayerHasClaimedBetaKey;

            Events.MainMenuEvents.KeyAndTagScreenClose += Stop;
            Events.MainMenuEvents.KeyAndTagScreenOpen?.Invoke();

            var betaKeyEntry = new CyclopsState
            {
                Entered = () =>
                {
                    Events.MainMenuEvents.BetaKeyEntryOpen?.Invoke();
                    Events.MainMenuEvents.BetaKeySubmitted += OnBetaKeySubmitted;
                },
                Exited = () => Events.MainMenuEvents.BetaKeySubmitted -= OnBetaKeySubmitted
            };

            var personaEntry = new CyclopsState
            {
                Entered = () =>
                {
                    Events.MainMenuEvents.PlayerTagEntryOpen?.Invoke();
                    Events.MainMenuEvents.PlayerTagSubmitted += OnPlayerTagSubmitted;
                },
                Exited = () => Events.MainMenuEvents.PlayerTagSubmitted -= OnPlayerTagSubmitted
            };

            var toPersonaEntry = new CyclopsStateTransition
            {
                Target = personaEntry,
                Condition = () => isKeyClaimed
            };

            betaKeyEntry.AddTransition(toPersonaEntry);

            _stateMachine = new CyclopsStateMachine();

            if (isKeyClaimed || !IsBetaKeyRequired)
                _stateMachine.PushState(personaEntry);
            else
                _stateMachine.PushState(betaKeyEntry);

            Engine.Immediately.Loop(_stateMachine.Update);

            //state functions -- note: Hey, sorry I added some blank lines here; was trying to debug stuff and needed to quickly scan the code.
            async void OnBetaKeySubmitted(string key)
            {
                if (string.IsNullOrEmpty(key))
                {
                    Events.MainMenuEvents.KeyAndTagMessageOpen?.Invoke("Invalid beta key");
                    
                    for (float t = 0f; (t < 2f) || Application.exitCancellationToken.IsCancellationRequested; t += Time.deltaTime)
                        await Task.Yield();
                    
                    Events.MainMenuEvents.KeyAndTagMessageClose?.Invoke();
                    
                    return;
                }
                
                Events.MainMenuEvents.KeyAndTagMessageOpen?.Invoke("Submitting");
                
                var resp = await CommunicationsManager.ClaimKeyAsync(new KeyMsg() { key = key }, Application.exitCancellationToken);
                
                if (resp.WasSuccessful)
                {
                    GameStorageManager.StoreBetaKey(key);
                    Events.MainMenuEvents.KeyAndTagMessageOpen?.Invoke("Success");
                    
                    for (float t = 0f; (t < 2f) || Application.exitCancellationToken.IsCancellationRequested; t += Time.deltaTime)
                        await Task.Yield();
                    
                    Events.MainMenuEvents.KeyAndTagMessageClose?.Invoke();
                    Events.MainMenuEvents.BetaKeyEntryClose?.Invoke();
                    
                    isKeyClaimed = true;
                }
                else
                {
                    Events.MainMenuEvents.KeyAndTagMessageOpen?.Invoke("Invalid beta key");
                    
                    for (float t = 0f; (t < 2f) || Application.exitCancellationToken.IsCancellationRequested; t += Time.deltaTime)
                        await Task.Yield();
                    
                    Events.MainMenuEvents.KeyAndTagMessageClose?.Invoke();
                }
            }

            async void OnPlayerTagSubmitted(string personaTag)
            {
                Events.MainMenuEvents.KeyAndTagMessageOpen?.Invoke("Submitting");
                Player.PersonaTag = personaTag;

                bool wasSuccess = false;

                // Try 4 times. Quit on the 4th.
                for (int i = 0; i < 4; ++i)
                {
                    if (i == 3)
                    {
                        Debug.LogError("Can't confirm persona tag.");
                        break;
                    }

                    SocketResponse<ClaimPersonaReceipt> response = await CommunicationsManager.TryClaimPersonaAsync(Player.PersonaTag, Application.exitCancellationToken);
                    bool ok = response.Result.WasSuccessful;
                    bool isPending = response.Result.Payload.Contains("pending"); // !ok && ((response.Result.Error.code == 409) && response.Result.Error.message.Contains("pending"));

                    if (isPending)
                    {
                        Debug.LogWarning("Persona tag status is pending. That's probably fine. We'll wait a bit.");
                        
                        for (float t = 0f; t < 2f; t += Time.deltaTime)
                            await Task.Yield();

                        ok = true;
                    }

                    if (ok)
                    {
                        (RpcResult rpcResult, ShowPersonaMsg msg) = await CommunicationsManager.TryShowPersonaAsync(Player.PersonaTag, Application.exitCancellationToken);

                        if (msg.Status == ResponseStatus.Rejected)
                        {
                            break;
                        }

                        if (rpcResult.WasSuccessful)
                        {
                            Player.PersonaTag = msg.PersonaTag;

                            if (GameStorageManager.TryStorePersonaTag(Player.PersonaTag))
                            {
                                if (!IsBetaKeyRequired)
                                    GameStorageManager.StoreBetaKey("n/a");
                            }
                            else
                            {
                                Debug.LogError("Can't store persona.");
                                break;
                            }

                            wasSuccess = true;
                            break;
                        }
                    }

                    // Delay before we try again.
                    for (float t = 0f; (t < 2f) && !Application.exitCancellationToken.IsCancellationRequested; t += Time.deltaTime)
                        await Task.Yield();
                }

                if (!wasSuccess)
                {
                    Events.MainMenuEvents.KeyAndTagMessageOpen?.Invoke("Player tag invalid");

                    for (float t = 0f; t < 2f; t += Time.deltaTime)
                        await Task.Yield();

                    Events.MainMenuEvents.KeyAndTagMessageClose?.Invoke();
                    
                    return;
                }

                Events.MainMenuEvents.KeyAndTagMessageOpen?.Invoke("Success");

                for (float t = 0f; t < 2f; t += Time.deltaTime)
                    await Task.Yield();

                Events.MainMenuEvents.KeyAndTagMessageClose?.Invoke();
                Events.MainMenuEvents.PlayerTagEntryClose?.Invoke();
                Events.MainMenuEvents.LinkStartOpen?.Invoke(); //UI will start the fade out animation and call the KeyAndTagInputClose which calls ExitState()
            }
        }

        protected override void OnExit()
        {
            Events.MainMenuEvents.KeyAndTagScreenClose -= Stop;
        }
    }
}
