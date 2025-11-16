namespace MyFirstSolution.Models
{
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
    }
}
