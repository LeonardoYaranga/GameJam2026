using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NidoCero.Tests
{
    public sealed class NidoCeroPlayModeTests
    {
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
            Assert.AreEqual(18, Object.FindObjectsByType<RobotEnemy>(FindObjectsSortMode.None).Length);
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
    }
}
