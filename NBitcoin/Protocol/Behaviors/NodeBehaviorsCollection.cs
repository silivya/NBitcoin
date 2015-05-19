﻿#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	public class NodeBehaviorsCollection : IEnumerable<INodeBehavior>
	{
		Node _Node;
		public NodeBehaviorsCollection(Node node)
		{
			_Node = node;
		}

		List<INodeBehavior> _Behaviors = new List<INodeBehavior>();
		object cs = new object();
		public void Add(INodeBehavior behavior)
		{
			if(behavior == null)
				throw new ArgumentNullException("behavior");
			lock(cs)
			{
				if(CanAttach)
					behavior.Attach(_Node);
				_Behaviors.Add(behavior);
			}
		}

		bool CanAttach
		{
			get
			{
				return _Node != null && !DelayAttach && _Node.State != NodeState.Offline && _Node.State != NodeState.Failed && _Node.State != NodeState.Disconnecting;
			}
		}

		public bool Remove(INodeBehavior behavior)
		{
			lock(cs)
			{
				var removed = _Behaviors.Remove(behavior);
				if(removed)
					behavior.Detach();
				return removed;
			}
		}

		public void Clear()
		{
			lock(cs)
			{
				foreach(var behavior in _Behaviors)
					behavior.Detach();
				_Behaviors.Clear();
			}
		}

		public T FindOrCreate<T>() where T : INodeBehavior, new()
		{
			return FindOrCreate<T>(() => new T());
		}
		public T FindOrCreate<T>(Func<T> create) where T : INodeBehavior
		{
			lock(cs)
			{
				var result = _Behaviors.OfType<T>().FirstOrDefault();
				if(result == null)
				{
					result = create();
					_Behaviors.Add(result);
					if(CanAttach)
						result.Attach(_Node);
				}
				return result;
			}
		}
		public T Find<T>() where T : INodeBehavior
		{
			lock(cs)
			{
				return _Behaviors.OfType<T>().FirstOrDefault();
			}
		}

		public void Remove<T>() where T : INodeBehavior
		{
			lock(cs)
			{
				foreach(var b in _Behaviors.OfType<T>().ToList())
				{
					_Behaviors.Remove(b);
				}
			}
		}

		#region IEnumerable<NodeBehavior> Members

		public IEnumerator<INodeBehavior> GetEnumerator()
		{
			lock(cs)
			{
				return _Behaviors.ToList().GetEnumerator();
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		bool _DelayAttach;
		internal bool DelayAttach
		{
			get
			{
				return _DelayAttach;
			}
			set
			{
				_DelayAttach = value;
				if(CanAttach)
					foreach(var b in _Behaviors)
						b.Attach(_Node);
			}
		}
	}
}
#endif