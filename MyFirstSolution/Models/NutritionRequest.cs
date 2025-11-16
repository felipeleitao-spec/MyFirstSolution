using System.ComponentModel.DataAnnotations;

namespace MyFirstSolution.Models
{
    public enum Sex
    {
        Male = 1,
        Female = 2
    }

    public enum ActivityLevel
    {
        Sedentary = 1,       // little or no exercise
        LightlyActive = 2,   // light exercise/sports 1-3 days/week
        ModeratelyActive =3 ,// moderate exercise/sports 3-5 days/week
        VeryActive = 4,      // hard exercise/sports 6-7 days a week
        ExtraActive = 5      // very hard exercise or physical job
    }

    public enum Goal
    {
        Lose = 1,
        Maintain = 2,
        Gain = 3
    }

    public class NutritionRequest
    {
        [Required]
        [Range(10, 120)]
        public int Age { get; set; }

        [Required]
        public Sex Sex { get; set; }

        [Required]
        [Range(30, 300)]
        public double WeightKg { get; set; }

        [Required]
        [Range(100, 250)]
        public double HeightCm { get; set; }

        [Required]
        public ActivityLevel ActivityLevel { get; set; }

        // Optional: desired deficit or surplus percentage (0-50). Default 20% deficit for Lose, 10% surplus for Gain.
        [Range(0,50)]
        public double? AdjustmentPercent { get; set; }

        // Goal: Lose, Maintain, Gain (affects calculation and suggestions)
        public Goal Goal { get; set; } = Goal.Lose;
    }
}
