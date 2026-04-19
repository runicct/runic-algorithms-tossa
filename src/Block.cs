/*
 * MIT License
 * 
 * Copyright (c) 2025 Runic Compiler Toolkit Contributors
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runic.Algorithms
{
    public abstract partial class ToSSA<T>
    {
        internal class Block<T>
        {
            HashSet<int> _importedLocals = new HashSet<int>();
            public HashSet<int> ImportedLocals { get { return _importedLocals; }  set { _importedLocals = value; } }
            Dictionary<int, int> _exportedLocals = new Dictionary<int, int>();
            public Dictionary<int, int> ExportedLocals { get { return _exportedLocals; } set { _exportedLocals = value; } }

            int _offset;
            public int Offset { get { return _offset; } }
            Dictionary<int, Block<T>> _predecessor = new Dictionary<int, Block<T>>();
            Dictionary<int, Block<T>> _successor = new Dictionary<int, Block<T>>();

            public void AddPredecessor(Block<T> block)
            {
                if (!_predecessor.ContainsKey(block.Offset))
                {
                    _predecessor.Add(block.Offset, block);
                    block.AddSuccessor(this);
                }
            }
            public void AddSuccessor(Block<T> block)
            {
                if (!_successor.ContainsKey(block.Offset))
                {
                    _successor.Add(block.Offset, block);
                    block.AddPredecessor(this);
                }
            }
            class PassthroughPhi
            {
                int _local;
                public int Local { get { return _local; } set { _local = value; } }
                int _phi;
                public int Phi { get { return _phi; } set { _phi = value; } }
                Dictionary<int, int> _predecessors = new Dictionary<int, int>();
                public Dictionary<int, int> Predecessors { get { return _predecessors; } }
                public PassthroughPhi(int local, int phi)
                {
                    _local = local;
                    _phi = phi;
                    _predecessors = new Dictionary<int, int>();
                }
            }


            Dictionary<int, PassthroughPhi> _passthroughPhi = new Dictionary<int, PassthroughPhi>();
            int GetPhiRec(int local)
            {
                int blockLocal = 0;
                if (_exportedLocals.TryGetValue(local, out blockLocal)) { return blockLocal; }
                else
                {
                    PassthroughPhi passthroughPhi;
                    if (!_passthroughPhi.TryGetValue(local, out passthroughPhi))
                    {
                        passthroughPhi = new PassthroughPhi(local, _parent.NewLocal());
                        _passthroughPhi.Add(local, passthroughPhi);
                        foreach (Block<T> predecessor in _predecessor.Values)
                        {
                            int phiLocal = predecessor.GetPhiRec(local);
                            passthroughPhi.Predecessors.Add(predecessor.Offset, phiLocal);
                        }
                        if (passthroughPhi.Predecessors.Count <= 1)
                        {
                            foreach (int predecessor in passthroughPhi.Predecessors.Values)
                            {
                                return predecessor;
                            }
                        }
                    }

                    return passthroughPhi.Phi;
                }
            }
            public Dictionary<int, int> GetPhi(int local)
            {
                HashSet<int> possibilities = new HashSet<int>();
                Dictionary<int, int> result = new Dictionary<int, int>();
                foreach (Block<T> predecessor in _predecessor.Values)
                {
                    result.Add(predecessor.Offset, predecessor.GetPhiRec(local));
                }
                return result;
            }
            public void EmitPassthroughPhi(int offset)
            {
                foreach (PassthroughPhi passthroughPhi in _passthroughPhi.Values)
                {
                    if (passthroughPhi.Predecessors != null)
                    {
                        _parent.Phi(offset, passthroughPhi.Phi, passthroughPhi.Predecessors);
                    }
                }
            }
            ToSSA<T> _parent;

            List<Instruction<T>> _instructions;
            public List<Instruction<T>> Instructions { get { return _instructions; } set { _instructions = value; } } 
            public Block(ToSSA<T> parent, int offset, List<Instruction<T>> instructions)
            {
                _parent = parent;
                _offset = offset;
                _instructions = instructions;
            }
        }
    }

}
