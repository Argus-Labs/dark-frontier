//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.7.0
//     from Packages/gg.argus.df-client/Assets/Input/DF Input Actions.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ArgusLabs.DF.Input
{
    public partial class @DfInputActions: IInputActionCollection2, IDisposable
    {
        public InputActionAsset asset { get; }
        public @DfInputActions()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""DF Input Actions"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""1fa3e1ff-fab9-4aaa-b9a5-b7dfe41016b7"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""28d9fded-2a89-4432-a0a6-821a2493a617"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Select"",
                    ""type"": ""Button"",
                    ""id"": ""afbe89fd-6e37-462b-b816-b31d84e5285b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Toggle Debug Info"",
                    ""type"": ""Button"",
                    ""id"": ""310f62ca-5abe-4835-950a-ed01ad413146"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Toggle Spreadsheet"",
                    ""type"": ""Button"",
                    ""id"": ""27feb12e-6fce-4974-8297-84217dae5222"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Back"",
                    ""type"": ""Button"",
                    ""id"": ""90302361-d305-4648-80bf-f6d2074b2a1c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Energy Boost"",
                    ""type"": ""Button"",
                    ""id"": ""1c69b738-5074-4563-b7a1-46101abf5b9b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Zoom"",
                    ""type"": ""Value"",
                    ""id"": ""2ebaf3ad-7653-42f0-8310-888f61f8a3d3"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""9ab496fb-8752-4650-941b-b417e09e7598"",
                    ""path"": ""<Gamepad>/dpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2d46860b-b679-4772-9116-a1436d9ed051"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""52946f41-786f-4315-ab3c-d37901d2b338"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""15c1462c-c388-49a5-9474-5de7f5d60b21"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""e047d84b-b05d-46fa-b93f-87fd6c45871f"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""e99530ff-c6ff-40bd-9b3c-478db1564df3"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""56984c6e-3106-444e-8e5a-f71da85a9066"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""8261e6c0-3fd8-41a2-a66a-d1f2f39fcaa3"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""f880b2b1-5fe5-4146-8ce5-ead56e3a95fe"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""543adcc8-0084-42e1-aad4-948b75cc9ba1"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""ec7dea1a-5d42-445b-8021-c9748cdb3d88"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""c56dc3c6-f52e-45b6-b4f3-0bb9c51bb5b9"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""3a1b2ae9-4339-409c-a483-e7a62d6d0f6e"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gameplay"",
                    ""action"": ""Select"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""fd42e373-ade7-490e-bd39-84514836092f"",
                    ""path"": ""*/{PrimaryAction}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Select"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e9cf894e-b11e-4b63-8a9b-1833429a211c"",
                    ""path"": ""<Keyboard>/#(`)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gameplay"",
                    ""action"": ""Toggle Debug Info"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c1e81186-6a6f-4931-9f55-aebac3f04215"",
                    ""path"": ""*/{Back}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gameplay"",
                    ""action"": ""Back"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b17205d1-6e63-4855-a100-b2d5c7bd3dca"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": ""Gameplay"",
                    ""action"": ""Energy Boost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""36dbe6c3-083e-41ab-babc-f53dec4ef620"",
                    ""path"": ""<Keyboard>/tab"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Toggle Spreadsheet"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f03a3d8b-e6d0-4860-a948-b71009902541"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gameplay"",
                    ""action"": ""Zoom"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Gameplay"",
            ""bindingGroup"": ""Gameplay"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Gamepad>"",
                    ""isOptional"": true,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
            // Gameplay
            m_Gameplay = asset.FindActionMap("Gameplay", throwIfNotFound: true);
            m_Gameplay_Move = m_Gameplay.FindAction("Move", throwIfNotFound: true);
            m_Gameplay_Select = m_Gameplay.FindAction("Select", throwIfNotFound: true);
            m_Gameplay_ToggleDebugInfo = m_Gameplay.FindAction("Toggle Debug Info", throwIfNotFound: true);
            m_Gameplay_ToggleSpreadsheet = m_Gameplay.FindAction("Toggle Spreadsheet", throwIfNotFound: true);
            m_Gameplay_Back = m_Gameplay.FindAction("Back", throwIfNotFound: true);
            m_Gameplay_EnergyBoost = m_Gameplay.FindAction("Energy Boost", throwIfNotFound: true);
            m_Gameplay_Zoom = m_Gameplay.FindAction("Zoom", throwIfNotFound: true);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        public IEnumerable<InputBinding> bindings => asset.bindings;

        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            return asset.FindAction(actionNameOrId, throwIfNotFound);
        }

        public int FindBinding(InputBinding bindingMask, out InputAction action)
        {
            return asset.FindBinding(bindingMask, out action);
        }

        // Gameplay
        private readonly InputActionMap m_Gameplay;
        private List<IGameplayActions> m_GameplayActionsCallbackInterfaces = new List<IGameplayActions>();
        private readonly InputAction m_Gameplay_Move;
        private readonly InputAction m_Gameplay_Select;
        private readonly InputAction m_Gameplay_ToggleDebugInfo;
        private readonly InputAction m_Gameplay_ToggleSpreadsheet;
        private readonly InputAction m_Gameplay_Back;
        private readonly InputAction m_Gameplay_EnergyBoost;
        private readonly InputAction m_Gameplay_Zoom;
        public struct GameplayActions
        {
            private @DfInputActions m_Wrapper;
            public GameplayActions(@DfInputActions wrapper) { m_Wrapper = wrapper; }
            public InputAction @Move => m_Wrapper.m_Gameplay_Move;
            public InputAction @Select => m_Wrapper.m_Gameplay_Select;
            public InputAction @ToggleDebugInfo => m_Wrapper.m_Gameplay_ToggleDebugInfo;
            public InputAction @ToggleSpreadsheet => m_Wrapper.m_Gameplay_ToggleSpreadsheet;
            public InputAction @Back => m_Wrapper.m_Gameplay_Back;
            public InputAction @EnergyBoost => m_Wrapper.m_Gameplay_EnergyBoost;
            public InputAction @Zoom => m_Wrapper.m_Gameplay_Zoom;
            public InputActionMap Get() { return m_Wrapper.m_Gameplay; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
            public void AddCallbacks(IGameplayActions instance)
            {
                if (instance == null || m_Wrapper.m_GameplayActionsCallbackInterfaces.Contains(instance)) return;
                m_Wrapper.m_GameplayActionsCallbackInterfaces.Add(instance);
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @Select.started += instance.OnSelect;
                @Select.performed += instance.OnSelect;
                @Select.canceled += instance.OnSelect;
                @ToggleDebugInfo.started += instance.OnToggleDebugInfo;
                @ToggleDebugInfo.performed += instance.OnToggleDebugInfo;
                @ToggleDebugInfo.canceled += instance.OnToggleDebugInfo;
                @ToggleSpreadsheet.started += instance.OnToggleSpreadsheet;
                @ToggleSpreadsheet.performed += instance.OnToggleSpreadsheet;
                @ToggleSpreadsheet.canceled += instance.OnToggleSpreadsheet;
                @Back.started += instance.OnBack;
                @Back.performed += instance.OnBack;
                @Back.canceled += instance.OnBack;
                @EnergyBoost.started += instance.OnEnergyBoost;
                @EnergyBoost.performed += instance.OnEnergyBoost;
                @EnergyBoost.canceled += instance.OnEnergyBoost;
                @Zoom.started += instance.OnZoom;
                @Zoom.performed += instance.OnZoom;
                @Zoom.canceled += instance.OnZoom;
            }

            private void UnregisterCallbacks(IGameplayActions instance)
            {
                @Move.started -= instance.OnMove;
                @Move.performed -= instance.OnMove;
                @Move.canceled -= instance.OnMove;
                @Select.started -= instance.OnSelect;
                @Select.performed -= instance.OnSelect;
                @Select.canceled -= instance.OnSelect;
                @ToggleDebugInfo.started -= instance.OnToggleDebugInfo;
                @ToggleDebugInfo.performed -= instance.OnToggleDebugInfo;
                @ToggleDebugInfo.canceled -= instance.OnToggleDebugInfo;
                @ToggleSpreadsheet.started -= instance.OnToggleSpreadsheet;
                @ToggleSpreadsheet.performed -= instance.OnToggleSpreadsheet;
                @ToggleSpreadsheet.canceled -= instance.OnToggleSpreadsheet;
                @Back.started -= instance.OnBack;
                @Back.performed -= instance.OnBack;
                @Back.canceled -= instance.OnBack;
                @EnergyBoost.started -= instance.OnEnergyBoost;
                @EnergyBoost.performed -= instance.OnEnergyBoost;
                @EnergyBoost.canceled -= instance.OnEnergyBoost;
                @Zoom.started -= instance.OnZoom;
                @Zoom.performed -= instance.OnZoom;
                @Zoom.canceled -= instance.OnZoom;
            }

            public void RemoveCallbacks(IGameplayActions instance)
            {
                if (m_Wrapper.m_GameplayActionsCallbackInterfaces.Remove(instance))
                    UnregisterCallbacks(instance);
            }

            public void SetCallbacks(IGameplayActions instance)
            {
                foreach (var item in m_Wrapper.m_GameplayActionsCallbackInterfaces)
                    UnregisterCallbacks(item);
                m_Wrapper.m_GameplayActionsCallbackInterfaces.Clear();
                AddCallbacks(instance);
            }
        }
        public GameplayActions @Gameplay => new GameplayActions(this);
        private int m_GameplaySchemeIndex = -1;
        public InputControlScheme GameplayScheme
        {
            get
            {
                if (m_GameplaySchemeIndex == -1) m_GameplaySchemeIndex = asset.FindControlSchemeIndex("Gameplay");
                return asset.controlSchemes[m_GameplaySchemeIndex];
            }
        }
        public interface IGameplayActions
        {
            void OnMove(InputAction.CallbackContext context);
            void OnSelect(InputAction.CallbackContext context);
            void OnToggleDebugInfo(InputAction.CallbackContext context);
            void OnToggleSpreadsheet(InputAction.CallbackContext context);
            void OnBack(InputAction.CallbackContext context);
            void OnEnergyBoost(InputAction.CallbackContext context);
            void OnZoom(InputAction.CallbackContext context);
        }
    }
}