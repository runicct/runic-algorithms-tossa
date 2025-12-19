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
        internal abstract class Instruction<T>
        {
            int _offset;
            public int Offset { get { return _offset; } }
            int[] _parameters;
            public int[] Parameters { get { return _parameters; } set { _parameters = value; } }
            T _tag;
            public T Tag { get { return _tag; } }
            private Instruction(int offset, T tag, int[] parameters) { _offset = offset; _tag = tag; _parameters = parameters; }
            public abstract void Emit(ToSSA<T> toSSA);
            internal class Assignment<T> : Instruction<T>
            {
                int _destination;
                public int Destination { get { return _destination; } set { _destination = value; } }
                public Assignment(int offset, T tag, int destination, int[] parameters) : base(offset, tag, parameters)
                {
                    _destination = destination;
                }
                public override void Emit(ToSSA<T> toSSA)
                {
                    toSSA.Assignment(Offset, Tag, Destination, Parameters);
                }
            }
            internal class Statement<T> : Instruction<T>
            {
                public Statement(int offset, T tag, int[] parameters) : base(offset, tag, parameters)
                {
                }
                public override void Emit(ToSSA<T> toSSA)
                {
                    toSSA.Statement(Offset, Tag, Parameters);
                }
            }
            internal class Branch<T> : Instruction<T>
            {
                int _target;
                public int Target { get { return _target; } }
                bool _conditional;
                public bool Conditional { get { return _conditional; } }
                public Branch(int offset, T tag, int[] parameters, bool conditional, int target) : base(offset, tag, parameters)
                {
                    _target = target;
                    _conditional = conditional;
                }
                public override void Emit(ToSSA<T> toSSA)
                {
                    toSSA.Branch(Offset, Tag, Parameters, _conditional, _target);
                }
            }
        }
    }
}
