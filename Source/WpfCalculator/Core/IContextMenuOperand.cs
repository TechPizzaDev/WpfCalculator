using System.Windows.Controls;

namespace WpfCalculator
{
    public interface IContextMenuOperand
    {
        void RegisterContextMenuItem(MenuItem menuItem);
        void UnregisterContextMenuItem(MenuItem menuItem);
    }
}
