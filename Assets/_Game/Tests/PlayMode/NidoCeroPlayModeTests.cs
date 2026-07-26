using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NidoCero.Tests
{
    public sealed class NidoCeroPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator ResetPersistentRunState()
        {
            Time.timeScale = 1f;
            if (GameSession.Instance != null) GameSession.Instance.ResetRun();
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainScene_StartsWithAllRuntimeSystems()
        {
            SceneManager.LoadScene("02_MainScene");
            yield return null;
            yield return new WaitForSeconds(0.25f);

            Assert.NotNull(Object.FindFirstObjectByType<GameSession>());
            Assert.NotNull(Object.FindFirstObjectByType<PlayerController>());
            Assert.NotNull(Object.FindFirstObjectByType<HudController>());
            Assert.NotNull(Object.FindFirstObjectByType<CardChoiceController>());
            Assert.NotNull(Object.FindFirstObjectByType<DialogueController>());
            Assert.NotNull(Object.FindFirstObjectByType<FinalSacrificeController>());
            Assert.NotNull(Object.FindFirstObjectByType<BossEncounter>());
            StructuralTileFaceController tileController =
                Object.FindFirstObjectByType<StructuralTileFaceController>();
            Assert.NotNull(tileController);
            Assert.AreEqual(3, tileController.ActivePhysicalFloor);
            Assert.IsTrue(GameObject.Find("TileFaces_Piso_3").activeSelf);
            GameObject activeCeiling = GameObject.Find("Ceiling_Piso_3_VisualShell");
            Assert.NotNull(activeCeiling);
            Assert.IsNull(activeCeiling.GetComponent<Collider>());
            Assert.AreEqual(6, Object.FindObjectsByType<RobotEnemy>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator LauncherFlow_TransitionsThroughIntroToMainScene()
        {
            SceneManager.LoadScene("00_Launcher");
            yield return null;

            MainMenuController menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.NotNull(menu);
            menu.NewGame();
            yield return null;
            Assert.AreEqual("01_CinematicIntro", SceneManager.GetActiveScene().name);

            CinematicController intro = Object.FindFirstObjectByType<CinematicController>();
            Assert.NotNull(intro);
            intro.Continue();
            yield return null;
            Assert.AreEqual("02_MainScene", SceneManager.GetActiveScene().name);
            Assert.NotNull(Object.FindFirstObjectByType<PlayerController>());
        }

        [UnityTest]
        public IEnumerator CompleteRun_RespectsCardsGeorgeGatesAndFinalCost()
        {
            SceneManager.LoadScene("02_MainScene");
            yield return null;
            yield return new WaitForSeconds(0.25f);

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            Assert.NotNull(player);
            Rigidbody playerBody = player.GetComponent<Rigidbody>();
            playerBody.useGravity = false;
            playerBody.linearVelocity = Vector3.zero;
            foreach (RobotEnemy enemy in
                     Object.FindObjectsByType<RobotEnemy>(FindObjectsSortMode.None))
                enemy.enabled = false;

            yield return DefeatCollectAndChoose(
                player, "Robot_Flyer_P3_FRAGA_TUTORIAL", "george_first_choice", 0);
            Assert.AreEqual(0, GameSession.Instance.State.keyFloor);

            WiseTurtleInteraction george = Object.FindFirstObjectByType<WiseTurtleInteraction>();
            Assert.NotNull(george);
            player.transform.position = george.transform.position + new Vector3(-1.2f, 1.05f, 0f);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(george.IsPlayerNearby);
            Assert.IsTrue(george.TryBeginConversation());
            Assert.IsTrue(DialogueController.IsOpen);
            DialogueController.Instance.Advance();
            DialogueController.Instance.Advance();
            DialogueController.Instance.Advance();
            Assert.IsFalse(DialogueController.IsOpen);
            Assert.Contains(WiseTurtleInteraction.MissionStoryFlag,
                GameSession.Instance.State.storyFlags);
            AssertGateOpen(0, "GateUnlockZone_Piso_3");

            GameSession.Instance.State.currentFloor = 1;
            yield return DefeatCollectAndChoose(
                player, "Robot_Walker_P2_CANGRE_1", "enemy_drop_P2_CANGRE_1", 0);
            yield return DefeatCollectAndChoose(
                player, "Robot_Walker_P2_CANGRE_2", "enemy_drop_P2_CANGRE_2", 1);
            yield return DefeatCollectAndChoose(
                player, "Robot_Walker_P2_CANGRE_3", "enemy_drop_P2_CANGRE_3", 2);
            AssertGateOpen(1, "GateUnlockZone_Piso_2");

            GameSession.Instance.State.currentFloor = 2;
            yield return DefeatCollectAndChoose(
                player, "Robot_Flyer_P1_FRAGA_LLAVE_DORADA",
                "enemy_drop_P1_FRAGA_LLAVE_DORADA", 0);
            yield return DefeatCollectAndChoose(
                player, "Robot_Tank_P1_TORTU_TANK", "enemy_drop_P1_TORTU_TANK", 1);
            AssertGateOpen(2, "GateUnlockZone_Piso_1");

            RunState state = GameSession.Instance.State;
            Assert.Greater(state.elements.water + state.elements.fire + state.elements.vegetation, 0);
            state.stats.life = 9;
            state.stats.stamina = 135;
            state.elements.water += 2;
            state.currentFloor = 3;
            foreach (BossRelay relay in
                     Object.FindObjectsByType<BossRelay>(FindObjectsSortMode.None))
            {
                relay.Hit();
                relay.Hit();
            }
            BossCore core = Object.FindFirstObjectByType<BossCore>();
            Assert.NotNull(core);
            Assert.AreEqual(5, core.ElementLevel);
            core.Hit(999);
            Assert.IsTrue(FinalSacrificeController.IsOpen);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual("02_MainScene", SceneManager.GetActiveScene().name);
            UnityEngine.UI.Image sacrificeFill =
                GameObject.Find("SacrificeProgressFill").GetComponent<UnityEngine.UI.Image>();
            Assert.That(sacrificeFill.rectTransform.anchorMax.x,
                Is.EqualTo(sacrificeFill.rectTransform.anchorMin.x).Within(0.001f));

            FinalSacrificeController.Instance.ConfirmNow();
            Assert.AreEqual(5, state.stats.life);
            Assert.AreEqual(100, state.stats.stamina);
            Assert.AreEqual(0, state.elements.water);
            Assert.AreEqual(0, state.elements.fire);
            Assert.AreEqual(0, state.elements.vegetation);
            yield return null;
            Assert.AreEqual("03_CinematicEnd", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator MainScene_PhysicsRemainStableForTwoSeconds()
        {
            SceneManager.LoadScene("02_MainScene");
            yield return null;
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            Assert.NotNull(player);
            Vector3 start = player.transform.position;
            yield return new WaitForSeconds(2f);
            Assert.That(player.transform.position.z, Is.EqualTo(0f).Within(0.001f));
            Assert.Greater(player.transform.position.y, -4f);
            Assert.Less(Mathf.Abs(player.transform.position.x - start.x), 3f);
        }

        [UnityTest]
        public IEnumerator FlyerHover_RemainsCenteredWithoutVerticalDrift()
        {
            SceneManager.LoadScene("02_MainScene");
            yield return null;
            yield return new WaitForSeconds(0.25f);

            GameObject flyerObject = GameObject.Find("Robot_Flyer_P3_FRAGA_TUTORIAL");
            Assert.NotNull(flyerObject);
            float originY = flyerObject.transform.position.y;
            yield return new WaitForSeconds(3f);
            Assert.That(Mathf.Abs(flyerObject.transform.position.y - originY), Is.LessThan(0.17f));
        }

        [UnityTest]
        public IEnumerator PlayerDamage_PlaysSubtleCameraShakeAndRedScreenFlash()
        {
            SceneManager.LoadScene("02_MainScene");
            yield return null;
            yield return new WaitForSeconds(0.25f);

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            CameraFollow cameraFollow = Object.FindFirstObjectByType<CameraFollow>();
            HudController hud = Object.FindFirstObjectByType<HudController>();
            Assert.NotNull(player);
            Assert.NotNull(cameraFollow);
            Assert.NotNull(hud);

            int lifeBefore = player.CurrentLife;
            player.TakeDamage(1);
            yield return null;

            Assert.AreEqual(lifeBefore - 1, player.CurrentLife);
            Assert.IsTrue(cameraFollow.IsShaking);
            Assert.IsTrue(hud.IsDamageFlashActive);
            GameObject flash = GameObject.Find("DamageFlash");
            Assert.NotNull(flash);
            Assert.Greater(flash.GetComponent<UnityEngine.UI.Image>().color.a, 0f);

            yield return new WaitForSeconds(0.35f);
            Assert.IsFalse(cameraFollow.IsShaking);
            Assert.IsFalse(hud.IsDamageFlashActive);
        }

        [UnityTest]
        public IEnumerator EnemyElementalTint_IsPerceptibleCappedAndRestoredAfterHit()
        {
            SceneManager.LoadScene("02_MainScene");
            yield return null;
            yield return new WaitForSeconds(0.25f);

            string[] enemyNames =
            {
                "Robot_Walker_P2_CANGRE_1",
                "Robot_Flyer_P3_FRAGA_TUTORIAL",
                "Robot_Tank_P1_TORTU_TANK"
            };
            ElementId[] expectedElements =
            {
                ElementId.Water,
                ElementId.Fire,
                ElementId.Vegetation
            };

            for (int index = 0; index < enemyNames.Length; index++)
            {
                GameObject enemyObject = GameObject.Find(enemyNames[index]);
                RobotEnemy enemy = enemyObject != null ? enemyObject.GetComponent<RobotEnemy>() : null;
                Assert.NotNull(enemy, enemyNames[index]);
                Assert.AreEqual(expectedElements[index], enemy.Definition.element);
                Assert.That(enemy.ElementalTintStrength, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.3f));

                Renderer renderer = FindTintableRenderer(enemyObject);
                Assert.NotNull(renderer, enemyNames[index]);
                Color materialColor = GetMaterialColor(renderer);
                Color expectedColor =
                    Color.Lerp(materialColor, enemy.Definition.color, enemy.ElementalTintStrength);
                AssertColorsEqual(expectedColor, GetPropertyBlockColor(renderer), enemyNames[index]);
            }

            RobotEnemy crab = GameObject.Find("Robot_Walker_P2_CANGRE_1").GetComponent<RobotEnemy>();
            Renderer crabRenderer = FindTintableRenderer(crab.gameObject);
            Color expectedCrabColor = Color.Lerp(
                GetMaterialColor(crabRenderer),
                crab.Definition.color,
                crab.ElementalTintStrength);
            crab.TakeDamage(1);
            yield return new WaitForSeconds(0.35f);
            Assert.IsFalse(crab.IsHitFeedbackActive);
            AssertColorsEqual(expectedCrabColor, GetPropertyBlockColor(crabRenderer),
                "Water tint after hit");
        }

        [UnityTest]
        public IEnumerator EnemyDamage_FlashesAndLeavesGroundedUpsideDownCorpses()
        {
            SceneManager.LoadScene("02_MainScene");
            yield return null;
            yield return new WaitForSeconds(0.25f);

            GameObject crabObject = GameObject.Find("Robot_Walker_P2_CANGRE_1");
            RobotEnemy crab = crabObject != null ? crabObject.GetComponent<RobotEnemy>() : null;
            Assert.NotNull(crab);
            crab.TakeDamage(1);
            yield return null;
            Assert.IsTrue(crab.IsHitFeedbackActive);
            Assert.IsFalse(crab.IsDead);
            yield return new WaitForSeconds(0.35f);
            Assert.IsFalse(crab.IsHitFeedbackActive);

            crab.TakeDamage(9999);
            Assert.Greater(crab.GetComponent<Rigidbody>().linearVelocity.y, 0f);
            yield return new WaitForSeconds(1.35f);
            Assert.IsTrue(crab.gameObject.activeSelf);
            Assert.IsTrue(crab.IsDead);
            Assert.IsTrue(crab.CorpsePoseSettled);
            Assert.IsTrue(crab.GetComponent<Rigidbody>().isKinematic);
            Assert.IsFalse(crab.GetComponent<Collider>().enabled);

            GameObject flyerObject = GameObject.Find("Robot_Flyer_P3_FRAGA_TUTORIAL");
            RobotEnemy flyer = flyerObject != null ? flyerObject.GetComponent<RobotEnemy>() : null;
            Assert.NotNull(flyer);
            flyer.TakeDamage(9999);
            Assert.Greater(flyer.GetComponent<Rigidbody>().linearVelocity.y, 0f);
            yield return new WaitForSeconds(1.55f);
            Assert.IsTrue(flyer.gameObject.activeSelf);
            Assert.IsTrue(flyer.IsDead);
            Assert.IsTrue(flyer.CorpsePoseSettled);
            Assert.IsFalse(flyer.GetComponent<Collider>().enabled);
            Transform flyerVisual = flyer.transform.Find("Fragata_Visual");
            Assert.NotNull(flyerVisual);
            Assert.That(flyerVisual.localPosition.y, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator EnemyDeath_DropsDecisionAndPresentsOneCardPerElement()
        {
            SceneManager.LoadScene("02_MainScene");
            yield return null;
            yield return new WaitForSeconds(0.25f);

            GameObject choiceEnemy = GameObject.Find("Robot_Tank_P1_TORTU_TANK");
            RobotEnemy enemy = choiceEnemy != null ? choiceEnemy.GetComponent<RobotEnemy>() : null;
            Assert.NotNull(enemy);
            enemy.TakeDamage(9999);
            yield return null;

            CardDropPickup drop = Object.FindFirstObjectByType<CardDropPickup>();
            Assert.NotNull(drop);
            Assert.IsFalse(CardChoiceController.IsOpen);

            CardChoiceController cards = Object.FindFirstObjectByType<CardChoiceController>();
            Assert.NotNull(cards);
            Assert.IsTrue(cards.Open(drop.ChoiceId));
            Assert.IsTrue(CardChoiceController.IsOpen);
            Assert.AreEqual(ElementId.Water, cards.GetPresentedOffer(0).element);
            Assert.AreEqual(ElementId.Fire, cards.GetPresentedOffer(1).element);
            Assert.AreEqual(ElementId.Vegetation, cards.GetPresentedOffer(2).element);
            string selectedCardId = cards.GetPresentedOffer(0).sourceCardId;
            string rejectedFireId = cards.GetPresentedOffer(1).sourceCardId;
            string rejectedVegetationId = cards.GetPresentedOffer(2).sourceCardId;

            for (int index = 0; index < 3; index++)
            {
                RuntimeCardOffer offer = cards.GetPresentedOffer(index);
                Assert.Greater(offer.GetDelta(0), 0);
                Assert.Greater(offer.GetDelta(1), 0);
                Assert.Less(offer.GetDelta(2), 0);
            }

            cards.Choose(0);
            Assert.IsFalse(CardChoiceController.IsOpen);
            Assert.Contains(drop.ChoiceId, GameSession.Instance.State.resolvedChoices);
            Assert.AreEqual(1, GameSession.Instance.State.elements.water);
            Assert.IsFalse(GameSession.Instance.State.retiredCards.Contains(selectedCardId));
            Assert.Contains(rejectedFireId, GameSession.Instance.State.retiredCards);
            Assert.Contains(rejectedVegetationId, GameSession.Instance.State.retiredCards);
        }

        private static Renderer FindTintableRenderer(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material material = renderer.sharedMaterial;
                if (material != null &&
                    (material.HasProperty("_BaseColor") || material.HasProperty("_Color")))
                    return renderer;
            }
            return null;
        }

        private static Color GetMaterialColor(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            return material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.GetColor("_Color");
        }

        private static Color GetPropertyBlockColor(Renderer renderer)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return renderer.sharedMaterial.HasProperty("_BaseColor")
                ? block.GetColor("_BaseColor")
                : block.GetColor("_Color");
        }

        private static void AssertColorsEqual(Color expected, Color actual, string context)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.002f), context + " red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.002f), context + " green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.002f), context + " blue");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.002f), context + " alpha");
        }

        private static IEnumerator DefeatCollectAndChoose(PlayerController player, string enemyName,
            string choiceId, int cardIndex)
        {
            GameObject enemyObject = GameObject.Find(enemyName);
            RobotEnemy enemy = enemyObject != null ? enemyObject.GetComponent<RobotEnemy>() : null;
            Assert.NotNull(enemy, enemyName);
            enemy.TakeDamage(9999);
            yield return null;

            CardDropPickup target = null;
            foreach (CardDropPickup drop in
                     Object.FindObjectsByType<CardDropPickup>(FindObjectsSortMode.None))
            {
                if (drop.ChoiceId == choiceId)
                {
                    target = drop;
                    break;
                }
            }
            Assert.NotNull(target, choiceId);
            if (target.GrantsKey)
                Assert.AreEqual(GameSession.Instance.State.currentFloor, target.KeyFloorIndex);

            Rigidbody body = player.GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.zero;
            player.transform.position = target.transform.position;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(CardChoiceController.IsOpen, choiceId);
            Assert.AreEqual(0f, Time.timeScale);
            CardChoiceController.Instance.Choose(cardIndex);
            Assert.IsFalse(CardChoiceController.IsOpen);
            Assert.Contains(choiceId, GameSession.Instance.State.resolvedChoices);
            yield return null;
        }

        private static void AssertGateOpen(int floorIndex, string gateName)
        {
            GameObject gateObject = GameObject.Find(gateName);
            GateUnlockZone gate = gateObject != null ? gateObject.GetComponent<GateUnlockZone>() : null;
            Assert.NotNull(gate, gateName);
            Assert.IsTrue(gate.TryOpenFromProgress(), gateName);
            Assert.IsTrue(gate.IsOpen, gateName);
            Assert.Contains(floorIndex, GameSession.Instance.State.openedGates);
            Assert.AreEqual(-1, GameSession.Instance.State.keyFloor);
        }
    }
}
