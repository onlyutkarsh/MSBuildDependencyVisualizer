using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MsbuildDependencyVisualizer.UI.Common;
using MsbuildDependencyVisualizer.UI.ViewModel.Base;

namespace MsbuildDependencyVisualizer.UI.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private RelayCommand<object> _copyMessagesCommand;
        private string _rootFile;

        public ICommand CopyMessagesCommand
        {
            get
            {
                if (_copyMessagesCommand == null)
                {
                    _copyMessagesCommand = new RelayCommand<object>(OnCopyMessageClicked);
                }
                return _copyMessagesCommand;
            }
        }

        public string RootFile
        {
            get { return _rootFile; }
            set
            {
                _rootFile = value;
                OnPropertyChanged();
            }
        }

        private void OnCopyMessageClicked(object obj)
        {
            
        }



    }
}
