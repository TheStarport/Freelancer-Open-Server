using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FOS_ng.Structures
{
    /// <summary>
    /// Minimalistic tree node implementation.
    /// </summary>
    public class TreeNode:Dictionary<string,TreeNode>
    {
        /// <summary>
        /// Contains node's name.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Contains node's data.
        /// </summary>
        public object Data { get; set; }

        public TreeNode Parent { get; private set; }


        public TreeNode() { }

        public TreeNode(string name)
        {
            Name = name;
        }


        public virtual object Clone()
        {
            var tn = new TreeNode(Name);
            foreach (var key in Keys)
            {
                tn[key] = this[key];
            }

            return tn;
        }

        public new void Add(string key, TreeNode node)
        {
            node.Parent = this;
            base.Add(key,node);
        }

        public virtual TreeNode Add(string key, string text)
        {
            var tn = new TreeNode(text);
            Add(key,tn);
            return tn;
        }

        public virtual TreeNode Add(TreeNode tn)
        {
            Add(tn.Name, tn);
            return tn;
        }

        public virtual TreeNode FirstNode()
        {
            return Values.FirstOrDefault();
        }

        public virtual TreeNode NextNode()
        {
            var p = Parent.Values.ToList();

            return p[
            p.FindIndex(w => w == this)
            +1];
        }

        public TreeNode Find(string key,bool recursive = false)
        {
            if (Name == key) return this;

            foreach (var subNode in Values)
            {
                if (subNode.Name == key) return subNode;
                if (!recursive) continue;
                var src = subNode.Find(key, true);
                if (src != null) return src;
            }
            return null;

        }

    }
}
