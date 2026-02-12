namespace Utilities.Permissions
{
    public static class Permissions
    {
        //User Permissions
        public const string Reporter = "RJD#SS1#";
        

        public static readonly List<PermissionMeta> PermissionsList =
        [
                // User Permissions
            new PermissionMeta(Reporter, nameof(Reporter), "see reports", ["Admin"]),
        ];
        public static readonly IEnumerable<string> AllRoles = ["Admin", "User"];
    }
    public record PermissionMeta(string Code, string Title, string Description, IEnumerable<string> Roles);
}
