// Assets/Scripts/Voxel/Runtime/Placement/BlockHotbarPlacer.cs
// Input System. 1..9 pour choisir le bloc, clic gauche pour placer orienté (stairs/columns), clic droit pour retirer.
// Shift = variante "Top" (slab/stairs), Ctrl = "Double" (slab).

using UnityEngine;
using UnityEngine.InputSystem;
using Voxel.Domain.Registry;
using Voxel.Domain.Blocks;
using UPhysics = UnityEngine.Physics;

namespace Voxel.Runtime.Placement
{
    public sealed class BlockHotbarPlacer : MonoBehaviour
    {
        [Header("Refs")]
        public WorldRuntime world;
        public Camera cam;

        [Header("Raycast")]
        public float maxDistance = 8f;
        public LayerMask hitMask = ~0;

        // Hotbar index 1..9
        private int slot = 1;

        void Awake()
        {
            if (!world) world = FindAnyObjectByType<WorldRuntime>();
            if (!cam) cam = Camera.main;
            BlockRegister.Init();
        }

        void Update()
        {
            if (Keyboard.current == null) return;

            // --- Sélection 1..9
            if (Keyboard.current.digit1Key.wasPressedThisFrame) slot = 1;
            if (Keyboard.current.digit2Key.wasPressedThisFrame) slot = 2;
            if (Keyboard.current.digit3Key.wasPressedThisFrame) slot = 3;
            if (Keyboard.current.digit4Key.wasPressedThisFrame) slot = 4;
            if (Keyboard.current.digit5Key.wasPressedThisFrame) slot = 5;
            if (Keyboard.current.digit6Key.wasPressedThisFrame) slot = 6;
            if (Keyboard.current.digit7Key.wasPressedThisFrame) slot = 7;
            if (Keyboard.current.digit8Key.wasPressedThisFrame) slot = 8;
            if (Keyboard.current.digit9Key.wasPressedThisFrame) slot = 9;

            // --- Ray
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (!UPhysics.Raycast(ray, out var hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore)) return;

            // Voxel frappé + case adjacente pour placement
            var p = hit.point - hit.normal * 0.001f;
            int wx = Mathf.FloorToInt(p.x), wy = Mathf.FloorToInt(p.y), wz = Mathf.FloorToInt(p.z);
            int tx = wx + Mathf.RoundToInt(hit.normal.x);
            int ty = wy + Mathf.RoundToInt(hit.normal.y);
            int tz = wz + Mathf.RoundToInt(hit.normal.z);

            // ---- Placer
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceSelected(slot, tx, ty, tz, hit);
            }

            // ---- Retirer
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                world.SetBlock(wx, wy, wz, 0, 0);
            }
        }

        void PlaceSelected(int s, int tx, int ty, int tz, RaycastHit hit)
        {
            // Modif variantes (Shift/Ctrl)
            bool topVariant = Keyboard.current.shiftKey.isPressed;
            bool doubleVariant = Keyboard.current.ctrlKey.isPressed;

            switch (s)
            {
                case 1: // stone
                {
                    var id = BlockRegistry.Get("stone").Id;
                    world.SetBlock(tx, ty, tz, id, 0);
                } break;

                case 2: // dirt
                {
                    var id = BlockRegistry.Get("dirt").Id;
                    world.SetBlock(tx, ty, tz, id, 0);
                } break;

                case 3: // grass (cube_bottom_top)
                {
                    var id = BlockRegistry.Get("grass").Id;
                    world.SetBlock(tx, ty, tz, id, 0);
                } break;

                case 4: // oak_log (ColumnBlock) orienté par la face
                {
                    var blk = BlockRegistry.Get("oak_log"); ushort id = blk.Id;
                    Axis axis = Mathf.Abs(hit.normal.y) > 0.5f ? Axis.Y :
                                Mathf.Abs(hit.normal.x) > 0.5f ? Axis.X : Axis.Z;
                    byte st = blk.EncodeState(new StateProps { axis = axis });
                    world.SetBlock(tx, ty, tz, id, st);
                } break;

                case 5: // oak_slab (SlabBlock) : bottom/top/double
                {
                    var blk = BlockRegistry.Get("oak_slab"); ushort id = blk.Id;
                    SlabType t = doubleVariant ? SlabType.Double : (topVariant ? SlabType.Top : SlabType.Bottom);
                    byte st = blk.EncodeState(new StateProps { slab = t, half = (t==SlabType.Top ? Half.Top : t==SlabType.Bottom ? Half.Bottom : (Half?)null) });
                    world.SetBlock(tx, ty, tz, id, st);
                } break;

                case 6: // oak_stairs (StairsBlock) : facing + half
                {
                    var blk = BlockRegistry.Get("oak_stairs"); ushort id = blk.Id;

                    // Facing: projette la caméra sur XZ, ou utilise la face si up/down
                    Direction facing = Direction.North;
                    Vector3 fwd = cam.transform.forward; fwd.y = 0f;
                    if (Mathf.Abs(hit.normal.y) > 0.5f)
                    {
                        // posé sur le dessus/dessous -> orientation selon la caméra
                        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                        fwd.Normalize();
                        if (Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z))
                            facing = fwd.x > 0 ? Direction.East : Direction.West;
                        else
                            facing = fwd.z > 0 ? Direction.North : Direction.South;
                    }
                    else
                    {
                        // posé sur un côté -> orientation opposée à la normale (l'escalier "monte" vers l'intérieur)
                        Vector3 n = hit.normal;
                        if (Mathf.Abs(n.x) > Mathf.Abs(n.z))
                            facing = n.x > 0 ? Direction.West : Direction.East;   // face frappée → inverse
                        else
                            facing = n.z > 0 ? Direction.North : Direction.South;
                    }

                    // Half: Shift = Top, sinon Bottom
                    Half half = topVariant ? Half.Top : Half.Bottom;

                    byte st = blk.EncodeState(new StateProps { facing = facing, half = half /*, shape = StairsShape.Straight */ });
                    world.SetBlock(tx, ty, tz, id, st);
                } break;

                case 7: // torch (émissif)
                {
                    var id = BlockRegistry.Get("torch").Id;
                    world.SetBlock(tx, ty, tz, id, 0);
                } break;

                default:
                {
                    // fallback stone
                    var id = BlockRegistry.Get("stone").Id;
                    world.SetBlock(tx, ty, tz, id, 0);
                } break;
            }
        }
    }
}