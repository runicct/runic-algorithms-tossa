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
            void GetPhi(int local, Dictionary<int, int> locals, HashSet<int> visited)
            {
                int blockLocal = 0;
                if (locals.ContainsKey(_offset) || visited.Contains(_offset)) { return; }
                visited.Add(_offset);
                if (_exportedLocals.TryGetValue(local, out blockLocal))
                {
                    locals.Add(_offset, blockLocal);
                    return;
                }
                else
                {
                    foreach (Block<T> predecessor in _predecessor.Values)
                    {
                        predecessor.GetPhi(local, locals, visited);
                    }
                }
            }
            public Dictionary<int, int> GetPhi(int local)
            {
                Dictionary<int, int> result = new Dictionary<int, int>();
                HashSet<int> visited = new HashSet<int>();
                foreach (Block<T> predecessor in _predecessor.Values)
                {
                    predecessor.GetPhi(local, result, visited);
                }
                return result;
            }
            List<Instruction<T>> _instructions;
            public List<Instruction<T>> Instructions { get { return _instructions; } set { _instructions = value; } } 
            public Block(int offset, List<Instruction<T>> instructions)
            {
                _offset = offset;
                _instructions = instructions;
            }
        }
    }

}
