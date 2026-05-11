namespace itpayroll.Services
{
    public class TaxService
    {
        public decimal ComputeTax(decimal taxableIncome)
        {
            if (taxableIncome < 0)
                taxableIncome = 0;

            if (taxableIncome <= 20833) return 0;

            if (taxableIncome <= 33333)
                return (taxableIncome - 20833) * 0.15m;

            if (taxableIncome <= 66667)
                return 1875 + (taxableIncome - 33333) * 0.20m;

            if (taxableIncome <= 166667)
                return 8541.80m + (taxableIncome - 66667) * 0.25m;

            return 33541.80m + (taxableIncome - 166667) * 0.30m;
        }
    }
}