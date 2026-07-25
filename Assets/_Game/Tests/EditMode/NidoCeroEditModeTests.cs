using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NidoCero.Tests
{
    public sealed class NidoCeroEditModeTests
    {
        private const string CatalogPath = "Assets/_Game/Generated/Data/GameCatalog.asset";
        private const string SceneRoot = "Assets/_Game/Scenes/";

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
