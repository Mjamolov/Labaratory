namespace Labaratory.Helpers
{
    public static class LicenseHelper
    {
        private static readonly DateTime ExpirationDate = new(2026, 10, 30); 

        public static void CheckLicense()
        {
            if (DateTime.UtcNow > ExpirationDate)
            {
                throw new Exception("Срок действия демо-версии истёк. Обратитесь за активацией.");
            }
        }
    }
}
