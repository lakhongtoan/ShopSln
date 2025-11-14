namespace Shop.Models.ViewModel
{
    public class CheckoutViewModel
    {
        public Order Order { get; set; } = new Order();
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
