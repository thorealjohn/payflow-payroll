namespace itpayroll.Services
{
    public class GovernmentService
    {
        public decimal ComputeSSS(decimal salary)
        {
            decimal min = 5000;
            decimal max = 35000;

            var msc = Math.Min(Math.Max(salary, min), max);

            return msc * 0.05m; // employee share
        }

        public decimal ComputePhilHealth(decimal salary)
        {
            decimal min = 10000;
            decimal max = 80000;
            decimal rate = 0.04m;

            var baseSalary = Math.Min(Math.Max(salary, min), max);
            return (baseSalary * rate) / 2;
        }

        public decimal ComputePagIBIG(decimal salary)
        {
            if (salary <= 1500)
                return salary * 0.01m;

            return Math.Min(salary * 0.02m, 100);
        }
    }
}