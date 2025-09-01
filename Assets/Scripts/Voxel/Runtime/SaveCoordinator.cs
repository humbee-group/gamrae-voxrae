// Assets/Scripts/Voxel/Runtime/SaveCoordinator.cs
// Sauvegarde batchée des sections dirty via SaveWorker
using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.World;
using Voxel.IO;

namespace Voxel.Runtime
{
    [RequireComponent(typeof(WorldRuntime))]
    public sealed class SaveCoordinator : MonoBehaviour
    {
        public int saveBudgetPerFrame = 8;
        private WorldRuntime world;
        private SaveWorker saver;

        private void Awake(){ world=GetComponent<WorldRuntime>(); saver=new SaveWorker(); saver.EnsureStarted(); }
        private void OnDestroy(){ saver.Stop(); }

        private void Update()
        {
            int budget = saveBudgetPerFrame;
            foreach (var (sp, sec) in world.Sections())
            {
                if (budget<=0) break;
                if (!sec.Dirty) continue;

                // snapshot arrays 4096
                var ids = new ushort[4096];
                var st  = new byte[4096];
                System.Array.Copy(sec.ids, ids, 4096);
                System.Array.Copy(sec.st,  st,  4096);
                sec.ClearDirty();

                var path = LevelStorage.SectionPath(world.saveRoot, sp.x, sp.y, sp.z);
                saver.Enqueue(() => LevelStorage.SaveSection(path, ids, st));
                budget--;
            }
        }
    }
}