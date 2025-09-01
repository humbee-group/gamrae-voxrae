// Assets/Scripts/Voxel/Input/VoxelInputBridge.cs
// Ne jamais supprimer les commentaires

using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.Runtime.Placement;

namespace Voxel.Inputs
{
    [RequireComponent(typeof(Voxel.Runtime.Player.VoxelCharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class VoxelInputBridge : MonoBehaviour
    {
        [Header("Refs")]
        public Voxel.Runtime.WorldRuntime world;
        public Camera cam;
        public PlayerInput playerInput;

        [Header("Interact")]
        public ushort placeBlockId = 1;      // stone
        public float interactMaxDist = 128f;

        private Voxel.Runtime.Player.VoxelCharacterController _ctrl;

        // Actions
        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _place;
        private InputAction _remove;

        private void Awake()
        {
            if (!cam) cam = Camera.main;
            _ctrl = GetComponent<Voxel.Runtime.Player.VoxelCharacterController>();
            if (!playerInput) playerInput = GetComponent<PlayerInput>();

            // S’assure que la map par défaut est active
            if (playerInput.currentActionMap == null)
            {
                var map = playerInput.actions.FindActionMap(playerInput.defaultActionMap, true);
                map.Enable();
            }

            // Récupère les actions par nom dans la map active (ex: "Player")
            var mapActive = playerInput.currentActionMap;
            _move   = mapActive.FindAction("Move",   true);
            _look   = mapActive.FindAction("Look",   true);
            _jump   = mapActive.FindAction("Jump",   true);
            _place  = mapActive.FindAction("Place",  true);
            _remove = mapActive.FindAction("Remove", true);
        }

        private void OnEnable()
        {
            _move.performed   += OnMove;
            _move.canceled    += OnMove;

            _look.performed   += OnLook;
            _look.canceled    += OnLook;

            _jump.performed   += OnJump;
            _place.performed  += OnPlace;
            _remove.performed += OnRemove;

            _move.Enable(); _look.Enable(); _jump.Enable(); _place.Enable(); _remove.Enable();
        }

        private void OnDisable()
        {
            _move.performed   -= OnMove;   _move.canceled   -= OnMove;
            _look.performed   -= OnLook;   _look.canceled   -= OnLook;
            _jump.performed   -= OnJump;
            _place.performed  -= OnPlace;
            _remove.performed -= OnRemove;

            _move.Disable(); _look.Disable(); _jump.Disable(); _place.Disable(); _remove.Disable();
        }

        // ----- Handlers -----
        private void OnMove(InputAction.CallbackContext ctx)
        {
            _ctrl.SetMove(ctx.ReadValue<Vector2>());
        }

        private void OnLook(InputAction.CallbackContext ctx)
        {
            _ctrl.SetLook(ctx.ReadValue<Vector2>());
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            _ctrl.RequestJump();
        }

        private void OnPlace(InputAction.CallbackContext ctx)
        {
            if (world && cam)
                PlacementSystem.PlaceByRay(cam, world.SetBlockAndStateAndMark, placeBlockId, interactMaxDist);
        }

        private void OnRemove(InputAction.CallbackContext ctx)
        {
            if (world && cam)
                PlacementSystem.RemoveByRay(cam, world.SetBlockAndStateAndMark, interactMaxDist);
        }
    }
}