using System;

namespace itpayroll.Services
{
    public class GovernmentService
    {
        public decimal ComputeSSS(decimal salary)
        {
            decimal minMSC = 5000;
            decimal maxMSC = 35000;

            var msc = Math.Min(Math.Max(salary, minMSC), maxMSC);

            return msc * 0.05m; // employee share
        }

        public decimal ComputePhilHealth(decimal salary)
        {
            decimal rate = 0.04m; // 4%
            decimal contribution = salary * rate;
            return contribution / 2; // employee share
        }

        public decimal ComputePagIBIG(decimal salary)
        {
            if (salary <= 1500) return salary * 0.01m;
            return Math.Min(salary * 0.02m, 100); // capped
        }

        public decimal ComputeTotalGovernment(decimal salary)
        {
            return ComputeSSS(salary)
                 + ComputePhilHealth(salary)
                 + ComputePagIBIG(salary);
        }
    }
}