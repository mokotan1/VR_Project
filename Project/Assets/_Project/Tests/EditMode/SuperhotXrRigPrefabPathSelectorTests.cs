using System.Collections.Generic;
using NUnit.Framework;
using VRProject.Application.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class SuperhotXrRigPrefabPathSelectorTests
    {
        [Test]
        public void SelectPreferredPath_NullOrEmpty_ReturnsNull()
        {
            Assert.IsNull(SuperhotXrRigPrefabPathSelector.SelectPreferredPath(null));
            Assert.IsNull(SuperhotXrRigPrefabPathSelector.SelectPreferredPath(new List<string>()));
        }

        [Test]
        public void SelectPreferredPath_PrefersStarterAssetsFolderOverOtherVrPath()
        {
            var paths = new[]
            {
                "Assets/Samples/XR Interaction Toolkit/3.4.0/SomeOther/Prefabs/XR Origin (XR Rig).prefab",
                "Assets/Samples/XR Interaction Toolkit/3.4.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab"
            };

            var chosen = SuperhotXrRigPrefabPathSelector.SelectPreferredPath(paths);
            Assert.AreEqual(paths[1], chosen);
        }

        [Test]
        public void SelectPreferredPath_SkipsArStarterAssetsFolder()
        {
            var paths = new[]
            {
                "Assets/Samples/XR Interaction Toolkit/3.4.0/AR Starter Assets/Prefabs/XR Origin (XR Rig).prefab",
                "Assets/Samples/XR Interaction Toolkit/3.4.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab"
            };

            var chosen = SuperhotXrRigPrefabPathSelector.SelectPreferredPath(paths);
            Assert.AreEqual(paths[1], chosen);
        }

        [Test]
        public void SelectPreferredPath_SingleValidPath_ReturnsIt()
        {
            var path = "Assets/Samples/XR Interaction Toolkit/3.4.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            var paths = new[] { path };

            Assert.AreEqual(path, SuperhotXrRigPrefabPathSelector.SelectPreferredPath(paths));
        }
    }
}
