using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Interaction;

namespace VRProject.Tests.EditMode
{
    public sealed class GazeInteractionTests
    {
        [Test]
        public void GazeInteractable_InvokesConfiguredEvents()
        {
            var go = new GameObject("Interactable");
            var interactable = go.AddComponent<GazeInteractable>();
            var entered = 0;
            var exited = 0;
            var clicked = 0;
            interactable.GazeEntered.AddListener(() => entered++);
            interactable.GazeExited.AddListener(() => exited++);
            interactable.Clicked.AddListener(() => clicked++);

            interactable.NotifyGazeEnter();
            interactable.NotifyClick();
            interactable.NotifyGazeExit();

            Assert.That(entered, Is.EqualTo(1));
            Assert.That(clicked, Is.EqualTo(1));
            Assert.That(exited, Is.EqualTo(1));
        }

        [Test]
        public void GazeRaycastState_EmitsEnterOnlyOnce_ForSameTarget()
        {
            var target = new GameObject("Target").AddComponent<GazeInteractable>();

            var first = GazeRaycastState.EvaluateTransition(null, target);
            var second = GazeRaycastState.EvaluateTransition(target, target);

            Assert.That(first.Enter, Is.SameAs(target));
            Assert.That(first.Exit, Is.Null);
            Assert.That(second.Enter, Is.Null);
            Assert.That(second.Exit, Is.Null);
        }

        [Test]
        public void GazeRaycastState_ExitsPreviousAndEntersNext_WhenTargetChanges()
        {
            var previous = new GameObject("Previous").AddComponent<GazeInteractable>();
            var next = new GameObject("Next").AddComponent<GazeInteractable>();

            var transition = GazeRaycastState.EvaluateTransition(previous, next);

            Assert.That(transition.Exit, Is.SameAs(previous));
            Assert.That(transition.Enter, Is.SameAs(next));
        }
    }
}
