﻿//using System;
//using System.Collections.Generic;
//using Moq;
//using NUnit.Framework;
//using NzbDrone.Common;
//using NzbDrone.Core.Messaging.Commands;
//using NzbDrone.Core.Messaging.Commands.Tracking;
//using NzbDrone.Core.Messaging.Events;
//using NzbDrone.Test.Common;
//
//namespace NzbDrone.Core.Test.Messaging.Commands
//{
//    [TestFixture]
//    public class CommandExecutorFixture : TestBase<CommandExecutor>
//    {
//        private Mock<IExecute<CommandA>> _executorA;
//        private Mock<IExecute<CommandB>> _executorB;
//
//        [SetUp]
//        public void Setup()
//        {
//            _executorA = new Mock<IExecute<CommandA>>();
//            _executorB = new Mock<IExecute<CommandB>>();
//
//            Mocker.GetMock<IServiceFactory>()
//                  .Setup(c => c.Build(typeof(IExecute<CommandA>)))
//                  .Returns(_executorA.Object);
//
//            Mocker.GetMock<IServiceFactory>()
//                  .Setup(c => c.Build(typeof(IExecute<CommandB>)))
//                  .Returns(_executorB.Object);
//
//
//            Mocker.GetMock<ITrackCommands>()
//                  .Setup(c => c.FindExisting(It.IsAny<Command>()))
//                  .Returns<Command>(null);
//        }
//
//        [Test]
//        public void should_publish_command_to_executor()
//        {
//            var commandA = new CommandA();
//
//            Subject.Push(commandA);
//
//            _executorA.Verify(c => c.Execute(commandA), Times.Once());
//        }
//
//        [Test]
//        public void should_publish_command_by_with_optional_arg_using_name()
//        {
//            Mocker.GetMock<IServiceFactory>().Setup(c => c.GetImplementations(typeof(Command)))
//                  .Returns(new List<Type> { typeof(CommandA), typeof(CommandB) });
//
//            Subject.Push(typeof(CommandA).FullName);
//            _executorA.Verify(c => c.Execute(It.IsAny<CommandA>()), Times.Once());
//        }
//
//
//        [Test]
//        public void should_not_publish_to_incompatible_executor()
//        {
//            var commandA = new CommandA();
//
//            Subject.Push(commandA);
//
//            _executorA.Verify(c => c.Execute(commandA), Times.Once());
//            _executorB.Verify(c => c.Execute(It.IsAny<CommandB>()), Times.Never());
//        }
//
//        [Test]
//        public void broken_executor_should_throw_the_exception()
//        {
//            var commandA = new CommandA();
//
//            _executorA.Setup(c => c.Execute(It.IsAny<CommandA>()))
//                       .Throws(new NotImplementedException());
//
//            Assert.Throws<NotImplementedException>(() => Subject.Push(commandA));
//        }
//
//
//        [Test]
//        public void broken_executor_should_publish_executed_event()
//        {
//            var commandA = new CommandA();
//
//            _executorA.Setup(c => c.Execute(It.IsAny<CommandA>()))
//                       .Throws(new NotImplementedException());
//
//            Assert.Throws<NotImplementedException>(() => Subject.Push(commandA));
//
//            VerifyEventPublished<CommandExecutedEvent>();
//        }
//
//        [Test]
//        public void should_publish_executed_event_on_success()
//        {
//            var commandA = new CommandA();
//            Subject.Push(commandA);
//
//            VerifyEventPublished<CommandExecutedEvent>();
//        }
//    }
//
//    public class CommandA : Command
//    {
//        public CommandA(int id = 0)
//        {
//        }
//    }
//
//    public class CommandB : Command
//    {
//
//        public CommandB()
//        {
//        }
//    }
//
//}