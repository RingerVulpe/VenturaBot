using System.Text.Json;

namespace VenturaBot.Services
{
    public static class DuneFactService
    {
        private static readonly List<string> _facts = new();

        public static void LoadFacts()
        {
            try
            {
                string path = Path.Combine("Data", "DuneFunFacts.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<List<string>>(json);
                    if (loaded != null)
                        _facts.AddRange(loaded);
                    Console.WriteLine($"[DuneFactService] Loaded {_facts.Count} facts.");
                }
                else
                {
                    Console.WriteLine($"[DuneFactService] File not found: {path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DuneFactService] Error loading facts: {ex.Message}");
            }
        }

        public static string GetRandomFact()
        {
            if (_facts.Count == 0)
                return "No facts available. Fear is the mind-killer, but so is missing data.";

            var rng = new Random();
            return _facts[rng.Next(_facts.Count)];
        }
    }
}
