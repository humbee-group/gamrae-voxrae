# Plan de Refactoring pour un moteur voxel de type Minecraft

Ce document expose les grandes lignes pour transformer le moteur actuel en un environnement familier aux moddeurs de Minecraft, tout en préparant une architecture client-serveur future.

## 1. Organisation des dossiers
- **Assets/Scripts/Core** : code partagé entre client et serveur.
- **Assets/Scripts/Client** : logique, rendu et entrée spécifiques au client Unity.
- **Assets/Scripts/Server** : logique d'autorité, chargement de monde et gestion des entités côté serveur headless.
- **Assets/StreamingAssets** : packs de ressources (textures, modèles, états de blocs) structurés comme dans Minecraft.

## 2. Concepts clés alignés sur Minecraft
- **Blocks & BlockStates** : chaque bloc possède une définition et des états, stockés dans un registre similaire à Minecraft.
- **Chunks & Sections** : monde découpé en chunks de 16×16×16, divisés en sections pour le streaming.
- **Registries** : utilisation d'identifiants de ressource (`namespace:path`) pour enregistrer blocs, matériaux et autres données.

## 3. Séparation Client/Serveur
- Définir des interfaces de communication (par exemple via Netcode ou RPC) permettant au client de recevoir les mises à jour d'état du serveur.
- Préparer un mode `Server` sans rendu pouvant exécuter la simulation.
- Le client se contente d'afficher le monde et d'envoyer les entrées (placement, destruction de blocs, mouvement).

## 4. Étapes recommandées
1. **Réorganiser les scripts** selon la structure ci-dessus.
2. **Mettre en place des namespaces** reflétant la distinction client/serveur (`Voxrae.Core`, `Voxrae.Client`, `Voxrae.Server`).
3. **Isoler la logique de bloc** pour qu'elle puisse être partagée par les deux côtés.
4. **Introduire une couche réseau** minimale pour synchroniser un chunk et les actions de base.
5. **Ajouter des tests unitaires** pour valider la sérialisation des états et la logique de streaming.

## 5. Points d'attention
- Garder un vocabulaire générique (pas de noms provenant de Minecraft) tout en préservant la logique qui sera intuitive pour un moddeur Minecraft.
- Documenter chaque étape afin de faciliter la migration.

Ce plan sert de base pour un refactoring progressif. Chaque étape peut être réalisée par commits successifs afin d'éviter une réécriture massive et risquée.
