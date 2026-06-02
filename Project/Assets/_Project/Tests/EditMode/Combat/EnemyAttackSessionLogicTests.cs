using NUnit.Framework;
using VRProject.Application.Combat;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class EnemyAttackSessionLogicTests
    {
        static readonly EnemyMeleeAttackTimings Timings = new EnemyMeleeAttackTimings(0.6f, 0.15f, 0.8f);

        [Test]
        public void CanBeginAttack_True_WhenIdleInRangeWithTarget()
        {
            var ok = EnemyAttackSessionLogic.CanBeginAttack(
                EnemyAttackState.Idle,
                distanceToTarget: 1f,
                attackRange: 1.6f,
                hasTarget: true);
            Assert.IsTrue(ok);
        }

        [Test]
        public void CanBeginAttack_False_WhenNotIdle()
        {
            var state = new EnemyAttackState(EnemyAttackPhase.WindUp, 0f);
            var ok = EnemyAttackSessionLogic.CanBeginAttack(state, 1f, 1.6f, true);
            Assert.IsFalse(ok);
        }

        [Test]
        public void BeginAttack_StartsWindUp_FromIdle()
        {
            var next = EnemyAttackSessionLogic.BeginAttack(EnemyAttackState.Idle);
            Assert.AreEqual(EnemyAttackPhase.WindUp, next.Phase);
            Assert.AreEqual(0f, next.PhaseElapsedSeconds, 1e-5f);
        }

        [Test]
        public void Advance_WindUpToActive_WhenElapsed()
        {
            var state = new EnemyAttackState(EnemyAttackPhase.WindUp, 0.59f);
            var result = EnemyAttackSessionLogic.Advance(state, Timings, 0.02f);
            Assert.AreEqual(EnemyAttackPhase.Active, result.NextState.Phase);
            Assert.IsTrue(result.EnteredActive);
            Assert.IsFalse(result.AttackCompleted);
        }

        [Test]
        public void IsHitboxActive_OnlyDuringActivePhase()
        {
            Assert.IsFalse(EnemyAttackSessionLogic.IsHitboxActive(EnemyAttackPhase.Idle));
            Assert.IsFalse(EnemyAttackSessionLogic.IsHitboxActive(EnemyAttackPhase.WindUp));
            Assert.IsTrue(EnemyAttackSessionLogic.IsHitboxActive(EnemyAttackPhase.Active));
            Assert.IsFalse(EnemyAttackSessionLogic.IsHitboxActive(EnemyAttackPhase.Recovery));
        }

        [Test]
        public void Advance_CompletesFullCycle_ToIdle()
        {
            var state = EnemyAttackSessionLogic.BeginAttack(EnemyAttackState.Idle);

            state = EnemyAttackSessionLogic.Advance(state, Timings, 0.6f).NextState;
            Assert.AreEqual(EnemyAttackPhase.Active, state.Phase);

            state = EnemyAttackSessionLogic.Advance(state, Timings, 0.15f).NextState;
            Assert.AreEqual(EnemyAttackPhase.Recovery, state.Phase);

            var final = EnemyAttackSessionLogic.Advance(state, Timings, 0.8f);
            Assert.AreEqual(EnemyAttackPhase.Idle, final.NextState.Phase);
            Assert.IsTrue(final.AttackCompleted);
        }
    }
}
