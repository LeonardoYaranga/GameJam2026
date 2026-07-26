using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NidoCero.Tests
{
    public sealed class NidoCeroEditModeTests
    {
        private const string CatalogPath = "Assets/_Game/Generated/Data/GameCatalog.asset";
        private const string SceneRoot = "Assets/_Game/Scenes/";
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

        [Test]
        public void ElementalResolver_UsesApprovedExposureFormula()
        {
            ElementLevels defender = new ElementLevels { fire = 3, water = 1, vegetation = 0 };
            Assert.AreEqual(2, ElementalResolver.Bonus(ElementId.Water, 4, defender));
            Assert.AreEqual(1, ElementalResolver.Bonus(ElementId.Water, 1, defender));
            Assert.AreEqual(1f,
                ElementalResolver.DamageMultiplier(ElementId.Water, 0, ElementId.Fire, 3));
            Assert.Greater(
                ElementalResolver.DamageMultiplier(ElementId.Water, 2, ElementId.Fire, 2),
                1f);
            Assert.Less(
                ElementalResolver.DamageMultiplier(ElementId.Fire, 2, ElementId.Water, 2),
                1f);
            Assert.That(ElementalResolver.ProjectileColor(ElementId.Fire, 1).r,
                Is.GreaterThan(0.95f));
            Assert.That(ElementalResolver.ProjectileColor(ElementId.Water, 1).b,
                Is.GreaterThan(0.95f));
            Assert.That(ElementalResolver.ProjectileColor(ElementId.Vegetation, 1).g,
                Is.GreaterThan(0.85f));
            Assert.AreEqual(Color.white,
                ElementalResolver.ProjectileColor(ElementId.Water, 0));
        }

        [Test]
        public void CombatMath_UsesPercentagesStatsElementsAndMinimumThreeHitCap()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            Assert.NotNull(catalog);
            EnemyDefinition flyer = catalog.FindEnemy(EnemyArchetype.Flyer);
            EnemyDefinition tank = catalog.FindEnemy(EnemyArchetype.Tank);
            Assert.NotNull(flyer);
            Assert.NotNull(tank);

            RuntimeStats baseStats = new RuntimeStats();
            baseStats.ResetToCatalog(catalog);
            ElementLevels noBoost = new ElementLevels();
            float baseDamage = CombatMath.EnemyProjectileDamagePercent(
                baseStats, noBoost, ElementId.Water, 0, flyer);

            RuntimeStats boostedStats = new RuntimeStats();
            boostedStats.ResetToCatalog(catalog);
            boostedStats.strength = 8;
            ElementLevels boostedElements = new ElementLevels { water = 5, fire = 5 };
            float boostedDamage = CombatMath.EnemyProjectileDamagePercent(
                boostedStats, boostedElements, ElementId.Water, 5, flyer);
            float tankDamage = CombatMath.EnemyProjectileDamagePercent(
                boostedStats, boostedElements, ElementId.Fire, 5, tank);
            float resistedDamage = CombatMath.EnemyProjectileDamagePercent(
                boostedStats, boostedElements, ElementId.Fire, 5, flyer);

            Assert.Greater(boostedDamage, baseDamage);
            Assert.Greater(boostedDamage, resistedDamage);
            Assert.LessOrEqual(boostedDamage, CombatMath.MaximumRegularHitPercent);
            Assert.Less(2f * boostedDamage, 100f);
            Assert.LessOrEqual(tankDamage, CombatMath.MaximumRegularHitPercent);
            Assert.GreaterOrEqual(CombatMath.HitsToDefeat(baseDamage), 3);
            Assert.GreaterOrEqual(CombatMath.HitsToDefeat(tankDamage), 3);

            float offenseBase = CombatMath.PlayerOffensePower(baseStats);
            Assert.Greater(CombatMath.PlayerOffensePower(
                new RuntimeStats { strength = 3, speed = 5, defense = 1, agility = 2, life = 5, stamina = 100 }),
                offenseBase);
            Assert.Greater(CombatMath.PlayerOffensePower(
                new RuntimeStats { strength = 2, speed = 6, defense = 1, agility = 2, life = 5, stamina = 100 }),
                offenseBase);
            Assert.Greater(CombatMath.PlayerOffensePower(
                new RuntimeStats { strength = 2, speed = 5, defense = 2, agility = 2, life = 5, stamina = 100 }),
                offenseBase);
            Assert.Greater(CombatMath.PlayerOffensePower(
                new RuntimeStats { strength = 2, speed = 5, defense = 1, agility = 3, life = 5, stamina = 100 }),
                offenseBase);
            Assert.Greater(CombatMath.PlayerOffensePower(
                new RuntimeStats { strength = 2, speed = 5, defense = 1, agility = 2, life = 6, stamina = 100 }),
                offenseBase);
            Assert.Greater(CombatMath.PlayerOffensePower(
                new RuntimeStats { strength = 2, speed = 5, defense = 1, agility = 2, life = 5, stamina = 110 }),
                offenseBase);

            RuntimeStats vulnerablePlayer = new RuntimeStats { defense = 0, life = 1 };
            float vulnerableContact = CombatMath.PlayerIncomingDamagePercent(
                vulnerablePlayer, new ElementLevels(), ElementId.Vegetation, 2,
                EnemyArchetype.Tank, true);
            RuntimeStats resistantPlayer = new RuntimeStats { defense = 5, life = 9 };
            float resistantContact = CombatMath.PlayerIncomingDamagePercent(
                resistantPlayer, new ElementLevels { fire = 5 }, ElementId.Vegetation, 2,
                EnemyArchetype.Tank, true);

            Assert.LessOrEqual(vulnerableContact, CombatMath.MaximumRegularHitPercent);
            Assert.Less(2f * vulnerableContact, 100f);
            Assert.Less(resistantContact, vulnerableContact);

            float bossDamage = CombatMath.BossProjectileDamagePercent(
                boostedStats, boostedElements, ElementId.Water, 5, ElementId.Fire);
            Assert.LessOrEqual(bossDamage, CombatMath.MaximumBossHitPercent);
            float relayDamage = CombatMath.RelayProjectileDamagePercent(
                boostedStats, boostedElements, ElementId.Water, 5, ElementId.Fire, 3);
            Assert.LessOrEqual(relayDamage, CombatMath.MaximumRegularHitPercent);
            Assert.GreaterOrEqual(CombatMath.HitsToDefeat(relayDamage), 3);
        }

        [Test]
        public void RuntimeStats_ClampToCatalogBounds()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            Assert.NotNull(catalog);
            RuntimeStats stats = new RuntimeStats();
            stats.ResetToCatalog(catalog);
            stats.ApplyDelta(catalog, StatId.Life, 100);
            stats.ApplyDelta(catalog, StatId.Defense, -100);
            stats.ApplyDelta(catalog, StatId.Stamina, -1000);
            Assert.AreEqual(9, stats.life);
            Assert.AreEqual(0, stats.defense);
            Assert.AreEqual(50, stats.stamina);
        }

        [Test]
        public void Catalog_HasAllSerializedGameData()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            Assert.NotNull(catalog);
            Assert.AreEqual(6, catalog.stats.Length);
            Assert.AreEqual(3, catalog.elements.Length);
            Assert.AreEqual(18, catalog.cards.Length);
            Assert.AreEqual(3, catalog.enemies.Length);
            Assert.AreEqual(4, catalog.floors.Length);
            Assert.AreEqual(18, catalog.cards.Select(card => card.cardId).Distinct().Count());
            Assert.AreEqual("Cangre-Cam", catalog.FindEnemy(EnemyArchetype.Walker).displayName);
            Assert.AreEqual(ElementId.Water, catalog.FindEnemy(EnemyArchetype.Walker).element);
            Assert.AreEqual("Fraga-Dron", catalog.FindEnemy(EnemyArchetype.Flyer).displayName);
            Assert.AreEqual(ElementId.Fire, catalog.FindEnemy(EnemyArchetype.Flyer).element);
            Assert.AreEqual("Tortu-Tank", catalog.FindEnemy(EnemyArchetype.Tank).displayName);
            Assert.AreEqual(ElementId.Vegetation, catalog.FindEnemy(EnemyArchetype.Tank).element);
        }

        [Test]
        public void RuntimeCardOffers_HaveTwoBenefitsOneCostAndNoRepeatedStats()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            Assert.NotNull(catalog);
            RunState state = new RunState();
            state.Reset(catalog);

            foreach (ElementId element in System.Enum.GetValues(typeof(ElementId)))
            {
                RuntimeCardOffer offer = CardOfferGenerator.Generate(catalog, state, element, 2026);
                Assert.AreEqual(element, offer.element);
                Assert.Greater(offer.GetDelta(0), 0);
                Assert.Greater(offer.GetDelta(1), 0);
                Assert.Less(offer.GetDelta(2), 0);
                Assert.AreNotEqual(offer.GetStat(0), offer.GetStat(1));
                Assert.AreNotEqual(offer.GetStat(0), offer.GetStat(2));
                Assert.AreNotEqual(offer.GetStat(1), offer.GetStat(2));
            }

            RuntimeCardOffer firstWater = CardOfferGenerator.Generate(catalog, state, ElementId.Water, 404);
            state.retiredCards.Add(firstWater.sourceCardId);
            RuntimeCardOffer nextWater = CardOfferGenerator.Generate(catalog, state, ElementId.Water, 404);
            Assert.AreNotEqual(firstWater.sourceCardId, nextWater.sourceCardId);
        }

        [Test]
        public void LauncherAndMainScene_ContainIntegratedFunctionalUi()
        {
            EditorSceneManager.OpenScene(SceneRoot + "00_Launcher.unity");
            Assert.NotNull(Object.FindFirstObjectByType<MainMenuController>());
            Assert.NotNull(FindIncludingInactive("GameTitle"));
            Assert.NotNull(FindIncludingInactive("OptionsPanel"));

            EditorSceneManager.OpenScene(SceneRoot + "02_MainScene.unity");
            Assert.NotNull(Object.FindFirstObjectByType<HudController>());
            Assert.NotNull(Object.FindFirstObjectByType<CardChoiceController>());
            Assert.NotNull(Object.FindFirstObjectByType<DialogueController>());
            Assert.NotNull(Object.FindFirstObjectByType<FinalSacrificeController>());
            Assert.NotNull(FindIncludingInactive("LifeBarFill"));
            Assert.NotNull(FindIncludingInactive("StaminaBarFill"));
            Assert.NotNull(FindIncludingInactive("BottomShade"));
            Assert.NotNull(FindIncludingInactive("PauseButton"));
            Assert.NotNull(FindIncludingInactive("Card_1"));
            Assert.NotNull(FindIncludingInactive("Card_2"));
            Assert.NotNull(FindIncludingInactive("Card_3"));
        }

        [Test]
        public void Cinematics_FollowTheLiteraryOpeningAndEscapingCoreEnding()
        {
            EditorSceneManager.OpenScene(SceneRoot + "01_CinematicIntro.unity");
            CinematicController intro = Object.FindFirstObjectByType<CinematicController>();
            Assert.NotNull(intro);
            SerializedProperty introLines = new SerializedObject(intro).FindProperty("lines");
            Assert.AreEqual(4, introLines.arraySize);
            Assert.AreEqual(
                "Cuando los humanos desaparecieron, sus órdenes siguieron vivas.",
                introLines.GetArrayElementAtIndex(0).stringValue);
            StringAssert.Contains("George",
                introLines.GetArrayElementAtIndex(2).stringValue);

            EditorSceneManager.OpenScene(SceneRoot + "03_CinematicEnd.unity");
            CinematicController ending = Object.FindFirstObjectByType<CinematicController>();
            Assert.NotNull(ending);
            SerializedProperty endingLines = new SerializedObject(ending).FindProperty("lines");
            Assert.AreEqual(4, endingLines.arraySize);
            StringAssert.Contains("escapa",
                endingLines.GetArrayElementAtIndex(2).stringValue);
            StringAssert.Contains("Esto apenas empieza",
                endingLines.GetArrayElementAtIndex(3).stringValue);
        }

        [Test]
        public void MainScene_HasFourDescendingFloorsAndGddEnemyRoster()
        {
            EditorSceneManager.OpenScene(SceneRoot + "02_MainScene.unity");
            Assert.NotNull(Object.FindFirstObjectByType<PlayerController>());
            Assert.AreEqual(6, Object.FindObjectsByType<RobotEnemy>(FindObjectsSortMode.None).Length);
            Assert.NotNull(Object.FindFirstObjectByType<BossEncounter>());
            for (int floor = 3; floor >= 0; floor--)
            {
                GameObject block = GameObject.Find("Floor_" + floor + "_Blocking");
                Assert.NotNull(block, "Missing floor " + floor);
                Assert.NotNull(block.GetComponent<BoxCollider>());
                Assert.That(block.transform.localScale.x, Is.EqualTo(32f).Within(0.001f));
            }
            Assert.NotNull(GameObject.Find("Ceiling_Piso_3_Blocking"));
            GameObject wiseTurtle = GameObject.Find("Mentor_Tortuga_Sabia");
            Assert.NotNull(wiseTurtle);
            Assert.NotNull(wiseTurtle.GetComponent<WiseTurtleInteraction>());
            GameObject rescueCage = GameObject.Find("Rescue_Cage_Piso_2");
            Assert.NotNull(rescueCage);
            Assert.NotNull(rescueCage.GetComponent<RescueCageController>());
            Assert.AreEqual(3, Object.FindObjectsByType<BossRelay>(FindObjectsSortMode.None).Length);
            Assert.IsNull(GameObject.Find("Orthographic_Face_Proxies"));

            RobotEnemy[] enemies = Object.FindObjectsByType<RobotEnemy>(FindObjectsSortMode.None);
            Assert.AreEqual(3, enemies.Count(enemy => enemy.Definition.archetype == EnemyArchetype.Walker));
            Assert.AreEqual(2, enemies.Count(enemy => enemy.Definition.archetype == EnemyArchetype.Flyer));
            Assert.AreEqual(1, enemies.Count(enemy => enemy.Definition.archetype == EnemyArchetype.Tank));

            GateUnlockZone[] gates = Object.FindObjectsByType<GateUnlockZone>(FindObjectsSortMode.None);
            Assert.AreEqual(3, gates.Length);
            foreach (GateUnlockZone gate in gates)
            {
                SerializedObject serializedGate = new SerializedObject(gate);
                Assert.IsNotEmpty(serializedGate.FindProperty("requiredChoiceId").stringValue);
                if (gate.FloorIndex == 0)
                    Assert.AreEqual(WiseTurtleInteraction.MissionStoryFlag,
                        serializedGate.FindProperty("requiredStoryFlag").stringValue);
                if (gate.FloorIndex == 1)
                    Assert.AreEqual(RescueCageController.StoryFlag,
                        serializedGate.FindProperty("requiredStoryFlag").stringValue);
            }

            DescendingElevatorController[] elevators =
                Object.FindObjectsByType<DescendingElevatorController>(FindObjectsSortMode.None);
            Assert.AreEqual(3, elevators.Length);
            foreach (DescendingElevatorController elevator in elevators)
            {
                Assert.AreEqual(elevator.SourceFloorIndex + 1, elevator.DestinationFloorIndex);
                Assert.That(elevator.TravelDistance, Is.EqualTo(5f).Within(0.001f));
                Assert.NotNull(elevator.ArrivalBarrier);
            }
            Assert.IsNull(GameObject.Find("Descent_P3_1"));
            Assert.NotNull(GameObject.Find("BoundaryWall_Piso_3_Start"));
            Assert.NotNull(GameObject.Find("BoundaryWall_Piso_0_End"));

            BossRelay[] relays =
                Object.FindObjectsByType<BossRelay>(FindObjectsSortMode.None)
                    .OrderBy(relay => relay.name).ToArray();
            Assert.AreEqual(ElementId.Water, relays[0].Element);
            Assert.AreEqual(ElementId.Fire, relays[1].Element);
            Assert.AreEqual(ElementId.Vegetation, relays[2].Element);
        }

        [Test]
        public void MainScene_Uses720pSingleFloorFramingAndVisibleControls()
        {
            EditorSceneManager.OpenScene(SceneRoot + "02_MainScene.unity");

            Camera camera = Camera.main;
            Assert.NotNull(camera);
            Assert.IsTrue(camera.orthographic);
            Assert.That(camera.orthographicSize, Is.EqualTo(3f).Within(0.01f));
            Assert.That(camera.rect.y, Is.EqualTo(0.09f).Within(0.001f));
            Assert.That(camera.rect.height, Is.EqualTo(0.79f).Within(0.001f));

            GameObject floor = GameObject.Find("Floor_3_Blocking");
            GameObject ceiling = GameObject.Find("Ceiling_Piso_3_Blocking");
            Assert.NotNull(floor);
            Assert.NotNull(ceiling);
            Assert.That(ceiling.transform.position.y - floor.transform.position.y,
                Is.EqualTo(5f).Within(0.001f));

            StructuralTileFaceController tileController =
                Object.FindFirstObjectByType<StructuralTileFaceController>(FindObjectsInactive.Include);
            Assert.NotNull(tileController);
            for (int physicalFloor = 0; physicalFloor < 4; physicalFloor++)
            {
                GameObject group = FindIncludingInactive("TileFaces_Piso_" + physicalFloor);
                Assert.NotNull(group);
                Assert.AreEqual(physicalFloor == 3, group.activeSelf);

                Transform floorFace = group.transform.Find("FloorTileFace_Piso_" + physicalFloor);
                Transform ceilingShell = group.transform.Find("Ceiling_Piso_" + physicalFloor + "_VisualShell");
                Transform ceilingFace = group.transform.Find("CeilingTileFace_Piso_" + physicalFloor);
                Assert.NotNull(floorFace);
                Assert.NotNull(ceilingShell);
                Assert.NotNull(ceilingFace);
                Assert.AreEqual(new Vector3(32f, 1f, 1f), floorFace.localScale);
                Assert.AreEqual(new Vector3(32f, 1f, 2.02f), ceilingShell.localScale);
                Assert.AreEqual(new Vector3(32f, 1f, 1f), ceilingFace.localScale);
                Assert.IsNull(floorFace.GetComponent<Collider>());
                Assert.IsNull(ceilingShell.GetComponent<Collider>());
                Assert.IsNull(ceilingFace.GetComponent<Collider>());
                Assert.That(ceilingShell.position.y,
                    Is.EqualTo((physicalFloor + 1) * 5f).Within(0.001f));
                Assert.That(floorFace.position.z, Is.EqualTo(-1.011f).Within(0.001f));
                Assert.That(ceilingFace.position.z, Is.EqualTo(-1.021f).Within(0.001f));
                Assert.AreEqual("Unlit/Texture",
                    floorFace.GetComponent<MeshRenderer>().sharedMaterial.shader.name);
                Assert.AreEqual("Unlit/Texture",
                    ceilingFace.GetComponent<MeshRenderer>().sharedMaterial.shader.name);

                GameObject physicalFloorObject = GameObject.Find("Floor_" + physicalFloor + "_Blocking");
                Assert.NotNull(physicalFloorObject);
                Assert.NotNull(physicalFloorObject.GetComponent<BoxCollider>());
                Assert.AreEqual(new Vector3(32f, 1f, 2f), physicalFloorObject.transform.localScale);
                Assert.That(physicalFloorObject.transform.position.y,
                    Is.EqualTo(physicalFloor * 5f).Within(0.001f));
            }

            AssertRepeatTextureImport(CeilingTileTexturePath);
            AssertRepeatTextureImport(FloorTileTexturePath);

            TextMesh floorLabel = GameObject.Find("FloorLabel_3").GetComponent<TextMesh>();
            Assert.NotNull(floorLabel);
            Assert.LessOrEqual(floorLabel.characterSize, 0.08f);
            Assert.Less(floorLabel.transform.position.x, 0f);

            GameObject controlsBar = GameObject.Find("ControlsBar");
            GameObject controlsObject = GameObject.Find("Controls");
            GameObject damageFlash = FindIncludingInactive("DamageFlash");
            Assert.NotNull(controlsBar);
            Assert.NotNull(controlsObject);
            Assert.NotNull(damageFlash);
            Assert.IsFalse(damageFlash.activeSelf);
            Assert.IsFalse(damageFlash.GetComponent<Image>().raycastTarget);
            Text controls = controlsObject.GetComponent<Text>();
            Assert.NotNull(controls);
            StringAssert.Contains("[A / D]", controls.text);
            StringAssert.Contains("[ESPACIO]", controls.text);

            CanvasScaler scaler = Object.FindFirstObjectByType<CanvasScaler>();
            Assert.NotNull(scaler);
            Assert.AreEqual(new Vector2(1280f, 720f), scaler.referenceResolution);
            Assert.GreaterOrEqual(scaler.GetComponent<Canvas>().sortingOrder, 100);
        }

        [Test]
        public void MainScene_UsesRiggedPiqueroAndGuideProportions()
        {
            EditorSceneManager.OpenScene(SceneRoot + "02_MainScene.unity");

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            Assert.NotNull(player);
            CapsuleCollider playerCollider = player.GetComponent<CapsuleCollider>();
            Assert.NotNull(playerCollider);
            Assert.That(playerCollider.height, Is.EqualTo(2.1f).Within(0.01f));
            Assert.That(playerCollider.radius, Is.EqualTo(0.39f).Within(0.01f));
            Assert.AreEqual(Vector3.one, player.transform.localScale);

            Transform visual = player.transform.Find("Piquero_Visual");
            Assert.NotNull(visual);
            Assert.AreEqual(Vector3.one * 0.72f, visual.localScale);
            Assert.That(visual.localPosition.y, Is.EqualTo(-1.05f).Within(0.01f));
            SkinnedMeshRenderer skinned = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.NotNull(skinned);
            Assert.AreEqual(53, skinned.bones.Length);
            Assert.AreEqual("Standard", skinned.sharedMaterial.shader.name);

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
            Assert.NotNull(model);
            SkinnedMeshRenderer modelRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.NotNull(modelRenderer);
            long triangles = 0;
            for (int subMesh = 0; subMesh < modelRenderer.sharedMesh.subMeshCount; subMesh++)
                triangles += (long)modelRenderer.sharedMesh.GetIndexCount(subMesh) / 3;
            Assert.LessOrEqual(triangles, 10000);

            GameObject walker = GameObject.Find("Robot_Walker_P2_CANGRE_1");
            Assert.NotNull(walker);
            Assert.AreEqual(Vector3.one, walker.transform.localScale);
            Assert.AreEqual(new Vector3(3.3f, 2.175f, 2.55f), walker.GetComponent<BoxCollider>().size);
            Assert.IsNull(walker.GetComponent<SphereCollider>());
            Transform crabVisual = walker.transform.Find("Cangrejo_Visual");
            Assert.NotNull(crabVisual);
            Assert.AreEqual(new Vector3(0.8325f, 0.9795f, 0.954f), crabVisual.localScale);
            Assert.That(crabVisual.localPosition.y, Is.EqualTo(-1.10f).Within(0.01f));
            Assert.AreEqual(44, crabVisual.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length);

            GameObject flyer = GameObject.Find("Robot_Flyer_P3_FRAGA_TUTORIAL");
            Assert.NotNull(flyer);
            Assert.AreEqual(Vector3.one, flyer.transform.localScale);
            Assert.AreEqual(new Vector3(2.34f, 1.43f, 1.95f), flyer.GetComponent<BoxCollider>().size);
            Assert.IsNull(flyer.GetComponent<SphereCollider>());
            Transform flyerVisual = flyer.transform.Find("Fragata_Visual");
            Assert.NotNull(flyerVisual);
            Assert.AreEqual(new Vector3(0.949f, 0.741f, 1.2584f), flyerVisual.localScale);
            Assert.That(flyerVisual.localPosition.y, Is.EqualTo(-0.65f).Within(0.01f));
            Assert.Less(Quaternion.Angle(flyerVisual.localRotation, Quaternion.Euler(90f, 90f, 0f)), 0.1f);
            Assert.AreEqual(32, flyerVisual.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length);

            GameObject tank = GameObject.Find("Robot_Tank_P1_TORTU_TANK");
            Assert.NotNull(tank);
            Assert.AreEqual(Vector3.one, tank.transform.localScale);
            Assert.AreEqual(new Vector3(5.2f, 3.2f, 4f), tank.GetComponent<BoxCollider>().size);
            Assert.IsNull(tank.GetComponent<SphereCollider>());
            Transform turtleVisual = tank.transform.Find("Tortuga_Visual");
            Assert.NotNull(turtleVisual);
            Assert.AreEqual(new Vector3(1.332f, 1.074f, 1.736f), turtleVisual.localScale);
            Assert.That(turtleVisual.localPosition.y, Is.EqualTo(-1.60f).Within(0.01f));
            Assert.AreEqual(18, turtleVisual.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length);

            GameObject boss = GameObject.Find("AI_Core_BOSS_FINAL");
            Assert.NotNull(boss);
            Assert.AreEqual(new Vector3(3.8f, 4.2f, 2.8f), boss.GetComponent<BoxCollider>().size);
            Transform bossVisual = boss.transform.Find("BOSS-FINAL-RIG");
            Assert.NotNull(bossVisual);
            Assert.That(bossVisual.localPosition.y, Is.EqualTo(-2.10f).Within(0.01f));
            Assert.AreEqual(30, bossVisual.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length);

            GameObject wiseTurtle = GameObject.Find("Mentor_Tortuga_Sabia");
            Assert.NotNull(wiseTurtle);
            Assert.NotNull(wiseTurtle.GetComponent<CapsuleCollider>());
            Transform wiseTurtleVisual = wiseTurtle.transform.Find("Tortuga_Sabia_Visual");
            Assert.NotNull(wiseTurtleVisual);
            Assert.AreEqual(Vector3.one * 1.1f, wiseTurtleVisual.localScale);
            Assert.That(wiseTurtleVisual.localPosition.y, Is.EqualTo(0f).Within(0.01f));
            Assert.AreEqual(50,
                wiseTurtleVisual.GetComponentInChildren<SkinnedMeshRenderer>(true).bones.Length);

            GameObject rescueCage = GameObject.Find("Rescue_Cage_Piso_2");
            Assert.NotNull(rescueCage);
            Assert.NotNull(rescueCage.GetComponent<BoxCollider>());
            Transform cageVisual = rescueCage.transform.Find("Rescue_Cage_Visual");
            Assert.NotNull(cageVisual);
            Assert.AreEqual(Vector3.one * 1.75f, cageVisual.localScale);
            Assert.That(cageVisual.localPosition.y, Is.EqualTo(1.668f).Within(0.01f));

            AssertOptimizedEnemyRig(WiseTurtleModelPath, 50);
            AssertOptimizedMesh(RescueCageModelPath);
            AssertOptimizedEnemyRig(TurtleModelPath, 18);
            AssertOptimizedEnemyRig(CrabModelPath, 44);
            AssertOptimizedEnemyRig(FlyerModelPath, 32);
            AssertOptimizedEnemyRig(BossModelPath, 30);
        }

        private static void AssertOptimizedEnemyRig(string modelPath, int expectedBones)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Assert.NotNull(model);
            SkinnedMeshRenderer renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.NotNull(renderer);
            Assert.AreEqual(expectedBones, renderer.bones.Length);

            long triangles = 0;
            for (int subMesh = 0; subMesh < renderer.sharedMesh.subMeshCount; subMesh++)
                triangles += (long)renderer.sharedMesh.GetIndexCount(subMesh) / 3;
            Assert.LessOrEqual(triangles, 10000, modelPath + " exceeds the 10k triangle budget.");

            Texture2D[] textures = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Texture2D>().ToArray();
            Assert.GreaterOrEqual(textures.Length, 2);
            Assert.IsTrue(textures.All(texture => texture.width <= 1024 && texture.height <= 1024),
                modelPath + " contains a texture larger than 1024.");
        }

        private static void AssertOptimizedMesh(string modelPath)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Assert.NotNull(model);
            MeshFilter filter = model.GetComponentInChildren<MeshFilter>(true);
            Assert.NotNull(filter);

            long triangles = 0;
            for (int subMesh = 0; subMesh < filter.sharedMesh.subMeshCount; subMesh++)
                triangles += (long)filter.sharedMesh.GetIndexCount(subMesh) / 3;
            Assert.LessOrEqual(triangles, 10000, modelPath + " exceeds the 10k triangle budget.");

            Texture2D[] textures = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Texture2D>().ToArray();
            Assert.GreaterOrEqual(textures.Length, 2);
            Assert.IsTrue(textures.All(texture => texture.width <= 1024 && texture.height <= 1024),
                modelPath + " contains a texture larger than 1024.");
        }

        private static void AssertRepeatTextureImport(string texturePath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            Assert.NotNull(texture);
            Assert.NotNull(importer);
            Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode);
            Assert.LessOrEqual(importer.maxTextureSize, 1024);
        }

        private static GameObject FindIncludingInactive(string objectName)
        {
            return Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == objectName);
        }

        [Test]
        public void BuildSettings_ContainsFourReleaseScenesOnly()
        {
            string[] releaseScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            Assert.AreEqual(4, releaseScenes.Length);
            CollectionAssert.DoesNotContain(releaseScenes, SceneRoot + "99_DevValidation.unity");
            CollectionAssert.Contains(releaseScenes, SceneRoot + "00_Launcher.unity");
            CollectionAssert.Contains(releaseScenes, SceneRoot + "03_CinematicEnd.unity");
        }

        [Test]
        public void EveryGeneratedScene_HasNoMissingMonoBehaviours()
        {
            string[] sceneNames =
            {
                "00_Launcher", "01_CinematicIntro", "02_MainScene", "03_CinematicEnd", "99_DevValidation"
            };

            foreach (string sceneName in sceneNames)
            {
                EditorSceneManager.OpenScene(SceneRoot + sceneName + ".unity");
                GameObject[] objects = Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                int missing = objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
                Assert.AreEqual(0, missing, sceneName + " contains missing MonoBehaviour references.");
            }
        }
    }
}
