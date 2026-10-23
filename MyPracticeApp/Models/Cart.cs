using System.Collections.Generic;

public class Cart
{
    public int CartId { get; set; }
    public string Owner { get; set; }

    public ICollection<CartItem> CartItems { get; set; }
}