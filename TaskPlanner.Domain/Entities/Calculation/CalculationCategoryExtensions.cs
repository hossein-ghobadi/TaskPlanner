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

        /// <summary>
        /// دسته برش هزینه SMD ندارد.
        /// </summary>
        public static bool CanHaveSmdCost(this CalculationCategory category) =>
            category != CalculationCategory.Cut;

        /// <summary>
        /// آهن ساده و استیل ساده SMD جلو ندارند.
        /// </summary>
        public static bool CanHaveFrontSmd(this CalculationCategory category) =>
            category.CanHaveSmdCost() && !category.IsAlwaysSingleLayer();

        /// <summary>
        /// همه دسته‌ها به‌جز برش می‌توانند SMD پشت داشته باشند.
        /// </summary>
        public static bool CanHaveBackSmd(this CalculationCategory category) =>
            category.CanHaveSmdCost();

        /// <summary>
        /// آهن/استیل ساده: تعداد SMD پشت از قطر قلم حروف محاسبه می‌شود.
        /// </summary>
        public static bool UsesPenDiameterForBackSmd(this CalculationCategory category) =>
            category.IsAlwaysSingleLayer();

        /// <summary>
        /// ماژول فلزات فقط برای آهن/استیل ساده، رینگی و وکیوم با رینگ.
        /// </summary>
        public static bool CanHaveMetalsCost(this CalculationCategory category) =>
            category is CalculationCategory.SimpleIron
                or CalculationCategory.SimpleSteel
                or CalculationCategory.RingIron
                or CalculationCategory.RingSteel
                or CalculationCategory.VacuumIronRing
                or CalculationCategory.VacuumSteelRing;

        /// <summary>
        /// هزینه دولبه رینگ فقط برای دسته‌های رینگی/وکیوم‌رینگی.
        /// </summary>
        public static bool CanHaveDoubleEdgeRingCost(this CalculationCategory category) =>
            category is CalculationCategory.RingIron
                or CalculationCategory.RingSteel
                or CalculationCategory.VacuumIronRing
                or CalculationCategory.VacuumSteelRing;

        /// <summary>
        /// دسته برش هزینه PVC ندارد.
        /// </summary>
        public static bool CanHavePvcCost(this CalculationCategory category) =>
            category != CalculationCategory.Cut;

        /// <summary>
        /// آهن ساده و استیل ساده هزینه چسب ندارند.
        /// </summary>
        public static bool CanHaveGlueCost(this CalculationCategory category) =>
            !category.IsAlwaysSingleLayer();

        /// <summary>
        /// برش: هزینه چسب از مجموع مساحت لایه‌ها محاسبه می‌شود.
        /// </summary>
        public static bool UsesLayerAreasForGlueCost(this CalculationCategory category) =>
            category == CalculationCategory.Cut;

        /// <summary>
        /// برش، آهن ساده و استیل ساده هزینه کریستال ندارند.
        /// </summary>
        public static bool CanHaveCrystalCost(this CalculationCategory category) =>
            category != CalculationCategory.Cut && !category.IsAlwaysSingleLayer();

        /// <summary>
        /// هزینه رنگ فقط برای برش، آهن ساده، آهن رینگی و وکیوم با رینگ آهن.
        /// </summary>
        public static bool CanHavePaintCost(this CalculationCategory category) =>
            category is CalculationCategory.Cut
                or CalculationCategory.SimpleIron
                or CalculationCategory.RingIron
                or CalculationCategory.VacuumIronRing;

        /// <summary>
        /// هزینه چوب فقط برای وکیوم با رینگ آهن و وکیوم با رینگ استیل.
        /// </summary>
        public static bool CanHaveWoodCost(this CalculationCategory category) =>
            category is CalculationCategory.VacuumIronRing
                or CalculationCategory.VacuumSteelRing;

        /// <summary>
        /// دستمزد وکیوم فقط برای وکیوم با رینگ آهن و وکیوم با رینگ استیل.
        /// </summary>
        public static bool CanHaveVacuumWageCost(this CalculationCategory category) =>
            category is CalculationCategory.VacuumIronRing
                or CalculationCategory.VacuumSteelRing;

        /// <summary>
        /// ماژول اختصاصی برش فقط برای دسته برش.
        /// </summary>
        public static bool CanHaveCutSpecificCost(this CalculationCategory category) =>
            category == CalculationCategory.Cut;

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
