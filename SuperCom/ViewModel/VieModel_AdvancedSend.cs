using GalaSoft.MvvmLight;
using SuperCom.Config;
using SuperCom.Entity;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;

namespace SuperCom.ViewModel
{

    public class VieModel_AdvancedSend : ViewModelBase
    {

        private ObservableCollection<AdvancedSend> _ProjectNames;
        public ObservableCollection<AdvancedSend> ProjectNames
        {
            get { return _ProjectNames; }
            set { _ProjectNames = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<SendCommand> _SendCommands;

        public ObservableCollection<SendCommand> SendCommands
        {
            get { return _SendCommands; }
            set
            {
                _SendCommands = value;
                RaisePropertyChanged();
            }
        }


        public VieModel_AdvancedSend()
        {
            Init();
        }


        private void Init()
        {
            ProjectNames = new ObservableCollection<AdvancedSend>();
            for (int i = 0; i < 10; i++)
            {
                ProjectNames.Add(new AdvancedSend(i, $"ÏîÄ¿ {i}"));
            }

            SendCommands = new ObservableCollection<SendCommand>();
            for (int i = 0; i < 10; i++)
            {
                SendCommand sendCommand = new SendCommand();
                sendCommand.Order = i;
                sendCommand.Delay = i * 1000;
                sendCommand.Command = $"AT^PHYNUM={i}";
                SendCommands.Add(sendCommand);
            }
        }

    }
}