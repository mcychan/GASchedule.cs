using System;
using System.Collections.Generic;

namespace GaSchedule.Model
{
    // Stores data about professor
    public class Professor
    {
        // Initializes professor data
        public Professor(int id, string name)  {
            Id = id;
            Name = name;
            CourseClasses = new List<CourseClass>();
        }

	    // Bind professor to course
	    public void AddCourseClass(CourseClass courseClass)
        {
            CourseClasses.Add(courseClass);
        }

        public override bool Equals(object obj)
        {
            return obj is Professor professor &&
                   Id == professor.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        // Returns professor's ID
        public int Id { get; set; }

        // Returns professor's name
        public string Name { get; set; }

        // Returns reference to list of classes that professor teaches
        public List<CourseClass> CourseClasses { get; set; }

    }
}
