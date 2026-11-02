using NUnit.Framework;

namespace RingOut.Core.Tests
{
    public class ArenaTests
    {
        [TestCase(1, ArenaLayout.Disc)]
        [TestCase(5, ArenaLayout.Disc)]
        [TestCase(6, ArenaLayout.Ring)]
        [TestCase(10, ArenaLayout.Ring)]
        [TestCase(11, ArenaLayout.Islands)]
        [TestCase(16, ArenaLayout.Shrinking)]
        [TestCase(20, ArenaLayout.Shrinking)]
        [TestCase(21, ArenaLayout.Disc)]
        public void EveryFiveWavesMoveToTheNextArena(int wave, ArenaLayout layout)
        {
            Assert.That(ArenaShape.ForWave(wave), Is.EqualTo(layout));
        }

        [Test]
        public void TheDiscIsSolidUpToItsEdge()
        {
            ArenaShape disc = ArenaShape.Of(ArenaLayout.Disc);

            Assert.That(disc.IsSolid(0f, 0f), Is.True);
            Assert.That(disc.IsSolid(9.9f, 0f), Is.True);
            Assert.That(disc.IsSolid(7.1f, 7.1f), Is.False);
            Assert.That(disc.IsSolid(0f, -10.1f), Is.False);
        }

        [Test]
        public void TheRingHasAHoleInTheMiddle()
        {
            ArenaShape ring = ArenaShape.Of(ArenaLayout.Ring);

            Assert.That(ring.IsSolid(0f, 0f), Is.False);
            Assert.That(ring.IsSolid(3f, 0f), Is.False);
            Assert.That(ring.IsSolid(0f, 5f), Is.True);
            Assert.That(ring.IsSolid(0f, 11.2f), Is.False);
            Assert.That(ring.IsSolid(ring.PlayerStart.X, ring.PlayerStart.Z), Is.True);
        }

        [Test]
        public void TheIslandsAreJoinedByANarrowBridge()
        {
            ArenaShape islands = ArenaShape.Of(ArenaLayout.Islands);

            Assert.That(islands.IsSolid(-7f, 0f), Is.True);
            Assert.That(islands.IsSolid(7f, 4f), Is.True);
            Assert.That(islands.IsSolid(0f, 0f), Is.True, "the bridge");
            Assert.That(islands.IsSolid(0f, 1.5f), Is.True);
            Assert.That(islands.IsSolid(0f, 2f), Is.False, "beside the bridge");
            Assert.That(islands.IsSolid(0f, 6f), Is.False);
            Assert.That(islands.IsSolid(islands.PlayerStart.X, islands.PlayerStart.Z), Is.True);
        }

        [Test]
        public void TheShrinkingIslandLosesItsEdgeUntilAFloor()
        {
            ArenaShape shrinking = ArenaShape.Of(ArenaLayout.Shrinking);

            Assert.That(shrinking.OuterRadius(0f), Is.EqualTo(11f));
            Assert.That(shrinking.OuterRadius(10f), Is.EqualTo(9.8f).Within(1e-4f));
            Assert.That(shrinking.OuterRadius(1000f), Is.EqualTo(ArenaShape.ShrinkMin));
            Assert.That(shrinking.IsSolid(10f, 0f, 0f), Is.True);
            Assert.That(shrinking.IsSolid(10f, 0f, 20f), Is.False);
            Assert.That(ArenaShape.Of(ArenaLayout.Disc).OuterRadius(1000f), Is.EqualTo(ArenaShape.DiscRadius), "only that one shrinks");
        }

        [TestCase(ArenaLayout.Disc)]
        [TestCase(ArenaLayout.Ring)]
        [TestCase(ArenaLayout.Islands)]
        [TestCase(ArenaLayout.Shrinking)]
        public void SpawnsLandOnTheFloorAwayFromThePlayer(ArenaLayout layout)
        {
            ArenaShape shape = ArenaShape.Of(layout);
            var rng = new Rng(5);
            Vec3 player = shape.PlayerStart;
            for (int i = 0; i < 500; i++)
            {
                Vec3 spawn = shape.SpawnPoint(rng, player, 4f, 1.1f, 15f);

                Assert.That(shape.IsSafe(spawn.X, spawn.Z, 1.1f, 15f), Is.True, $"spawn {spawn} is on the floor");
                Assert.That(Vec3.Distance(spawn, player), Is.GreaterThanOrEqualTo(4f), $"spawn {spawn} is away from the player");
                Assert.That(spawn.Y, Is.EqualTo(0f));
            }
        }

        [Test]
        public void WithNoRoomFarEnoughASpawnStillLandsOnTheFloorAsFarAsItCan()
        {
            ArenaShape disc = ArenaShape.Of(ArenaLayout.Disc);
            Vec3 spawn = disc.SpawnPoint(new Rng(3), Vec3.Zero, 50f, 1f);

            Assert.That(disc.IsSafe(spawn.X, spawn.Z, 1f), Is.True);
            Assert.That(Vec3.Distance(spawn, Vec3.Zero), Is.GreaterThan(6f));
        }
    }
}
