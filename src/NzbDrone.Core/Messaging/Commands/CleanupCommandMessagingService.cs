namespace NzbDrone.Core.Messaging.Commands
{
    public class CleanupCommandMessagingService : IExecute<CommandMessagingCleanupCommand>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public CleanupCommandMessagingService(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Execute(CommandMessagingCleanupCommand message)
        {
            _commandQueueManager.CleanMessages();
        }
    }
}
