using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Zork
{
    public class Game
    {
        public World World { get; private set; }
        public Player Player { get; private set; }

        public static Game Load(string filename)
        {
            Game game = JsonConvert.DeserializeObject<Game>(File.ReadAllText(filename));

            // Ensure that the player is spawned with a valid starting location
            game.Player = new Player(game.World, game.World.StartingLocation);
            return game;
        }

        public void Run()
        {
            while (true)
            {
                Console.WriteLine(Player.Location.Description);
                Console.WriteLine("Which direction would you like to go?");
                string input = Console.ReadLine();
                if (Enum.TryParse(input, true, out Directions direction))
                {
                    Player.Move(direction);
                }
                else
                {
                    Console.WriteLine("Invalid direction.");
                }
            }
        }
    }

    public class World
    {
        [JsonProperty]
        public string StartingLocation { get; set; } // Ensure it is public to be deserialized
        public HashSet<Room> Rooms { get; private set; }
        [JsonIgnore]
        public IReadOnlyDictionary<string, Room> RoomsByName => mRoomsByName;
        private Dictionary<string, Room> mRoomsByName;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            mRoomsByName = Rooms.ToDictionary(room => room.Name, room => room);
            foreach (Room room in Rooms)
            {
                room.UpdateNeighbors(this);
            }
        }

        public Room GetRoom(string name)
        {
            Room room;
            return mRoomsByName.TryGetValue(name, out room) ? room : null;
        }
    }

    public class Player
    {
        public World World { get; private set; }
        public Room Location { get; private set; }

        public Player(World world, string startingLocation)
        {
            World = world;

            // Ensure LocationName is initialized with the starting location
            LocationName = startingLocation;
        }

        public string LocationName
        {
            get => Location?.Name;
            set
            {
                if (World != null)
                {
                    Location = World.GetRoom(value);
                    if (Location == null)
                    {
                        Console.WriteLine($"Room '{value}' does not exist.");
                    }
                }
            }
        }

        public void Move(Directions direction)
        {
            if (Location.Neighbors.TryGetValue(direction, out Room destination))
            {
                Location = destination;
            }
            else
            {
                Console.WriteLine("The way is shut!");
            }
        }
    }

    public class Room
    {
        [JsonProperty(Order = 1)]
        public string Name { get; private set; }
        [JsonProperty(Order = 2)]
        public string Description { get; private set; }
        [JsonProperty("Neighbors", Order = 3)]
        private Dictionary<Directions, string> NeighborNames { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<Directions, Room> Neighbors { get; private set; }

        public void UpdateNeighbors(World world)
        {
            Neighbors = NeighborNames
                .Where(neighbor => world.GetRoom(neighbor.Value) != null)
                .ToDictionary(
                    neighbor => neighbor.Key,
                    neighbor => world.GetRoom(neighbor.Value)
                );
        }

        public override int GetHashCode() => Name.GetHashCode();
    }

    public enum Directions
    {
        North,
        South,
        East,
        West
    }

    class Program
    {
        static void Main(string[] args)
        {
            Game game = Game.Load("Zork.json");
            game.Run();
        }
    }
}
