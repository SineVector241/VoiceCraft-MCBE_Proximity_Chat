using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VoiceCraftProximityChat.ViewModels
{
    public class DelegateCommand : ICommand
    {
        //Fields
        private readonly Action<object> ExecuteAction;
        private readonly Predicate<object> CanExecuteAction;

        public DelegateCommand(Action<object> executeAction)
        {
            ExecuteAction = executeAction;
            CanExecuteAction = null;
        }

        public DelegateCommand(Action<object> executeAction, Predicate<object> canExecuteAction)
        {
            ExecuteAction = executeAction;
            CanExecuteAction = canExecuteAction;
        }

        //Events

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        //Methods
        public bool CanExecute(object? parameter)
        {
            return CanExecuteAction == null? true: CanExecuteAction(parameter);
        }

        public void Execute(object? parameter)
        {
            ExecuteAction(parameter);
        }
    }
}
