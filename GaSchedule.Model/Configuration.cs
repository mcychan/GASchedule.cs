using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GaSchedule.Model
{
    // Reads configration file and stores parsed objects
    public class Configuration
    {
		// Parsed professors
		private readonly Dictionary<int, Professor> _professors;

		// Parsed student groups
		private readonly Dictionary<int, StudentsGroup> _studentGroups;

		// Parsed courses
		private readonly Dictionary<int, Course> _courses;

		// Parsed rooms
		private readonly Dictionary<int, Room> _rooms;

        // Generate a random number  
        private static Random _random = new(DateTime.Now.Millisecond);

		// Initialize data
		public Configuration()  {
			Empty = true;
			_professors = new();
			_studentGroups = new();
            _courses = new();
            _rooms = new();
            CourseClasses = new();
        }

		// Returns professor with specified ID
		// If there is no professor with such ID method returns NULL
		Professor GetProfessorById(int id)
		{
			if (!_professors.ContainsKey(id))
				return null;
			return _professors[id];
		}

		// Returns number of parsed professors
		public int NumberOfProfessors => _professors.Count;

		// Returns student group with specified ID
		// If there is no student group with such ID method returns NULL
		StudentsGroup GetStudentsGroupById(int id)
		{
			if (!_studentGroups.ContainsKey(id))
				return null;
			return _studentGroups[id];
		}

		// Returns number of parsed student groups
		public int NumberOfStudentGroups => _studentGroups.Count;

		// Returns course with specified ID
		// If there is no course with such ID method returns NULL
		Course GetCourseById(int id)
		{
			if (!_courses.ContainsKey(id))
				return null;
			return _courses[id];	
		}

		public int NumberOfCourses => _courses.Count;

		// Returns room with specified ID
		// If there is no room with such ID method returns NULL
		public Room GetRoomById(int id)
		{
			if (!_rooms.ContainsKey(id))
				return null;
			return _rooms[id];
		}

		// Returns number of parsed rooms
		public int NumberOfRooms => _rooms.Count;

        // Returns reference to list of parsed classes
        public List<CourseClass> CourseClasses { get; }

        // Returns number of parsed classes
        public int NumberOfCourseClasses => CourseClasses.Count;

        // Returns TRUE if configuration is not parsed yet
        public bool Empty { get; private set; }

        private static void GetMember<T>(JsonElement element, ref T value)
		{
			switch (element.ValueKind)
			{
				case JsonValueKind.Number:
					if(value is int)
						value = (T) (object) element.GetInt32();
					else
						value = (T)(object)element.GetDouble();
					break;
				case JsonValueKind.False:
					value = (T)(object)false;
					break;
				case JsonValueKind.True:
					value = (T)(object)true;
					break;
				case JsonValueKind.String:
					value = (T)(object)element.GetString();
					break;
				case JsonValueKind.Object:
					value = (T)(object)element.GetRawText();
					break;
				case JsonValueKind.Array:
					value = (T)(object)element.EnumerateArray();
					break;
				default:
					value = default(T);
					break;
			}			
		}

		// Reads professor's data from config file, makes object and returns
		// Returns NULL if method cannot parse configuration data
		private Professor ParseProfessor(Dictionary<string, JsonElement> data)
		{
			if (!data.ContainsKey("id"))
				return null;
			int id = 0;
			GetMember(data["id"], ref id);

			if (!data.ContainsKey("name"))
				return null;
			string name = "";
			GetMember(data["name"], ref name);
			return new Professor(id, name);
		}

		// Reads StudentsGroup's data from config file, makes object and returns
		// Returns NULL if method cannot parse configuration data
		private StudentsGroup ParseStudentsGroup(Dictionary<string, JsonElement> data)
		{
			if (!data.ContainsKey("id"))
				return null;
			int id = 0;
			GetMember(data["id"], ref id);

			if (!data.ContainsKey("name"))
				return null;
			string name = "";
			GetMember(data["name"], ref name);

			if (!data.ContainsKey("size"))
				return null;
			int size = 0;
			GetMember(data["size"], ref size);
			return new StudentsGroup(id, name, size);
		}

		// Reads course's data from config file, makes object and returns
		// Returns NULL if method cannot parse configuration data
		private Course ParseCourse(Dictionary<string, JsonElement> data)
		{
			if (!data.ContainsKey("id"))
				return null;
			int id = 0;
			GetMember(data["id"], ref id);

			if (!data.ContainsKey("name"))
				return null;
			string name = "";
			GetMember(data["name"], ref name);

			return new Course(id, name);
		}

		// Reads rooms's data from config file, makes object and returns
		// Returns NULL if method cannot parse configuration data
		private Room ParseRoom(Dictionary<string, JsonElement> data)
		{	
			bool lab = false;
			if (data.ContainsKey("lab"))
				GetMember(data["lab"], ref lab);

			if (!data.ContainsKey("name"))
				return null;
			string name = "";
			GetMember(data["name"], ref name);

			if (!data.ContainsKey("size"))
				return null;
			int size = 0;
			GetMember(data["size"], ref size);
			return new Room(name, lab, size);
		}

		// Reads class' data from config file, makes object and returns pointer
		// Returns NULL if method cannot parse configuration data
		private CourseClass ParseCourseClass(Dictionary<string, JsonElement> data)
		{
			int pid = 0, cid = 0, dur = 1;
			bool lab = false;

			var groups = new List<StudentsGroup>();
			foreach(string key in data.Keys)
            {
				switch(key)
                {
					case "professor":
						GetMember(data[key], ref pid);
						break;
					case "course":
						GetMember(data[key], ref cid);
						break;
					case "lab":
						GetMember(data[key], ref lab);
						break;
					case "duration":
						GetMember(data[key], ref dur);
						break;
					case "group":
					case "groups":
						if (JsonValueKind.Array.Equals(data[key].ValueKind))
						{
							var grpList = data[key].EnumerateArray();
							foreach (var grp in grpList)
							{
								var g = GetStudentsGroupById(grp.GetInt32());
								if (g != null)
									groups.Add(g);
							}
						}
						else
						{
							int group = -1;
							GetMember(data[key], ref group);
							var g = GetStudentsGroupById(group);
							if (g != null)
								groups.Add(g);
						}
						break;
                }
            }

			// get professor who teaches class and course to which this class belongs
			Professor p = GetProfessorById(pid);
			Course c = GetCourseById(cid);

			// does professor and class exists
			if (c == null || p == null)
				return null;

			// make object and return
			return new CourseClass(p, c, lab, dur, groups.ToArray());
		}

		// Parse file and store parsed object
		public void ParseFile(string fileName)
		{
			// clear previously parsed objects
			_professors.Clear();
			_studentGroups.Clear();
			_courses.Clear();
			_rooms.Clear();
			CourseClasses.Clear();

			Room.RestartIDs();

			// read file into a string and deserialize JSON to a type
			var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement> >[]>(File.ReadAllText(fileName));
			foreach (Dictionary<string, Dictionary<string, JsonElement> > item in data)
			{
				foreach (var obj in item)
				{
					switch (obj.Key) {
						case "prof":
							var prof = ParseProfessor(obj.Value);
							_professors.Add(prof.Id, prof);
							break;
						case "course":
							var course = ParseCourse(obj.Value);
							_courses.Add(course.Id, course);
							break;
						case "room":
							var room = ParseRoom(obj.Value);
							_rooms.Add(room.Id, room);
							break;
						case "group":
							var group = ParseStudentsGroup(obj.Value);
							_studentGroups.Add(group.Id, group);
							break;
						case "class":
							var courseClass = ParseCourseClass(obj.Value);
							CourseClasses.Add(courseClass);
							break;
					}
				}
			}
			Empty = false;
		}

		public static int Rand()
		{
			return _random.Next(0, 32767);
		}
		public static double Random()
		{
			return _random.NextDouble();
		}

		public static int Rand(int size)
		{
			return _random.Next(size);
		}

        public static int Rand(int min, int max)
        {
            return min + Rand(max - min + 1);
        }

        public static double Rand(float min, float max)
        {
            return min + _random.NextDouble() * (max - min);
        }

        public static void Seed()
		{
			_random = new Random(DateTime.Now.Millisecond);
		}

	}
}
