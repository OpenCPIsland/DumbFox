using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Common.Utils
{
    public static class PickupableManager
    {
        public static List<PickupableData> Pickupables { get; private set; } = new();

        private static bool _loaded;

        public static void Load(string file)
        {
            if (_loaded)
                return;
            string configPath = Path.Combine(AppContext.BaseDirectory, file);
            string json = File.ReadAllText(configPath);

            var rooms = JsonSerializer.Deserialize<
                Dictionary<string, Dictionary<string, PickupableData>>
            >(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (rooms == null)
                return;

            foreach (var room in rooms)
            {
                foreach (var entry in room.Value)
                {
                    entry.Value.Room = room.Key;
                    entry.Value.Path = entry.Key;

                    Pickupables.Add(entry.Value);
                }
            }

            _loaded = true;
        }
    }
}
