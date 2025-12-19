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
        HashSet<int> _locals = new HashSet<int>();
        List<Instruction<T>> _instructions = new List<Instruction<T>>();
        public void EmitAssignment(int offset, T tag, int destination, int[] parameters)
        {
            _locals.Add(destination);
            for (int n = 0; n < parameters.Length; n++)
            {
                _locals.Add(parameters[n]);
            }
            _instructions.Add(new Instruction<T>.Assignment<T>(offset, tag, destination, parameters));
        }
        public void EmitStatement(int offset, T tag, int[] parameters)
        {
            for (int n = 0; n < parameters.Length; n++)
            {
                _locals.Add(parameters[n]);
            }
            _instructions.Add(new Instruction<T>.Statement<T>(offset, tag, parameters));
        }
        HashSet<int> _labels = new HashSet<int>();
        public void EmitBranch(int offset, T tag,  int[] parameters, bool conditional, int target)
        {
            for (int n = 0; n < parameters.Length; n++)
            {
                _locals.Add(parameters[n]);
            }
            _instructions.Add(new Instruction<T>.Branch<T>(offset, tag, parameters, conditional, target));
            _labels.Add(target);
        }

        public virtual void Phi(int offset, int destination, Dictionary<int, int> locals) { }
        public virtual void Assignment(int offset, T tag, int destination, int[] parameters) { }
        public virtual void Statement(int offset, T tag, int[] parameters) { }
        public virtual void Branch(int offset, T tag, int[] parameters, bool conditional, int target) { }
        public virtual void UnaliasLocal(int local, int sourceLocal) { }

        List<Block<T>> ToBlocks()
        {
            List<Block<T>> blocksInOrder = new List<Block<T>>();
            Dictionary<int, Block<T>> blocks = new Dictionary<int, Block<T>>();
            List<Instruction<T>> currentBlockInstructions = new List<Instruction<T>>();
            Block<T>? previousBlock = null;
            int startOffset = _instructions[0].Offset;
            Block<T> currentBlock = new Block<T>(startOffset, currentBlockInstructions);
            blocks.Add(startOffset, currentBlock);
            blocksInOrder.Add(currentBlock);
            bool previousWasBranch = false;
            bool conditionalBranch = false;
            for (int n = 0; n < _instructions.Count; n++)
            {
                int offset = _instructions[n].Offset;
                if (previousWasBranch || _labels.Contains(offset))
                {
                    Block<T> block = null;
                    if (!blocks.TryGetValue(offset, out block))
                    {
                        currentBlockInstructions = new List<Instruction<T>>();
                        block = new Block<T>(offset, currentBlockInstructions);
                        blocks.Add(offset, block);
                    }
                    else
                    {
                        currentBlockInstructions = block.Instructions;
                    }
                    if (!previousWasBranch || conditionalBranch)
                    {
                        block.AddPredecessor(currentBlock);
                    }
                    previousWasBranch = false;
                    conditionalBranch = false;
                    blocksInOrder.Add(block);
                    currentBlock = block;
                    startOffset = offset;
                }
                currentBlockInstructions.Add(_instructions[n]);
                Instruction<T>.Branch<T>? branch = _instructions[n] as Instruction<T>.Branch<T>;
                if (branch != null)
                {
                    Block<T> targetBlock;
                    if (!blocks.TryGetValue(branch.Target, out targetBlock))
                    {
                        targetBlock = new Block<T>(branch.Target, new List<Instruction<T>>());
                        blocks.Add(branch.Target, targetBlock);
                    }
                    currentBlock.AddSuccessor(targetBlock);
                    previousWasBranch = true;
                    conditionalBranch = !branch.Conditional;
                }
            }
            return blocksInOrder;
        }

        public void Process()
        {
            // First pass: Identify basic blocks
            List<Block<T>> blocks = ToBlocks();

            // Second pass: Rename locals and determine imported/exported locals foreach block
            {
                foreach (Block<T> block in blocks)
                {
                    HashSet<int> importedLocals = new HashSet<int>();
                    Dictionary<int, int> exportedLocals = new Dictionary<int, int>();
                    foreach (Instruction<T> instruction in block.Instructions)
                    {
                        int[] parameters = new int[instruction.Parameters.Length];
                        for (int n = 0; n < instruction.Parameters.Length; n++)
                        {
                            int translatedLocal = 0;
                            if (exportedLocals.TryGetValue(instruction.Parameters[n], out translatedLocal))
                            {
                                parameters[n] = translatedLocal;
                            }
                            else
                            {
                                importedLocals.Add(instruction.Parameters[n]);
                                parameters[n] = instruction.Parameters[n];
                            }
                        }
                        instruction.Parameters = parameters;
                        switch (instruction)
                        {
                            case Instruction<T>.Assignment<T> assignment:
                                {
                                    int newLocal = 0;
                                    do
                                    {
                                        newLocal++;
                                    } while (_locals.Contains(newLocal) || importedLocals.Contains(newLocal));
                                    _locals.Add(newLocal);
                                    UnaliasLocal(newLocal, assignment.Destination);
                                    if (!exportedLocals.ContainsKey(assignment.Destination)) { exportedLocals.Add(assignment.Destination, newLocal); }
                                    else { exportedLocals[assignment.Destination] = newLocal; }
                                    assignment.Destination = newLocal;
                                }
                                break;
                        }
                    }
                    block.ImportedLocals = importedLocals;
                    block.ExportedLocals = exportedLocals;
                }
            }

            // Third pass: Insert phi functions and emit instructions
            {
                foreach (Block<T> block in blocks)
                {
                    foreach (Instruction<T> instruction in block.Instructions)
                    {
                        Dictionary<int, int> translatedLocals = new Dictionary<int, int>();
                        for (int n = 0; n < instruction.Parameters.Length; n++)
                        {
                            if (block.ImportedLocals.Contains(instruction.Parameters[n]))
                            {
                                int newLocal = 0;
                                if (translatedLocals.TryGetValue(instruction.Parameters[n], out newLocal))
                                {
                                    instruction.Parameters[n] = newLocal;
                                    continue;
                                }
                                else
                                {

                                    Dictionary<int, int> phi = block.GetPhi(instruction.Parameters[n]);
                                    if (phi.Count > 0)
                                    {
                                        if (phi.Count == 1)
                                        {
                                            foreach (KeyValuePair<int, int> firstValue in phi)
                                            {
                                                translatedLocals.Add(instruction.Parameters[n], firstValue.Value);
                                                instruction.Parameters[n] = firstValue.Value;
                                            }
                                        }
                                        else
                                        {
                                            do
                                            {
                                                newLocal++;
                                            } while (_locals.Contains(newLocal));
                                            UnaliasLocal(newLocal, instruction.Parameters[n]);
                                            translatedLocals.Add(instruction.Parameters[n], newLocal);
                                            Phi(instruction.Offset, newLocal, phi);
                                            instruction.Parameters[n] = newLocal;
                                        }
                                    }
                                    else
                                    {
                                        translatedLocals.Add(instruction.Parameters[n], instruction.Parameters[n]);
                                    }
                                }
                            }

                        }
                        instruction.Emit(this);
                    }
                }
            }
        }
    }
}
