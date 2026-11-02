using System.Collections;
using NUnit.Framework;
using RingOut.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RingOut.Tests
{
    /// <summary>The 2021 prototype scene: power-ups, waves and how enemies chase the player.</summary>
    public class PrototypeTests
    {
        PlayerController player;
        SpawnManager spawner;

        IEnumerator Load()
        {
            SceneManager.LoadScene("Prototype 4");
            yield return null;
            yield return null;
            player = Object.FindFirstObjectByType<PlayerController>();
            spawner = Object.FindFirstObjectByType<SpawnManager>();
            Assert.That(player, Is.Not.Null);
            Assert.That(spawner, Is.Not.Null);
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 0;
        }

        static void Clear(string tag)
        {
            foreach (GameObject item in GameObject.FindGameObjectsWithTag(tag))
            {
                Object.Destroy(item);
            }
        }

        void PickUpPowerUp()
        {
            var pickup = new GameObject("Test PowerUp") { tag = "PowerUp" };
            var trigger = pickup.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            player.SendMessage("OnTriggerEnter", trigger);
        }

        bool IndicatorShown => player.transform.Find("PowerUpIndicator").gameObject.activeSelf;

        [UnityTest]
        public IEnumerator ASecondPowerUpPickedUpBeforeTheFirstEndsLastsItsFullDuration()
        {
            yield return Load();
            spawner.enabled = false;
            Clear("Enemy");
            Clear("PowerUp");
            Time.timeScale = 4f;

            PickUpPowerUp();
            yield return new WaitForSeconds(3f);
            PickUpPowerUp();
            yield return new WaitForSeconds(2.5f);

            Assert.That(player.HasPowerUp, Is.True, "5.5 s after the first pick-up, 2.5 s after the second");
            Assert.That(IndicatorShown, Is.True);

            yield return new WaitForSeconds(3f);
            Assert.That(player.HasPowerUp, Is.False, "8.5 s in, the second power-up has run out");
            Assert.That(IndicatorShown, Is.False);
        }

        [UnityTest]
        public IEnumerator PowerUpsThatNobodyPicksUpDoNotPileUpWaveAfterWave()
        {
            yield return Load();
            var body = player.GetComponent<Rigidbody>();
            body.isKinematic = true;
            player.transform.position = new Vector3(0f, 40f, 0f);

            for (int i = 0; i < 3; i++)
            {
                Clear("Enemy");
                yield return null;
                yield return null;
            }

            Assert.That(spawner.Wave, Is.EqualTo(4));
            Assert.That(GameObject.FindGameObjectsWithTag("PowerUp").Length, Is.EqualTo(1));
        }
    }
}
