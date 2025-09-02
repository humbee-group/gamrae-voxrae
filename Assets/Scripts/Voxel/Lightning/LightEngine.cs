// Assets/Scripts/Voxel/Lighting/LightEngine.cs
// Sky/Block propagation MC-like, avec purge propre du block-light quand un émetteur est retiré.

using System.Collections.Generic;
using UnityEngine;
using Voxel.Domain.World;
using Voxel.Domain.Registry;
using Voxel.Domain.Blocks;

namespace Voxel.Lighting
{
    [RequireComponent(typeof(Voxel.Runtime.WorldRuntime))]
    public sealed class LightEngine : MonoBehaviour
    {
        [Header("Perf")]
        public int updatesPerFrame = 64;

        private Voxel.Runtime.WorldRuntime world;

        private readonly Queue<(int wx,int wy,int wz, byte level)> skyQ = new();
        private readonly Queue<(int wx,int wy,int wz, byte level)> blkQ = new();

        void Awake() => world = GetComponent<Voxel.Runtime.WorldRuntime>();

        // ======= Public API =======

        public void OnSectionLoaded(SectionPos sp)
        {
            SeedSkyForSection(sp);  FloodSky();
            SeedBlockForSection(sp); FloodBlock();

            world.renderDispatcher?.MarkSectionDirty(sp);
        }

        public void OnBlockChanged(int wx,int wy,int wz, ushort oldId, byte oldSt, ushort newId, byte newSt)
        {
            var oldB = BlockRegistry.Get(oldId);
            var newB = BlockRegistry.Get(newId);

            // --- SKY (si opacité change)
            if (oldB.IsOpaque(oldSt) != newB.IsOpaque(newSt))
            {
                for (int dz=-2; dz<=2; dz++)
                for (int dy=-2; dy<=2; dy++)
                for (int dx=-2; dx<=2; dx++)
                {
                    int x=wx+dx, y=wy+dy, z=wz+dz;
                    var (id, st) = world.GetBlock(x, y, z);
                    if (id==0) EnqueueSky(x,y,z, GuessSkySeed(x,y,z));
                }
                FloodSky();
            }

            // --- BLOCK (émetteur changé)
            if (oldB.LightEmission(oldSt) != newB.LightEmission(newSt))
            {
                var L = newB.LightEmission(newSt);
                var sp = WorldToSection(wx,wy,wz);

                if (L > 0)
                {
                    // source posée
                    if (world.TryGetSection(sp, out var sec))
                    {
                        int lx = Mod(wx,16), ly = Mod(wy,16), lz = Mod(wz,16);
                        sec.SetBlk(lx,ly,lz, L);
                    }
                    EnqueueBlk(wx,wy,wz, L);
                    FloodBlock();
                }
                else
                {
                    // source retirée -> purge locale + reseed + flood
                    ClearBlockLightAround(wx,wy,wz, radius:8);
                    ReseedBlockSourcesInArea(wx,wy,wz, radius:8);
                    FloodBlock();
                }

                // rebuild visuel zone
                MarkAreaDirty(wx,wy,wz, radiusSections:1);
            }
        }

        // ======= Impl =======

        private void SeedSkyForSection(SectionPos sp)
        {
            for (int lz=0; lz<16; lz++)
            for (int lx=0; lx<16; lx++)
            {
                int light = 15;
                for (int sy = world.sectionsY-1; sy>=0; sy--)
                {
                    var spCol = new SectionPos(sp.x, sy, sp.z);
                    if (!world.TryGetSection(spCol, out var sec)) continue;

                    for (int ly=15; ly>=0; ly--)
                    {
                        var (id, st) = sec.Get(lx,ly,lz);
                        var b = BlockRegistry.Get(id);
                        if (b.IsOpaque(st))
                        {
                            light = 0;
                            sec.SetSky(lx,ly,lz,0);
                        }
                        else
                        {
                            if (sec.GetSky(lx,ly,lz) < light)
                            {
                                sec.SetSky(lx,ly,lz,(byte)light);
                                EnqueueSky(sp.x*16+lx, sy*16+ly, sp.z*16+lz, (byte)light);
                            }
                        }
                    }
                }
            }
        }

        private void FloodSky()
        {
            int processed=0;
            while (skyQ.Count>0 && processed<updatesPerFrame)
            {
                var (wx,wy,wz,L) = skyQ.Dequeue();
                processed++;

                for (int i=0;i<6;i++)
                {
                    int nx=wx, ny=wy, nz=wz;
                    switch(i){case 0:nz--;break;case 1:nz++;break;case 2:nx--;break;case 3:nx++;break;case 4:ny++;break;case 5:ny--;break;}
                    if (!TryGet(nx,ny,nz, out var sp, out var sec, out int lx, out int ly, out int lz)) continue;

                    var (nid,nst)=sec.Get(lx,ly,lz);
                    if (BlockRegistry.Get(nid).IsOpaque(nst)) continue;

                    byte next = (byte)((ny<wy) ? L : (L>0 ? L-1 : 0));
                    if (next > sec.GetSky(lx,ly,lz))
                    {
                        sec.SetSky(lx,ly,lz,next);
                        EnqueueSky(nx,ny,nz,next);
                    }
                }
            }
        }

        private void SeedBlockForSection(SectionPos sp)
        {
            if (!world.TryGetSection(sp, out var sec)) return;

            for (int ly=0; ly<16; ly++)
            for (int lz=0; lz<16; lz++)
            for (int lx=0; lx<16; lx++)
            {
                var (id, st) = sec.Get(lx,ly,lz);
                var b = BlockRegistry.Get(id);
                byte L = b.LightEmission(st);
                if (L>0)
                {
                    sec.SetBlk(lx,ly,lz, L);
                    EnqueueBlk(sp.x*16+lx, sp.y*16+ly, sp.z*16+lz, L);
                }
            }
        }

        private void FloodBlock()
        {
            int processed=0;
            while (blkQ.Count>0 && processed<updatesPerFrame)
            {
                var (wx,wy,wz,L) = blkQ.Dequeue();
                processed++;

                for (int i=0;i<6;i++)
                {
                    int nx=wx, ny=wy, nz=wz;
                    switch(i){case 0:nz--;break;case 1:nz++;break;case 2:nx--;break;case 3:nx++;break;case 4:ny++;break;case 5:ny--;break;}
                    if (!TryGet(nx,ny,nz, out var sp, out var sec, out int lx, out int ly, out int lz)) continue;

                    var (nid,nst)=sec.Get(lx,ly,lz);
                    var nb = BlockRegistry.Get(nid);
                    if (nb.IsOpaque(nst)) continue;

                    byte atten = nb.LightAttenuation(nst);
                    byte next  = (byte)(L > atten ? L - atten : 0);
                    if (next>sec.GetBlk(lx,ly,lz))
                    {
                        sec.SetBlk(lx,ly,lz,next);
                        EnqueueBlk(nx,ny,nz,next);
                    }
                }
            }
        }

        private void ClearBlockLightAround(int wx,int wy,int wz, int radius)
        {
            int r = Mathf.Max(1, radius);
            for (int z=wz-r; z<=wz+r; z++)
            for (int y=wy-r; y<=wy+r; y++)
            for (int x=wx-r; x<=wx+r; x++)
            {
                var sp = WorldToSection(x,y,z);
                if (!world.TryGetSection(sp, out var sec)) continue;
                int lx = Mod(x,16), ly = Mod(y,16), lz = Mod(z,16);
                int idx = ((ly*16)+lz)*16 + lx;
                if (sec.block[idx] != 0) sec.block[idx] = 0;
            }
        }

        private void ReseedBlockSourcesInArea(int wx,int wy,int wz, int radius)
        {
            int sx0 = FloorDiv(wx - radius, 16), sx1 = FloorDiv(wx + radius, 16);
            int sy0 = FloorDiv(wy - radius, 16), sy1 = FloorDiv(wy + radius, 16);
            int sz0 = FloorDiv(wz - radius, 16), sz1 = FloorDiv(wz + radius, 16);

            for (int sz=sz0; sz<=sz1; sz++)
            for (int sy=sy0; sy<=sy1; sy++)
            for (int sx=sx0; sx<=sx1; sx++)
            {
                var sp = new SectionPos(sx,sy,sz);
                SeedBlockForSection(sp);
                world.renderDispatcher?.MarkSectionDirty(sp);
            }
        }

        private void MarkAreaDirty(int wx,int wy,int wz, int radiusSections)
        {
            int rs = Mathf.Max(0, radiusSections);
            int sx0 = FloorDiv(wx,16)-rs, sx1 = FloorDiv(wx,16)+rs;
            int sy0 = FloorDiv(wy,16)-rs, sy1 = FloorDiv(wy,16)+rs;
            int sz0 = FloorDiv(wz,16)-rs, sz1 = FloorDiv(wz,16)+rs;

            for (int sz=sz0; sz<=sz1; sz++)
            for (int sy=sy0; sy<=sy1; sy++)
            for (int sx=sx0; sx<=sx1; sx++)
                world.renderDispatcher?.MarkSectionDirty(new SectionPos(sx,sy,sz));
        }

        // === Utils ===

        private void EnqueueSky(int wx,int wy,int wz, byte L){ if (L>0) skyQ.Enqueue((wx,wy,wz,L)); }
        private void EnqueueBlk(int wx,int wy,int wz, byte L){ if (L>0) blkQ.Enqueue((wx,wy,wz,L)); }

        private bool TryGet(int wx,int wy,int wz, out SectionPos sp, out LevelChunkSection sec, out int lx,out int ly,out int lz)
        {
            sp = WorldToSection(wx,wy,wz);
            lx = Mod(wx,16); ly=Mod(wy,16); lz=Mod(wz,16);
            if (world.TryGetSection(sp, out sec)) return true;
            sec=null; return false;
        }

        private SectionPos WorldToSection(int wx,int wy,int wz)
        {
            int sx = FloorDiv(wx,16), sy = FloorDiv(wy,16), sz = FloorDiv(wz,16);
            return new SectionPos(sx,sy,sz);
        }

        private static int FloorDiv(int a,int b)=> (a>=0)? a/b : ((a-(b-1))/b);
        private static int Mod(int a,int b){ int m=a%b; return m<0? m+b:m; }

        private byte GuessSkySeed(int wx,int wy,int wz)
        {
            if (IsOpenToSky(wx,wy+1,wz)) return 15;
            byte best=0;
            for (int i=0;i<6;i++)
            {
                int nx=wx,ny=wy,nz=wz;
                switch(i){case 0:nz--;break;case 1:nz++;break;case 2:nx--;break;case 3:nx++;break;case 4:ny++;break;case 5:ny--;break;}
                if (!TryGet(nx,ny,nz, out var sp,out var sec,out int lx,out int ly,out int lz)) continue;
                byte s = sec.GetSky(lx,ly,lz);
                if (i==5) { if (s>best) best=s; } else { if (s>0 && (byte)(s-1)>best) best=(byte)(s-1); }
            }
            return best;
        }

        private bool IsOpenToSky(int wx,int wy,int wz)
        {
            for (int y = wy; y < world.sectionsY*16; y++)
            {
                if (!TryGet(wx,y,wz, out _, out var sec, out int lx, out int ly, out int lz)) continue;
                var (id, st) = sec.Get(lx,ly,lz);
                if (BlockRegistry.Get(id).IsOpaque(st)) return false;
            }
            return true;
        }
    }
}