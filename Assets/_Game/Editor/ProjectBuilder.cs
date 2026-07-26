using System;
using System.Collections.Generic;
using System.IO;
using NidoCero;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NidoCero.Editor
{
    public static class ProjectBuilder
    {
        private const string GeneratedRoot = "Assets/_Game/Generated";
        private const string DataRoot = GeneratedRoot + "/Data";
        private const string MaterialRoot = GeneratedRoot + "/Materials";
        private const string SceneRoot = "Assets/_Game/Scenes";
        private const string CeilingTileTexturePath =
            "Assets/_Game/Art/Environment/Tiles/TercerPiso_Ceiling_Tile.png";
        private const string FloorTileTexturePath =
            "Assets/_Game/Art/Environment/Tiles/TercerPiso_Floor_Tile.png";
        private const string PlayerModelPath =
            "Assets/_Game/Art/Characters/Piquero_Female_Player_Rigged_Optimized.glb";
        private const string WiseTurtleModelPath =
            "Assets/_Game/Art/Characters/Tortuga_Sabia_Rigged_Optimized.glb";
        private const string RescueCageModelPath =
            "Assets/_Game/Art/Props/Rescue_Cage_Optimized.glb";
        private const string TurtleModelPath =
            "Assets/_Game/Art/Enemies/Tortuga_Tank_Rigged_Optimized.glb";
        private const string CrabModelPath =
            "Assets/_Game/Art/Enemies/Cangrejo_Walker_Rigged_Optimized.glb";
        private const string FlyerModelPath =
            "Assets/_Game/Art/Enemies/Fragata_Flyer_Rigged_Optimized.glb";
        private const string BossModelPath =
            "Assets/_Game/Art/Boss/BOSS-FINAL-RIG.glb";
        private const int FloorCount = 4;
        private const float CorridorHeight = 5f;
        private const float CorridorLength = 32f;
        private const float TopFloorBaseY = 15f;
        private const float CharacterScale = 0.85f;
        private const float PlayerStandingOffset = 0.5f + 2.1f * CharacterScale * 0.5f;

        private static Font runtimeFont;

        [MenuItem("Nido Cero/Build Complete Prototype _F8")]
        public static void BuildAll()
        {
            try
            {
                EnsureFolders();
                ConfigureProject();
                GameCatalog catalog = BuildCatalog();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ConfigureEnvironmentTexture(CeilingTileTexturePath);
                ConfigureEnvironmentTexture(FloorTileTexturePath);
                ConfigureGameplaySprites();
                catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(DataRoot + "/GameCatalog.asset");
                if (catalog == null || catalog.enemies == null || catalog.enemies.Length != 3)
                    throw new InvalidOperationException("GameCatalog could not be reloaded with its enemy definitions.");
                MaterialLibrary materials = BuildMaterials();

                BuildLauncher(catalog, materials);
                BuildCinematic(catalog, materials, true);
                BuildMain(catalog, materials);
                BuildCinematic(catalog, materials, false);
                BuildValidation(catalog, materials);
                IntegratedUIBuilder.Apply(catalog);

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(SceneRoot + "/00_Launcher.unity", true),
                    new EditorBuildSettingsScene(SceneRoot + "/01_CinematicIntro.unity", true),
                    new EditorBuildSettingsScene(SceneRoot + "/02_MainScene.unity", true),
                    new EditorBuildSettingsScene(SceneRoot + "/03_CinematicEnd.unity", true)
                };

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(SceneRoot + "/00_Launcher.unity");
                Debug.Log("[NIDO CERO] BUILD_ALL_COMPLETE: 5 scenes, 18 cards, 4 descending floors.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        public static void BuildAllBatch()
        {
            BuildAll();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Game");
            EnsureFolder("Assets/_Game", "Scenes");
            EnsureFolder("Assets/_Game", "Generated");
            EnsureFolder("Assets/_Game", "Art");
            EnsureFolder("Assets/_Game/Art", "Environment");
            EnsureFolder("Assets/_Game/Art/Environment", "Tiles");
            EnsureFolder(GeneratedRoot, "Data");
            EnsureFolder(GeneratedRoot, "Materials");
            EnsureFolder("Assets/_Game", "Tests");
            EnsureFolder("Assets/_Game/Tests", "EditMode");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static void ConfigureProject()
        {
            PlayerSettings.productName = "NIDO CERO";
            PlayerSettings.companyName = "GameJam2026";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static GameCatalog BuildCatalog()
        {
            string[] statNames = { "Fuerza", "Velocidad", "Defensa", "Agilidad", "Vida", "Energía" };
            int[] defaults = { 2, 5, 1, 2, 5, 100 };
            int[] minimums = { 1, 2, 0, 0, 1, 50 };
            int[] maximums = { 8, 9, 5, 7, 9, 150 };
            StatDefinition[] stats = new StatDefinition[6];
            for (int i = 0; i < stats.Length; i++)
            {
                StatDefinition definition = GetOrCreate<StatDefinition>(DataRoot + "/Stat_" + ((StatId)i) + ".asset");
                definition.id = (StatId)i;
                definition.displayName = statNames[i];
                definition.defaultValue = defaults[i];
                definition.minimum = minimums[i];
                definition.maximum = maximums[i];
                EditorUtility.SetDirty(definition);
                stats[i] = definition;
            }

            Color[] elementColors =
            {
                new Color(0.12f, 0.62f, 1f),
                new Color(1f, 0.28f, 0.08f),
                new Color(0.22f, 0.78f, 0.3f)
            };
            string[] elementNames = { "Agua", "Fuego", "Vegetación" };
            string[] elementDescriptions =
            {
                "Apaga el fuego y abre una vulnerabilidad mecánica.",
                "Quema la vegetación y sobrecarga circuitos.",
                "Absorbe el agua y atrapa mecanismos."
            };
            ElementDefinition[] elements = new ElementDefinition[3];
            for (int i = 0; i < elements.Length; i++)
            {
                ElementDefinition definition =
                    GetOrCreate<ElementDefinition>(DataRoot + "/Element_" + ((ElementId)i) + ".asset");
                definition.id = (ElementId)i;
                definition.displayName = elementNames[i];
                definition.color = elementColors[i];
                definition.description = elementDescriptions[i];
                EditorUtility.SetDirty(definition);
                elements[i] = definition;
            }

            EnemyDefinition[] enemies = new EnemyDefinition[3];
            enemies[0] = ConfigureEnemy("Enemy_Walker", EnemyArchetype.Walker, "Cangre-Cam", 2, 2.1f, 2.2f,
                ElementId.Water, 1, new Color(0.2f, 0.65f, 1f));
            enemies[1] = ConfigureEnemy("Enemy_Flyer", EnemyArchetype.Flyer, "Fraga-Dron", 1, 1.5f, 1.7f,
                ElementId.Fire, 1, new Color(1f, 0.32f, 0.12f));
            enemies[2] = ConfigureEnemy("Enemy_Tank", EnemyArchetype.Tank, "Tortu-Tank", 4, 1.05f, 2.8f,
                ElementId.Vegetation, 2, new Color(0.28f, 0.75f, 0.32f));

            string[] floorNames =
            {
                "Cráter / Tutorial", "Galerías de captura", "Forja del archivo", "Cámara del Núcleo"
            };
            string[] floorObjectives =
            {
                "Derrota al Fraga-Dron, escucha a la Tortuga Sabia y asume la primera decisión.",
                "Libera el corredor de 3 Cangre-Cam y elige un costo.",
                "Obtén la llave dorada y supera al Tortu-Tank.",
                "Destruye los 3 relés y apaga a Núcleo Cero."
            };
            FloorDefinition[] floors = new FloorDefinition[FloorCount];
            for (int i = 0; i < floors.Length; i++)
            {
                FloorDefinition definition =
                    GetOrCreate<FloorDefinition>(DataRoot + "/Floor_" + (i + 1).ToString("00") + ".asset");
                definition.floorIndex = i;
                definition.displayName = floorNames[i];
                definition.ambientColor = Color.Lerp(new Color(0.14f, 0.2f, 0.25f),
                    elementColors[i % 3] * 0.65f, 0.48f);
                definition.dominantElement = (ElementId)(i % 3);
                definition.objective = floorObjectives[i];
                EditorUtility.SetDirty(definition);
                floors[i] = definition;
            }
            for (int legacyFloor = FloorCount + 1; legacyFloor <= 6; legacyFloor++)
                AssetDatabase.DeleteAsset(DataRoot + "/Floor_" + legacyFloor.ToString("00") + ".asset");

            CardTemplate[] cards = new CardTemplate[18];
            string[] titles =
            {
                "Pluma templada", "Alas ligeras", "Coraza de sal",
                "Instinto eléctrico", "Pulso vital", "Reserva marina",
                "Garra de cobre", "Paso de vapor", "Nido blindado",
                "Reflejo ceniza", "Sangre verde", "Tanque de lluvia",
                "Pico de presión", "Turbina dorsal", "Memoria mineral",
                "Ojo de tormenta", "Último latido", "Marea interna"
            };
            for (int i = 0; i < cards.Length; i++)
            {
                CardTemplate card = GetOrCreate<CardTemplate>(DataRoot + "/Card_" + (i + 1).ToString("00") + ".asset");
                StatId gain = (StatId)(i % 6);
                StatId loss = (StatId)((i + 2 + i / 6) % 6);
                if (gain == loss) loss = (StatId)(((int)loss + 1) % 6);
                card.cardId = "card_" + (i + 1).ToString("00");
                card.title = titles[i];
                card.gainStat = gain;
                card.lossStat = loss;
                card.gainAmount = gain == StatId.Stamina ? 10 : 1;
                card.lossAmount = loss == StatId.Stamina ? 10 : 1;
                card.element = (ElementId)(i % 3);
                card.elementAmount = 1;
                card.accent = Color.Lerp(elementColors[i % 3], Color.white, 0.22f);
                card.description = "Ganar " + statNames[(int)gain] + " exige renunciar a " +
                                   statNames[(int)loss] + ". La opción rechazada desaparece.";
                EditorUtility.SetDirty(card);
                cards[i] = card;
            }

            GameCatalog catalog = GetOrCreate<GameCatalog>(DataRoot + "/GameCatalog.asset");
            catalog.stats = stats;
            catalog.elements = elements;
            catalog.cards = cards;
            catalog.enemies = enemies;
            catalog.floors = floors;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static EnemyDefinition ConfigureEnemy(string assetName, EnemyArchetype archetype, string displayName,
            int health, float speed, float interval, ElementId element, int level, Color color)
        {
            EnemyDefinition definition = GetOrCreate<EnemyDefinition>(DataRoot + "/" + assetName + ".asset");
            definition.archetype = archetype;
            definition.displayName = displayName;
            definition.maxHealth = health;
            definition.moveSpeed = speed;
            definition.attackInterval = interval;
            definition.element = element;
            definition.elementLevel = level;
            definition.color = color;
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Could not load required asset: " + path);
            return asset;
        }

        private static MaterialLibrary BuildMaterials()
        {
            return new MaterialLibrary
            {
                dark = Material("DarkMetal", new Color(0.12f, 0.15f, 0.18f)),
                metal = Material("Iron", new Color(0.28f, 0.32f, 0.34f)),
                copper = Material("Copper", new Color(0.62f, 0.28f, 0.12f)),
                teal = Material("Ocean", new Color(0.06f, 0.42f, 0.5f)),
                green = Material("Vegetation", new Color(0.16f, 0.5f, 0.2f)),
                ember = Material("Ember", new Color(0.95f, 0.25f, 0.08f)),
                player = Material("Player", new Color(0.18f, 0.72f, 0.92f)),
                playerModel = ModelMaterial(PlayerModelPath, "Piquero_Femenino_WebGL"),
                wiseTurtleModel = ModelMaterial(WiseTurtleModelPath, "Tortuga_Sabia_WebGL"),
                rescueCageModel = ModelMaterial(RescueCageModelPath, "Jaula_Rescate_WebGL"),
                turtleModel = ModelMaterial(TurtleModelPath, "Tortuga_WebGL"),
                crabModel = ModelMaterial(CrabModelPath, "Cangrejo_WebGL"),
                flyerModel = ModelMaterial(FlyerModelPath, "Fragata_WebGL"),
                bossModel = ModelMaterial(BossModelPath, "Boss_Final_WebGL"),
                key = Material("Key", new Color(1f, 0.82f, 0.12f)),
                white = Material("BoneWhite", new Color(0.9f, 0.91f, 0.84f)),
                warning = Material("Warning", new Color(0.8f, 0.16f, 0.08f)),
                purple = Material("AI", new Color(0.45f, 0.16f, 0.7f)),
                ceilingTile = TiledTextureMaterial("Ceiling_Front_Tile", CeilingTileTexturePath,
                    new Vector2(1f, 1f)),
                floorTile = TiledTextureMaterial("Floor_Front_Tile", FloorTileTexturePath,
                    new Vector2(2f, 1f))
            };
        }

        private static void ConfigureEnvironmentTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Could not configure environment texture: " + path);

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureGameplaySprites()
        {
            string[] uiSprites =
            {
                "profile_azul.png",
                "hud_stat_attack.png",
                "hud_stat_defense.png",
                "hud_stat_health.png",
                "hud_stat_agility.png",
                "hud_stat_speed.png",
                "hud_stat_stamina.png"
            };
            foreach (string fileName in uiSprites)
                ConfigureSpriteTexture("Assets/_Game/UI/Sprites/" + fileName, 500f, 512);

            ConfigureSpriteTexture(
                "Assets/_Game/Resources/Visuals/Pickups/decision_orb.png",
                1080f,
                512);

            string[] elements = { "fire", "water", "nature" };
            foreach (string element in elements)
                for (int frame = 1; frame <= 4; frame++)
                    ConfigureSpriteTexture(
                        "Assets/_Game/Resources/Visuals/Projectiles/projectile_" +
                        element + "_" + frame.ToString("00") + ".png",
                        512f,
                        512);
        }

        private static void ConfigureSpriteTexture(string path, float pixelsPerUnit, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Could not configure gameplay sprite: " + path);

            importer.textureType = TextureImporterType.Sprite;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material TiledTextureMaterial(string name, string texturePath, Vector2 tiling)
        {
            string path = MaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null) throw new InvalidOperationException("Required shader Unlit/Texture was not found.");

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.mainTexture = LoadRequired<Texture2D>(texturePath);
            material.mainTextureScale = tiling;
            material.mainTextureOffset = Vector2.zero;
            if (material.HasProperty("_Color")) material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Material(string name, Color color)
        {
            string path = MaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.35f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.28f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material ModelMaterial(string modelPath, string materialName)
        {
            string path = MaterialRoot + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            Texture2D baseColor = null;
            Texture2D normal = null;
            UnityEngine.Object[] modelAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            foreach (UnityEngine.Object asset in modelAssets)
            {
                Texture2D texture = asset as Texture2D;
                if (texture == null) continue;
                string normalizedName = texture.name.Replace("_", string.Empty)
                    .Replace(" ", string.Empty).ToLowerInvariant();
                if (baseColor == null &&
                    (normalizedName.Contains("basecolor") || normalizedName.Contains("albedo") ||
                     normalizedName.Contains("diffuse")))
                    baseColor = texture;
                if (normal == null && normalizedName.Contains("normal")) normal = texture;
            }

            if (baseColor == null)
                throw new InvalidOperationException(modelPath + " is missing a base color texture.");

            material.color = Color.white;
            material.mainTexture = baseColor;
            material.SetFloat("_Metallic", 0.05f);
            material.SetFloat("_Glossiness", 0.32f);
            material.SetColor("_EmissionColor", Color.black);
            material.SetTexture("_EmissionMap", null);
            material.DisableKeyword("_EMISSION");
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.SetTexture("_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildLauncher(GameCatalog catalog, MaterialLibrary materials)
        {
            Scene scene = NewScene();
            AddSession(catalog, true);
            Camera camera = CreateCamera(new Color(0.025f, 0.055f, 0.075f), 6f, Vector3.zero);
            CreateLight();

            Transform backdrop = new GameObject("Launcher_Blockout").transform;
            Cube("Tower", new Vector3(5.2f, 0f, 2f), new Vector3(3f, 12f, 2f), materials.dark, backdrop);
            for (int i = 0; i < 6; i++)
                Cube("FloorBand_" + (i + 1), new Vector3(5.2f, -4.8f + i * 1.9f, 0.8f),
                    new Vector3(4.6f, 0.25f, 0.35f), i % 2 == 0 ? materials.copper : materials.teal, backdrop);
            CreatePiqueroVisual("Protagonist_Piquero", new Vector3(-5f, -3.87f, 0f),
                0.62f * CharacterScale,
                90f, backdrop, materials.playerModel);

            Canvas canvas = CreateCanvas("HUD_Launcher");
            Text title = UiText(canvas.transform, "Title", "NIDO CERO", new Vector2(0.5f, 0.82f),
                new Vector2(760f, 100f), Vector2.zero, 64, TextAnchor.MiddleCenter, new Color(0.92f, 0.78f, 0.38f));
            title.fontStyle = FontStyle.Bold;
            UiText(canvas.transform, "Subtitle", "TODO TIENE UN COSTO", new Vector2(0.5f, 0.72f),
                new Vector2(600f, 42f), Vector2.zero, 24, TextAnchor.MiddleCenter, Color.white);
            UiText(canvas.transform, "Description",
                "Un piquero. Seis pisos. Tres opciones que nunca regresan.\nBlockout funcional — Unity 6000.3.14f1",
                new Vector2(0.5f, 0.57f), new Vector2(680f, 90f), Vector2.zero, 20,
                TextAnchor.MiddleCenter, new Color(0.78f, 0.85f, 0.86f));

            Button start = UiButton(canvas.transform, "StartButton", "INICIAR ASCENSO",
                new Vector2(0.5f, 0.4f), new Vector2(320f, 64f), new Color(0.12f, 0.48f, 0.54f));
            SceneFlow startFlow = start.gameObject.AddComponent<SceneFlow>();
            startFlow.Configure("01_CinematicIntro", catalog, true);
            UnityEventTools.AddPersistentListener(start.onClick, startFlow.Go);

            Button quit = UiButton(canvas.transform, "QuitButton", "SALIR",
                new Vector2(0.5f, 0.29f), new Vector2(220f, 50f), new Color(0.32f, 0.2f, 0.18f));
            SceneFlow quitFlow = quit.gameObject.AddComponent<SceneFlow>();
            UnityEventTools.AddPersistentListener(quit.onClick, quitFlow.Quit);

            UiPanel(canvas.transform, "LauncherInfo", new Vector2(0.02f, 0.02f), new Vector2(0.31f, 0.18f),
                new Color(0.02f, 0.04f, 0.05f, 0.86f));
            UiText(canvas.transform, "Info", "A/D  MOVER   SHIFT  CORRER\nESPACIO  SALTAR   MOUSE  APUNTAR\nCLICK  DISPARAR   E  INTERACTUAR",
                new Vector2(0.165f, 0.1f), new Vector2(350f, 95f), Vector2.zero, 15,
                TextAnchor.MiddleCenter, new Color(0.65f, 0.78f, 0.8f));
            AddEventSystem();
            SaveScene(scene, "00_Launcher");
        }

        private static void BuildCinematic(GameCatalog catalog, MaterialLibrary materials, bool intro)
        {
            Scene scene = NewScene();
            AddSession(catalog, false);
            Camera camera = CreateCamera(intro ? new Color(0.035f, 0.09f, 0.11f) : new Color(0.08f, 0.04f, 0.035f),
                6f, Vector3.zero);
            CreateLight();

            Transform set = new GameObject(intro ? "Intro_Blockout" : "Ending_Blockout").transform;
            Cube("RuinGround", new Vector3(0f, -4.7f, 2f), new Vector3(22f, 1f, 3f), materials.dark, set);
            for (int i = 0; i < 9; i++)
            {
                float height = 1.5f + (i % 4) * 1.2f;
                Cube("Ruin_" + i, new Vector3(-9f + i * 2.2f, -4.2f + height * 0.5f, 2f),
                    new Vector3(1.1f, height, 1.4f), i % 3 == 0 ? materials.copper : materials.metal, set);
            }
            if (intro)
            {
                CreatePiqueroVisual("Piquero", new Vector3(-5f, -3.89f, 0f),
                    0.58f * CharacterScale,
                    90f, set, materials.playerModel);
            }
            else
            {
                CreatePiqueroVisual("Piquero", new Vector3(-3f, -3.89f, 0f),
                    0.58f * CharacterScale,
                    90f, set, materials.playerModel);
                Sphere("Nido", new Vector3(0f, -3.2f, 0f), new Vector3(2.2f, 1.2f, 1f),
                    materials.green, set, false);
            }
            if (intro)
                Cube("TowerDistant", new Vector3(6f, 0.5f, 2f), new Vector3(4f, 11f, 2f), materials.dark, set);
            else
                Sphere("DyingCore", new Vector3(5.5f, -1f, 1f), Vector3.one * 2f, materials.warning, set, false);

            Canvas canvas = CreateCanvas(intro ? "HUD_CinematicIntro" : "HUD_CinematicEnd");
            UiText(canvas.transform, "SequenceLabel", intro ? "SECUENCIA 01 — LA TORRE" : "SECUENCIA FINAL — EL COSTO",
                new Vector2(0.5f, 0.93f), new Vector2(700f, 38f), Vector2.zero, 18,
                TextAnchor.MiddleCenter, new Color(0.75f, 0.78f, 0.72f));
            GameObject subtitlePanel = UiPanel(canvas.transform, "SubtitlePanel", new Vector2(0.1f, 0.04f),
                new Vector2(0.9f, 0.22f), new Color(0f, 0f, 0f, 0.78f));
            Text subtitle = UiText(subtitlePanel.transform, "Subtitle", "", new Vector2(0.5f, 0.57f),
                new Vector2(850f, 86f), Vector2.zero, 24, TextAnchor.MiddleCenter, Color.white);
            Slider progress = UiSlider(canvas.transform, "Progress", new Vector2(0.5f, 0.025f),
                new Vector2(760f, 8f), new Color(0.82f, 0.5f, 0.16f));
            UiText(canvas.transform, "Skip", "ESPACIO / ESC — OMITIR", new Vector2(0.88f, 0.95f),
                new Vector2(230f, 28f), Vector2.zero, 13, TextAnchor.MiddleRight, new Color(0.65f, 0.7f, 0.7f));

            CinematicController controller = new GameObject("CinematicController").AddComponent<CinematicController>();
            string[] lines = intro
                ? new[]
                {
                    "Cuando los humanos desaparecieron, sus órdenes siguieron vivas.",
                    "Núcleo Cero convirtió Galápagos en una torre de captura.",
                    "George confía la vida restante a Azul, una piquero de patas azules.",
                    "Toda mejora tendrá un costo. Toda elección dejará algo atrás."
                }
                : new[]
                {
                    "Azul sacrifica cada mejora y abre los hábitats.",
                    "Raíces rompen el metal. El mar vuelve a circular.",
                    "Núcleo escapa bajo la muralla, hacia el mar.",
                    "Una voz más humana ríe: Esto apenas empieza."
                };
            controller.Configure(lines, subtitle, progress, intro ? "02_MainScene" : "00_Launcher", 3.2f);
            AddEventSystem();
            SaveScene(scene, intro ? "01_CinematicIntro" : "03_CinematicEnd");
        }

        private static void BuildMain(GameCatalog catalog, MaterialLibrary materials)
        {
            EnemyDefinition[] enemyDefinitions =
            {
                LoadRequired<EnemyDefinition>(DataRoot + "/Enemy_Walker.asset"),
                LoadRequired<EnemyDefinition>(DataRoot + "/Enemy_Flyer.asset"),
                LoadRequired<EnemyDefinition>(DataRoot + "/Enemy_Tank.asset")
            };
            FloorDefinition[] floorDefinitions = new FloorDefinition[FloorCount];
            for (int i = 0; i < floorDefinitions.Length; i++)
                floorDefinitions[i] =
                    LoadRequired<FloorDefinition>(DataRoot + "/Floor_" + (i + 1).ToString("00") + ".asset");

            Scene scene = NewScene();
            AddSession(catalog, true);
            Camera camera = CreateCamera(new Color(0.025f, 0.055f, 0.065f), 3f,
                new Vector3(-9f, TopFloorBaseY + CorridorHeight * 0.5f, -20f));
            camera.rect = new Rect(0f, 0.09f, 1f, 0.79f);
            CreateLight();

            Transform world = new GameObject("WORLD_BLOCKOUT").transform;
            Transform floorsRoot = new GameObject("Floors_03_to_00").transform;
            floorsRoot.SetParent(world);
            Transform enemiesRoot = new GameObject("Enemies_GDD").transform;
            enemiesRoot.SetParent(world);
            Transform gatesRoot = new GameObject("Descending_Transitions").transform;
            gatesRoot.SetParent(world);
            Transform essentialRoot = new GameObject("Essential_Narrative_Blockout").transform;
            essentialRoot.SetParent(world);

            GameObject playerObject = new GameObject("Player_Capsule_Piquero");
            playerObject.name = "Player_Capsule_Piquero";
            playerObject.transform.position =
                new Vector3(-14f, TopFloorBaseY + PlayerStandingOffset, 0f);
            CapsuleCollider playerCollider = playerObject.AddComponent<CapsuleCollider>();
            playerCollider.direction = 1;
            playerCollider.center = Vector3.zero;
            playerCollider.radius = 0.39f * CharacterScale;
            playerCollider.height = 2.1f * CharacterScale;
            Rigidbody playerBody = playerObject.AddComponent<Rigidbody>();
            playerBody.mass = 1.2f;
            playerBody.interpolation = RigidbodyInterpolation.Interpolate;
            PlayerController player = playerObject.AddComponent<PlayerController>();
            // The rig bounds include non-visible influence below the rendered feet.
            // Keep physics grounded at y=0.5 and offset only the visual so the claws meet the floor.
            GameObject playerVisual = CreatePiqueroVisual("Piquero_Visual",
                new Vector3(0f, -1.05f * CharacterScale, 0f),
                0.72f * CharacterScale, 90f, playerObject.transform, materials.playerModel);
            player.ConfigureVisual(playerVisual.transform);
            CameraFollow cameraFollow = camera.gameObject.AddComponent<CameraFollow>();
            cameraFollow.SetTarget(playerObject.transform);
            cameraFollow.ConfigureLayout(CorridorHeight, CorridorHeight * 0.5f, CorridorLength * 0.5f, 3);

            // Five solid bands form four stacked rooms. Clear corridor height is 4 units and
            // playable length is 32 units, preserving the requested 5600:700 (8:1) proportion.
            Cube("Ceiling_Piso_3_Blocking",
                new Vector3(0f, TopFloorBaseY + CorridorHeight, 0f),
                new Vector3(CorridorLength, 1f, 2f), materials.dark, floorsRoot);
            for (int sequence = 0; sequence < FloorCount; sequence++)
            {
                int physicalFloor = 3 - sequence;
                float y = TopFloorBaseY - sequence * CorridorHeight;
                Material floorMaterial = sequence == 0 ? materials.teal :
                    sequence == 1 ? materials.green :
                    sequence == 2 ? materials.copper : materials.dark;
                Cube("Floor_" + physicalFloor + "_Blocking", new Vector3(0f, y, 0f),
                    new Vector3(CorridorLength, 1f, 2f), floorMaterial, floorsRoot);
                if (physicalFloor > 0)
                {
                    // A thin non-physical skirt closes rig bounds at the underside of each ceiling
                    // without changing the established walkable surface or collider height.
                    Cube("CeilingSkirt_Piso_" + (physicalFloor - 1),
                        new Vector3(0f, y - 0.565f, -0.01f),
                        new Vector3(CorridorLength, 0.13f, 2.04f),
                        floorMaterial, floorsRoot, false);
                }

                float direction = sequence % 2 == 0 ? 1f : -1f;
                float startX = -direction * 14f;
                TextMesh floorLabel = WorldText("PISO " + physicalFloor + " · " +
                                                floorDefinitions[sequence].displayName.ToUpperInvariant(),
                    new Vector3(startX * 0.58f, y + 4.05f, -0.9f), 0.032f, 42,
                    new Color(0.83f, 0.78f, 0.55f), TextAnchor.MiddleCenter);
                floorLabel.name = "FloorLabel_" + physicalFloor;
                floorLabel.transform.SetParent(essentialRoot);

                CreateCheckpoint(sequence, new Vector3(startX, y + 0.8f, 0f), materials.key, floorsRoot);
                CreateFloorMarker(sequence, physicalFloor, new Vector3(startX, y + 1.8f, 0f), floorsRoot);

                if (sequence == 0)
                {
                    CreateEnemy("P3_FRAGA_TUTORIAL", enemyDefinitions[(int)EnemyArchetype.Flyer],
                        sequence, true, true, new Vector3(-1f, y + 2.85f, 0f), enemiesRoot, materials,
                        "george_first_choice");
                    CreateWiseTurtle(new Vector3(10.4f, y + 0.5f, 0f), essentialRoot, materials);
                }
                else if (sequence == 1)
                {
                    float[] crabX = { 8f, 0f, -8f };
                    for (int crab = 0; crab < crabX.Length; crab++)
                    {
                        bool finalCrab = crab == crabX.Length - 1;
                        CreateEnemy("P2_CANGRE_" + (crab + 1),
                            enemyDefinitions[(int)EnemyArchetype.Walker], sequence,
                            finalCrab, true,
                            new Vector3(crabX[crab],
                                y + 0.5f + 2.175f * CharacterScale * 0.5f, 0f),
                            enemiesRoot, materials);
                    }
                    CreateRescueCage(new Vector3(-11.25f, y + 0.5f, 0f), essentialRoot, materials);
                }
                else if (sequence == 2)
                {
                    CreateEnemy("P1_FRAGA_LLAVE_DORADA", enemyDefinitions[(int)EnemyArchetype.Flyer],
                        sequence, true, true, new Vector3(-3.5f, y + 2.85f, 0f), enemiesRoot, materials);
                    CreateEnemy("P1_TORTU_TANK", enemyDefinitions[(int)EnemyArchetype.Tank],
                        sequence, false, true,
                        new Vector3(7.8f,
                            y + 0.5f + 3.2f * CharacterScale * 0.5f, 0f),
                        enemiesRoot, materials);
                }

                if (sequence < FloorCount - 1)
                    CreateDescendingTransition(sequence, physicalFloor, y, direction, gatesRoot, materials);
            }

            // The only corridor ends without an elevator connection are the run's entrance
            // and the far side of the boss floor. Solid full-height walls prevent falls there.
            CreateEndWall("BoundaryWall_Piso_3_Start", -1f, TopFloorBaseY, floorsRoot, materials);
            CreateEndWall("BoundaryWall_Piso_0_End", -1f, 0f, floorsRoot, materials);

            CreateStructuralTileFaces(playerObject.transform, floorsRoot, materials);
            CreateBossArena(0f, materials, world);

            GameObject kill = new GameObject("KillZone");
            kill.transform.position = new Vector3(0f, -6f, 0f);
            BoxCollider killCollider = kill.AddComponent<BoxCollider>();
            killCollider.isTrigger = true;
            killCollider.size = new Vector3(100f, 2f, 8f);
            kill.AddComponent<KillZone>();
            kill.transform.SetParent(world);

            BuildMainHud(camera, player, catalog, materials);
            AddEventSystem();
            SaveScene(scene, "02_MainScene");
        }

        private static void CreateEnemy(string id, EnemyDefinition definition, int floor, bool carriesKey,
            bool triggersChoice, Vector3 position, Transform parent, MaterialLibrary materials,
            string choiceIdOverride = null)
        {
            string enemyName = "Robot_" + definition.archetype + "_" + id;
            GameObject enemy = new GameObject(enemyName);
            enemy.transform.SetParent(parent);
            enemy.transform.position = position;

            BoxCollider enemyCollider = enemy.AddComponent<BoxCollider>();
            enemyCollider.size = definition.archetype == EnemyArchetype.Tank
                ? new Vector3(5.2f, 3.2f, 4f) * CharacterScale
                : definition.archetype == EnemyArchetype.Flyer
                    ? new Vector3(2.34f, 1.43f, 1.95f) * CharacterScale
                    : new Vector3(3.3f, 2.175f, 2.55f) * CharacterScale;

            if (definition.archetype == EnemyArchetype.Tank)
            {
                CreateRiggedModelVisual(TurtleModelPath, "Tortuga_Visual",
                    new Vector3(0f, -1.60f * CharacterScale, 0f),
                    new Vector3(1.332f, 1.074f, 1.736f) * CharacterScale,
                    90f, enemy.transform, materials.turtleModel);
            }
            else if (definition.archetype == EnemyArchetype.Flyer)
            {
                CreateRiggedModelVisual(FlyerModelPath, "Fragata_Visual",
                    new Vector3(0f, -0.65f * CharacterScale, 0f),
                    new Vector3(0.949f, 0.741f, 1.2584f) * CharacterScale,
                    90f, enemy.transform, materials.flyerModel, 90f);
            }
            else
            {
                CreateRiggedModelVisual(CrabModelPath, "Cangrejo_Visual",
                    new Vector3(0f, -1.10f * CharacterScale, 0f),
                    new Vector3(0.8325f, 0.9795f, 0.954f) * CharacterScale,
                    90f, enemy.transform, materials.crabModel);
            }

            Rigidbody body = enemy.AddComponent<Rigidbody>();
            body.mass = definition.archetype == EnemyArchetype.Tank ? 4f : 1f;
            RobotEnemy robot = enemy.AddComponent<RobotEnemy>();
            robot.Configure(id, definition, floor, carriesKey, triggersChoice, choiceIdOverride);
        }

        private static GameObject CreatePiqueroVisual(string name, Vector3 position, float scale, float facingY,
            Transform parent, Material material)
        {
            return CreateRiggedModelVisual(PlayerModelPath, name, position, Vector3.one * scale,
                facingY, parent, material);
        }

        private static GameObject CreateRiggedModelVisual(string modelPath, string name, Vector3 position,
            Vector3 scale, float facingY, Transform parent, Material material, float tiltX = 0f)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null)
                throw new InvalidOperationException("Could not load optimized rig: " + modelPath);

            Scene destinationScene = parent != null ? parent.gameObject.scene : SceneManager.GetActiveScene();
            GameObject visual = PrefabUtility.InstantiatePrefab(source, destinationScene) as GameObject;
            if (visual == null)
                throw new InvalidOperationException("Could not instantiate optimized rig: " + modelPath);

            visual.name = name;
            if (parent != null) visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localRotation = Quaternion.Euler(tiltX, facingY, 0f);
            visual.transform.localScale = scale;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.updateWhenOffscreen = false;
            return visual;
        }

        private static void CreateCheckpoint(int floor, Vector3 position, Material material, Transform parent)
        {
            GameObject checkpoint = Cylinder("Checkpoint_" + (floor + 1), position,
                new Vector3(0.7f, 0.12f, 0.7f), material, parent, false);
            CapsuleCollider trigger = checkpoint.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;
            trigger.height = 3f;
            Checkpoint component = checkpoint.AddComponent<Checkpoint>();
            component.Configure(floor, checkpoint.GetComponent<Renderer>());
        }

        private static void CreateFloorMarker(int floor, int physicalFloor, Vector3 position, Transform parent)
        {
            GameObject marker = new GameObject("FloorMarker_" + physicalFloor);
            marker.transform.position = position;
            marker.transform.SetParent(parent);
            BoxCollider trigger = marker.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(4f, 4f, 3f);
            marker.AddComponent<FloorMarker>().Configure(floor);
        }

        private static void CreateDescendingTransition(int floor, int physicalFloor, float baseY, float side,
            Transform parent, MaterialLibrary materials)
        {
            float gateX = side * 15.65f;
            int destinationFloor = Mathf.Max(0, physicalFloor - 1);
            GameObject gate = Cube("Gate_Piso_" + physicalFloor, new Vector3(gateX, baseY + 2.5f, 0f),
                new Vector3(0.7f, 4f, 2f), materials.warning, parent);
            string closedLabel = "ASCENSOR A PISO " + destinationFloor;
            TextMesh label = WorldText(closedLabel,
                new Vector3(gateX - side * 1.25f, baseY + 3.75f, -1.1f),
                0.065f, 42, Color.white, TextAnchor.MiddleCenter);
            label.transform.SetParent(parent);

            GameObject zone = new GameObject("GateUnlockZone_Piso_" + physicalFloor);
            zone.transform.position = new Vector3(gateX - side * 1.2f, baseY + 1.4f, 0f);
            zone.transform.SetParent(parent);
            BoxCollider trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.2f, 2.8f, 3f);
            string requiredChoice = floor == 0 ? "george_first_choice" :
                floor == 1 ? "enemy_drop_P2_CANGRE_3" :
                "enemy_drop_P1_TORTU_TANK";
            GateUnlockZone gateZone = zone.AddComponent<GateUnlockZone>();
            gateZone.Configure(
                floor, gate, label, closedLabel, requiredChoice,
                floor == 0
                    ? WiseTurtleInteraction.MissionStoryFlag
                    : floor == 1
                        ? RescueCageController.StoryFlag
                        : null);

            GameObject arrivalBarrier = Cube("ArrivalBarrier_Piso_" + destinationFloor,
                new Vector3(gateX, baseY - CorridorHeight + 2.5f, 0f),
                new Vector3(0.7f, 4f, 2f), materials.warning, parent);

            float elevatorX = side * 17.2f;
            GameObject elevator = new GameObject("Elevator_Piso_" + physicalFloor + "_to_" + destinationFloor);
            elevator.transform.position = new Vector3(elevatorX, baseY, 0f);
            elevator.transform.SetParent(parent);
            Rigidbody elevatorBody = elevator.AddComponent<Rigidbody>();
            elevatorBody.isKinematic = true;
            elevatorBody.useGravity = false;
            elevatorBody.interpolation = RigidbodyInterpolation.Interpolate;

            Cube("ElevatorPlatform_Piso_" + physicalFloor,
                new Vector3(elevatorX, baseY, 0f),
                new Vector3(2.4f, 1f, 2f), materials.metal, elevator.transform);
            Cube("ElevatorOuterWall_Piso_" + physicalFloor,
                new Vector3(elevatorX + side * 1.2f, baseY + 2.5f, 0f),
                new Vector3(0.35f, 4f, 2f), materials.warning, elevator.transform);
            Cube("ElevatorCanopy_Piso_" + physicalFloor,
                new Vector3(elevatorX, baseY + 4.35f, 0f),
                new Vector3(2.4f, 0.3f, 2f), materials.metal, elevator.transform);
            Cube("ElevatorBackPanel_Piso_" + physicalFloor,
                new Vector3(elevatorX, baseY + 2.45f, 0.92f),
                new Vector3(2.4f, 3.7f, 0.12f), materials.dark, elevator.transform, false);

            GameObject boardingTrigger = new GameObject("ElevatorBoardingTrigger_Piso_" + physicalFloor);
            boardingTrigger.transform.position =
                new Vector3(elevatorX, baseY + PlayerStandingOffset, 0f);
            boardingTrigger.transform.SetParent(elevator.transform);
            BoxCollider boardingCollider = boardingTrigger.AddComponent<BoxCollider>();
            boardingCollider.isTrigger = true;
            boardingCollider.size = new Vector3(1.85f, 2.2f, 2.4f);

            TextMesh elevatorStatus = WorldText("SUBE AL ASCENSOR",
                new Vector3(elevatorX, baseY + 3.6f, -1.1f),
                0.048f, 42, Color.white, TextAnchor.MiddleCenter);
            elevatorStatus.name = "ElevatorStatus_Piso_" + physicalFloor;
            elevatorStatus.transform.SetParent(elevator.transform);

            DescendingElevatorController controller =
                elevator.AddComponent<DescendingElevatorController>();
            controller.Configure(floor, floor + 1, CorridorHeight,
                side * 14f, gateZone, arrivalBarrier, elevatorStatus);
        }

        private static void CreateEndWall(string name, float side, float baseY,
            Transform parent, MaterialLibrary materials)
        {
            Cube(name, new Vector3(side * 15.65f, baseY + 2.5f, 0f),
                new Vector3(0.7f, 4f, 2f), materials.dark, parent);
        }

        private static void CreateWiseTurtle(Vector3 position, Transform parent, MaterialLibrary materials)
        {
            Transform george = new GameObject("Mentor_Tortuga_Sabia").transform;
            george.SetParent(parent);
            george.position = position;

            CapsuleCollider collider = george.gameObject.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.center = new Vector3(0f, 0.94f * CharacterScale, 0f);
            collider.radius = 0.58f * CharacterScale;
            collider.height = 1.88f * CharacterScale;
            Rigidbody body = george.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            SphereCollider interactionTrigger = george.gameObject.AddComponent<SphereCollider>();
            interactionTrigger.isTrigger = true;
            interactionTrigger.center = new Vector3(0f, 0.95f * CharacterScale, 0f);
            interactionTrigger.radius = 2.35f;

            CreateRiggedModelVisual(WiseTurtleModelPath, "Tortuga_Sabia_Visual",
                Vector3.zero, Vector3.one * (1.1f * CharacterScale), -90f,
                george, materials.wiseTurtleModel);

            TextMesh label = WorldText("TORTUGA SABIA",
                position + new Vector3(0f, 2.25f * CharacterScale, -0.9f), 0.045f, 36,
                new Color(0.85f, 1f, 0.8f), TextAnchor.MiddleCenter);
            label.transform.SetParent(parent);
            george.gameObject.AddComponent<WiseTurtleInteraction>().Configure(
                label, "george_first_choice", 0);
        }

        private static void CreateRescueCage(Vector3 position, Transform parent, MaterialLibrary materials)
        {
            Transform cage = new GameObject("Rescue_Cage_Piso_2").transform;
            cage.SetParent(parent);
            cage.position = position;

            BoxCollider collider = cage.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.67f * CharacterScale, 0f);
            collider.size = new Vector3(1.82f, 3.34f, 1.82f) * CharacterScale;
            Rigidbody body = cage.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            GameObject cageVisual = CreateRiggedModelVisual(RescueCageModelPath, "Rescue_Cage_Visual",
                new Vector3(0f, 1.668f * CharacterScale, 0f),
                Vector3.one * (1.75f * CharacterScale), 0f,
                cage, materials.rescueCageModel);

            TextMesh label = WorldText("ANIMAL POR LIBERAR",
                position + new Vector3(0f, 3.72f * CharacterScale, -0.92f), 0.04f, 34,
                new Color(1f, 0.82f, 0.3f), TextAnchor.MiddleCenter);
            label.transform.SetParent(parent);
            cage.gameObject.AddComponent<RescueCageController>().Configure(
                label, collider, cageVisual.transform);
        }

        private static void CreateChoicePickup(string choiceId, Vector3 position, Transform parent,
            Material material)
        {
            GameObject pickup = Cube("CardDrop_" + choiceId, position, Vector3.one * 0.72f,
                material, parent, true);
            pickup.GetComponent<BoxCollider>().isTrigger = true;
            pickup.AddComponent<CardDropPickup>().Configure(choiceId);
            TextMesh label = WorldText("DECISIÓN", position + new Vector3(0f, 0.8f, -0.55f),
                0.055f, 36, Color.white, TextAnchor.MiddleCenter);
            label.name = "PickupLabel";
            label.transform.SetParent(pickup.transform);
        }

        private static void CreateBossArena(float baseY, MaterialLibrary materials, Transform parent)
        {
            Transform bossRoot = new GameObject("Boss_Arena").transform;
            bossRoot.SetParent(parent);

            TextMesh title = WorldText("NÚCLEO CERO · ROMPE LOS 3 RELÉS",
                new Vector3(0f, baseY + 4.05f, -0.8f), 0.095f, 48,
                new Color(1f, 0.72f, 0.22f), TextAnchor.MiddleCenter);
            title.transform.SetParent(bossRoot);

            BossRelay[] relays = new BossRelay[3];
            float[] relayX = { -8f, 0f, 8f };
            for (int i = 0; i < relays.Length; i++)
            {
                GameObject relay = Sphere("Relay_" + (i + 1), new Vector3(relayX[i], baseY + 2.3f, 0f),
                    Vector3.one * 1.35f, materials.purple, bossRoot, true);
                Rigidbody body = relay.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                relays[i] = relay.AddComponent<BossRelay>();
                relays[i].Configure(relay.GetComponent<Renderer>(), (ElementId)i);
            }

            GameObject coreObject = new GameObject("AI_Core_BOSS_FINAL");
            coreObject.transform.SetParent(bossRoot);
            coreObject.transform.position =
                new Vector3(0f, baseY + 0.5f + 4.2f * CharacterScale * 0.5f, 0f);
            BoxCollider coreCollider = coreObject.AddComponent<BoxCollider>();
            coreCollider.size = new Vector3(3.8f, 4.2f, 2.8f) * CharacterScale;
            GameObject bossVisual = CreateRiggedModelVisual(BossModelPath, "BOSS-FINAL-RIG",
                new Vector3(0f, -2.10f * CharacterScale, 0f),
                new Vector3(1.543f, 1.832f, 2.477f) * CharacterScale,
                90f, coreObject.transform, materials.bossModel);
            Rigidbody coreBody = coreObject.AddComponent<Rigidbody>();
            coreBody.isKinematic = true;
            coreBody.useGravity = false;
            BossCore core = coreObject.AddComponent<BossCore>();
            core.Configure(bossVisual.GetComponentInChildren<SkinnedMeshRenderer>(true), coreCollider);

            TextMesh status = WorldText("RELÉS ACTIVOS: 3", new Vector3(0f, baseY + 3.62f, -1f), 0.065f, 42,
                Color.white, TextAnchor.MiddleCenter);
            status.transform.SetParent(bossRoot);
            BossEncounter encounter = bossRoot.gameObject.AddComponent<BossEncounter>();
            encounter.Configure(relays, core, status);

            GameObject arenaTrigger = new GameObject("BossArenaTrigger");
            arenaTrigger.transform.SetParent(bossRoot);
            arenaTrigger.transform.position = new Vector3(0f, baseY + 2f, 0f);
            BoxCollider triggerCollider = arenaTrigger.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(34f, 4f, 3f);
            arenaTrigger.AddComponent<BossArenaTrigger>().Configure(encounter);
        }

        private static void BuildMainHud(Camera camera, PlayerController player, GameCatalog catalog,
            MaterialLibrary materials)
        {
            Canvas canvas = CreateCanvas("HUD_MainScene");
            GameObject top = UiPanel(canvas.transform, "TopBar", new Vector2(0f, 0.88f), new Vector2(1f, 1f),
                new Color(0.025f, 0.045f, 0.05f, 0.91f));
            Text life = UiText(top.transform, "Life", "VIDA  5 / 5", new Vector2(0.08f, 0.7f),
                new Vector2(190f, 32f), Vector2.zero, 18, TextAnchor.MiddleLeft, new Color(0.95f, 0.48f, 0.38f));
            Text stamina = UiText(top.transform, "Stamina", "ENERGIA  100 / 100", new Vector2(0.08f, 0.27f),
                new Vector2(220f, 30f), Vector2.zero, 16, TextAnchor.MiddleLeft, new Color(0.3f, 0.75f, 0.95f));
            Text stats = UiText(top.transform, "Stats", "FUE 2   VEL 5   DEF 1   AGI 2", new Vector2(0.38f, 0.67f),
                new Vector2(390f, 30f), Vector2.zero, 17, TextAnchor.MiddleCenter, Color.white);
            Text elements = UiText(top.transform, "Elements", "AGUA 0   FUEGO 0   VEG 0", new Vector2(0.38f, 0.25f),
                new Vector2(390f, 30f), Vector2.zero, 16, TextAnchor.MiddleCenter, Color.white);
            Text floor = UiText(top.transform, "Floor", "PISO 3", new Vector2(0.73f, 0.68f),
                new Vector2(170f, 30f), Vector2.zero, 17, TextAnchor.MiddleCenter, new Color(0.95f, 0.8f, 0.38f));
            Text key = UiText(top.transform, "Key", "LLAVE: --", new Vector2(0.89f, 0.68f),
                new Vector2(160f, 30f), Vector2.zero, 17, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
            Text objective = UiText(top.transform, "Objective", "Encuentra al robot que custodia la llave",
                new Vector2(0.81f, 0.24f), new Vector2(390f, 30f), Vector2.zero, 14,
                TextAnchor.MiddleCenter, new Color(0.72f, 0.78f, 0.78f));

            Text crosshair = UiText(canvas.transform, "Crosshair", "+", new Vector2(0.5f, 0.5f),
                new Vector2(30f, 30f), Vector2.zero, 22, TextAnchor.MiddleCenter, Color.white);
            crosshair.fontStyle = FontStyle.Bold;
            GameObject controlsBar = UiPanel(canvas.transform, "ControlsBar", Vector2.zero,
                new Vector2(1f, 0.09f), new Color(0.018f, 0.035f, 0.04f, 0.97f));
            Text controls = UiText(controlsBar.transform, "Controls",
                "<color=#F5C451>[A / D]</color> MOVER     " +
                "<color=#F5C451>[SHIFT]</color> CORRER     " +
                "<color=#F5C451>[ESPACIO]</color> SALTAR     " +
                "<color=#F5C451>[CLICK IZQ.]</color> DISPARAR     " +
                "<color=#F5C451>[ESC]</color> PAUSA",
                new Vector2(0.5f, 0.5f), new Vector2(1180f, 44f), Vector2.zero, 18,
                TextAnchor.MiddleCenter, new Color(0.9f, 0.94f, 0.92f));
            controls.fontStyle = FontStyle.Bold;

            GameObject pause = UiPanel(canvas.transform, "PausePanel", new Vector2(0.32f, 0.32f),
                new Vector2(0.68f, 0.68f), new Color(0f, 0f, 0f, 0.9f));
            UiText(pause.transform, "PauseTitle", "PAUSA", new Vector2(0.5f, 0.68f),
                new Vector2(300f, 60f), Vector2.zero, 38, TextAnchor.MiddleCenter, Color.white);
            UiText(pause.transform, "PauseHint", "ESC para continuar", new Vector2(0.5f, 0.38f),
                new Vector2(300f, 40f), Vector2.zero, 18, TextAnchor.MiddleCenter, Color.gray);

            GameObject boss = UiPanel(canvas.transform, "BossPanel", new Vector2(0.24f, 0.78f),
                new Vector2(0.76f, 0.87f), new Color(0.12f, 0.02f, 0.04f, 0.88f));
            Text bossText = UiText(boss.transform, "BossText", "IA CENTRAL — RELÉS", new Vector2(0.5f, 0.68f),
                new Vector2(470f, 26f), Vector2.zero, 16, TextAnchor.MiddleCenter, Color.white);
            Slider bossSlider = UiSlider(boss.transform, "BossHealth", new Vector2(0.5f, 0.25f),
                new Vector2(470f, 12f), new Color(0.85f, 0.18f, 0.12f));
            Image damageFlash = UiPanel(canvas.transform, "DamageFlash", Vector2.zero, Vector2.one,
                new Color(0.78f, 0.02f, 0.025f, 0f)).GetComponent<Image>();
            damageFlash.raycastTarget = false;
            damageFlash.gameObject.SetActive(false);

            HudController hud = canvas.gameObject.AddComponent<HudController>();
            hud.Configure(life, stamina, stats, elements, floor, key, objective, pause, boss, bossText, bossSlider);
            hud.ConfigureDamageFlash(damageFlash);
            hud.BindPlayer(player);

            BuildCards(camera, canvas, materials);
        }

        private static void BuildCards(Camera camera, Canvas canvas, MaterialLibrary materials)
        {
            GameObject cardWorld = new GameObject("Card_Planes_World");
            cardWorld.transform.SetParent(camera.transform, false);
            cardWorld.transform.localPosition = new Vector3(0f, 0f, 10f);
            GameObject[] cardObjects = new GameObject[3];
            Renderer[] planes = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "CardPlane_" + (i + 1);
                quad.transform.SetParent(cardWorld.transform, false);
                quad.transform.localPosition = new Vector3(-4.2f + i * 4.2f, -0.1f, 0f);
                quad.transform.localScale = new Vector3(3.5f, 4.7f, 1f);
                quad.GetComponent<Renderer>().sharedMaterial =
                    i == 0 ? materials.teal : i == 1 ? materials.copper : materials.green;
                Collider collider = quad.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
                cardObjects[i] = quad;
                planes[i] = quad.GetComponent<Renderer>();
            }

            GameObject overlay = UiPanel(canvas.transform, "CardChoiceOverlay", Vector2.zero, Vector2.one,
                new Color(0.015f, 0.025f, 0.03f, 0.72f));
            UiText(overlay.transform, "DecisionTitle", "ELIGE UN COSTO — LAS OTRAS DOS CARTAS DESAPARECERÁN",
                new Vector2(0.5f, 0.91f), new Vector2(940f, 48f), Vector2.zero, 26,
                TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.36f));
            UiText(overlay.transform, "DecisionHint", "Haz click o presiona 1, 2 o 3",
                new Vector2(0.5f, 0.085f), new Vector2(500f, 35f), Vector2.zero, 16,
                TextAnchor.MiddleCenter, Color.white);

            Text[] titles = new Text[3];
            Text[] descriptions = new Text[3];
            Text[] values = new Text[3];
            GameObject[] proxyPanels = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                float minX = 0.09f + i * 0.31f;
                float maxX = minX + 0.26f;
                GameObject panel = UiPanel(overlay.transform, "CardClickArea_" + (i + 1),
                    new Vector2(minX, 0.17f), new Vector2(maxX, 0.84f), new Color(0.05f, 0.06f, 0.06f, 0.22f));
                proxyPanels[i] = panel;
                titles[i] = UiText(panel.transform, "Title", "CARTA " + (i + 1), new Vector2(0.5f, 0.82f),
                    new Vector2(280f, 72f), Vector2.zero, 23, TextAnchor.MiddleCenter, Color.white);
                titles[i].fontStyle = FontStyle.Bold;
                descriptions[i] = UiText(panel.transform, "Description", "Descripción", new Vector2(0.5f, 0.57f),
                    new Vector2(270f, 145f), Vector2.zero, 17, TextAnchor.MiddleCenter, Color.white);
                values[i] = UiText(panel.transform, "Values", "+1 STAT\n−1 STAT\n+1 ELEMENTO",
                    new Vector2(0.5f, 0.26f), new Vector2(260f, 120f), Vector2.zero, 21,
                    TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f));
            }

            CardChoiceController controller = canvas.gameObject.AddComponent<CardChoiceController>();
            controller.Configure(overlay, cardObjects, planes, titles, descriptions, values);
            for (int i = 0; i < proxyPanels.Length; i++)
                proxyPanels[i].AddComponent<CardButtonProxy>().Configure(i, controller);
        }

        private static void BuildValidation(GameCatalog catalog, MaterialLibrary materials)
        {
            Scene scene = NewScene();
            AddSession(catalog, true);
            CreateCamera(new Color(0.04f, 0.04f, 0.05f), 6f, Vector3.zero);
            CreateLight();
            Cube("ColliderCube", new Vector3(-4f, 0f, 0f), new Vector3(2f, 2f, 2f), materials.copper, null);
            Sphere("PhysicsSphere", new Vector3(0f, 2f, 0f), Vector3.one * 1.5f, materials.teal, null, true)
                .AddComponent<Rigidbody>();
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "CharacterCapsule";
            capsule.transform.position = new Vector3(4f, 0f, 0f);
            capsule.GetComponent<Renderer>().sharedMaterial = materials.player;
            capsule.AddComponent<Rigidbody>();

            Canvas canvas = CreateCanvas("HUD_DevValidation");
            UiText(canvas.transform, "Title", "99 — DEV VALIDATION", new Vector2(0.5f, 0.9f),
                new Vector2(650f, 55f), Vector2.zero, 34, TextAnchor.MiddleCenter, Color.white);
            UiText(canvas.transform, "Checklist",
                "✓ Cubos con BoxCollider\n✓ Esferas con SphereCollider + Rigidbody\n✓ Cápsula con CapsuleCollider + Rigidbody\n✓ Datos serializados: stats, cartas, enemigos y pisos\nEsta escena no forma parte del build final.",
                new Vector2(0.5f, 0.27f), new Vector2(700f, 170f), Vector2.zero, 20,
                TextAnchor.MiddleCenter, new Color(0.75f, 0.86f, 0.8f));
            SaveScene(scene, "99_DevValidation");
        }

        private static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void SaveScene(Scene scene, string name)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SceneRoot + "/" + name + ".unity"))
                throw new InvalidOperationException("Could not save scene " + name);
        }

        private static void AddSession(GameCatalog catalog, bool reset)
        {
            GameObject host = new GameObject("GameSession");
            GameSession session = host.AddComponent<GameSession>();
            session.Configure(catalog, reset);
            SerializedObject serialized = new SerializedObject(session);
            serialized.FindProperty("resetOnAwake").boolValue = reset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Camera CreateCamera(Color background, float size, Vector3 position)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.backgroundColor = background;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = position.z == 0f
                ? new Vector3(position.x, position.y, -20f)
                : position;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.86f, 0.7f);
            lightObject.transform.rotation = Quaternion.Euler(32f, -28f, 0f);
            RenderSettings.ambientLight = new Color(0.3f, 0.36f, 0.4f);
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material,
            Transform parent, bool keepCollider = true)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.position = position;
            result.transform.localScale = scale;
            if (parent != null) result.transform.SetParent(parent);
            result.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = result.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            }
            return result;
        }

        private static void CreateStructuralTileFaces(Transform player, Transform parent,
            MaterialLibrary materials)
        {
            GameObject root = new GameObject("Structural_Front_Tiles");
            root.transform.SetParent(parent);
            GameObject[] groups = new GameObject[FloorCount];

            for (int physicalFloor = 0; physicalFloor < FloorCount; physicalFloor++)
            {
                GameObject group = new GameObject("TileFaces_Piso_" + physicalFloor);
                group.transform.SetParent(root.transform);
                groups[physicalFloor] = group;
                float floorY = physicalFloor * CorridorHeight;

                TileFace("FloorTileFace_Piso_" + physicalFloor,
                    new Vector3(0f, floorY, -1.011f),
                    new Vector3(CorridorLength, 1f, 1f), materials.floorTile, group.transform);

                // Each room owns an independent, non-physical ceiling shell. It is deliberately
                // stacked at the same boundary as the floor above, but it never participates in
                // physics, so the established floor colliders and character grounding stay intact.
                Cube("Ceiling_Piso_" + physicalFloor + "_VisualShell",
                    new Vector3(0f, floorY + CorridorHeight, 0f),
                    new Vector3(CorridorLength, 1f, 2.02f), materials.dark, group.transform, false);
                TileFace("CeilingTileFace_Piso_" + physicalFloor,
                    new Vector3(0f, floorY + CorridorHeight, -1.021f),
                    new Vector3(CorridorLength, 1f, 1f), materials.ceilingTile, group.transform);
            }

            StructuralTileFaceController controller = root.AddComponent<StructuralTileFaceController>();
            controller.Configure(player, groups, CorridorHeight);
        }

        private static GameObject TileFace(string name, Vector3 position, Vector3 scale, Material material,
            Transform parent)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Quad);
            result.name = name;
            result.transform.position = position;
            result.transform.localScale = scale;
            result.transform.SetParent(parent);
            MeshRenderer renderer = result.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Collider collider = result.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return result;
        }

        private static GameObject Sphere(string name, Vector3 position, Vector3 scale, Material material,
            Transform parent, bool keepCollider)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            result.name = name;
            result.transform.position = position;
            result.transform.localScale = scale;
            if (parent != null) result.transform.SetParent(parent);
            result.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = result.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            }
            return result;
        }

        private static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material,
            Transform parent, bool keepCollider)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            result.name = name;
            result.transform.position = position;
            result.transform.localScale = scale;
            if (parent != null) result.transform.SetParent(parent);
            result.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = result.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            }
            return result;
        }

        private static TextMesh WorldText(string content, Vector3 position, float characterSize, int fontSize,
            Color color, TextAnchor anchor)
        {
            GameObject host = new GameObject("WorldText");
            host.transform.position = position;
            TextMesh text = host.AddComponent<TextMesh>();
            text.text = content;
            text.characterSize = characterSize;
            text.fontSize = fontSize;
            text.color = color;
            text.anchor = anchor;
            text.alignment = TextAlignment.Center;
            return text;
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject host = new GameObject(name);
            Canvas canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            host.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject UiPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static Text UiText(Transform parent, string name, string content, Vector2 anchor,
            Vector2 size, Vector2 position, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject host = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            host.transform.SetParent(parent, false);
            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Text text = host.GetComponent<Text>();
            text.font = RuntimeFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button UiButton(Transform parent, string name, string label, Vector2 anchor,
            Vector2 size, Color color)
        {
            GameObject host = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            host.transform.SetParent(parent, false);
            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            Image image = host.GetComponent<Image>();
            image.color = color;
            Button button = host.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.22f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            button.colors = colors;
            Text text = UiText(host.transform, "Label", label, new Vector2(0.5f, 0.5f),
                size, Vector2.zero, 20, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            return button;
        }

        private static Slider UiSlider(Transform parent, string name, Vector2 anchor, Vector2 size, Color fillColor)
        {
            GameObject host = new GameObject(name, typeof(RectTransform), typeof(Slider));
            host.transform.SetParent(parent, false);
            RectTransform rect = host.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            GameObject background = UiPanel(host.transform, "Background", Vector2.zero, Vector2.one,
                new Color(0.1f, 0.1f, 0.1f, 0.9f));
            GameObject fillArea = UiPanel(host.transform, "Fill Area", new Vector2(0.01f, 0.12f),
                new Vector2(0.99f, 0.88f), Color.clear);
            GameObject fill = UiPanel(fillArea.transform, "Fill", Vector2.zero, Vector2.one, fillColor);
            Slider slider = host.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            return slider;
        }

        private static void AddEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Font RuntimeFont()
        {
            if (runtimeFont == null)
                runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return runtimeFont;
        }

        [Serializable]
        private sealed class MaterialLibrary
        {
            public Material dark;
            public Material metal;
            public Material copper;
            public Material teal;
            public Material green;
            public Material ember;
            public Material player;
            public Material playerModel;
            public Material wiseTurtleModel;
            public Material rescueCageModel;
            public Material turtleModel;
            public Material crabModel;
            public Material flyerModel;
            public Material bossModel;
            public Material key;
            public Material white;
            public Material warning;
            public Material purple;
            public Material ceilingTile;
            public Material floorTile;
        }
    }
}
