using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using GaSchedule.Model;

namespace GaSchedule
{
    public class HtmlOutput
    {
        private const int ROOM_COLUMN_NUMBER = Constant.DAYS_NUM + 1;
        private const int ROOM_ROW_NUMBER = Constant.DAY_HOURS + 1;

		private const string COLOR1 = "#319378";
		private const string COLOR2 = "#CE0000";
		private static char[] CRITERIAS = { 'R', 'S', 'L', 'P', 'G'};
		private static string[] OK_DESCR = { "Current room has no overlapping", "Current room has enough seats", "Current room with enough computers if they are required",
			"Professors have no overlapping classes", "Student groups has no overlapping classes" };
		private static string[] FAIL_DESCR = { "Current room has overlapping", "Current room has not enough seats", "Current room with not enough computers if they are required",
			"Professors have overlapping classes", "Student groups has overlapping classes" };
		private static string[] PERIODS = {"", "9 - 10", "10 - 11", "11 - 12", "12 - 13", "13 - 14", "14 - 15", "15 - 16", "16 - 17", "17 - 18", "18 - 19", "19 - 20", "20 - 21" };
		private static string[] WEEK_DAYS = { "MON", "TUE", "WED", "THU", "FRI"};

		private static string GetTableHeader(Room room)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<tr><th style='border: .1em solid black' scope='col' colspan='2'>Room: ");
			sb.Append(room.Name);
			sb.Append("</th>\n");
			foreach(string weekDay in WEEK_DAYS)
			sb.Append("<th style='border: .1em solid black; padding: .25em; width: 15%' scope='col' rowspan='2'>").Append(weekDay).Append("</th>\n");
			sb.Append("</tr>\n");
			sb.Append("<tr>\n");
			sb.Append("<th style='border: .1em solid black; padding: .25em'>Lab: ").Append(room.Lab ? "Yes" : "No").Append("</th>\n");
			sb.Append("<th style='border: .1em solid black; padding: .25em'>Seats: ").Append(room.NumberOfSeats).Append("</th>\n");
			sb.Append("</tr>\n");
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
				var reservation = Reservation.GetReservation(classes[cc]);
				int dayId = reservation.Day + 1;
				int periodId = reservation.Time + 1;
				int roomId = reservation.Room;

				var key = new Point(periodId, roomId);
				var roomDuration = slotTable.ContainsKey(key) ? slotTable[key] : null;
				if (roomDuration == null)
				{
					roomDuration = new int[ROOM_COLUMN_NUMBER];
					slotTable[key] = roomDuration;
				}
				roomDuration[dayId] = cc.Duration;
				for (int m = 1; m < cc.Duration; ++m)
				{
					var nextRow = new Point(periodId + m, roomId);
					if (!slotTable.ContainsKey(nextRow))
						slotTable.Add(nextRow, new int[ROOM_COLUMN_NUMBER]);
					if (slotTable[nextRow][dayId] < 1)
						slotTable[nextRow][dayId] = -1;
				}

				var roomSchedule = timeTable.ContainsKey(key) ? timeTable[key] : null;
				var sb = new StringBuilder();
				if (roomSchedule == null) {
					roomSchedule = new string[ROOM_COLUMN_NUMBER];
					timeTable[key] = roomSchedule;
				}
				sb.Append(cc.Course.Name).Append("<br />").Append(cc.Professor.Name).Append("<br />");
				sb.Append(string.Join("/", cc.Groups.Select(grp => grp.Name).ToArray()));
				sb.Append("<br />");
				if (cc.LabRequired)
					sb.Append("Lab<br />");

				for(int i=0; i< CRITERIAS.Length; ++i)
				{
					sb.Append("<span style='color:");
					if(solution.Criteria[ci + i])
					{
						sb.Append(COLOR1).Append("' title='");
						sb.Append(OK_DESCR[i]);
					}
					else
					{
						sb.Append(COLOR2).Append("' title='");
						sb.Append(FAIL_DESCR[i]);
					}
					sb.Append("'> ").Append(CRITERIAS[i]);
					sb.Append(" </span>");
				}
				roomSchedule[dayId] = sb.ToString();
				ci += CRITERIAS.Length;
			}
			return timeTable;
		}

		private static string GetHtmlCell(string content, int rowspan)
		{
			if (rowspan == 0)
				return "<td></td>";

			if (content == null)
				return "";

			StringBuilder sb = new StringBuilder();
			if (rowspan > 1)
				sb.Append("<td style='border: .1em solid black; padding: .25em' rowspan='").Append(rowspan).Append("'>");
			else
				sb.Append("<td style='border: .1em solid black; padding: .25em'>");

			sb.Append(content);
			sb.Append("</td>");
			return sb.ToString();
		}

		public static string GetResult(Schedule solution)
		{
			StringBuilder sb = new StringBuilder();
			int nr = solution.Configuration.NumberOfRooms;

			var slotTable = new Dictionary<Point, int[]>();
			var timeTable = GenerateTimeTable(solution, slotTable); // Point.X = time, Point.Y = roomId
			if (slotTable.Count == 0 || timeTable.Count == 0)
				return "";

			for (int roomId = 0; roomId < nr; ++roomId)
			{
				var room = solution.Configuration.GetRoomById(roomId);
				for (int periodId = 0; periodId < ROOM_ROW_NUMBER; ++periodId)
				{
					if (periodId == 0)
					{
						sb.Append("<div id='room_").Append(room.Name).Append("' style='padding: 0.5em'>\n");
						sb.Append("<table style='border-collapse: collapse; width: 95%'>\n");
						sb.Append(GetTableHeader(room));
					}
					else
					{						
						var key = new Point(periodId, roomId);							
						var roomDuration = slotTable.ContainsKey(key) ? slotTable[key] : null;
						var roomSchedule = timeTable.ContainsKey(key) ? timeTable[key] : null;
						sb.Append("<tr>");
						for (int i = 0; i < ROOM_COLUMN_NUMBER; ++i)
						{
							if(i == 0)
							{
								sb.Append("<th style='border: .1em solid black; padding: .25em' scope='row' colspan='2'>").Append(PERIODS[periodId]).Append("</th>\n");
								continue;
							}

							if (roomSchedule == null && roomDuration == null)
								continue;

							string content = (roomSchedule != null) ? roomSchedule[i] : null;
							sb.Append(GetHtmlCell(content, roomDuration[i]));							
						}
						sb.Append("</tr>\n");							
					}

					if (periodId == ROOM_ROW_NUMBER - 1)
						sb.Append("</table>\n</div>\n");
				}
			}

			return sb.ToString();
		}

	}
}
