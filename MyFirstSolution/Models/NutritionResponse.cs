namespace MyFirstSolution.Models
{
    public class FoodPortion
    {
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double Calories { get; set; }
        public double ProteinGrams { get; set; }
        public double CarbsGrams { get; set; }
        public double FatGrams { get; set; }
    }

    public class MealPlanItem
    {
        public string MealName { get; set; } = string.Empty;
        public double Calories { get; set; }
        public double ProteinGrams { get; set; }
        public double CarbsGrams { get; set; }
        public double FatGrams { get; set; }
        public string Suggestion { get; set; } = string.Empty; // short textual suggestion/example foods
        public List<FoodPortion> Foods { get; set; } = new List<FoodPortion>();
    }

    public class MacronutrientSuggestion
    {
        public double Calories { get; set; }
        public double ProteinGrams { get; set; }
        public double CarbsGrams { get; set; }
        public double FatGrams { get; set; }
        public double ProteinPercent { get; set; }
        public double CarbsPercent { get; set; }
        public double FatPercent { get; set; }
    }

    public class NutritionResponse
    {
        public double BMR { get; set; }
        public double TDEE { get; set; }
        public double TargetCalories { get; set; }
        public MacronutrientSuggestion Suggestion { get; set; } = new MacronutrientSuggestion();
        public List<MealPlanItem> DailyMeals { get; set; } = new List<MealPlanItem>();
    }
}
