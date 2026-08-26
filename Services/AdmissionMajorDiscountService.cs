using System.Data;
using Dapper;

namespace University_Agent_System.Services
{
    public class AdmissionMajorDiscountService
        : IAdmissionMajorDiscountService
    {
        private readonly IDbConnection _db;

        public AdmissionMajorDiscountService(
            IDbConnection db)
        {
            _db = db;
        }

        public void SaveDiscount(
            int admissionMajorId,
            int semesterId,
            decimal discountPercentage,
            string changedBy)
        {
            if (discountPercentage < 0 ||
                discountPercentage > 100)
            {
                throw new ArgumentException(
                    "نسبة الخصم يجب أن تكون بين 0 و100."
                );
            }

            bool closeConnection = false;

            if (_db.State != ConnectionState.Open)
            {
                _db.Open();
                closeConnection = true;
            }

            using var transaction = _db.BeginTransaction();

            try
            {
                decimal? currentDiscount =
                    _db.QueryFirstOrDefault<decimal?>(
                        @"SELECT DiscountPercentage
                          FROM AdmissionMajorDiscounts
                          WHERE AdmissionMajorId =
                                @AdmissionMajorId
                            AND SemesterId = @SemesterId
                            AND IsActive = 1",
                        new
                        {
                            AdmissionMajorId =
                                admissionMajorId,

                            SemesterId = semesterId
                        },
                        transaction
                    );

                // إذا لم تتغير النسبة لا ننشئ سجلًا جديدًا.
                if (currentDiscount.HasValue &&
                    currentDiscount.Value ==
                    discountPercentage)
                {
                    transaction.Commit();
                    return;
                }

                _db.Execute(
                    @"UPDATE AdmissionMajorDiscounts
                      SET
                          IsActive = 0,
                          UpdatedAt = SYSDATETIME(),
                          UpdatedBy = @ChangedBy
                      WHERE AdmissionMajorId =
                            @AdmissionMajorId
                        AND SemesterId = @SemesterId
                        AND IsActive = 1",
                    new
                    {
                        AdmissionMajorId =
                            admissionMajorId,

                        SemesterId = semesterId,
                        ChangedBy = changedBy
                    },
                    transaction
                );

                _db.Execute(
                    @"INSERT INTO AdmissionMajorDiscounts
                      (
                          AdmissionMajorId,
                          SemesterId,
                          DiscountPercentage,
                          IsActive,
                          CreatedAt,
                          CreatedBy
                      )
                      VALUES
                      (
                          @AdmissionMajorId,
                          @SemesterId,
                          @DiscountPercentage,
                          1,
                          SYSDATETIME(),
                          @ChangedBy
                      )",
                    new
                    {
                        AdmissionMajorId =
                            admissionMajorId,

                        SemesterId = semesterId,

                        DiscountPercentage =
                            discountPercentage,

                        ChangedBy = changedBy
                    },
                    transaction
                );

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                if (closeConnection)
                {
                    _db.Close();
                }
            }
        }
    }
}