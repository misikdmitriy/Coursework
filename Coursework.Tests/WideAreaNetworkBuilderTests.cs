﻿using System;
using System.Collections.Generic;
using System.Linq;
using Coursework.Data.Builder;
using Coursework.Data.Entities;
using Coursework.Data.MessageServices;
using Coursework.Data.NetworkData;
using Moq;
using NUnit.Framework;

namespace Coursework.Tests
{
    [TestFixture]
    public class WideAreaNetworkBuilderTests
    {
        private Mock<INetworkBuilder> _simpleNetworkBuilderMock;
        private WideAreaNetworkBuilder _wideAreaNetworkBuilder;
        private int _numberOfMetropolitanNetworks;
        private int _satelliteChannelCapacity;
        private int _nodesCount;
        private uint _lastId;

        [SetUp]
        public void Setup()
        {
            _simpleNetworkBuilderMock = new Mock<INetworkBuilder>();

            _numberOfMetropolitanNetworks = 5;
            _nodesCount = 5;
            _satelliteChannelCapacity = 10;

            _wideAreaNetworkBuilder = new WideAreaNetworkBuilder(_simpleNetworkBuilderMock.Object,
                _numberOfMetropolitanNetworks, _satelliteChannelCapacity);

            _simpleNetworkBuilderMock.Setup(n => n.Build())
                .Returns(() => GenerateNetworkMockForTests(_nodesCount).Object);
        }

        [Test]
        public void BuildShouldReturnCorrectNetworkWithSpecifiedNumberOfRegionalNetworks()
        {
            // Arrange

            // Act
            var result = _wideAreaNetworkBuilder.Build();

            // Assert
            var resultWithoutDublicates = result.Nodes
                .GroupBy(n => n.Id)
                .Select(n => n.Key);

            Assert.That(resultWithoutDublicates.Count(), Is.EqualTo(result.Nodes.Length));
            Assert.That(result.Channels.Count(c => c.ChannelType == ChannelType.Satellite), 
                Is.EqualTo(_numberOfMetropolitanNetworks));
        }

        [Test]
        public void BuildShouldReturnNetworkWithOneCentralMachine()
        {
            // Arrange
            // Act
            var result = _wideAreaNetworkBuilder.Build();

            // Assert
            Assert.That(result.Nodes.Count(n => n.NodeType == NodeType.CentralMachine), Is.EqualTo(1));
        }

        [Test]
        public void BuildShouldReturnNetworkWithSpecifiedNumberOfMetropolitanLeaderMachines()
        {
            // Arrange
            // Act
            var result = _wideAreaNetworkBuilder.Build();

            // Assert
            Assert.That(result.Nodes.Count(n => n.NodeType == NodeType.MainMetropolitanMachine), 
                Is.EqualTo(_numberOfMetropolitanNetworks));
        }

        [Test]
        public void ConstrcutorShouldThrowExceptionIfCapacityIsZeroZero()
        {
            // Arrange
            const int capacity = 0;

            // Act
            TestDelegate testDelegate = () => _wideAreaNetworkBuilder = new WideAreaNetworkBuilder
            (_simpleNetworkBuilderMock.Object, _numberOfMetropolitanNetworks, capacity);

            // Assert
            Assert.That(testDelegate, Throws.ArgumentException);
        }

        [Test]
        public void ConstrcutorShouldThrowExceptionIfCapacityLessThanZero()
        {
            // Arrange
            const int capacity = -2;

            // Act
            TestDelegate testDelegate = () => _wideAreaNetworkBuilder = new WideAreaNetworkBuilder
            (_simpleNetworkBuilderMock.Object, _numberOfMetropolitanNetworks, capacity);

            // Assert
            Assert.That(testDelegate, Throws.ArgumentException);
        }

        [Test]
        public void BuildShouldCreateChannelsWithSpecifiedParams()
        {
            // Arrange
            // Act
            var result = _wideAreaNetworkBuilder.Build();

            // Assert
            Assert.That(result.Channels
                .Where(m => m.ChannelType == ChannelType.Satellite)
                .All(c => c.Capacity == _satelliteChannelCapacity));
        }

        private Mock<INetworkHandler> GenerateNetworkMockForTests(int count)
        {
            var networkMock = new Mock<INetworkHandler>();

            var nodes = new List<Node>();

            for (var i = 0; i < count; i++)
            {
                var node = new Node
                {
                    Id = _lastId,
                    LinkedNodesId = new SortedSet<uint>(),
                    MessageQueueHandlers = new List<MessageQueueHandler>()
                };
                _lastId++;

                nodes.Add(node);
            }

            networkMock.Setup(n => n.Nodes)
                .Returns(nodes.ToArray());

            networkMock.Setup(n => n.Channels)
                .Returns(new []
                {
                    new Channel
                    {
                        Id = Guid.NewGuid(),
                        FirstNodeId = nodes.First().Id,
                        SecondNodeId = nodes.First().Id + 1,
                        Price = 10,
                        ChannelType = ChannelType.Ground
                    }
                });

            return networkMock;
        }
    }
}
