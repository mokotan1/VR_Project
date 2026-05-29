namespace VRProject.Application.Mobile
{
    public static class VirtualJoystickLogic
    {
        public static float ComputeAxes(float touchX, float touchY, float anchorX, float anchorY, float radius, out float axisX, out float axisY)
        {
            if (radius <= 0f)
            {
                axisX = 0f;
                axisY = 0f;
                return 0f;
            }

            var deltaX = touchX - anchorX;
            var deltaY = touchY - anchorY;
            var magnitude = (float)System.Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (magnitude > radius)
            {
                var scale = radius / magnitude;
                deltaX *= scale;
                deltaY *= scale;
                magnitude = radius;
            }

            axisX = deltaX / radius;
            axisY = deltaY / radius;
            return magnitude;
        }
    }
}
