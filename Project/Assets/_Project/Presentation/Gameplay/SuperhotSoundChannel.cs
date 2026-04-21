using System;
using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public readonly struct SuperhotSoundEvent
    {
        public readonly Vector3 Origin;
        public readonly float Radius;

        public SuperhotSoundEvent(Vector3 origin, float radius)
        {
            Origin = origin;
            Radius = radius;
        }
    }

    public static class SuperhotSoundChannel
    {
        public static event Action<SuperhotSoundEvent> OnSoundEmitted;

        public static void Emit(SuperhotSoundEvent e) => OnSoundEmitted?.Invoke(e);
    }
}
