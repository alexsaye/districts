using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using RoadPlanning;
using System;

public class RoadBuilder : MonoBehaviour, IRoadBuilder
{
    public string Name => name;

    public Vector3 Position => transform.position;

    public IEnumerable<IRoadBuilder> Connections => connections;

    [SerializeField]
    private List<RoadBuilder> connections;

    public bool IsConnected(RoadBuilder node)
    {
        return connections.Contains(node);
    }

    public void Connect(RoadBuilder node)
    {
        if (node.IsConnected(this))
        {
            throw new ArgumentException("Nodes can only be connected in one direction.");
        }

        if (!IsConnected(node))
        {
            connections.Add(node);
            Debug.Log($"Connected {name} to {node.name}.");
        }
    }

    public void Disconnect(RoadBuilder node)
    {
        if (IsConnected(node))
        {
            connections.Remove(node);
            Debug.Log($"Disconnected {name} from {node.name}.");
        }
    }

    public IRoad Build(INode node)
    {
        // TODO: more road types, for now a straight road will do.
        return new StraightRoad(this, node, $"{Name}->{node.Name}");
    }

    private void OnDrawGizmos()
    {
        DrawConnectionsGizmos(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawConnectionsGizmos(true);
    }

    private void DrawConnectionsGizmos(bool selected)
    {
        foreach (var connection in connections)
        {
            Gizmos.color = selected ? Selection.Contains(connection.gameObject) ? Color.green : Color.red : Color.white * 0.5f;

            Gizmos.DrawLine(transform.position, connection.transform.position);

            var towardsConnection = connection.transform.position - transform.position;
            Gizmos.DrawSphere(transform.position + towardsConnection * 0.25f, 0.1f);
            Gizmos.DrawSphere(transform.position + towardsConnection * 0.5f, 0.2f);
            Gizmos.DrawSphere(transform.position + towardsConnection * 0.75f, 0.3f);
        }
        Gizmos.color = Color.white;
    }
}

[CustomEditor(typeof(RoadBuilder))]
[CanEditMultipleObjects]
public class RoadBuilderEditor: Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (targets.Length == 2)
        {
            // The earliest clicked is the latest in the array.
            RoadBuilder a = (RoadBuilder)targets[1];
            RoadBuilder b = (RoadBuilder)targets[0];
            if (a.IsConnected(b) || b.IsConnected(a))
            {
                if (GUILayout.Button("Disconnect Selected Nodes"))
                {
                    a.Disconnect(b);
                    b.Disconnect(a);
                }
                else if (GUILayout.Button("Reverse Selected Nodes"))
                {
                    if (a.IsConnected(b))
                    {
                        a.Disconnect(b);
                        b.Connect(a);
                    }
                    else
                    {
                        b.Disconnect(a);
                        a.Connect(b);
                    }
                }
            }
            else if (GUILayout.Button("Connect Selected Nodes"))
            {
                a.Connect(b);
                b.Disconnect(a);
            }
        }
    }
}