using NUnit.Framework;
using BOTF3D.Combat;
using BOTF3D.Core;
using UnityEngine;

namespace BOTF3D.Tests
{
    /// <summary>
    /// Unit tests for combat order mechanics.
    /// Run via Unity Test Runner (Window > General > Test Runner)
    /// </summary>
    [TestFixture]
    public class CombatOrderTests
    {
        [Test]
        public void OrderMultiplier_SameOrders_ReturnsNeutral()
        {
            // Arrange & Act
            float result = CombatOrderHelper.GetOrderMultiplier(CombatOrders.Engage, CombatOrders.Engage);

            // Assert
            Assert.AreEqual(1.0f, result, "Same orders should have neutral multiplier");
        }

        [Test]
        public void OrderMultiplier_NoneOrder_ReturnsNeutral()
        {
            // Arrange & Act
            float result1 = CombatOrderHelper.GetOrderMultiplier(CombatOrders.None, CombatOrders.Rush);
            float result2 = CombatOrderHelper.GetOrderMultiplier(CombatOrders.Engage, CombatOrders.None);

            // Assert
            Assert.AreEqual(1.0f, result1, "None order should have neutral multiplier");
            Assert.AreEqual(1.0f, result2, "Against None order should have neutral multiplier");
        }

        [Test]
        public void OrderDescription_AllOrders_ReturnsValidStrings()
        {
            // Arrange
            CombatOrders[] allOrders = new[]
            {
                CombatOrders.Engage,
                CombatOrders.Rush,
                CombatOrders.Retreat,
                CombatOrders.Formation,
                CombatOrders.AttackTransports
            };

            // Act & Assert
            foreach (var order in allOrders)
            {
                string description = CombatOrderHelper.GetOrderDescription(order);
                Assert.IsFalse(string.IsNullOrEmpty(description), $"Order {order} should have a description");
                Debug.Log($"{order}: {description}");
            }
        }

        [Test]
        public void IsRetreating_RetreatOrder_ReturnsTrue()
        {
            // Arrange & Act
            bool result = CombatOrderHelper.IsRetreating(CombatOrders.Retreat);

            // Assert
            Assert.IsTrue(result, "Retreat order should be detected as retreating");
        }

        [Test]
        public void IsRetreating_NonRetreatOrder_ReturnsFalse()
        {
            // Arrange & Act
            bool result = CombatOrderHelper.IsRetreating(CombatOrders.Engage);

            // Assert
            Assert.IsFalse(result, "Engage order should not be detected as retreating");
        }

        [Test]
        public void OrderProtectsTransports_FormationOrder_ReturnsTrue()
        {
            // Arrange & Act
            bool result = CombatOrderHelper.OrderProtectsTransports(CombatOrders.Formation);

            // Assert
            Assert.IsTrue(result, "Formation order should protect transports");
        }

        [Test]
        public void OrderProtectsTransports_RushOrder_ReturnsFalse()
        {
            // Arrange & Act
            bool result = CombatOrderHelper.OrderProtectsTransports(CombatOrders.Rush);

            // Assert
            Assert.IsFalse(result, "Rush order should not protect transports");
        }

        [Test]
        public void OrderBypassesLOS_AttackTransports_ReturnsTrue()
        {
            // Arrange & Act
            bool result = CombatOrderHelper.OrderBypassesLOS(CombatOrders.AttackTransports);

            // Assert
            Assert.IsTrue(result, "AttackTransports order should bypass LOS");
        }

        [Test]
        public void OrderSummary_ValidOrders_ReturnsFormattedString()
        {
            // Arrange & Act
            string summary = CombatOrderHelper.GetOrderSummary(CombatOrders.Rush, CombatOrders.Formation);

            // Assert
            Assert.IsTrue(summary.Contains("Rush"), "Summary should contain Side 1 order");
            Assert.IsTrue(summary.Contains("Formation"), "Summary should contain Side 2 order");
            Debug.Log($"Order Summary: {summary}");
        }
    }

    /// <summary>
    /// Tests for ship state and combat data structures
    /// </summary>
    [TestFixture]
    public class CombatDataTests
    {
        [Test]
        public void CombatData_Constructor_InitializesLists()
        {
            // Arrange & Act
            var data = new CombatData();

            // Assert
            Assert.IsNotNull(data.SideOneShipCons, "Side One ship list should be initialized");
            Assert.IsNotNull(data.SideTwoShipCons, "Side Two ship list should be initialized");
            Assert.AreEqual(0, data.SideOneShipCons.Count, "Side One ship list should be empty");
            Assert.AreEqual(0, data.SideTwoShipCons.Count, "Side Two ship list should be empty");
        }

        [Test]
        public void CombatData_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var data = new CombatData();

            // Assert
            Assert.AreEqual(CombatOrders.None, data.OrderSideOne, "Default order should be None");
            Assert.AreEqual(CombatOrders.None, data.OrderSideTwo, "Default order should be None");
            Assert.AreEqual(CivEnum.None, data.CivEnumSideOne, "Default civ should be None");
            Assert.AreEqual(CivEnum.None, data.CivEnumSideTwo, "Default civ should be None");
        }

        [Test]
        public void ShipPhaseTracker_Initialization_WorksCorrectly()
        {
            // Arrange
            GameObject testObj = new GameObject("TestShip");
            var tracker = testObj.AddComponent<ShipPhaseTracker>();

            // Act
            tracker.initialized = true;
            tracker.sideDir = 1;
            tracker.currentSpeed = 0f;
            tracker.travelDist = 0f;

            // Assert
            Assert.IsTrue(tracker.initialized, "Tracker should be initialized");
            Assert.AreEqual(1, tracker.sideDir, "Side direction should be 1");
            Assert.AreEqual(0f, tracker.currentSpeed, "Initial speed should be 0");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }
    }

    /// <summary>
    /// Integration tests for turn results
    /// </summary>
    [TestFixture]
    public class TurnResultTests
    {
        [Test]
        public void TurnResult_Construction_InitializesFields()
        {
            // Arrange & Act
            var result = new TurnResult
            {
                TurnNumber = 1,
                SideOneOrder = CombatOrders.Engage,
                SideTwoOrder = CombatOrders.Rush,
                SideOneDamageDealt = 100,
                SideTwoDamageDealt = 150
            };

            // Assert
            Assert.AreEqual(1, result.TurnNumber);
            Assert.AreEqual(CombatOrders.Engage, result.SideOneOrder);
            Assert.AreEqual(CombatOrders.Rush, result.SideTwoOrder);
            Assert.AreEqual(100, result.SideOneDamageDealt);
            Assert.AreEqual(150, result.SideTwoDamageDealt);
        }

        [Test]
        public void TurnResult_ShipsDestroyedList_IsInitialized()
        {
            // Arrange & Act
            var result = new TurnResult();

            // Assert
            Assert.IsNotNull(result.ShipsDestroyed, "ShipsDestroyed list should be initialized");
            Assert.AreEqual(0, result.ShipsDestroyed.Count, "ShipsDestroyed list should be empty");
        }
    }
}
