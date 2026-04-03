using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Tests.EditMode
{
    public sealed class OsFpsInspiredAimFireGateTests
    {
        [Test]
        public void Passes_When_AimNotRequired()
        {
            var ok = OsFpsInspiredAimFireGate.PassesFireAimGate(
                requireRightMouseAimToFire: false,
                allowHipFireWhileLocomoting: false,
                locomotionAxesSqrThreshold: 1e-5f,
                motorIsNull: false,
                motorReportsAiming: false,
                locomotionAxes: Vector2.zero);
            Assert.That(ok, Is.True);
        }

        [Test]
        public void Passes_When_MotorNull_And_AimRequired()
        {
            var ok = OsFpsInspiredAimFireGate.PassesFireAimGate(
                requireRightMouseAimToFire: true,
                allowHipFireWhileLocomoting: false,
                locomotionAxesSqrThreshold: 1e-5f,
                motorIsNull: true,
                motorReportsAiming: false,
                locomotionAxes: Vector2.zero);
            Assert.That(ok, Is.True);
        }

        [Test]
        public void Passes_When_Aiming()
        {
            var ok = OsFpsInspiredAimFireGate.PassesFireAimGate(
                requireRightMouseAimToFire: true,
                allowHipFireWhileLocomoting: false,
                locomotionAxesSqrThreshold: 1e-5f,
                motorIsNull: false,
                motorReportsAiming: true,
                locomotionAxes: Vector2.zero);
            Assert.That(ok, Is.True);
        }

        [Test]
        public void Fails_When_AimRequired_NotAiming_NotMoving_And_HipWhileMoveOff()
        {
            var ok = OsFpsInspiredAimFireGate.PassesFireAimGate(
                requireRightMouseAimToFire: true,
                allowHipFireWhileLocomoting: false,
                locomotionAxesSqrThreshold: 1e-5f,
                motorIsNull: false,
                motorReportsAiming: false,
                locomotionAxes: Vector2.zero);
            Assert.That(ok, Is.False);
        }

        [Test]
        public void Passes_When_HipWhileLocomoting_And_AxesAboveThreshold()
        {
            var ok = OsFpsInspiredAimFireGate.PassesFireAimGate(
                requireRightMouseAimToFire: true,
                allowHipFireWhileLocomoting: true,
                locomotionAxesSqrThreshold: 1e-5f,
                motorIsNull: false,
                motorReportsAiming: false,
                locomotionAxes: new Vector2(0f, 0.2f));
            Assert.That(ok, Is.True);
        }

        [Test]
        public void Fails_When_HipWhileLocomoting_But_AxesBelowThreshold()
        {
            var ok = OsFpsInspiredAimFireGate.PassesFireAimGate(
                requireRightMouseAimToFire: true,
                allowHipFireWhileLocomoting: true,
                locomotionAxesSqrThreshold: 1e-5f,
                motorIsNull: false,
                motorReportsAiming: false,
                locomotionAxes: Vector2.zero);
            Assert.That(ok, Is.False);
        }
    }
}
