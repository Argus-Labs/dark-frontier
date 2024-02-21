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
using Smonch.CyclopsFramework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArgusLabs.DF.Core.States
{
    // This is a quick and hacky terminal.
    public class Terminal : CyclopsGameState
    {
        private const string Prompt = "> ";
        private readonly Queue<string> _terminalQueue = new ();
        private string _inputText = "";
        private bool _backspaceWasPressed;
        
        private readonly OverlayController _overlayController;
        
        public event Action<Terminal, string> OnCommandEntered;

        public Vector2 Position { get; set; } = new Vector2(292, 224);
        public int FontSize { get; set; } = 20;
        public int MaxLines { get; set; } = 25;
        public bool IsVisible { get; set; } = true;
        
        public Terminal(OverlayController overlayController)
        {
            _overlayController = overlayController;
        }
        
        protected override void OnEnter()
        {
            Keyboard.current.onTextInput += OnTextInput;

            Engine.NextFrame.Loop(1f / 10f, float.MaxValue, () =>
            {
                if (!_backspaceWasPressed)
                    return;
                
                _backspaceWasPressed = false;
                
                if (_inputText.Length > 0)
                    _inputText = _inputText[..^1];
            });
        }
        
        protected override void OnExit()
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
        
        private void OnTextInput(char c)
        {
            if (!IsVisible)
                return;
            
            if (c == 8)
            {
                _backspaceWasPressed = true;
                return;
            }

            if (char.IsControl(c))
                return;
            
            _inputText += c;
        }

        // Note: We don't care about layering, so won't check.
        protected override void OnUpdate(CyclopsStateUpdateContext updateContext)
        {
            if (updateContext.UpdateSystem != CyclopsGame.UpdateSystem.Update)
                return;

            var kb = Keyboard.current;

            if (kb.escapeKey.wasPressedThisFrame)
                IsVisible = !IsVisible;
            
            if (!IsVisible)
                return;
            
            if (kb.deleteKey.wasPressedThisFrame || kb.deleteKey.isPressed)
                _backspaceWasPressed = true;
            
            if (kb.ctrlKey.isPressed && kb.vKey.wasPressedThisFrame)
                _inputText += GUIUtility.systemCopyBuffer;
            
            if (kb.enterKey.wasPressedThisFrame)
            {
                WriteLine($"{Prompt}{_inputText}");
                OnCommandEntered?.Invoke(this, _inputText);
                _inputText = "";
            }
            
            int queueLength = _terminalQueue.Count;
            
            for (int i = 0; i < queueLength; ++i)
            {
                var line = _terminalQueue.Dequeue();
                
                if (queueLength - i < MaxLines)
                    _terminalQueue.Enqueue(line);
                
                _overlayController.DrawImGuiText(Position + Vector2.up * (Mathf.Min(MaxLines, queueLength) - i) * (FontSize + 8), FontSize, new Color(.85f, .85f, .85f, 1f), line, false);
            }
            
            string promptLine = $"{Prompt}{_inputText}█";
            
            _overlayController.DrawImGuiText(Position, FontSize, new Color(.85f, .85f, .85f, 1f), promptLine, false);
        }

        public void WriteLine(string text = "")
        {
            if (text.Contains('\n'))
            {
                foreach (var line in text.Split('\n'))
                    WriteLine(line);
                
                return;
            }
            
            _terminalQueue.Enqueue(text);
        }
    }
}