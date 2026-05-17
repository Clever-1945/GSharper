using GSharper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GSharper.Controls
{
    /// <summary>
    /// Логика взаимодействия для ListBranchControl.xaml
    /// </summary>
    public partial class ListBranchControl : UserControl
    {
        private GitBranchInfo[] _listBranch;
        private GitBranchInfo[] _listFilteredBranch;
        public Action<GitBranchInfo> OnSelectedBranch;

        public ListBranchControl()
        {
            InitializeComponent();
        }

        public void SetListBranch(GitBranchInfo[] listBranch)
        {
            _listBranch = listBranch;
            Dispatcher.Invoke(() => 
            {
                _textBoxBranchName.Text = "";
                ApplyFilteredBranch();
            });
        }

        private GitBranchInfo[] GetFilteredBranch(string filter, GitBranchInfo[] listBranch)
        {
            if (listBranch == null)
                return Array.Empty<GitBranchInfo>();

            if(listBranch.Length < 1)
                return Array.Empty<GitBranchInfo>();

            if (String.IsNullOrWhiteSpace(filter))
                return listBranch;

            return listBranch.Where(x => x != null && x.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        private void OnSelectChaged(object sender, SelectionChangedEventArgs e)
        {
            var branch = _listViewBranch.SelectedValue as GitBranchInfo;
            if (branch != null)
            {
                OnSelectedBranch?.Invoke(branch);
            }
        }

        private void ApplyFilteredBranch()
        {
            _listFilteredBranch = GetFilteredBranch(_textBoxBranchName.Text, _listBranch);
            _listViewBranch.ItemsSource = _listFilteredBranch;
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilteredBranch();
        }
    }
}
