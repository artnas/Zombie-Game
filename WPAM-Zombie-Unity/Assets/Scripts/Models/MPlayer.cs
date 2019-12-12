namespace DefaultNamespace.Models
{
    [System.Serializable]
    public class MPlayer
    {
        public static readonly int HealthMax = 3;
        public static readonly int AmmoMax = 10;
        
        public int Health;
        public int Ammo;
        public float FoodSupplies;
        public float WaterSupplies;
        public int BuildingMaterials;
    }
}