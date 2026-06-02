using NUnit.Framework;
using VRProject.Application.Startup;

namespace VRProject.Tests.EditMode
{
    public sealed class VrPlayAvailabilityTests
    {
        [Test]
        public void IsVrPlayAvailable_WhenEditorWithoutSignals_ReturnsTrue()
        {
            var readiness = new VrConnectionReadiness(
                isEditor: true,
                bluetoothAdapterEnabled: false,
                metaQuestBluetoothLinked: false,
                metaQuestBluetoothDeviceName: string.Empty,
                xrRuntimeActive: false,
                xrDeviceName: string.Empty);

            Assert.IsTrue(VrPlayAvailability.IsVrPlayAvailable(readiness));
        }

        [Test]
        public void IsVrPlayAvailable_WhenRuntimeWithoutBluetoothOrQuest_ReturnsFalse()
        {
            var readiness = new VrConnectionReadiness(
                isEditor: false,
                bluetoothAdapterEnabled: false,
                metaQuestBluetoothLinked: false,
                metaQuestBluetoothDeviceName: string.Empty,
                xrRuntimeActive: false,
                xrDeviceName: string.Empty);

            Assert.IsFalse(VrPlayAvailability.IsVrPlayAvailable(readiness));
        }

        [Test]
        public void IsVrPlayAvailable_WhenQuestLinkedOverBluetooth_ReturnsTrue()
        {
            var readiness = new VrConnectionReadiness(
                isEditor: false,
                bluetoothAdapterEnabled: true,
                metaQuestBluetoothLinked: true,
                metaQuestBluetoothDeviceName: "Meta Quest 3",
                xrRuntimeActive: false,
                xrDeviceName: string.Empty);

            Assert.IsTrue(VrPlayAvailability.IsVrPlayAvailable(readiness));
        }

        [Test]
        public void IsVrPlayAvailable_WhenOnlyBluetoothAdapterEnabled_ReturnsFalse()
        {
            var readiness = new VrConnectionReadiness(
                isEditor: false,
                bluetoothAdapterEnabled: true,
                metaQuestBluetoothLinked: false,
                metaQuestBluetoothDeviceName: string.Empty,
                xrRuntimeActive: false,
                xrDeviceName: string.Empty);

            Assert.IsFalse(VrPlayAvailability.IsVrPlayAvailable(readiness));
        }

        [Test]
        public void IsVrPlayAvailable_WhenXrRuntimeActiveOnQuest_ReturnsTrue()
        {
            var readiness = new VrConnectionReadiness(
                isEditor: false,
                bluetoothAdapterEnabled: false,
                metaQuestBluetoothLinked: false,
                metaQuestBluetoothDeviceName: string.Empty,
                xrRuntimeActive: true,
                xrDeviceName: "Oculus Quest 3");

            Assert.IsTrue(VrPlayAvailability.IsVrPlayAvailable(readiness));
        }

        [Test]
        public void MetaQuestDeviceMatcher_RecognizesQuestNames()
        {
            Assert.IsTrue(MetaQuestDeviceMatcher.IsQuestDevice("Meta Quest 3"));
            Assert.IsTrue(MetaQuestDeviceMatcher.IsQuestDevice("Oculus Quest 2"));
            Assert.IsFalse(MetaQuestDeviceMatcher.IsQuestDevice("Galaxy Buds"));
        }
    }
}
