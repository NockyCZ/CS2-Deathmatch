namespace Deathmatch
{
    public partial class Deathmatch
    {
        public enum RestrictType
        {
            VIP,
            NonVIP,
        }

        public enum AcquireResult : int
        {
            Allowed = 0,
            InvalidItem,
            AlreadyOwned,
            AlreadyPurchased,
            ReachedGrenadeTypeLimit,
            ReachedGrenadeTotalLimit,
            NotAllowedByTeam,
            NotAllowedByMap,
            NotAllowedByMode,
            NotAllowedForPurchase,
            NotAllowedByProhibition,
        };

        public enum AcquireMethod : int
        {
            PickUp = 0,
            Buy,
        };
    }
}