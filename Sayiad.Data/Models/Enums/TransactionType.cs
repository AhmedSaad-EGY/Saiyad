namespace Sayiad.Data.Models;

public enum TransactionType
{
    Unknown,
    Deposit,
    Withdrawal,
    HoldDeduction,
    HoldRelease,
    OrderPayment,
    OrderRefund,
    SellerCredit,
    SellerCreditHeld,
    SellerCreditReleased,
    AuctioneerFee,
    PlatformFee,
    PlatformFeeRefunded,
    SubscriptionPayment,
    BidHold,
    BidRelease
}
