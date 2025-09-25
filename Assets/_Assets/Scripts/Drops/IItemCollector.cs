public interface IItemCollector
{
    bool CanAccept(Item item, int quantity);
    void AddItem(Item item, int quantity);
}