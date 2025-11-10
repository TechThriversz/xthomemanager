namespace XTHomeManager.API.Data
{
    public class UserPermissionsDto
    {
        public bool CanUsePasswordVault { get; set; }
        public bool CanUseFamilyMembers { get; set; }
        public bool CanUseMedicalRecords { get; set; }
    }
}
