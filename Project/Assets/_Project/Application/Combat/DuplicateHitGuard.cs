namespace VRProject.Application.Combat
{
    public sealed class DuplicateHitGuard
    {
        int _lastSessionId = -1;
        int _lastTargetId = -1;
        int _lastZoneId = -1;
        int _lastTargetOnlyId = -1;
        float _lastTargetHitTime = float.NegativeInfinity;

        public bool TryRegisterHit(
            int sessionId,
            int targetId,
            int zoneId,
            float timeSeconds,
            float perTargetCooldownSeconds)
        {
            if (sessionId == _lastSessionId && targetId == _lastTargetId && zoneId == _lastZoneId)
                return false;

            if (targetId == _lastTargetOnlyId && timeSeconds - _lastTargetHitTime < perTargetCooldownSeconds)
                return false;

            _lastSessionId = sessionId;
            _lastTargetId = targetId;
            _lastZoneId = zoneId;
            _lastTargetOnlyId = targetId;
            _lastTargetHitTime = timeSeconds;
            return true;
        }

        public void Reset()
        {
            _lastSessionId = -1;
            _lastTargetId = -1;
            _lastZoneId = -1;
            _lastTargetOnlyId = -1;
            _lastTargetHitTime = float.NegativeInfinity;
        }
    }
}
