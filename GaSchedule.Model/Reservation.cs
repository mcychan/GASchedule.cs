using System;
using System.Collections.Generic;

namespace GaSchedule.Model
{
    public class Reservation
    {
        private static readonly Dictionary<int, Reservation> _reservationPool = new();

        private static int NR;
        private readonly int day;
		private readonly int time;
		private readonly int room;

		public Reservation(int day, int time, int room)
		{
			this.day = day;
			this.time = time;
			this.room = room;
		}

		public int Day { get { return day; } }

        public int Time { get { return time; } }

		public int Room { get { return room; } }
        public static Reservation GetReservation(int hashCode)
        {
            Reservation reservation;
            _reservationPool.TryGetValue(hashCode, out reservation);
            if (reservation == null)
            {
                int day = hashCode / (Constant.DAY_HOURS * NR);
                int hashCode2 = hashCode - (day * Constant.DAY_HOURS * NR);
                int room = hashCode2 / Constant.DAY_HOURS;
                int time = hashCode2 % Constant.DAY_HOURS;
                reservation = new Reservation(day, time, room);
                _reservationPool[hashCode] = reservation;
            }
            return reservation;
        }

        private static int HashCode(int day, int time, int room)
        {
            return day * Constant.DAY_HOURS * NR + room * Constant.DAY_HOURS + time;
        }
        public static Reservation GetReservation(int nr, int day, int time, int room)
        {
            if (nr != NR && nr > 0)
            {
                NR = nr;
                _reservationPool.Clear();
            }

            int hashCode = HashCode(day, time, room);
            Reservation reservation = GetReservation(hashCode);
            if (reservation == null)
            {
                reservation = new Reservation(day, time, room);
                _reservationPool[hashCode] = reservation;
            }
            return reservation;
        }

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
			return HashCode(day, time, room);
		}
	}
}
