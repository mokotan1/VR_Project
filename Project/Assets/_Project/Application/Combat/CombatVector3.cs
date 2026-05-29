using System;

namespace VRProject.Application.Combat
{
    public readonly struct CombatVector3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public CombatVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float SqrMagnitude => X * X + Y * Y + Z * Z;

        public float Magnitude => SqrMagnitude <= 0f ? 0f : (float)Math.Sqrt(SqrMagnitude);

        public CombatVector3 Normalized
        {
            get
            {
                var mag = Magnitude;
                if (mag <= 1e-6f)
                    return Zero;
                return this / mag;
            }
        }

        public static CombatVector3 Zero => new CombatVector3(0f, 0f, 0f);

        public static float Dot(CombatVector3 a, CombatVector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static CombatVector3 operator -(CombatVector3 a, CombatVector3 b) =>
            new CombatVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static CombatVector3 operator /(CombatVector3 v, float scalar) =>
            new CombatVector3(v.X / scalar, v.Y / scalar, v.Z / scalar);
    }
}
