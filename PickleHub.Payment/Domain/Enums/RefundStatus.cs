namespace PickleHub.Payment.Domain.Enums;

public enum RefundStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Completed = 3,
    WaitingForBankInfo = 4
}