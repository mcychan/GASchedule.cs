using System;
using System.Collections.Generic;
using System.Text;

namespace GaSchedule.Model
{
    public class Reservation
    {
		private readonly int nr;
		private readonly int day;
		private readonly int time;
		private readonly int room;

		public Reservation(int nr, int day, int time, int room)
		{
			this.nr = nr;
			this.day = day;
			this.time = time;
			this.room = room;
		}
		public int Nr { get { return nr; } }

		public int Day { get { return day; } }

        public int Time { get { return time; } }

		public int Room { get { return room; } }

		public override bool Equals(Object obj)
		{
			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
				return false;

			var other = (Reservation) obj;
			return GetHashCode().Equals(other.GetHashCode());
		}

		public override int GetHashCode()
		{
			return day * nr * Constant.DAY_HOURS + room * Constant.DAY_HOURS + time;
		}
	}
}
