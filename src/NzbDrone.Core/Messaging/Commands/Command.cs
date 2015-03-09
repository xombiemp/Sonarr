using System;

namespace NzbDrone.Core.Messaging.Commands
{
    public abstract class Command
    {
        public virtual Boolean SendUpdatesToClient
        {
            get
            {
                return false;
            }
        }

        public virtual string CompletionMessage
        {
            get
            {
                return "Completed";
            }
        }

        public String Name { get; private set; }
        public DateTime? LastExecutionTime { get; set; }
        public CommandTrigger Trigger { get; set; }

        public Command()
        {
            Name = GetType().Name.Replace("Command", "");
        }
    }
}
