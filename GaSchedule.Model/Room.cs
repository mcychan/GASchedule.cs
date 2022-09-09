
namespace GaSchedule.Model
{
    // Stores data about classroom
    public class Room
    {
        // ID counter used to assign IDs automatically
        private static int _nextRoomId = 0;

        // Initializes room data and assign ID to room
        public Room(string name, bool lab, int numberOfSeats)
        {
            Id = _nextRoomId++;
            Name = name;
            Lab = lab;
            NumberOfSeats = numberOfSeats;
        }

        // Returns room ID - automatically assigned
        public int Id { get; set; }

        // Returns name
        public string Name { get; set; }

        // Returns TRUE if room has computers otherwise it returns FALSE
        public bool Lab { get; set; }

        // Returns number of seats in room
        public int NumberOfSeats { get; set; }

        // Restarts ID assigments
        public static void RestartIDs() { _nextRoomId = 0; }
    }
}
