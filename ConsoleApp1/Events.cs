using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceCraft_Server
{
    public class Events
    {
        public static event EventHandler OnExit;
        public Events()
        {

        }

        //Event Methods
        protected virtual void Exit()
        {
            var eventHandler = OnExit;
            if (eventHandler != null)
            {
                eventHandler(this, EventArgs.Empty);
            }
        }
    }
}
