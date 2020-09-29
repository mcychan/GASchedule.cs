using System;
using System.Collections.Generic;
using System.Text;

namespace GaSchedule.Algorithm
{
    public class Reservation
    {
		private int nr;
		private int day;
		private int time;
		private int room;

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

		public int Index
		{
			get
			{
				return day * nr * Constant.DAY_HOURS + room * Constant.DAY_HOURS + time;
			}
		}
	}
}
