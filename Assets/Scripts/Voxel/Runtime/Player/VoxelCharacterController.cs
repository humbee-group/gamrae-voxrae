// Assets/Scripts/Voxel/Runtime/Player/VoxelCharacterController.cs
// Ne jamais supprimer les commentaires

using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.Runtime.Physics;

namespace Voxel.Runtime.Player
{
    /// Controller basique + InputActions. Pas de step-up. Zéro gravité au sol. Jump fiable.
    public sealed class VoxelCharacterController : MonoBehaviour
    {
        [Header("World")] public Voxel.Runtime.WorldRuntime world;

        [Header("AABB")]  public Vector3 halfExtents = new(0.3f, 0.9f, 0.3f);

        [Header("Move")]  public float maxGroundSpeed = 5f;
        public float maxAirSpeed = 5f;
        public float groundAccel = 50f;
        public float airAccel = 6f;
        public float friction = 10f;

        [Header("Jump/Gravity")] public float jumpHeight = 1.6f;
        public float gravity = -22f;        // NÉGATIF (m/s²)
        public float coyoteTime = 0.12f;
        public float terminalFall = -60f;   // NÉGATIF (m/s)

        [Header("Look")] public Camera cam;
        public float mouseSensitivity = 2f;
        public float maxPitch = 89f;

        // État
        Vector3 _vel;
        bool _onGround;
        float _coyoteTimer;
        float _yaw, _pitch;

        // === API pour VoxelInputBridge ===
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool requestJump;

        public void SetMove(Vector2 v) => moveInput = Vector2.ClampMagnitude(v, 1f);
        public void SetLook(Vector2 v) => lookInput = v;
        public void RequestJump() => requestJump = true;

        void Start()
        {
            if (!cam) cam = Camera.main;
            _yaw = transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        void Update()
        {
            if (world == null) return;
            float dt = Mathf.Max(0.0005f, Time.deltaTime);

            // ===== Look (InputActions "Look") =====
            _yaw   += lookInput.x * mouseSensitivity * dt * 50f;
            _pitch -= lookInput.y * mouseSensitivity * dt * 50f;
            _pitch = Mathf.Clamp(_pitch, -maxPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cam) cam.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            lookInput = Vector2.zero; // reset

            // ===== Coyote =====
            if (_onGround) _coyoteTimer = coyoteTime;
            else _coyoteTimer = Mathf.Max(0f, _coyoteTimer - dt);

            // ===== Jump =====
            if (_coyoteTimer > 0f && requestJump)
            {
                _vel.y = Mathf.Sqrt(Mathf.Max(0.0001f, jumpHeight) * -2f * gravity);
                _coyoteTimer = 0f; _onGround = false; requestJump = false;
            }

            // ===== Wish (InputActions "Move") =====
            Vector3 wish = new(moveInput.x, 0f, moveInput.y);
            wish = wish.sqrMagnitude > 0f ? transform.TransformDirection(wish).normalized : Vector3.zero;

            // ===== Horizontal control =====
            Vector3 vXZ = new(_vel.x, 0f, _vel.z);
            if (_onGround)
            {
                Vector3 wishVel = wish * maxGroundSpeed;
                Vector3 dv = wishVel - vXZ;
                float add = groundAccel * dt;
                if (dv.magnitude <= add) vXZ = wishVel; else vXZ += dv.normalized * add;

                // Friction au sol
                float sp = vXZ.magnitude;
                float drop = Mathf.Min(sp, friction * dt);
                if (sp > 0f) vXZ *= Mathf.Max(0f, (sp - drop)) / sp;

                // Au sol: pas de gravité résiduelle ni vy négatif
                if (_vel.y < 0f) _vel.y = 0f;
            }
            else
            {
                Vector3 wishVel = wish * maxAirSpeed;
                Vector3 dv = wishVel - vXZ;
                float add = airAccel * dt;
                if (dv.magnitude <= add) vXZ = wishVel; else vXZ += dv.normalized * add;

                // Gravité seulement en l’air
                _vel.y += gravity * dt;                        // gravity < 0
                if (_vel.y < terminalFall) _vel.y = terminalFall;
            }
            _vel.x = vXZ.x; _vel.z = vXZ.z;

            // ===== Solve =====
            var box = new GridAABB.Box(transform.position, halfExtents);
            var res = GridAABB.MoveAABB(world, box, _vel, dt);

            if (res.hitX) _vel.x = 0f;
            if (res.hitZ) _vel.z = 0f;

            transform.position = res.position;
            _vel = res.velocity;
            _onGround = res.onGround;

            // ESC unlock / click relock
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
    }
}