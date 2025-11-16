using Microsoft.AspNetCore.Mvc;
using MyFirstSolution.Models;
using System.Linq;

namespace MyFirstSolution.Controllers
{
    [ApiController]
    [Route("nutrition")]
    public class NutritionController : ControllerBase
    {
        [HttpPost("calculate")]
        public ActionResult<NutritionResponse> Calculate([FromBody] NutritionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Calculate BMR using Mifflin-St Jeor equation
            double bmr = 10 * request.WeightKg + 6.25 * request.HeightCm - 5 * request.Age + (request.Sex == Sex.Male ? 5 : -161);

            // Activity multipliers
            double activityMultiplier = request.ActivityLevel switch
            {
                ActivityLevel.Sedentary => 1.2,
                ActivityLevel.LightlyActive => 1.375,
                ActivityLevel.ModeratelyActive => 1.55,
                ActivityLevel.VeryActive => 1.725,
                ActivityLevel.ExtraActive => 1.9,
                _ => 1.2
            };

            double tdee = bmr * activityMultiplier;

            // Determine adjustment percent based on goal if not provided
            double adjustmentPercent;
            switch (request.Goal)
            {
                case Goal.Lose:
                    adjustmentPercent = request.AdjustmentPercent.HasValue && request.AdjustmentPercent.Value > 0 ? request.AdjustmentPercent.Value : 20.0; // deficit
                    break;
                case Goal.Gain:
                    adjustmentPercent = request.AdjustmentPercent.HasValue && request.AdjustmentPercent.Value > 0 ? request.AdjustmentPercent.Value : 10.0; // surplus
                    break;
                default:
                    adjustmentPercent = request.AdjustmentPercent ?? 0.0; // maintain or provided custom
                    break;
            }

            double targetCalories = request.Goal == Goal.Gain ? tdee * (1 + adjustmentPercent / 100.0) : tdee * (1 - adjustmentPercent / 100.0);

            // Ensure we consider BMR explicitly: split target into basal and remaining parts. If target < bmr, allocate all to basal.
            double basalCalories = bmr;
            double remainingCalories = targetCalories - basalCalories;
            if (remainingCalories < 0)
            {
                // aggressive deficit: all target calories are below basal; treat entire target as basal allocation
                basalCalories = targetCalories;
                remainingCalories = 0;
            }

            // Macronutrient splits by goal (percent of calories)
            double proteinPercent, carbsPercent, fatPercent;
            if (request.Goal == Goal.Lose)
            {
                proteinPercent = 30; carbsPercent = 40; fatPercent = 30;
            }
            else if (request.Goal == Goal.Gain)
            {
                proteinPercent = 25; carbsPercent = 50; fatPercent = 25;
            }
            else // Maintain
            {
                proteinPercent = 25; carbsPercent = 45; fatPercent = 30;
            }

            double proteinCalories = targetCalories * (proteinPercent / 100.0);
            double carbsCalories = targetCalories * (carbsPercent / 100.0);
            double fatCalories = targetCalories * (fatPercent / 100.0);

            double proteinGrams = proteinCalories / 4.0;
            double carbsGrams = carbsCalories / 4.0;
            double fatGrams = fatCalories / 9.0;

            var suggestion = new MacronutrientSuggestion
            {
                Calories = Math.Round(targetCalories, 0),
                ProteinGrams = Math.Round(proteinGrams, 0),
                CarbsGrams = Math.Round(carbsGrams, 0),
                FatGrams = Math.Round(fatGrams, 0),
                ProteinPercent = proteinPercent,
                CarbsPercent = carbsPercent,
                FatPercent = fatPercent
            };

            // Distribute calories across 5 meals: breakfast, lunch, afternoon snack, dinner, ceia
            var mealDistribution = new Dictionary<string, double>
            {
                { "Café da Manhã", 0.20 },
                { "Almoço", 0.30 },
                { "Lanche da Tarde", 0.15 },
                { "Jantar", 0.25 },
                { "Ceia", 0.10 }
            };

            var dailyMeals = new List<MealPlanItem>();

            foreach (var kv in mealDistribution)
            {
                // Calculate meal calories considering BMR: allocate basal and remaining parts per meal proportionally
                var mealBasal = basalCalories * kv.Value;
                var mealRemaining = remainingCalories * kv.Value;
                var mealCalories = mealBasal + mealRemaining;

                var mealProteinGrams = (mealCalories * (proteinPercent / 100.0)) / 4.0;
                var mealCarbsGrams = (mealCalories * (carbsPercent / 100.0)) / 4.0;
                var mealFatGrams = (mealCalories * (fatPercent / 100.0)) / 9.0;

                // Simple food suggestions based on meal and goal
                string foodsText;
                if (request.Goal == Goal.Lose)
                {
                    foodsText = kv.Key switch
                    {
                        "Café da Manhã" => "Ovos mexidos, tapioca ou pão integral, frutas vermelhas",
                        "Almoço" => "Peito de frango grelhado, salada, arroz integral, legumes",
                        "Lanche da Tarde" => "Iogurte natural, castanhas ou uma fruta",
                        "Jantar" => "Peixe assado, salada e vegetais cozidos",
                        "Ceia" => "Queijo cottage ou iogurte proteico",
                        _ => "Alimentos balanceados"
                    };
                }
                else if (request.Goal == Goal.Gain)
                {
                    foodsText = kv.Key switch
                    {
                        "Café da Manhã" => "Aveia com leite, banana, ovos",
                        "Almoço" => "Arroz, feijão, carne vermelha, legumes",
                        "Lanche da Tarde" => "Sanduíche natural com peito de peru e queijo",
                        "Jantar" => "Massa integral com molho e frango",
                        "Ceia" => "Vitamina de leite com aveia",
                        _ => "Alimentos calóricos e nutritivos"
                    };
                }
                else
                {
                    foodsText = kv.Key switch
                    {
                        "Café da Manhã" => "Iogurte, granola, frutas",
                        "Almoço" => "Carne magra, arroz integral, salada",
                        "Lanche da Tarde" => "Frutas e oleaginosas",
                        "Jantar" => "Opção leve com proteína e vegetais",
                        "Ceia" => "Chá e torrada integral",
                        _ => "Alimentos balanceados"
                    };
                }

                // Build food portions by scaling a template for the meal
                var template = GetFoodTemplateForMeal(kv.Key, request.Goal);
                double templateCalories = template.Sum(f => f.Calories * f.Quantity);
                double scale = templateCalories > 0 ? mealCalories / templateCalories : 1.0;

                var portions = template.Select(f => new FoodPortion
                {
                    Name = f.Name,
                    Unit = f.Unit,
                    Quantity = Math.Round(f.Quantity * scale, 1),
                    Calories = Math.Round(f.Calories * f.Quantity * scale, 0),
                    ProteinGrams = Math.Round(f.ProteinGrams * f.Quantity * scale, 1),
                    CarbsGrams = Math.Round(f.CarbsGrams * f.Quantity * scale, 1),
                    FatGrams = Math.Round(f.FatGrams * f.Quantity * scale, 1)
                }).ToList();

                dailyMeals.Add(new MealPlanItem
                {
                    MealName = kv.Key,
                    Calories = Math.Round(mealCalories, 0),
                    ProteinGrams = Math.Round(mealProteinGrams, 0),
                    CarbsGrams = Math.Round(mealCarbsGrams, 0),
                    FatGrams = Math.Round(mealFatGrams, 0),
                    Suggestion = foodsText,
                    Foods = portions
                });
            }

            var response = new NutritionResponse
            {
                BMR = Math.Round(bmr, 0),
                TDEE = Math.Round(tdee, 0),
                TargetCalories = Math.Round(targetCalories, 0),
                Suggestion = suggestion,
                DailyMeals = dailyMeals
            };

            return Ok(response);

            // Local helper: returns a template list of foods (per serving) for a meal and goal
            List<FoodPortion> GetFoodTemplateForMeal(string mealName, Goal goal)
            {
                // Each FoodPortion here uses Quantity = 1 representing one serving of the unit described
                var list = new List<FoodPortion>();
                if (mealName == "Café da Manhã")
                {
                    if (goal == Goal.Lose)
                    {
                        list.Add(new FoodPortion { Name = "Ovo (cozido)", Unit = "un", Quantity = 1, Calories = 78, ProteinGrams = 6.0, CarbsGrams = 0.6, FatGrams = 5.3 });
                        list.Add(new FoodPortion { Name = "Pão integral", Unit = "fatia", Quantity = 1, Calories = 70, ProteinGrams = 3.5, CarbsGrams = 12.0, FatGrams = 1.0 });
                        list.Add(new FoodPortion { Name = "Frutas vermelhas", Unit = "porção (100g)", Quantity = 1, Calories = 50, ProteinGrams = 0.7, CarbsGrams = 12.0, FatGrams = 0.3 });
                    }
                    else if (goal == Goal.Gain)
                    {
                        list.Add(new FoodPortion { Name = "Aveia", Unit = "40g", Quantity = 1, Calories = 150, ProteinGrams = 5.0, CarbsGrams = 27.0, FatGrams = 3.0 });
                        list.Add(new FoodPortion { Name = "Leite integral", Unit = "200ml", Quantity = 1, Calories = 122, ProteinGrams = 6.4, CarbsGrams = 12.0, FatGrams = 4.8 });
                        list.Add(new FoodPortion { Name = "Banana", Unit = "un", Quantity = 1, Calories = 105, ProteinGrams = 1.3, CarbsGrams = 27.0, FatGrams = 0.4 });
                    }
                    else
                    {
                        list.Add(new FoodPortion { Name = "Iogurte natural", Unit = "170g", Quantity = 1, Calories = 100, ProteinGrams = 6.0, CarbsGrams = 12.0, FatGrams = 3.5 });
                        list.Add(new FoodPortion { Name = "Granola", Unit = "30g", Quantity = 1, Calories = 130, ProteinGrams = 3.0, CarbsGrams = 18.0, FatGrams = 5.0 });
                        list.Add(new FoodPortion { Name = "Fruta", Unit = "un", Quantity = 1, Calories = 60, ProteinGrams = 0.6, CarbsGrams = 15.0, FatGrams = 0.2 });
                    }
                }
                else if (mealName == "Almoço")
                {
                    if (goal == Goal.Lose)
                    {
                        list.Add(new FoodPortion { Name = "Peito de frango (grelhado)", Unit = "100g", Quantity = 1, Calories = 165, ProteinGrams = 31.0, CarbsGrams = 0, FatGrams = 3.6 });
                        list.Add(new FoodPortion { Name = "Arroz integral cozido", Unit = "100g", Quantity = 1, Calories = 110, ProteinGrams = 2.6, CarbsGrams = 23.0, FatGrams = 0.9 });
                        list.Add(new FoodPortion { Name = "Salada", Unit = "100g", Quantity = 1, Calories = 20, ProteinGrams = 1.0, CarbsGrams = 3.0, FatGrams = 0.2 });
                    }
                    else if (goal == Goal.Gain)
                    {
                        list.Add(new FoodPortion { Name = "Carne vermelha (cozida)", Unit = "150g", Quantity = 1, Calories = 330, ProteinGrams = 26.0, CarbsGrams = 0, FatGrams = 24.0 });
                        list.Add(new FoodPortion { Name = "Arroz branco cozido", Unit = "150g", Quantity = 1, Calories = 195, ProteinGrams = 4.0, CarbsGrams = 43.0, FatGrams = 0.6 });
                        list.Add(new FoodPortion { Name = "Feijão", Unit = "100g", Quantity = 1, Calories = 127, ProteinGrams = 8.7, CarbsGrams = 22.8, FatGrams = 0.5 });
                    }
                    else
                    {
                        list.Add(new FoodPortion { Name = "Peito de frango", Unit = "100g", Quantity = 1, Calories = 165, ProteinGrams = 31.0, CarbsGrams = 0, FatGrams = 3.6 });
                        list.Add(new FoodPortion { Name = "Arroz integral", Unit = "150g", Quantity = 1, Calories = 165, ProteinGrams = 3.9, CarbsGrams = 34.5, FatGrams = 1.4 });
                        list.Add(new FoodPortion { Name = "Salada/Legumes", Unit = "150g", Quantity = 1, Calories = 40, ProteinGrams = 2.0, CarbsGrams = 6.0, FatGrams = 0.5 });
                    }
                }
                else if (mealName == "Lanche da Tarde")
                {
                    if (goal == Goal.Lose)
                    {
                        list.Add(new FoodPortion { Name = "Iogurte natural", Unit = "170g", Quantity = 1, Calories = 100, ProteinGrams = 6.0, CarbsGrams = 12.0, FatGrams = 3.5 });
                        list.Add(new FoodPortion { Name = "Castanhas", Unit = "30g", Quantity = 1, Calories = 180, ProteinGrams = 4.0, CarbsGrams = 6.0, FatGrams = 16.0 });
                    }
                    else if (goal == Goal.Gain)
                    {
                        list.Add(new FoodPortion { Name = "Sanduíche natural", Unit = "1 porção", Quantity = 1, Calories = 350, ProteinGrams = 20.0, CarbsGrams = 35.0, FatGrams = 12.0 });
                        list.Add(new FoodPortion { Name = "Fruta", Unit = "un", Quantity = 1, Calories = 60, ProteinGrams = 0.6, CarbsGrams = 15.0, FatGrams = 0.2 });
                    }
                    else
                    {
                        list.Add(new FoodPortion { Name = "Frutas", Unit = "1 porção", Quantity = 1, Calories = 80, ProteinGrams = 1.0, CarbsGrams = 20.0, FatGrams = 0.5 });
                        list.Add(new FoodPortion { Name = "Oleaginosas", Unit = "30g", Quantity = 1, Calories = 180, ProteinGrams = 4.0, CarbsGrams = 6.0, FatGrams = 16.0 });
                    }
                }
                else if (mealName == "Jantar")
                {
                    if (goal == Goal.Lose)
                    {
                        list.Add(new FoodPortion { Name = "Peixe (assado)", Unit = "100g", Quantity = 1, Calories = 206, ProteinGrams = 22.0, CarbsGrams = 0, FatGrams = 12.0 });
                        list.Add(new FoodPortion { Name = "Vegetais cozidos", Unit = "150g", Quantity = 1, Calories = 50, ProteinGrams = 2.5, CarbsGrams = 10.0, FatGrams = 0.5 });
                    }
                    else if (goal == Goal.Gain)
                    {
                        list.Add(new FoodPortion { Name = "Massa integral cozida", Unit = "200g", Quantity = 1, Calories = 260, ProteinGrams = 9.0, CarbsGrams = 52.0, FatGrams = 2.0 });
                        list.Add(new FoodPortion { Name = "Frango grelhado", Unit = "150g", Quantity = 1, Calories = 247, ProteinGrams = 46.5, CarbsGrams = 0, FatGrams = 5.3 });
                    }
                    else
                    {
                        list.Add(new FoodPortion { Name = "Opção leve proteína", Unit = "100g", Quantity = 1, Calories = 150, ProteinGrams = 20.0, CarbsGrams = 5.0, FatGrams = 5.0 });
                        list.Add(new FoodPortion { Name = "Vegetais", Unit = "150g", Quantity = 1, Calories = 40, ProteinGrams = 2.0, CarbsGrams = 8.0, FatGrams = 0.5 });
                    }
                }
                else if (mealName == "Ceia")
                {
                    if (goal == Goal.Lose)
                    {
                        list.Add(new FoodPortion { Name = "Queijo cottage", Unit = "100g", Quantity = 1, Calories = 98, ProteinGrams = 11.1, CarbsGrams = 3.4, FatGrams = 4.3 });
                    }
                    else if (goal == Goal.Gain)
                    {
                        list.Add(new FoodPortion { Name = "Vitamina (leite + aveia)", Unit = "1 porção", Quantity = 1, Calories = 250, ProteinGrams = 10.0, CarbsGrams = 35.0, FatGrams = 6.0 });
                    }
                    else
                    {
                        list.Add(new FoodPortion { Name = "Iogurte pequeno", Unit = "100g", Quantity = 1, Calories = 60, ProteinGrams = 3.5, CarbsGrams = 7.0, FatGrams = 1.5 });
                    }
                }

                return list;
            }
        }
    }
}
