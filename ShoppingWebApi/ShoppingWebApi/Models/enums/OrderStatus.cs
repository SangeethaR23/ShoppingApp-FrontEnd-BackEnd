namespace ShoppingWebApi.Models.enums
{
    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4,
        ReturnRequested=5,   
        ReturnApproved=6,    
        ReturnRejected=7,  
        Returned=8

    }
}