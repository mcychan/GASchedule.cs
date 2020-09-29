
namespace GaSchedule.Algorithm
{
    // Stores data about course
    public class Course
    {
        // Initializes course
        public Course(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        // Returns course ID
        public int Id { get; set; }

        // Returns course name
        public string Name { get; set; }
    }
}
