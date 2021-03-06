﻿using System;
using System.Collections.Generic;
using System.Linq;
using Coursework.Data.Constants;
using Coursework.Data.Entities;
using Coursework.Data.NetworkData;

namespace Coursework.Data.MessageServices
{
    public class IndependentMessageRouter : IMessageRouter
    {
        protected readonly INetworkHandler Network;

        public IndependentMessageRouter(INetworkHandler network)
        {
            Network = network;
        }

        public Channel[] GetRoute(uint senderId, uint receiverId)
        {
            if (senderId == receiverId)
            {
                return new Channel[0];
            }

            var sender = Network.GetNodeById(senderId);

            var networkMatrix = sender.NetworkMatrix;

            if (networkMatrix == null
                || double.IsInfinity(networkMatrix.NodeIdWithCurrentPrice[receiverId]))
            {
                return null;
            }

            var route = BuildRoute(networkMatrix, senderId, receiverId);

            return route.ToArray();
        }

        public NetworkMatrix CountPriceMatrix(uint currentId, uint? startId, NetworkMatrix matrix = null,
            SortedSet<uint> visitedNodes = null)
        {
            StartCountingPriceProcess(currentId, startId, ref matrix, ref visitedNodes);

            if (visitedNodes.Contains(currentId))
            {
                visitedNodes.Add(currentId);
                return matrix;
            }

            visitedNodes.Add(currentId);
            var currentNode = Network.GetNodeById(currentId);

            foreach (var linkedNodeId in currentNode.LinkedNodesId)
            {
                if (!startId.HasValue || startId.Value == currentId)
                {
                    matrix.PriceMatrix[currentId][linkedNodeId] = CountPrice(currentId, linkedNodeId);
                }

                var currentPrice = matrix.NodeIdWithCurrentPrice[currentId]
                    + matrix.PriceMatrix[currentId][linkedNodeId];

                if (matrix.NodeIdWithCurrentPrice[linkedNodeId] > currentPrice)
                {
                    matrix.NodeIdWithCurrentPrice[linkedNodeId] = currentPrice;
                }
            }

            if (!Network.Nodes.All(n => visitedNodes.Contains(n.Id)))
            {
                var nextNodeId = matrix.NodeIdWithCurrentPrice
                    .Where(kv => !visitedNodes.Contains(kv.Key))
                    .Aggregate((l, r) => l.Value < r.Value ? l : r)
                    .Key;

                if (!double.IsInfinity(matrix.NodeIdWithCurrentPrice[nextNodeId]))
                {
                    return CountPriceMatrix(nextNodeId, startId, matrix, visitedNodes);
                }
            }

            return matrix;
        }

        public virtual double CountPrice(uint startId, uint destinationId)
        {
            if (startId == destinationId)
            {
                return 0.0;
            }

            var startNode = Network.GetNodeById(startId);
            var destinationNode = Network.GetNodeById(destinationId);

            var channel = Network.GetChannel(startId, destinationId);

            if (!startNode.IsActive || !destinationNode.IsActive || channel == null
                || channel.IsBusy)
            {
                return double.PositiveInfinity;
            }

            return 1.0;
        }

        private void StartCountingPriceProcess(uint currentId, uint? startId, ref NetworkMatrix matrix,
            ref SortedSet<uint> visitedNodes)
        {
            if (visitedNodes == null)
            {
                visitedNodes = new SortedSet<uint>();
            }

            if (matrix == null)
            {
                matrix = NetworkMatrix.Initialize(Network);
                matrix.NodeIdWithCurrentPrice[currentId] = 0.0;
            }

            if (startId.HasValue && currentId == startId.Value)
            {
                foreach (var key in matrix.NodeIdWithCurrentPrice.Keys.ToArray())
                {
                    matrix.NodeIdWithCurrentPrice[key] = double.PositiveInfinity;
                }

                matrix.NodeIdWithCurrentPrice[startId.Value] = 0.0;
            }
        }

        private List<Channel> BuildRoute(NetworkMatrix networkMatrix, uint senderId, uint receiverId)
        {
            var currentNodeId = receiverId;

            var route = new List<Channel>();

            while (currentNodeId != senderId)
            {
                var currentNode = Network.GetNodeById(currentNodeId);

                foreach (var linkedNodeId in currentNode.LinkedNodesId)
                {
                    var difference = Math.Abs(networkMatrix.NodeIdWithCurrentPrice[currentNodeId]
                                              - networkMatrix.NodeIdWithCurrentPrice[linkedNodeId]
                                              - networkMatrix.PriceMatrix[linkedNodeId][currentNodeId]);

                    if (difference < AllConstants.Eps)
                    {
                        var channel = Network.GetChannel(currentNodeId, linkedNodeId);
                        route.Add(channel);

                        currentNodeId = linkedNodeId;
                        break;
                    }
                }
            }

            route.Reverse();
            return route;
        }
    }
}
