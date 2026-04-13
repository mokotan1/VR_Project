namespace VRProject.Presentation.OsFpsInspired
{
    /// <summary>
    /// 빈 탄창일 때 발사 입력이 던지기로 처리되는지 판별합니다.
    /// </summary>
    public static class OsFpsInspiredWeaponThrowGate
    {
        public static bool ShouldThrowOnFire(int ammoInMag, bool isReloading)
        {
            return ammoInMag <= 0 && !isReloading;
        }
    }
}
