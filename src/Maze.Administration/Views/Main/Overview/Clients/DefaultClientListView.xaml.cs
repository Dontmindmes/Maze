using System.Windows.Controls;
using Maze.Administration.Library.Menu;
using Maze.Administration.Library.Menus;

namespace Maze.Administration.Views.Main.Overview.Clients
{
    /// <summary>
    ///     Interaction logic for DefaultClientListView.xaml
    /// </summary>
    public partial class DefaultClientListView : UserControl
    {
        public DefaultClientListView(ClientsContextMenu clientsContextMenu, IItemMenuFactory menuFactory)
        {
            InitializeComponent();

            var contextMenu = (ContextMenu) ClientsDataGrid.Resources["OnlineItemContextMenu"];
            var items = menuFactory.Create(clientsContextMenu, null);
            foreach (var item in items)
                contextMenu.Items.Add(item);

            ContextMenuExtensions.SetSelectedItems(contextMenu, ClientsDataGrid.SelectedItems);
        }
    }
}