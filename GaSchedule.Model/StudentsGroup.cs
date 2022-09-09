using System;
using System.Collections.Generic;

namespace GaSchedule.Model
{
	// Stores data about student group
	public class StudentsGroup
    {
		// Initializes student group data
		public StudentsGroup(int id, string name, int numberOfStudents)
        {
			Id = id;
			Name = name;
			NumberOfStudents = numberOfStudents;
			CourseClasses = new List<CourseClass>();
		}

		// Bind group to class
		public void AddClass(CourseClass courseClass)
        {			
			CourseClasses.Add(courseClass);
		}

        public override bool Equals(object obj)
        {
            return obj is StudentsGroup group &&
                   Id == group.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        // Returns student group ID
        public int Id { get; set; }

		// Returns name of student group
		public string Name { get; set; }

		// Returns number of students in group
		public int NumberOfStudents { get; set; }

		// Returns reference to list of classes that group attends
		public List<CourseClass> CourseClasses { get; set; }

	}
}
