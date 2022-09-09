using System.Collections.Generic;
using System.Linq;

namespace GaSchedule.Model
{
    public class CourseClass
    {

		// Initializes class object
		public CourseClass(Professor professor, Course course, bool requiresLab, int duration, params StudentsGroup[] groups)
        {
			Professor = professor;
			Course = course;
			NumberOfSeats = 0;
			LabRequired = requiresLab;
			Duration = duration;
			Groups = new List<StudentsGroup>();

			// bind professor to class
			Professor.AddCourseClass(this);

			// bind student groups to class
			foreach(StudentsGroup group in groups)
            {
				group.AddClass(this);
				Groups.Add(group);
				NumberOfSeats += group.NumberOfStudents;
			}
		}

		// Returns TRUE if another class has one or overlapping student groups.
		public bool GroupsOverlap(CourseClass c)
        {
			return Groups.Intersect(c.Groups).Any();
        }

		// Returns TRUE if another class has same professor.
		public bool ProfessorOverlaps(CourseClass c) {
			return Professor.Equals(c.Professor);
		}

		// Return pointer to professor who teaches
		public Professor Professor { get; set; }

		// Return pointer to course to which class belongs
		public Course Course { get; set; }

		// Returns reference to list of student groups who attend class
		public List<StudentsGroup> Groups { get; set; }

		// Returns number of seats (students) required in room
		public int NumberOfSeats { get; set; }

		// Returns TRUE if class requires computers in room.
		public bool LabRequired { get; set; }

		// Returns duration of class in hours
		public int Duration { get; set; }
	}
}
