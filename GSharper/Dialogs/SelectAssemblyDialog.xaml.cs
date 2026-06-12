using GSharper.Assistants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using static GSharper.Assistants.AssistantDecompile;

namespace GSharper.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для SelectAssemblyDialog.xaml
    /// </summary>
    public partial class SelectAssemblyDialog : Window
    {
        public bool IsOk { private set; get; }

        public ProjectPackageFile[] ListSelectedProjectPackage { private set; get; }

        public SelectAssemblyDialog()
        {
            InitializeComponent();
            ThreadPool.QueueUserWorkItem((s) => 
            {
                var fileName = new FileInfo(Assistant.GetWorkspace().CurrentSolution.FilePath);
                var cachDirectory = Assistant.Decompile.Value.GetCachDirectory();
                var fileSolution = new FileInfo(Assistant.GetWorkspace().CurrentSolution.FilePath);
                ProjectPackageFile[] list = Assistant.Decompile.Value.GetSolutionPackageReferences(fileSolution, cachDirectory).ToArray();
                Dispatcher.Invoke(() => 
                {
                    _dataGridFiles.ItemsSource = list;
                });
            });
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            IsOk = false;
            this.Close();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            ListSelectedProjectPackage = _dataGridFiles
                .ItemsSource
                .Cast<ProjectPackageFile>()
                .Where(x => x.IsSelected)
                .ToArray();
            IsOk = true;
            this.Close();
        }
    }
}
