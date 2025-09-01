Branchement rapide
	•	Ajoute UvProviderFromPack sur ta scène. Renseigne packFolderAbsolute vers un dossier contenant:
	•	pack.mcmeta, blockstates/stone.json, models/block/stone.json, textures/block/stone.png.
	•	Mets l’atlas sur tes matériaux via le composant (fait au Awake()).
	•	Si tu veux générer le VoxelBlockRegistry depuis le pack: crée un asset RegistryFromMcPack, pointe outputRegistry, les Block C# concrets, clique Build Registry.
	•	LevelRenderer peut pointer sur UvProviderFromPack comme IUVProvider.




	--




	Plan de réintégration vers parité “Minecraft-like”, sans détour. Pas de code ici.

Ordre d’exécution

1) Terrain “vrai” (remplace le sol plat)

Entrées: seed, amplitude, scale.
Levier: ISectionSource → NoiseTerrainGenerator.
Sortie: sections 16³ remplies (stone/grass) avec hauteur H(x,z).

À faire
	•	Implémenter NoiseTerrainGenerator : ISectionSource (Perlin/FBM).
	•	Dans ChunkStreamer.Awake(): remplacer FlatGenerator par NoiseTerrainGenerator.
	•	Critère OK: à viewRadius=4 tu vois des bosses/valleys, plus un carré.

2) IO disque (format sections 16³)

Entrées: LevelStorage, LevelReaderRaw.
Levier: priorité “disk first”, fallback générateur.
Sortie: chargement si fichier existe, génération sinon.

À faire
	•	Dans ChunkStreamer.JobHandler: TryReadSection(path) puis fallback ISectionSource.
	•	Dans SaveCoordinator: déjà présent → conserve snapshot+CRC.
	•	Critère OK: Play → sauve; Re-Play → retrouve exactement le terrain au même endroit.

3) Dirty + autosave réel

Entrées: mutations blocs.
Levier: LevelChunkSection.Dirty mis à true à chaque set; SaveCoordinator.saveBudgetPerFrame.
Sortie: fichiers .vxsc écrits en arrière-plan, pas de freeze.

À faire
	•	Vérifier que tous SetBlock... marquent la section dirty.
	•	Critère OK: nombre de fichiers augmente quand tu édites; plus de ré-écriture si rien ne change.

4) Placement/suppression intégrés (plus de “demo”)

Entrées: caméra/joueur; click.
Levier: PlacementSystem.PlaceByRay/RemoveByRay branchés sur WorldRuntime.SetBlockAndStateAndMark.
Sortie: blocs posés/supprimés, meshing + collider mis à jour par budgets.

À faire
	•	Un unique InputBridge (Mono) qui appelle PlacementSystem avec WorldRuntime.SetBlockAndStateAndMark.
	•	Critère OK: poser/supprimer stone/log/slab/stairs, fusion slab top/bottom → double, stairs facing/half corrects.

5) Grid-AABB + PlayerController fonctionnel

Entrées: input WASD + jump; map solide.
Levier: GridAABB.Move + callback IsSolidAt(x,y,z).
Sortie: déplacement avec glissade sur axes; saut; blocage contre voxels.

À faire
	•	Fournir à IsSolidAt un vrai lookup monde: BlockRegistry.Get(id).IsOccluding(state) via WorldRuntime (ids/states réels).
	•	Critère OK: pas de traversée de murs; déplacement stable sans jitter; step 1 voxel en pente simple.

6) SectionEntities auto (déjà fait, verrouiller)

Entrées: set wanted du ChunkStreamer.
Levier: SectionRenderRegistry.EnsureSectionTarget/RemoveSectionTarget.
Sortie: spawn/destroy propres, matériaux posés, pas de GO manuel.

À faire
	•	Laisser DespawnFar() implémenter un GC simple (si hors rayon et non dirty).
	•	Critère OK: la hiérarchie ne grossit pas indéfiniment quand tu te déplaces loin.

7) Packs + materials propres

Entrées: StreamingAssets/packs/default.
Levier: UvProviderFromPack + PackHotReload.
Sortie: textures correctes, hot-reload en Editor.

À faire
	•	Garder UvProviderFromPack assigné dans LevelRenderer.
	•	PackHotReload actif en Editor; appelle MarkAllRegisteredDirty().
	•	Critère OK: modifier un PNG/JSON → re-mesh visible sans relancer Play.

8) Tuning renderer

Entrées: budgets.
Levier: LevelRenderer.meshBuildBudgetPerFrame, meshAssignBudgetPerFrame, colliderAssignBudgetPerFrame.
Sortie: pas de stutters visibles quand tu entres en zone chargée.

À faire
	•	Opaque/Cutout/Translucent matériels finalisés (URP Lit: Opaque, Cutout, Transparent).
	•	Critère OK: pas de pics >10 ms en entrée de zone (Profiler).

Noms et fichiers touchés (conformes MC)
	•	Registry: BlockRegistry.cs, BlockRegister.cs (déjà OK).
	•	World: WorldRuntime.cs, Level.cs, LevelChunkSection.cs.
	•	Streaming: ChunkStreamer.cs (JobHandler disk-first), ISectionSource.cs, NoiseTerrainGenerator.cs (nouveau).
	•	IO: LevelStorage.cs, LevelReaderRaw.cs, SaveWorker.cs, SaveCoordinator.cs.
	•	Placement: PlacementSystem.cs, PlacementRules.cs, BlockPlaceContext.cs.
	•	Physics: GridAABB.cs, VoxelPlayerController.cs (brancher IsSolidAt au monde réel).
	•	Rendering: LevelRenderer.cs, SectionRenderRegistry.cs, ModelBakery.cs, SectionMesher.cs.
	•	Packs: Pack.cs, Atlas.cs, Json/*, BlockstateResolver.cs, UvProviderFromPack.cs, PackHotReload.cs.

Checks finaux (DoD)
	•	Démarrage à froid → terrain bruité, sections autour du joueur, textures OK.
	•	Déplacement joueur → collisions AABB stables.
	•	Pose/suppression blocs → meshing+collider se mettent à jour, IO dirty fichiers écrits.
	•	Quitter/Revenir → état identique (save/load).
	•	Déplacement long → streaming charge/décharge proprement.

Si tu veux, je te fournis uniquement les scripts nouveaux/manquants pour ces 8 points, dans l’ordre 1→5 d’abord (utile visuellement), puis 6→8.