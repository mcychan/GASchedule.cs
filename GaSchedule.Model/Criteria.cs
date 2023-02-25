using System.Collections.Generic;

namespace GaSchedule.Model
{
    public class Criteria
    {
        internal static bool IsRoomOverlapped(List<CourseClass>[] slots, Reservation reservation, int dur)
        {
            // check for room overlapping of classes
            for (int i = dur - 1; i >= 0; i--)
            {
                if (slots[reservation.GetHashCode() + i].Count > 1)
                    return true;
            }
            return false;
        }

        internal static bool IsSeatEnough(Room r, CourseClass cc)
        {
            // does current room have enough seats
            return r.NumberOfSeats >= cc.NumberOfSeats;
        }

        internal static bool IsComputerEnough(Room r, CourseClass cc)
        {
            // does current room have computers if they are required
            return !cc.LabRequired || (cc.LabRequired && r.Lab);
        }

        internal static bool[] IsOverlappedProfStudentGrp(List<CourseClass>[] slots, CourseClass cc, int numberOfRooms, int timeId)
        {
            bool po = false, go = false;

            int dur = cc.Duration;
            // check overlapping of classes for professors and student groups
            for (int i = numberOfRooms; i > 0; --i, timeId += Constant.DAY_HOURS)
            {
                // for each hour of class
                for (int j = dur - 1; j >= 0; --j)
                {
                    // check for overlapping with other classes at same time
                    var cl = slots[timeId + j];
                    foreach (var cc1 in cl)
                    {
                        if (cc != cc1)
                        {
                            // professor overlaps?
                            if (!po && cc.ProfessorOverlaps(cc1))
                                po = true;

                            // student group overlaps?
                            if (!go && cc.GroupsOverlap(cc1))
                                go = true;

                            // both type of overlapping? no need to check more
                            if (po && go)
                                return new bool[] { po, go };
                        }
                    }
                }
            }

            return new bool[] { po, go };
        }

        public static readonly float[] Weights = { 0f, .5f, .5f, 0f, 0f };
    }
}
