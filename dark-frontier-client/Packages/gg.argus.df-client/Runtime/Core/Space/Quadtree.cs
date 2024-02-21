// Copyright 2024 Argus Labs
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using ArgusLabs.DF.Core.Configs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace ArgusLabs.DF.Core.Space
{
    public class Quadtree
    {
        public class Node
        {
            private (Node, Node, Node, Node) _children;
            public Rect Aabb { get; set; }
            public Sector Sector { get; set; }
            public bool IsComplete { get; set; }
            public bool HasChildren { get; set; }
            public bool HasConsolidatedEnvironment { get; set; }
            
            public Node this[int i]
            {
                get
                {
                    return i switch
                    {
                        0 => _children.Item1,
                        1 => _children.Item2,
                        2 => _children.Item3,
                        3 => _children.Item4,
                        _ => throw new IndexOutOfRangeException()
                    };
                }
                
                set
                {
                    switch (i)
                    {
                        case 0: _children.Item1 = value; break;
                        case 1: _children.Item2 = value; break;
                        case 2: _children.Item3 = value; break;
                        case 3: _children.Item4 = value; break; 
                        default: throw new IndexOutOfRangeException();
                    }
                }
            }

            public void RemoveChildren()
            {
                _children.Item1 = null;
                _children.Item2 = null;
                _children.Item3 = null;
                _children.Item4 = null;
                
                HasChildren = false;
            }

            public bool Intersects(Rect other)
            {
                if (Aabb.Contains(other.min, true)
                    || Aabb.Contains(other.max, true)
                    || Aabb.Contains(new Vector2(other.xMin, other.yMax), true)
                    || Aabb.Contains(new Vector2(other.xMax, other.yMin), true))
                    return true;
                
                if (other.Contains(Aabb.min, true)
                    || other.Contains(Aabb.max, true)
                    || other.Contains(new Vector2(Aabb.xMin, Aabb.yMax), true)
                    || other.Contains(new Vector2(Aabb.xMax, Aabb.yMin), true))
                    return true;
                
                return false;
            }

            public bool HitTestPlanet(Vector2 position, float localScale = 1f)
            {
                if (!Sector.HasPlanet)
                    return false;

                var origin = (Vector2)Sector.Planet.Position;
                float scale = Sector.Planet.Scale * localScale;

                return (position - origin).sqrMagnitude <= (scale * scale);
            }

            public bool PlanetIntersects(Bounds other, float localScale = 1f)
                => PlanetIntersects(new Rect(other.min, other.size), localScale);
            
            public bool PlanetIntersects(Rect other, float localScale = 1f)
            {
                if (!Sector.HasPlanet)
                    return false;
                
                var planet = Sector.Planet;
                var p = new Vector2(planet.Position.x, planet.Position.y);
                float radius = planet.Radius * localScale;
                var planetRect = Rect.MinMaxRect(p.x - radius, p.y - radius, p.x + radius, p.y + radius);
                
                if (planetRect.Contains(other.center) // <-- Common case.
                    || planetRect.Contains(new Vector2(other.xMin, other.yMin), true)
                    || planetRect.Contains(new Vector2(other.xMin, other.yMax), true)
                    || planetRect.Contains(new Vector2(other.xMax, other.yMin), true)
                    || planetRect.Contains(new Vector2(other.xMax, other.yMax), true))
                    return true;

                if (other.Contains(planetRect.center) // <-- Common case.
                    || other.Contains(new Vector2(planetRect.xMin, planetRect.yMin), true)
                    || other.Contains(new Vector2(planetRect.xMin, planetRect.yMax), true)
                    || other.Contains(new Vector2(planetRect.xMax, planetRect.yMin), true)
                    || other.Contains(new Vector2(planetRect.xMax, planetRect.yMax), true))
                    return true;
                
                return false;
            }
        }
        
        private readonly HashSet<Vector2Int> _knownSectorPositions;
        private readonly Queue<Node> _searchQueue;
        private readonly Node _root;
        
        public Quadtree(Rect aabb)
        {
            _root = new Node { Aabb = aabb };
            _knownSectorPositions = new HashSet<Vector2Int>(2048);
            _searchQueue = new Queue<Node>(2048);
        }
        
        public bool AddSector(Sector additionalSector)
        {
            if (!_knownSectorPositions.Add(additionalSector.Position))
                return false;
            
            // Start at the root.
            Traverse(_root, 0);
            
            return true;
            
            void Traverse(Node node, int depth)
            {
                Assert.IsTrue(depth >= 0);
                Assert.IsTrue(depth < 32, $"Node depth ({depth}) is higher than it probably should be. Please check.");
                //Debug.Log($"Depth: {depth} HasPlanet? {additionalSector.HasPlanet} AddSector Position: {additionalSector.Position} Traverse: {node.Aabb}");
                
                var currentSector = node.Sector;
                var aabb = node.Aabb;
                
                if (currentSector.Environment == SpaceEnvironment.DarkSpace)
                    currentSector.Environment = SpaceEnvironment.BlankSpace;
                
                void TraverseChildNodes()
                {
                    if (!node.HasChildren)
                    {
                        node[0] = new Node { Aabb = Rect.MinMaxRect(aabb.center.x, aabb.center.y, aabb.xMax, aabb.yMax)}; // NE
                        node[1] = new Node { Aabb = Rect.MinMaxRect(aabb.xMin, aabb.center.y, aabb.center.x, aabb.yMax)}; // NW
                        node[2] = new Node { Aabb = Rect.MinMaxRect(aabb.xMin, aabb.yMin, aabb.center.x, aabb.center.y)}; // SW
                        node[3] = new Node { Aabb = Rect.MinMaxRect(aabb.center.x, aabb.yMin, aabb.xMax, aabb.center.y)}; // SE
                        node.HasChildren = true;

                        var darkSector = new Sector { Environment = SpaceEnvironment.DarkSpace };

                        for (int i = 0; i < 4; ++i)
                            node[i].Sector = darkSector;
                    }
                    
                    for (int i = 0; i < 4; ++i)
                    {
                        var child = node[i];
                        
                        if (child.Aabb.Contains(additionalSector.Position))
                        {
                            Traverse(child, depth + 1);
                            break;
                        }
                    }
                }
                
                // No planet to place
                // Note: Unless we're adding sectors without planets, this will never be called.
                if (!additionalSector.HasPlanet)
                {
                    // End of the road? // TODO: Test depth instead.
                    if (Mathf.Approximately(aabb.width, 1f))
                    {
                        currentSector.Environment = additionalSector.Environment;
                        node.IsComplete = true;
                    }
                    // Nothing to do here. Keep moving.
                    else
                    {
                        TraverseChildNodes();
                    }
                }
                // Planet to place but current sector already has a planet.
                else if (currentSector.HasPlanet)
                {
                    // This one has a planet, but maybe we should swap?
                    if (currentSector.Weight > additionalSector.Weight)
                        (additionalSector.Planet, currentSector.Planet) = (currentSector.Planet, additionalSector.Planet);
                    
                    // Still have a planet and an environment to place.
                    TraverseChildNodes();
                } 
                // Planet to place and current sector is available.
                else
                {
                    currentSector.Planet = additionalSector.Planet;
                    additionalSector.Planet = null;
                    
                    // End of the road? // TODO: Test depth instead.
                    if (Mathf.Approximately(aabb.width, 1f))
                    {
                        node.IsComplete = true;
                        currentSector.Environment = additionalSector.Environment;
                    }
                    else // Still need to place environment.
                    {
                        TraverseChildNodes();
                    }
                }
                
                node.Sector = currentSector;
                
                // Attempt consolidation starting one level above complete leaf nodes.

                if (node.IsComplete || node.HasConsolidatedEnvironment)
                    return;
                
                bool anyChildHasPlanet = false;
                
                // There can only be 4 positive environments.
                // We won't sneak another one in because they map to RGBA channels in the shader.
                int4 environmentCount = 0;
                int winnerIndex = 0;
                
                for (int i = 0; i < 4; ++i)
                {
                    var child = node[i];
                    
                    if ((child.Sector.Environment == node.Sector.Environment) && (node.Sector.Environment >= 0))
                        if (++environmentCount[i] > environmentCount[winnerIndex])
                            winnerIndex = i;
                    
                    if (child.Sector.HasPlanet)
                        anyChildHasPlanet = true;
                }
                
                // Note: Remove this check if merging is required.
                // This prevents consolidation unless all environments match.
                // As of 11/17/2023, we're not using environmental data from the quadtree.
                // However, if we were, this would likely be far more useful.
                if (environmentCount[winnerIndex] < 4)
                    return;
                
                currentSector.Environment = (SpaceEnvironment)winnerIndex;
                node.Sector = currentSector;
                
                node.HasConsolidatedEnvironment = true;
                
                if (anyChildHasPlanet)
                    return;
                
                node.RemoveChildren();

                //Debug.Log($"Consolidation! {node.Sector.Position} Quadrant Scale: {node.Aabb.width} Environment: {node.Sector.Environment}");
            }
        }
        
        public void WalkBfs(Func<Node, bool> f)
        {
            _searchQueue.Clear();
            _searchQueue.Enqueue(_root);

            while (_searchQueue.Count > 0)
            {
                Node node = _searchQueue.Dequeue();
                
                if (!f(node))
                    continue;
                
                if (!node.HasChildren)
                    continue;
                
                for (int i = 0; i < 4; ++i)
                    _searchQueue.Enqueue(node[i]);
            }
        }
        
        public void WalkDfs(Func<Node, bool> trickleDownCallback = null, Func<Node, bool> bubbleUpCallback = null)
        {
            bool isTricklingDown = trickleDownCallback is not null;
            bool isBubblingUp = bubbleUpCallback is not null;
            
            Walk(_root);
            
            void Walk(Node node)
            {
                if (isTricklingDown && !trickleDownCallback(node))
                    return;
                
                if (node.HasChildren)
                    for (int i = 0; i < 4; ++i)
                        Walk(node[i]);
                
                if (isBubblingUp)
                    isBubblingUp = bubbleUpCallback(node);
            }
        }
    }
}