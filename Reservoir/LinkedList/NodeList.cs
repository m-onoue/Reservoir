﻿/*
 *  Reservoir - C# Object Pooling and GC-Free Collections
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Reservoir
{
  /// <summary>
  /// A storage-free doubly linked list for node objects.
  /// Note that nodes can only safely be in one of these lists at a time.
  /// 
  /// This structure has no GC allocations for "foreach" iteration, but should
  /// not be iterated on more than once at a time (i.e. no nested n*n loops).
  /// </summary>
  public class NodeList<T> : IEnumerable<T>
    where T : class, INode<T>
  {
    public class NodeListEnumerator : IEnumerator<T>
    {
      private T current;
      private NodeList<T> list;

      public T Current { get { return this.current; } }
      object IEnumerator.Current { get { return this.current; } }

      public bool MoveNext()
      {
        if (current.Next == null)
          return false;
        current = current.Next;
        return true;
      }

      public void Dispose() { }

      public void Reset()
      {
        this.current = this.list.first;
      }

      internal NodeListEnumerator(NodeList<T> list)
      {
        this.current = null;
        this.list = list;
      }
    }

    internal T first;
    internal T last;

    // Preallocate a single enumerator to reuse over time
    private NodeListEnumerator enumerator;

    public int Count { get; private set; }

    public NodeList()
    {
      this.first = null;
      this.last = null;
      this.enumerator = new NodeListEnumerator(this);

      this.Count = 0;
    }

    /// <summary>
    /// Adds a node to the end of the list. O(1)
    /// </summary>
    public void Add(T value)
    {
      if (value.List != null)
        throw new InvalidOperationException();

      if (this.first == null)
        this.first = value;
      value.Previous = this.last;

      if (this.last != null)
        this.last.Next = value;
      value.Next = null;

      this.last = value;
      value.List = this;

      this.Count++;
    }

    /// <summary>
    /// Removes a node from the list. O(1)
    /// </summary>
    public void Remove(T value)
    {
      if (value.List != this)
        throw new AccessViolationException();

      if (this.first == value)
        this.first = value.Next;
      if (this.last == value)
        this.last = value.Previous;

      if (value.Previous != null)
        value.Previous.Next = value.Next;
      if (value.Next != null)
        value.Next.Previous = value.Previous;

      value.Next = null;
      value.Previous = null;
      value.List = null;

      this.Count--;
    }

    /// <summary>
    /// Removes and returns the first element. O(1)
    /// </summary>
    public T RemoveFirst()
    {
      if (this.first == null)
        throw new AccessViolationException();

      T result = this.first;
      if (result.Next != null)
        result.Next.Previous = null;
      this.first = result.Next;
      if (this.last == result)
        this.last = null;

      result.Next = null;
      result.Previous = null;
      result.List = null;

      this.Count--;

      return result;
    }

    /// <summary>
    /// Returns the first element, keeping it in the list. O(1)
    /// </summary>
    public T PeekFirst()
    {
      return this.first;
    }

    public IEnumerator<T> GetEnumerator()
    {
      this.enumerator.Reset();
      return this.enumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }
  }
}