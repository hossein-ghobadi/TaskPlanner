namespace TaskPlanner.Domain.Entities.Calculation
{
    public static class CalculationCategoryExtensions
    {
        public static string GetDisplayName(this CalculationCategory category) => category switch
        {
            CalculationCategory.Chanelium => "چنلیوم",
            CalculationCategory.Swedish => "سوئدی",
            CalculationCategory.SwedishMax => "سوئدی مکس",
            CalculationCategory.Plastic => "پلاستیک",
            CalculationCategory.Cut => "برش",
            CalculationCategory.SimpleSteel => "استیل ساده",
            CalculationCategory.SimpleIron => "آهن ساده",
            CalculationCategory.RingIron => "آهن رینگی",
            CalculationCategory.RingSteel => "استیل رینگی",
            CalculationCategory.VacuumIronRing => "وکیوم با رینگ آهن",
            CalculationCategory.VacuumSteelRing => "وکیوم با رینگ استیل",
            _ => "نامشخص"
        };

        /// <summary>
        /// آهن ساده و استیل ساده همیشه تک‌لایه هستند.
        /// </summary>
        public static bool IsAlwaysSingleLayer(this CalculationCategory category) =>
            category is CalculationCategory.SimpleIron or CalculationCategory.SimpleSteel;

        /// <summary>
        /// در وکیوم، پرتی به مساحت مصرفی اضافه می‌شود.
        /// </summary>
        public static bool IncludesWasteInMaterialCost(this CalculationCategory category) =>
            category is CalculationCategory.VacuumIronRing or CalculationCategory.VacuumSteelRing;

        /// <summary>
        /// دسته برش پانچ متریال ندارد.
        /// </summary>
        public static bool CanHavePunchMaterial(this CalculationCategory category) =>
            category != CalculationCategory.Cut;

        /// <summary>
        /// دسته برش هزینه لبه ندارد.
        /// </summary>
        public static bool CanHaveEdgeCost(this CalculationCategory category) =>
            category != CalculationCategory.Cut;

        public static bool SupportsLayerMode(this CalculationCategory category, LayerMode mode)
        {
            if (category.IsAlwaysSingleLayer())
                return mode == LayerMode.Single;

            return mode is LayerMode.Single or LayerMode.Double;
        }
    }

    public static class LayerModeExtensions
    {
        public static string GetDisplayName(this LayerMode mode) => mode switch
        {
            LayerMode.Single => "تک‌لایه",
            LayerMode.Double => "دو‌لایه",
            _ => "نامشخص"
        };
    }
}
