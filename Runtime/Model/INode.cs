using UnityEngine;

namespace Districts.Model
{
    /// <summary>
    /// Describes a connection position for roads.
    /// </summary>
    public interface INode
    {
        string Name { get; }

        Vector3 Position { get; }
    }
}