using System.Collections.Generic;
public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }

    public ICollection<CartItem> CartItems { get; set; }
}