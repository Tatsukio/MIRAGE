using ParaWorldStatus.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParaWorldStatus.ViewModel
{
    class PWSViewModel : BaseViewModel
    {
        public PWSViewModel()
        {
            ParaWorldStatus = new PWS();

            UpdatePWSAsyncCommand = new AsyncCommand(async () =>
            {
                await Task.Run(action: ParaWorldStatus.UpdatePWS);
            });
        }

        public PWS ParaWorldStatus { get; set; }

        public ICommand UpdatePWSCommand
        {
            get { return new DelegateCommand(ParaWorldStatus.UpdatePWS); }
        }

        public AsyncCommand UpdatePWSAsyncCommand { get; private set; }

    }
}
