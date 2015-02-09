using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Messaging.Commands
{
    public interface IManageCommandQueue
    {
        CommandModel Push<TCommand>(TCommand command, CommandPriority priority = CommandPriority.Normal, CommandTrigger trigger = CommandTrigger.Unspecified) where TCommand : Command;
        CommandModel Push(string commandName, DateTime? lastExecutionTime, CommandPriority priority = CommandPriority.Normal, CommandTrigger trigger = CommandTrigger.Unspecified);
        IEnumerable<CommandModel> Queue(CancellationToken cancellationToken);
        CommandModel Get(int id);
        List<CommandModel> GetQueued();
        List<CommandModel> GetStarted(); 
        void SetMessage(CommandModel command, string message);
        void Start(CommandModel commandModel);
        void Complete(CommandModel command);
        void Fail(CommandModel command, Exception e);
        void Requeue();
        void Clean();
    }

    public class CommandQueueManager : IManageCommandQueue, IHandle<ApplicationStartedEvent>
    {
        private readonly ICommandRepository _repo;
        private readonly IServiceFactory _serviceFactory;
        private readonly Logger _logger;

        private readonly ICached<string> _messageCache;
        private readonly BlockingCollection<CommandModel> _commandQueue; 

        public CommandQueueManager(ICommandRepository repo, 
                                   IServiceFactory serviceFactory,
                                   ICacheManager cacheManager,
                                   Logger logger)
        {
            _repo = repo;
            _serviceFactory = serviceFactory;
            _logger = logger;

            _messageCache = cacheManager.GetCache<string>(GetType());
            _commandQueue = new BlockingCollection<CommandModel>(new CommandQueue());
        }

        public CommandModel Push<TCommand>(TCommand command, CommandPriority priority = CommandPriority.Normal, CommandTrigger trigger = CommandTrigger.Unspecified) where TCommand : Command
        {
            Ensure.That(command, () => command).IsNotNull();

            _logger.Trace("Publishing {0}", command.GetType().Name);

            var existingCommands = _repo.FindQueuedOrStarted(command.Name);
            var existing = existingCommands.SingleOrDefault(c => CommandEqualityComparer.Instance.Equals(c.Body, command));

            if (existing != null)
            {
                _logger.Trace("Command is already in progress: {0}", command.GetType().Name);

                return existing;
            }

            var commandModel = new CommandModel
            {
                Name = command.Name,
                Body = command,
                QueuedAt = DateTime.UtcNow,
                Trigger = trigger,
                Priority = priority,
                Status = CommandStatus.Queued
            };

            _repo.Insert(commandModel);
            _commandQueue.Add(commandModel);

            return commandModel;
        }

        public CommandModel Push(string commandName, DateTime? lastExecutionTime, CommandPriority priority = CommandPriority.Normal, CommandTrigger trigger = CommandTrigger.Unspecified)
        {
            dynamic command = GetCommand(commandName);
            command.LastExecutionTime = lastExecutionTime;
            command.Trigger = trigger;

            return Push(command, priority, trigger);
        }

        public IEnumerable<CommandModel> Queue(CancellationToken cancellationToken)
        {
            return _commandQueue.GetConsumingEnumerable(cancellationToken);
        }

        public CommandModel Get(int id)
        {
            return FindMessage(_repo.Get(id));
        }

        public List<CommandModel> GetQueued()
        {
            return _repo.Queued();
        }

        public List<CommandModel> GetStarted()
        {
            return _repo.Started();
        }

        public void SetMessage(CommandModel command, string message)
        {
            _messageCache.Set(command.Id.ToString(), message, TimeSpan.FromMinutes(5));
        }

        public void Start(CommandModel commandModel)
        {
            commandModel.StartedAt = DateTime.UtcNow;
            commandModel.Status = CommandStatus.Started;

            _repo.Update(commandModel);
        }

        public void Complete(CommandModel command)
        {
            Update(command, CommandStatus.Completed);
        }

        public void Fail(CommandModel command, Exception e)
        {
            command.Exception = e.ToString();
            
            Update(command, CommandStatus.Failed);
        }

        public void Requeue()
        {
            foreach (var command in GetQueued())
            {
                _commandQueue.Add(command);
            }
        }

        public void Clean()
        {
            _messageCache.ClearExpired();
            _repo.Trim();
        }

        private dynamic GetCommand(string commandName)
        {
            commandName = commandName.Split('.').Last();

            var commandType = _serviceFactory.GetImplementations(typeof(Command))
                                             .Single(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

            return Json.Deserialize("{}", commandType);
        }

        private CommandModel FindMessage(CommandModel command)
        {
            command.Message = _messageCache.Find(command.Id.ToString());

            return command;
        }

        private void Update(CommandModel command, CommandStatus status)
        {
            command.EndedAt = DateTime.UtcNow;
            command.Duration = command.EndedAt.Value.Subtract(command.StartedAt.Value);
            command.Status = status;

            _repo.Update(command);

            //TODO: We need to clean up these messages periodically
            //_messageCache.Remove(command.Id.ToString());
        }

        public void Handle(ApplicationStartedEvent message)
        {
            _repo.OrphanStarted();
        }
    }
}
