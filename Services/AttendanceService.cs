using System;

namespace itpayroll.Services
{
    public class AttendanceService
    {
        public double ComputeHours(TimeSpan timeIn, TimeSpan timeOut)
        {
            return (timeOut - timeIn).TotalHours;
        }

        public double ComputeOvertime(double totalHours)
        {
            if (totalHours > 8)
                return totalHours - 8;

            return 0;
        }
    }
}