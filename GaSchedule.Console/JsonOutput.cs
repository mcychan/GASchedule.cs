using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using GaSchedule.Algorithm;

namespace GaSchedule
{
    public class JsonOutput
    {
        private const int ROOM_COLUMN_NUMBER = Constant.DAYS_NUM + 1;
        private const int ROOM_ROW_NUMBER = Constant.DAY_HOURS + 1;

		private static char[] CRITERIAS = { 'R', 'S', 'L', 'P', 'G'};
		private static string[] CRITERIAS_DESCR = { "Current room has {0}overlapping", "Current room has {0}enough seats", "Current room with {0}enough computers if they are required",
			"Professors have {0}overlapping classes", "Student groups has {0}overlapping classes" };
		private static string[] PERIODS = {"", "9 - 10", "10 - 11", "11 - 12", "12 - 13", "13 - 14", "14 - 15", "15 - 16", "16 - 17", "17 - 18", "18 - 19", "19 - 20", "20 - 21" };
		private static string[] WEEK_DAYS = { "MON", "TUE", "WED", "THU", "FRI"};

		private static string GetRoomJson(Room room)
		{
			var sb = new StringBuilder("\"Room ");
			sb.Append(room.Id).Append("\": ");
			sb.Append(JsonSerializer.Serialize(room));
			return sb.ToString();
		}

		private static Dictionary<Point, string[]> GenerateTimeTable(Schedule solution, Dictionary<Point, int[]> slotTable)
		{
			int numberOfRooms = solution.Configuration.NumberOfRooms;
			int daySize = Constant.DAY_HOURS * numberOfRooms;

			int ci = 0;
			var classes = solution.Classes;

			var timeTable = new Dictionary<Point, string[]>();
			foreach (var cc in classes.Keys)
			{
				// coordinate of time-space slot
				var reservation = classes[cc];
				int day = reservation.Day + 1;
				int time = reservation.Time + 1;
				int room = reservation.Room;

				var key = new Point(time, room);
				var roomDuration = slotTable.ContainsKey(key) ? slotTable[key] : null;
				if (roomDuration == null)
				{
					roomDuration = new int[ROOM_COLUMN_NUMBER];
					slotTable[key] = roomDuration;
				}
				roomDuration[day] = cc.Duration;
				for (int m = 1; m < cc.Duration; ++m)
				{
					var nextRow = new Point(time + m, room);
					if (!slotTable.ContainsKey(nextRow))
						slotTable.Add(nextRow, new int[ROOM_COLUMN_NUMBER]);
					if (slotTable[nextRow][day] < 1)
						slotTable[nextRow][day] = -1;
				}

				var roomSchedule = timeTable.ContainsKey(key) ? timeTable[key] : null;
				var sb = new StringBuilder();
				if (roomSchedule == null) {
					roomSchedule = new string[ROOM_COLUMN_NUMBER];
					timeTable[key] = roomSchedule;
				}
				sb.Append("\"Course\": \"").Append(cc.Course.Name).Append("\"");
				sb.Append(", \"Prosfessor\": \"").Append(cc.Professor.Name).Append("\"");
				sb.Append(", \"Groups\": \"").Append(string.Join("/", cc.Groups.Select(grp => grp.Name).ToArray()));
				sb.Append("\", ");
				if (cc.LabRequired)
					sb.Append("\"Lab\": true, ");
				sb.Append("\"Remarks\": [");

				for (int i=0; i< CRITERIAS.Length; ++i)
                {
					sb.Append("{");
					if(solution.Criteria[ci + i])
                    {
						sb.Append("\"Ok\": \"");
						sb.Append(string.Format(CRITERIAS_DESCR[i], (i == 1 || i == 2) ? "" : "no ")).Append("\"");
					}
					else
                    {
						sb.Append("\"Fail\": \"");
						sb.Append(string.Format(CRITERIAS_DESCR[i], (i == 1 || i == 2) ? "not " : "")).Append("\"");
					}
					sb.Append(", \"Code\": \"").Append(CRITERIAS[i]).Append("\"");
					sb.Append("}");

					if(i < CRITERIAS.Length - 1)
						sb.Append(", ");
				}
				sb.Append("]");
				roomSchedule[day] = sb.ToString();
				ci += CRITERIAS.Length;
			}
			return timeTable;
		}

		private static string GetCell(string content, int duration)
        {
			if (duration == 0)
				return "{}";

			if (content == null)
				return "{}";

			StringBuilder sb = new StringBuilder("{");
			sb.Append(content);
			sb.Append(", \"Duration\": ").Append(duration);
			return sb + "}";
		}

		public static string GetResult(Schedule solution)
		{
			StringBuilder sb = new StringBuilder();
			int nr = solution.Configuration.NumberOfRooms;

			var slotTable = new Dictionary<Point, int[]>();
			var timeTable = GenerateTimeTable(solution, slotTable); // Point.X = time, Point.Y = roomId
			if (slotTable.Count == 0 || timeTable.Count == 0)
				return "";

			for (int k = 0; k < nr; k++)
			{
				var room = solution.Configuration.GetRoomById(k);
				for (int j = 0; j < ROOM_ROW_NUMBER; ++j)
				{
					if (j == 0)
					{
						if (k > 0)
							sb.Append(", ");
						sb.Append(GetRoomJson(room));						
					}
					else
					{
						var key = new Point(j, k);
						var roomDuration = slotTable.ContainsKey(key) ? slotTable[key] : null;
						var roomSchedule = timeTable.ContainsKey(key) ? timeTable[key] : null;
						sb.Append("\"Room ").Append(room.Id).Append(" (");
						sb.Append(PERIODS[j]).Append(")\": {");
						for (int i = 0; i < ROOM_COLUMN_NUMBER; ++i)
						{
							if (i == 0)
								continue;

							if (roomSchedule == null && roomDuration == null)
								continue;

							string content = (roomSchedule != null) ? roomSchedule[i] : null;
							sb.Append("\"").Append(WEEK_DAYS[i - 1]).Append("\": ");
							sb.Append(GetCell(content, roomDuration[i]));

							if (i < ROOM_COLUMN_NUMBER - 1)
								sb.Append(", ");
						}
						sb.Append("}");
					}

					if (j < ROOM_ROW_NUMBER - 1)
						sb.Append(", ");
				}
			}

			return "{" + sb.Replace("\\u022", "\"") + "}";
		}

	}
}
