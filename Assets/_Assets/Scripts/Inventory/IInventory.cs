using System.Collections.Generic;

public interface IInventory
{
    bool HasItem(Item item, int quantity = 1);
    bool HasIngredients(IEnumerable<Ingredient> ingredients);
    bool Consume(Item item, int quantity = 1);
    bool ConsumeIngredients(IEnumerable<Ingredient> ingredients);
    void AddItem(Item item, int quantity = 1);

    event System.Action<Item,int> ItemAdded;
    event System.Action<Item,int> ItemRemoved;
}
