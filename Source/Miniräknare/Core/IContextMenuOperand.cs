using System.Windows.Controls;

namespace Miniräknare
{
    public interface IContextMenuOperand
    {
        void RegisterContextMenuItem(MenuItem menuItem);
        void UnregisterContextMenuItem(MenuItem menuItem);
    }
}
