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

    public class NutritionRequest
    {
        [Required]
        [Range(10, 100)]
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

        // Optional: desired deficit percentage (0-50). Default 20% when not provided or 0.
        [Range(0, 50)]
        public double? DeficitPercent { get; set; }
    }
}
