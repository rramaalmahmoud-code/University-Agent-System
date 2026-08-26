namespace University_Agent_System.Services
{
    public interface IAdmissionMajorDiscountService
    {
        void SaveDiscount(
            int admissionMajorId,
            int semesterId,
            decimal discountPercentage,
            string changedBy
        );
    }
}