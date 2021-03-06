﻿using System;
using System.Collections.Generic;
using System.Linq;
using Coursework.Data.Constants;
using Coursework.Data.Entities;
using Coursework.Data.Exceptions;
using Coursework.Data.MessageServices;
using Coursework.Data.NetworkData;
using NUnit.Framework;

namespace Coursework.Tests
{
    [TestFixture]
    public class NetworkTests
    {
        private INetworkHandler _network;
        private Channel _channel;
        private Node _node1;
        private Node _node2;

        [SetUp]
        public void Setup()
        {
            _network = new Network();

            _node1 = new Node
            {
                Id = 0,
                LinkedNodesId = new SortedSet<uint>(),
                MessageQueueHandlers = new List<MessageQueueHandler>(),
                ReceivedMessages = new List<Message>(),
                CanceledMessages = new List<Message>()
            };
            _node2 = new Node
            {
                Id = 1,
                LinkedNodesId = new SortedSet<uint>(),
                MessageQueueHandlers = new List<MessageQueueHandler>(),
                ReceivedMessages = new List<Message>(),
                CanceledMessages = new List<Message>()
            };

            _channel = new Channel
            {
                Id = Guid.Empty,
                SecondNodeId = _node2.Id,
                FirstNodeId = _node1.Id,
                Price = AllConstants.AllPrices.ElementAt(0),
                ChannelType = ChannelType.Ground,
                ErrorChance = 0.6,
                ConnectionType = ConnectionType.Duplex,
                Capacity = 1
            };
        }

        [Test]
        public void AddNodeShouldAddNodeWithCorrectId()
        {
            // Arrange
            // Act
            _network.AddNode(_node1);

            // Assert
            Assert.That(_network.Nodes.Any(n => n.Id == _node1.Id), Is.True);
        }

        [Test]
        public void AddNodeShouldThrowExceptionIfNodeExists()
        {
            // Arrange
            _network.AddNode(_node1);

            // Act
            TestDelegate testDelegate = () => _network.AddNode(_node1);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(NodeException)));
        }

        [Test]
        public void AddChannelShouldAddChannelBetweenTwoNodes()
        {
            // Arrange
            _network.AddNode(_node1);
            _network.AddNode(_node2);

            // Act
            _network.AddChannel(_channel);

            // Assert
            Assert.IsTrue(_network.Channels.Contains(_channel));
            Assert.IsTrue(_node1.LinkedNodesId.Contains(_node2.Id));
            Assert.IsTrue(_node2.LinkedNodesId.Contains(_node1.Id));
            Assert.IsTrue(_node1.LinkedNodesId.Contains(_node2.Id));
            Assert.IsTrue(_node2.LinkedNodesId.Contains(_node1.Id));
        }

        [Test]
        public void AddChannelShouldCreateTwoMessageQueuesInNodes()
        {
            // Arrange
            _network.AddNode(_node1);
            _network.AddNode(_node2);

            // Act
            _network.AddChannel(_channel);

            // Assert
            Assert.That(_node1.MessageQueueHandlers.Any(m => m.ChannelId == _channel.Id));
            Assert.That(_node2.MessageQueueHandlers.Any(m => m.ChannelId == _channel.Id));
        }

        [Test]
        public void AddChannelShouldThrowExceptionIfStartNodeNotExists()
        {
            // Arrange
            _channel.FirstNodeId = uint.MaxValue;

            // Act
            TestDelegate testDelegate = () => _network.AddChannel(_channel);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(NodeException)));
        }

        [Test]
        public void AddChannelShouldThrowExceptionIfEndNodeNotExists()
        {
            // Arrange
            _channel.SecondNodeId = uint.MaxValue;

            // Act
            TestDelegate testDelegate = () => _network.AddChannel(_channel);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(NodeException)));
        }

        [Test]
        public void AddChannelShouldThrowExceptionIfChannelWithSameIdExistsInNetwork()
        {
            // Arrange
            _network.AddNode(_node1);
            _network.AddNode(_node2);
            _network.AddChannel(_channel);

            // Act
            TestDelegate testDelegate = () => _network.AddChannel(_channel);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(ChannelException)));
        }

        [Test]
        public void AddChannelShouldThrowExceptionIfErrorChanceIsIncorrect()
        {
            // Arrange
            CreateTwoNodesForTests();
            _channel.ErrorChance = 1.2;

            // Act
            TestDelegate testDelegate = () => _network.AddChannel(_channel);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(ChannelException)));
        }

        [Test]
        public void AddChannelShouldThrowExceptionIfPriceIsNegative()
        {
            // Arrange
            CreateTwoNodesForTests();
            _channel.Price = -1;

            // Act
            TestDelegate testDelegate = () => _network.AddChannel(_channel);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(ChannelException)));
        }

        [Test]
        public void AddChannelShouldThrowExceptionIfCapacityIsLessOrEqualToZero()
        {
            // Arrange
            CreateTwoNodesForTests();
            _channel.Capacity = 0;

            // Act
            TestDelegate testDelegate = () => _network.AddChannel(_channel);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(ChannelException)));
        }

        [Test]
        public void GetChannelShouldReturnChannelIfChannelFromFirstToSecondNodeExists()
        {
            // Arrange
            CreateTwoNodesForTests();
            CreateChannelForTests();

            // Act
            var result = _network.GetChannel(_node1.Id, _node2.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.IsTrue(result.FirstNodeId == _node1.Id || result.FirstNodeId == _node2.Id);
            Assert.IsTrue(result.SecondNodeId == _node1.Id || result.SecondNodeId == _node2.Id);
        }

        [Test]
        public void GetChannelShouldReturnChannelIfChannelFromSecondToFirstNodeExists()
        {
            // Arrange
            CreateTwoNodesForTests();
            CreateChannelForTests();

            // Act
            var result = _network.GetChannel(_node2.Id, _node1.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.IsTrue(result.FirstNodeId == _node1.Id || result.FirstNodeId == _node2.Id);
            Assert.IsTrue(result.SecondNodeId == _node1.Id || result.SecondNodeId == _node2.Id);
        }

        [Test]
        public void GetChannelShouldReturnNullIfChannelNotExists()
        {
            // Arrange
            // Act
            var result = _network.GetChannel(_node2.Id, _node1.Id);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetChannelsShouldReturnAllChannelsLinkedWithCurrentNode()
        {
            // Arrange 
            CreateTwoNodesForTests();
            CreateChannelForTests();

            // Act
            var result = _network.GetChannels(_channel.FirstNodeId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.IsTrue(result.Any(c => c.SecondNodeId == _channel.SecondNodeId || c.FirstNodeId == _channel.SecondNodeId));
        }

        [Test]
        public void UpdateChannelShouldChangeExistedChannel()
        {
            // Arrange
            CreateTwoNodesForTests();
            CreateChannelForTests();

            var newCapacity = 90;

            _channel.Price = AllConstants.AllPrices.ElementAt(2);
            _channel.ChannelType = ChannelType.Satellite;
            _channel.Capacity = newCapacity;

            // Act
            _network.UpdateChannel(_channel);
            var updatedChannel = _network.GetChannel(_channel.FirstNodeId, _channel.SecondNodeId);

            // Assert
            Assert.That(updatedChannel.Price, Is.EqualTo(AllConstants.AllPrices.ElementAt(2)));
            Assert.That(updatedChannel.ChannelType, Is.EqualTo(ChannelType.Satellite));
            Assert.That(updatedChannel.ConnectionType, Is.EqualTo(_channel.ConnectionType));
            Assert.That(updatedChannel.Capacity, Is.EqualTo(newCapacity));
        }

        [Test]
        public void UpdateChannelShouldChangeSaveMessages()
        {
            // Arrange
            CreateTwoNodesForTests();
            CreateChannelForTests();

            var message = new Message();
            _channel.FirstMessage = message;

            // Act
            _network.UpdateChannel(_channel);
            var updatedChannel = _network.GetChannel(_channel.FirstNodeId, _channel.SecondNodeId);

            // Assert
            Assert.That(updatedChannel.FirstMessage, Is.EqualTo(message));
            Assert.IsNull(updatedChannel.SecondMessage);
        }

        private void CreateChannelForTests()
        {
            _network.AddChannel(_channel);
        }

        private void CreateTwoNodesForTests()
        {
            _network.AddNode(_node1);
            _network.AddNode(_node2);
        }

        [Test]
        public void UpdateChannelShouldThrowExceptionIfChannelNotExists()
        {
            // Arrange
            // Act
            TestDelegate testDelegate = () => _network.UpdateChannel(_channel);

            // Assert
            Assert.That(testDelegate, Throws.TypeOf(typeof(ChannelException)));
        }

        [Test]
        public void RemoveChannelShouldDropOneChannelIfItExists()
        {
            // Arrange
            CreateTwoNodesForTests();
            CreateChannelForTests();
            var initialChannelCount = _network.Channels.Length;

            // Act
            _network.RemoveChannel(_node1.Id, _node2.Id);
            var resultChannelCount = _network.Channels.Length;

            // Assert
            Assert.That(resultChannelCount, Is.EqualTo(initialChannelCount - 1));
        }

        [Test]
        public void RemoveChannelShouldDropMessageQueues()
        {
            // Arrange
            CreateTwoNodesForTests();
            CreateChannelForTests();

            // Act
            _network.RemoveChannel(_node1.Id, _node2.Id);

            // Assert
            Assert.That(_node1.MessageQueueHandlers.Count, Is.Zero);
            Assert.That(_node2.MessageQueueHandlers.Count, Is.Zero);
        }

        [Test]
        public void RemoveChannelShouldDropOneChannelIfItExistsAndIdsSwaped()
        {
            // Arrange
            CreateTwoNodesForTests();
            CreateChannelForTests();
            var initialChannelCount = _network.Channels.Length;

            // Act
            _network.RemoveChannel(_node2.Id, _node1.Id);
            var resultChannelCount = _network.Channels.Length;

            // Assert
            Assert.That(resultChannelCount, Is.EqualTo(initialChannelCount - 1));
        }

        [Test]
        public void RemoveChannelShouldDoNothingIfChannelNotExists()
        {
            // Arrange
            CreateTwoNodesForTests();
            var initialChannelCount = _network.Channels.Length;

            // Act
            _network.RemoveChannel(_node2.Id, _node1.Id);
            var resultChannelCount = _network.Channels.Length;

            // Assert
            Assert.That(resultChannelCount, Is.EqualTo(initialChannelCount));
        }

        [Test]
        public void RemoveChannelShouldRemoveRecordInLinkedNodesSet()
        {
            // Arrange
            CreateTwoNodesForTests();

            // Act
            _network.RemoveChannel(_node2.Id, _node1.Id);

            // Assert
            Assert.IsFalse(_node1.LinkedNodesId.Contains(_node2.Id));
            Assert.IsFalse(_node2.LinkedNodesId.Contains(_node1.Id));
        }

        [Test]
        public void RemoveNodeShouldRemoveNodeWithAllLinkedChannels()
        {
            // Arrange
            CreateTwoNodesForTests();
            CreateChannelForTests();
            var initialNodeCount = _network.Nodes.Length;
            var initialChannelCount = _network.Nodes.Length;

            // Act
            _network.RemoveNode(_node1.Id);
            var resultNodeCount = _network.Nodes.Length;
            var resultChannelCount = _network.Nodes.Length;

            // Assert
            Assert.That(resultNodeCount, Is.EqualTo(initialNodeCount - 1));
            Assert.That(resultChannelCount, Is.EqualTo(initialChannelCount - 1));
        }

        [Test]
        public void RemoveNodeShouldDoNothingIfNodeNotExists()
        {
            // Arrange
            var initialNodeCount = _network.Nodes.Length;
            var initialChannelCount = _network.Nodes.Length;

            // Act
            _network.RemoveNode(_node1.Id);
            var resultNodeCount = _network.Nodes.Length;
            var resultChannelCount = _network.Nodes.Length;

            // Assert
            Assert.That(resultNodeCount, Is.EqualTo(initialNodeCount));
            Assert.That(resultChannelCount, Is.EqualTo(initialChannelCount));
        }

        [Test]
        public void GetNodeByIdShouldReturnCorrectNode()
        {
            // Arrange
            _network.AddNode(_node1);

            // Act
            var result = _network.GetNodeById(_node1.Id);

            // Assert
            Assert.IsTrue(result.LinkedNodesId.All(i => _node1.LinkedNodesId.Any(i1 => i1 == i)));
            Assert.That(result.MessageQueueHandlers.Count, Is.EqualTo(_node1.MessageQueueHandlers.Count));
        }

        [Test]
        public void GetNodeByIdShouldReturnNullIfNodeNotExists()
        {
            // Arrange
            // Act
            var result = _network.GetNodeById(_node1.Id);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void AddInQueueShouldDoIt()
        {
            // Arrange
            _network.AddNode(_node1);
            _network.AddNode(_node2);
            _network.AddChannel(_channel);

            var message = new Message
            {
                SenderId = _node1.Id,
                ReceiverId = _node2.Id,
                Route = new[]
                {
                    _channel
                },
            };

            var messageQueue = _node1.MessageQueueHandlers.First();

            // Act
            _network.AddInQueue(message, 0);

            // Assert
            Assert.IsTrue(messageQueue.Messages.Contains(message));
        }

        [Test]
        public void RemoveFromQueueShouldDoIt()
        {
            // Arrange
            _network.AddNode(_node1);
            _network.AddNode(_node2);
            _network.AddChannel(_channel);

            var message = new Message
            {
                SenderId = _node1.Id,
                ReceiverId = _node2.Id,
                Route = new[]
                {
                    _channel
                },
            };

            var messageQueue = _node1.MessageQueueHandlers.First();

            _network.AddInQueue(message, 0);

            // Act
            _network.RemoveFromQueue(message, 0);

            // Assert
            Assert.IsFalse(messageQueue.Messages.Contains(message));
        }

        [Test]
        public void ClearMessagesShouldClearAllMessagesFromNodesAndChannels()
        {
            // Arrange
            _network.AddNode(_node1);
            _network.AddNode(_node2);
            _network.AddChannel(_channel);

            var message = new Message
            {
                SenderId = _node1.Id,
                ReceiverId = _node2.Id,
                Route = new[]
                {
                    _channel
                },
            };

            _node1.ReceivedMessages.Add(message);
            _node2.CanceledMessages.Add(message);
            _channel.FirstMessage = message;

            // Act
            _network.ClearMessages();

            // Assert
            Assert.IsEmpty(_node1.ReceivedMessages);
            Assert.IsEmpty(_node2.CanceledMessages);
            Assert.IsNull(_channel.FirstMessage);
        }

        [Test]
        public void ResetShouldDoAllChannelsFreeAndAllNodesUnactive()
        {
            // Arrange
            _network.AddNode(_node1);
            _network.AddNode(_node2);
            _network.AddChannel(_channel);

            _node1.IsActive = true;
            _channel.IsBusy = true;

            // Act
            _network.Reset();

            // Assert
            Assert.IsTrue(_network.Nodes.All(n => !n.IsTableUpdated));
            Assert.IsTrue(_network.Channels.All(n => !n.IsBusy));
        }

        [Test]
        public void ResetShouldRemoveAllTables()
        {
            // Arrange
            _network.AddNode(_node1);

            _node1.NetworkMatrix = NetworkMatrix.Initialize(_network);

            // Act
            _network.Reset();

            // Assert
            Assert.IsNull(_node1.NetworkMatrix);
        }
    }
}
