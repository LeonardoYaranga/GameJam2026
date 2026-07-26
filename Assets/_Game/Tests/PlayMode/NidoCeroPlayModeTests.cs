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
            Assert.NotNull(Object.FindFirstObjectByType<BossEncounter>());
            Assert.AreEqual(6, Object.FindObjectsByType<RobotEnemy>(FindObjectsSortMode.None).Length);
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
    }
}
