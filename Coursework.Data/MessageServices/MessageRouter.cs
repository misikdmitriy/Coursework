﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper.Execution;
using Coursework.Data.Constants;
using Coursework.Data.Entities;
using Coursework.Data.NetworkData;

namespace Coursework.Data.MessageServices
{
    public class MessageRouter : IMessageRouter
    {
        private class NetworkMatrix
        {
            public readonly IDictionary<uint, double> NodeIdWithCurrentPrice = new Dictionary<uint, double>();
            public readonly SortedSet<uint> VisitedNodes = new SortedSet<uint>();

            public static NetworkMatrix Initialize(INetwork network)
            {
                var networkMatrix = new NetworkMatrix();

                foreach (var node in network.Nodes)
                {
                    networkMatrix.NodeIdWithCurrentPrice[node.Id] = double.MaxValue;
                }

                return networkMatrix;
            }
        }

        private readonly INetworkHandler _network;

        public MessageRouter(INetworkHandler network)
        {
            _network = network;
        }

        public Channel[] GetRoute(uint senderId, uint receiverId)
        {
            if (senderId == receiverId)
            {
                return new Channel[0];
            }

            var networkMatrix = NetworkMatrix.Initialize(_network);
            networkMatrix.NodeIdWithCurrentPrice[senderId] = 0.0;

            CountPriceMatrix(senderId, receiverId, networkMatrix);

            if (!networkMatrix.VisitedNodes.Contains(receiverId))
            {
                return null;
            }

            var route = BuildRoute(networkMatrix, senderId, receiverId);
            return route.ToArray();
        }

        private List<Channel> BuildRoute(NetworkMatrix networkMatrix, uint senderId, uint receiverId)
        {
            var currentNodeId = receiverId;

            var route = new List<Channel>();

            while (currentNodeId != senderId)
            {
                var currentNode = _network.GetNodeById(currentNodeId);

                foreach (var linkedNodeId in currentNode.LinkedNodesId)
                {
                    if (Math.Abs(networkMatrix.NodeIdWithCurrentPrice[currentNodeId]
                                 - networkMatrix.NodeIdWithCurrentPrice[linkedNodeId]
                                 - CountPrice(currentNodeId, linkedNodeId)) < AllConstants.Eps)
                    {
                        var channel = _network.GetChannel(currentNodeId, linkedNodeId);
                        route.Add(channel);

                        currentNodeId = linkedNodeId;
                        break;
                    }
                }
            }

            route.Reverse();
            return route;
        }

        private void CountPriceMatrix(uint currentId, uint receiverId, NetworkMatrix matrix)
        {
            if (_network.Nodes.All(n => matrix.VisitedNodes.Contains(n.Id))
                || matrix.VisitedNodes.Contains(currentId)
                || currentId == receiverId)
            {
                matrix.VisitedNodes.Add(currentId);
                return;
            }

            matrix.VisitedNodes.Add(currentId);
            var currentNode = _network.GetNodeById(currentId);

            foreach (var linkedNodeId in currentNode.LinkedNodesId)
            {
                var currentPrice = matrix.NodeIdWithCurrentPrice[currentId] + CountPrice(currentId, linkedNodeId);

                if (matrix.NodeIdWithCurrentPrice[linkedNodeId] > currentPrice)
                {
                    matrix.NodeIdWithCurrentPrice[linkedNodeId] = currentPrice;
                }
            }

            foreach (var linkedNodeId in currentNode.LinkedNodesId)
            {
                CountPriceMatrix(linkedNodeId, receiverId, matrix);
            }
        }

        private double CountPrice(uint startId, uint destinationId)
        {
            var channel = _network.GetChannel(startId, destinationId);

            var startNode = _network.GetNodeById(startId);
            var destinationNode = _network.GetNodeById(destinationId);

            var startMessageQueue = startNode.MessageQueueHandlers
                .First(m => m.ChannelId == channel.Id);

            var destinationMessageQueue = destinationNode.MessageQueueHandlers
                .First(m => m.ChannelId == channel.Id);

            return channel.Price * channel.ErrorChance * channel.ErrorChance
                   * (startMessageQueue.MessagesCount + destinationMessageQueue.MessagesCount + 1)
                   * (startMessageQueue.MessagesCount + destinationMessageQueue.MessagesCount + 1);
        }
    }
}
