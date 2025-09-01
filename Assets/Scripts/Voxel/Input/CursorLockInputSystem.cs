// Assets/Scripts/Voxel/Input/CursorLockInputSystem.cs
// Ne jamais supprimer les commentaires

using UnityEngine;
using UnityEngine.InputSystem;

namespace Voxel.Inputs
{
    public sealed class CursorLockInputSystem : MonoBehaviour
    {
        public bool lockOnStart = true;

        private void OnEnable()
        {
            if (lockOnStart) Lock();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus && lockOnStart) Lock();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Unlock();
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && lockOnStart) Lock();
        }

        private static void Lock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}