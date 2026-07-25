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
        private const string PlayerModelPath =
            "Assets/_Game/Art/Characters/Piquero_Player_Rigged_Optimized.glb";

        [Test]
        public void ElementalResolver_UsesApprovedExposureFormula()
        {
            ElementLevels defender = new ElementLevels { fire = 3, water = 1, vegetation = 0 };
            Assert.AreEqual(2, ElementalResolver.Bonus(ElementId.Water, 4, defender));
            Assert.AreEqual(1, ElementalResolver.Bonus(ElementId.Water, 1, defender));
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
            Assert.AreEqual(6, catalog.floors.Length);
            Assert.AreEqual(18, catalog.cards.Select(card => card.cardId).Distinct().Count());
        }

        [Test]
        public void MainScene_HasSixFloorsPlayerEnemiesAndBoss()
        {
            EditorSceneManager.OpenScene(SceneRoot + "02_MainScene.unity");
            Assert.NotNull(Object.FindFirstObjectByType<PlayerController>());
            Assert.AreEqual(18, Object.FindObjectsByType<RobotEnemy>(FindObjectsSortMode.None).Length);
            Assert.NotNull(Object.FindFirstObjectByType<BossEncounter>());
            for (int floor = 1; floor <= 6; floor++)
            {
                GameObject block = GameObject.Find("Floor_" + floor + "_Blocking");
                Assert.NotNull(block, "Missing floor " + floor);
                Assert.NotNull(block.GetComponent<BoxCollider>());
            }
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

            GameObject floor = GameObject.Find("Floor_1_Blocking");
            GameObject ceiling = GameObject.Find("Floor_2_Blocking");
            Assert.NotNull(floor);
            Assert.NotNull(ceiling);
            Assert.That(ceiling.transform.position.y - floor.transform.position.y,
                Is.EqualTo(5f).Within(0.001f));

            TextMesh floorLabel = GameObject.Find("FloorLabel_1").GetComponent<TextMesh>();
            Assert.NotNull(floorLabel);
            Assert.LessOrEqual(floorLabel.characterSize, 0.08f);
            Assert.Less(floorLabel.transform.position.x, 0f);

            GameObject controlsBar = GameObject.Find("ControlsBar");
            GameObject controlsObject = GameObject.Find("Controls");
            Assert.NotNull(controlsBar);
            Assert.NotNull(controlsObject);
            Text controls = controlsObject.GetComponent<Text>();
            Assert.NotNull(controls);
            StringAssert.Contains("[A / D]", controls.text);
            StringAssert.Contains("[ESPACIO]", controls.text);

            CanvasScaler scaler = Object.FindFirstObjectByType<CanvasScaler>();
            Assert.NotNull(scaler);
            Assert.AreEqual(new Vector2(1280f, 720f), scaler.referenceResolution);
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
            SkinnedMeshRenderer skinned = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.NotNull(skinned);
            Assert.AreEqual(36, skinned.bones.Length);
            Assert.AreEqual("Standard", skinned.sharedMaterial.shader.name);

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
            Assert.NotNull(model);
            SkinnedMeshRenderer modelRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.NotNull(modelRenderer);
            long triangles = 0;
            for (int subMesh = 0; subMesh < modelRenderer.sharedMesh.subMeshCount; subMesh++)
                triangles += (long)modelRenderer.sharedMesh.GetIndexCount(subMesh) / 3;
            Assert.LessOrEqual(triangles, 25000);

            Assert.AreEqual(new Vector3(1.55f, 1.45f, 1.3f),
                GameObject.Find("Robot_Walker_F1_E1").transform.localScale);
            Assert.AreEqual(new Vector3(1.35f, 1.1f, 1.35f),
                GameObject.Find("Robot_Flyer_F1_E2").transform.localScale);
            Assert.AreEqual(new Vector3(2.2f, 1.6f, 1.6f),
                GameObject.Find("Robot_Tank_F1_E3").transform.localScale);
            Assert.NotNull(GameObject.Find("Robot_Tank_F1_E3").transform.Find("StompDeck"));
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
